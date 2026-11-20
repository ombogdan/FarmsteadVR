using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The big barn and the big animals (part of FarmPrototype, see FarmPrototype.cs):
// - after the house is repaired, Boris gives you the key of the old barn;
// - the key turns in the lock, the chain falls, the big doors creak open (dust!) -> stalls, pens, hay;
// - then Marquis sells animals, unlocked step by step:
//     she-goats: once the barn is open;  cows: once you have 2 she-goats;
// - they graze on the lawn in the yard by day and sleep in the barn at night.
// The things themselves are built in FarmWorldBuilder.Chores.cs (BarnDoors, Animals).
public partial class FarmPrototype
{
    static readonly string[] BorisBarn =
    {
        "Ме-е! Ти відчинив стодолу! Я сюди не заходив, відколи дід поїхав.",
        "Колись тут жили корови Зірка й Ласка, і кізочки — мої подружки. Ліворуч стійла, праворуч загончики, нагорі сіно.",
        "Маркіз може привезти кізочок. А як матимеш дві — то й корову. Корови люблять компанію.",
        "Пасовище вже є — дивись, яка трава! Ферма «Сидорівка» знову замукає. Ме-е!",
    };
    static readonly string[] CowNoises = { "Му-у-у!", "Му?", "*жує траву*", "Му-у." };
    static readonly string[] SheGoatNoises = { "Ме-е!", "Ме-е-е?", "*скубе травичку*" };

    GameObject barnDoors, barnLock, barnGlow;
    Transform leftDoor, rightDoor;
    BoxCollider barnCollider;
    FarmInteractable barnAction;
    readonly List<GameObject> cows = new List<GameObject>();
    readonly List<GameObject> goats = new List<GameObject>();
    int cowsBought, goatsBought;
    int animalTalk;

    void FindBarnParts()
    {
        barnDoors = Find("Barn Doors");
        barnLock = barnDoors.transform.Find("Lock").gameObject;
        barnGlow = barnDoors.transform.Find("Glow Ring").gameObject;
        leftDoor = barnDoors.transform.Find("Left Door");
        rightDoor = barnDoors.transform.Find("Right Door");
        barnCollider = barnDoors.GetComponent<BoxCollider>();
        barnAction = barnDoors.GetComponent<FarmInteractable>();
        for (int i = 0; i < FarmWorldBuilder.CowNames.Length; i++) cows.Add(Find("Cow " + i));
        for (int i = 0; i < FarmWorldBuilder.GoatNames.Length; i++) goats.Add(Find("She-goat " + i));
        itemLooks["barn_key"] = Find("Barn Key Look");
    }

    // At the end of the house repair (and for games saved after it): Boris hands over the key.
    void GiveBarnKey()
    {
        if (stage != Stage.EpisodeDone) return;
        stage = Stage.OpenBarn;
        AddItem("barn_key");
        barnGlow.SetActive(true);
        FarmAudio.Play2D("pickup", 0.8f);
        Say("Борис дав тобі ключ від старої великої стодоли! Відчини її — подивимось, що там.", 8f);
        SaveGame();
    }

    void UseBarn()
    {
        if (repairing) return;
        if (stage >= Stage.BarnOpen)
        {
            Say("Стодола відчинена. Заходь! Ліворуч стійла для корів, праворуч — загончики для кіз.", 5f);
            return;
        }
        if (stage < Stage.OpenBarn || !Has("barn_key"))
        {
            Say("Стара стодола діда Панаса. Ворота замкнені на великий іржавий замок, а ключа в тебе немає.", 6f);
            return;
        }
        StartCoroutine(OpenBarnDoors());
    }

