using System.Collections.Generic;
using UnityEngine;

// Side jobs (part of FarmPrototype, see FarmPrototype.cs): besides the main story there are always
// up to 3 small jobs at the same time - "collect 5 eggs", "milk the cow", "sell 3 carrots"...
// Each pays coins; a finished one is replaced by a new one a few seconds later. Which jobs can come up
// depends on what you already have (hens, a garden, cows, goats). So the loop is:
// do jobs and sell things -> buy animals -> animals give more to sell and more kinds of jobs.
//
// Every action in the game reports what happened with TaskEvent("egg"), TaskEvent("sell_egg", 4)...
public partial class FarmPrototype
{
    const int MaxSideTasks = 3;

    class SideTask
    {
        public string id;      // which kind (see TaskText / TaskEventOf / TaskAvailable)
        public int target, progress, reward;
    }

    readonly List<SideTask> sideTasks = new List<SideTask>();
    float nextTaskAt;
    int tasksVersion;
    string lastTaskDone;

    // All kinds of jobs: what to count, how many (min..max), coins for each one done.
    static readonly string[] TaskIds =
        { "eggs", "sell_eggs", "feed", "pet_hens", "talk_boris", "carrots", "sell_carrots", "boris_carrot",
          "milk_cow", "milk_goat", "sell_milk", "pet_animals", "buy_animal", "sleep" };

    bool TaskAvailable(string id)
    {
        switch (id)
        {
            case "eggs": case "pet_hens": return hensBought > 0;
            case "sell_eggs": return stage >= Stage.Trading;
            case "feed": return hensBought > 0 && !FedToday && !dayNight.IsNight;
            case "talk_boris": return stage >= Stage.Trading;
            case "carrots": case "sell_carrots": return stage >= Stage.Garden;
            case "boris_carrot": return stage > Stage.Garden;
            case "milk_cow": return cowsBought > 0;
            case "milk_goat": return goatsBought > 0;
            case "sell_milk": case "pet_animals": return cowsBought + goatsBought > 0;
            case "buy_animal": return stage >= Stage.Trading && (hensBought < HenLimit || (stage >= Stage.BarnOpen && goatsBought < goats.Count));
            case "sleep": return dayNight.running;
            default: return false;
        }
    }

    // (how many: min, max), coins for each
    void TaskSize(string id, out int min, out int max, out int coinsEach)
    {
        switch (id)
        {
            case "eggs": min = 3; max = 6; coinsEach = 2; return;
            case "sell_eggs": min = 4; max = 8; coinsEach = 1; return;
            case "pet_hens": min = 3; max = 5; coinsEach = 1; return;
            case "carrots": min = 2; max = 6; coinsEach = 2; return;
            case "sell_carrots": min = 2; max = 4; coinsEach = 1; return;
            case "milk_cow": min = 1; max = Mathf.Max(1, cowsBought); coinsEach = 4; return;
            case "milk_goat": min = 1; max = Mathf.Max(1, goatsBought); coinsEach = 3; return;
            case "sell_milk": min = 1; max = 3; coinsEach = 2; return;
            case "pet_animals": min = 3; max = 3; coinsEach = 1; return;
            case "feed": min = 1; max = 1; coinsEach = 4; return;
            case "boris_carrot": min = 1; max = 1; coinsEach = 5; return;
            case "buy_animal": min = 1; max = 1; coinsEach = 8; return;
            case "sleep": min = 1; max = 1; coinsEach = 3; return;
            default: min = 1; max = 1; coinsEach = 2; return; // talk_boris
        }
    }

    static string TaskText(string id, int n)
    {
        switch (id)
        {
            case "eggs": return "Збери " + Plural(n, "яйце", "яйця", "яєць") + " у курнику";
            case "sell_eggs": return "Продай Маркізу " + Plural(n, "яйце", "яйця", "яєць");
            case "feed": return "Нагодуй курочок";
            case "pet_hens": return "Погладь курочок (" + n + " рази)";
            case "talk_boris": return "Поговори з Борисом";
            case "carrots": return "Збери на грядках " + Plural(n, "морквину", "морквини", "морквин");
            case "sell_carrots": return "Продай Маркізу " + Plural(n, "морквину", "морквини", "морквин");
            case "boris_carrot": return "Пригости Бориса морквою";
            case "milk_cow": return n == 1 ? "Подої корову" : "Подої корів (" + n + ")";
            case "milk_goat": return n == 1 ? "Подої кізочку" : "Подої кізочок (" + n + ")";
            case "sell_milk": return "Продай Маркізу " + Plural(n, "пляшку", "пляшки", "пляшок") + " молока";
            case "pet_animals": return "Погладь корів чи кізочок (" + n + " рази)";
            case "buy_animal": return "Купи нову тварину в Маркіза";
            case "sleep": return "Виспися вдома до ранку";
            default: return id;
        }
    }