    // The key flies to the lock and turns, the chain falls, the doors swing open with a creak and dust.
    IEnumerator OpenBarnDoors()
    {
        repairing = true;
        barnGlow.SetActive(false);
        RemoveItem("barn_key", 1);
        Vector3 lockAt = barnLock.transform.position;
        Transform key = ToolCopy("barn_key");
        if (key != null)
        {
            Vector3 from = HandPosition();
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                key.position = Vector3.Lerp(from, lockAt + Vector3.back * 0.06f, t / 0.5f) + Vector3.up * Mathf.Sin(t / 0.5f * Mathf.PI) * 0.2f;
                key.rotation = Quaternion.LookRotation(Vector3.right);
                yield return null;
            }
            FarmAudio.Play("tick", lockAt, 1f, 0.6f);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) // turn!
            {
                key.rotation = Quaternion.LookRotation(Vector3.right) * Quaternion.Euler(0f, 0f, 90f * t / 0.35f);
                yield return null;
            }
            FarmAudio.Play("knock_2", lockAt, 1f, 1.3f);
            Destroy(key.gameObject);
        }
        // the lock and the chain drop to the ground
        Vector3 start = barnLock.transform.position;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            barnLock.transform.position = start + Vector3.down * (0.5f * 9.8f * t * t);
            barnLock.transform.rotation = Quaternion.Euler(0f, 0f, t * 200f);
            yield return null;
        }
        FarmAudio.Play("knock_1", barnLock.transform.position, 1f, 0.8f);
        barnLock.SetActive(false);

        FarmAudio.Play("creak", barnDoors.transform.position + new Vector3(0f, 2f, -4.6f), 1f, 1f, 8f);
        for (float t = 0f; t < 1.8f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 1.8f);
            leftDoor.localRotation = Quaternion.Euler(0f, 100f * k, 0f);   // both swing outwards
            rightDoor.localRotation = Quaternion.Euler(0f, -100f * k, 0f);
            yield return null;
        }
        for (int i = 0; i < 4; i++)
            StartCoroutine(Puff(barnDoors.transform.position + new Vector3(-1.5f + i, 0.1f, -4.6f), 0.6f));
        barnCollider.enabled = false; // the way in is free
        FarmAudio.Play("success", barnDoors.transform.position + new Vector3(0f, 2f, -4.6f), 1f);
        stage = Stage.BarnOpen;
        repairing = false;
        SaveGame();

        yield return new WaitForSeconds(1f);
        yield return BorisComesOver();
        if (!InDialogue)
            StartDialogue(goat, "Борис", BorisBarn, () =>
                Say("Нове: у Маркіза можна купити кізочок (а згодом і корову). Вони пастимуться на газоні.", 9f));
    }

    // A bought animal walks out of the barn onto the pasture.
    void BuyAnimal(List<GameObject> herd, int index)
    {
        if (index >= herd.Count) return;
        GameObject animal = herd[index];
        Transform t = animal.transform;
        t.position = FarmWorldBuilder.BarnDoorInside;
        t.rotation = Quaternion.Euler(0f, 180f, 0f); // facing out (animals look along -Z)
        animal.SetActive(true);
        StartCoroutine(Puff(t.position + Vector3.up * 0.2f, 0.7f));
        FarmAudio.Play("pop", t.position + Vector3.up, 1f, 0.8f);
        Grazer grazer = animal.GetComponent<Grazer>();
        grazer.GoVia(FarmWorldBuilder.BarnDoorOutside, PasturePoint());
        string name = animal.GetComponent<FarmInteractable>().itemId;
        bool isCow = herd == cows;
        SpeakerSays(t, name + ": " + (isCow ? "Му-у-у!" : "Ме-е!"), 4f);
        if (!isCow && goatsBought == 2)
            Say("Дві кізочки вже пасуться! Маркіз тепер може привезти й корову.", 7f);
    }

    Vector3 PasturePoint()
    {
        Vector2 min = FarmWorldBuilder.PastureMin, max = FarmWorldBuilder.PastureMax;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 p = new Vector3(Random.Range(min.x + 1f, max.x - 1f), 0f, Random.Range(min.y + 1f, max.y - 1f));
            bool blocked = false;
            foreach (Rect r in FarmWorldBuilder.PastureObstacles) blocked |= r.Contains(new Vector2(p.x, p.z));
            if (!blocked) return p;
        }
        return FarmWorldBuilder.BarnDoorOutside + Vector3.back * 3f;
    }

    void TalkToAnimal(FarmInteractable animal)
    {
        Grazer grazer = animal.GetComponent<Grazer>();
        bool isCow = animal.name.StartsWith("Cow");
        string[] noises = isCow ? CowNoises : SheGoatNoises;
        string sound = grazer != null && grazer.Sleeping ? "Хр-р-р..." : noises[animalTalk++ % noises.Length];
        SpeakerSays(animal.transform, animal.itemId + ": " + sound, 3f);
    }

    IEnumerable<Grazer> BigAnimals()
    {
        for (int i = 0; i < cowsBought && i < cows.Count; i++) yield return cows[i].GetComponent<Grazer>();
        for (int i = 0; i < goatsBought && i < goats.Count; i++) yield return goats[i].GetComponent<Grazer>();
    }

    Vector3 BarnSpot(Grazer animal)
    {
        int cow = cows.IndexOf(animal.gameObject);
        return cow >= 0 ? FarmWorldBuilder.CowStall(cow) : FarmWorldBuilder.GoatPen(goats.IndexOf(animal.gameObject));
    }

    // Evening: everybody walks into the barn to their stall. Morning: out to the pasture.
    void BarnOnEvening()
    {
        foreach (Grazer animal in BigAnimals())
        {
            animal.GoVia(FarmWorldBuilder.BarnDoorOutside, FarmWorldBuilder.BarnDoorInside, BarnSpot(animal));
            animal.SetSleeping(true);
        }
    }

    void BarnOnMorning()
    {
        foreach (Grazer animal in BigAnimals())
        {
            animal.GoVia(FarmWorldBuilder.BarnDoorInside, FarmWorldBuilder.BarnDoorOutside, PasturePoint());
            animal.SetSleeping(false);
        }
    }

    string BarnQuestText()
    {
        if (stage == Stage.OpenBarn) return "Відімкни велику стодолу ключем від Бориса: наведи на ворота.";
        if (goatsBought == 0) return "Купи в Маркіза кізочку (" + CoinsText(Prices["goat"]) + "): вона пастиметься на газоні, а спатиме в стодолі.";
        if (goatsBought < 2) return "Купи ще одну кізочку: коли їх буде дві, Маркіз привезе й корову.";
        if (cowsBought == 0) return "Тепер можна купити корову (" + CoinsText(Prices["cow"]) + ")! У стодолі є стійла для двох.";
        return "Ферма ожила: кізочок " + goatsBought + ", корів " + cowsBought + ". Далі буде: молоко, сир і ярмарок!";
    }

    // ---------- loading a saved game ----------

    void ApplyBarnState(GameSave save)
    {
        cowsBought = Mathf.Min(save.cowsBought, cows.Count);
        goatsBought = Mathf.Min(save.goatsBought, goats.Count);
        bool open = stage >= Stage.BarnOpen;
        barnLock.SetActive(!open);
        barnCollider.enabled = !open;
        leftDoor.localRotation = Quaternion.Euler(0f, open ? 100f : 0f, 0f);
        rightDoor.localRotation = Quaternion.Euler(0f, open ? -100f : 0f, 0f);
        barnGlow.SetActive(stage == Stage.OpenBarn);
        bool night = dayNight.running && dayNight.IsNight;
        foreach (Grazer animal in BigAnimals())
        {
            animal.gameObject.SetActive(true);
            animal.PlaceAt(night ? BarnSpot(animal) : PasturePoint());
            animal.SetSleeping(night);
        }
    }
}