    // Which reported event moves a job forward.
    static string TaskEventOf(string id)
    {
        switch (id)
        {
            case "eggs": return "egg";
            case "sell_eggs": return "sell_egg";
            case "carrots": return "harvest";
            case "sell_carrots": return "sell_carrot";
            case "milk_cow": return "milk";
            case "milk_goat": return "goat_milk";
            case "sell_milk": return "sell_milk";
            case "pet_hens": return "pet_hen";
            case "pet_animals": return "pet_animal";
            default: return id; // feed, talk_boris, boris_carrot, buy_animal, sleep
        }
    }

    // Something happened in the game: move the matching jobs forward, pay for finished ones.
    void TaskEvent(string what, int amount = 1)
    {
        for (int i = sideTasks.Count - 1; i >= 0; i--)
        {
            SideTask task = sideTasks[i];
            if (TaskEventOf(task.id) != what) continue;
            task.progress = Mathf.Min(task.target, task.progress + amount);
            tasksVersion++;
            if (task.progress < task.target) continue;
            sideTasks.RemoveAt(i);
            coins += task.reward;
            lastTaskDone = task.id;
            ShowBanner("ВИКОНАНО!  +" + task.reward, TaskText(task.id, task.target));
            FarmAudio.Play2D("success", 0.6f, 1.15f);
            nextTaskAt = Time.time + 4f; // a new job comes a little later
        }
    }

    // Keeps up to 3 jobs going (from the first eggs on).
    void UpdateSideTasks()
    {
        if (stage < Stage.WaitEggs || Time.time < nextTaskAt) return;
        if (sideTasks.Count >= MaxSideTasks) return;
        // A job that became impossible (e.g. "feed" after feeding, or at night) gives way to another one.
        for (int i = sideTasks.Count - 1; i >= 0; i--)
            if (!TaskAvailable(sideTasks[i].id) && sideTasks[i].progress == 0) sideTasks.RemoveAt(i);
        var choices = new List<string>();
        foreach (string id in TaskIds)
            if (TaskAvailable(id) && id != lastTaskDone && sideTasks.Find(t => t.id == id) == null) choices.Add(id);
        if (choices.Count == 0)
        {
            nextTaskAt = Time.time + 10f;
            return;
        }
        string pick = choices[Random.Range(0, choices.Count)];
        TaskSize(pick, out int min, out int max, out int each);
        int target = Random.Range(min, max + 1);
        if (pick == "carrots") target = Mathf.Max(2, target / 2 * 2); // carrots come in pairs
        sideTasks.Add(new SideTask { id = pick, target = target, reward = Mathf.Max(2, target * each) });
        tasksVersion++;
        nextTaskAt = Time.time + 1.5f;
        if (sideTasks.Count == 1 && !usedHints.Contains("#tasks"))
        {
            usedHints.Add("#tasks");
            Say("З'явилися побічні справи! Вони приносять монети. Дивись у завданнях (" + (FarmInput.HeadsetActive ? "X" : "Tab") + ").", 8f);
        }
    }

    string SideTasksText()
    {
        if (sideTasks.Count == 0) return "";
        string text = "\n\nСправи:";
        foreach (SideTask task in sideTasks)
            text += "\n• " + TaskText(task.id, task.target) + (task.target > 1 ? " (" + task.progress + "/" + task.target + ")" : "") + "  +" + task.reward;
        return text;
    }

    // ---------- saving ----------

    void SaveTasks(GameSave save)
    {
        save.taskIds = new string[sideTasks.Count];
        save.taskNumbers = new int[sideTasks.Count * 3];
        for (int i = 0; i < sideTasks.Count; i++)
        {
            save.taskIds[i] = sideTasks[i].id;
            save.taskNumbers[i * 3] = sideTasks[i].target;
            save.taskNumbers[i * 3 + 1] = sideTasks[i].progress;
            save.taskNumbers[i * 3 + 2] = sideTasks[i].reward;
        }
    }

    void LoadTasks(GameSave save)
    {
        sideTasks.Clear();
        if (save.taskIds == null || save.taskNumbers == null) return;
        for (int i = 0; i < save.taskIds.Length && i * 3 + 2 < save.taskNumbers.Length; i++)
            sideTasks.Add(new SideTask { id = save.taskIds[i], target = save.taskNumbers[i * 3], progress = save.taskNumbers[i * 3 + 1], reward = save.taskNumbers[i * 3 + 2] });
        tasksVersion++;
    }
}
