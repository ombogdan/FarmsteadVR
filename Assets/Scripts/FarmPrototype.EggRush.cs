using System.Collections;
using UnityEngine;

// Fun: "Яєчний переполох" (part of FarmPrototype, see FarmPrototype.cs) - a speed game in the coop.
// 30 seconds: eggs pop up in the nests one after another and vanish again after 2.5 s. Grab as many as
// you can (point + trigger). An egg 1 point, a golden one 3. The best score is kept; the first 3 rounds
// a day pay half the score (double on a fair day). These eggs are part of the game (not for selling).
// The sign by the coop door: FarmWorldBuilder.Chores.cs (EggRushSign).
public partial class FarmPrototype
{
    const float RushTime = 30f, RushEggLife = 2.5f;

    bool rushActive, rushRunning;
    float rushEndsAt, nextRushEgg, nextRushClock;
    int rushScore, rushBest, rushRounds, rushDay;
    readonly GameObject[] rushEggs = new GameObject[FarmWorldBuilder.NestsUpgraded];
    readonly float[] rushEggDies = new float[FarmWorldBuilder.NestsUpgraded];

    void UseEggRush()
    {
        if (AliveHens == 0 || stage < Stage.Trading)
        {
            Say("«Яєчний переполох» — гра на швидкість у курнику. Відкриється, коли з'являться курочки й Маркіз.", 5f);
            return;
        }
        if (dayNight.IsNight)
        {
            Say("Курочки сплять — переполох завтра!", 4f);
            return;
        }
        StartCoroutine(StartEggRush());
    }

    IEnumerator StartEggRush()
    {
        rushActive = true;
        FarmInput.MovementLocked = true;
        CloseShop();
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 0.3f);
        PlacePlayer(FarmWorldBuilder.CoopPosition + new Vector3(0f, 0f, -0.6f), 0f); // inside, facing the nests
        rushScore = 0;
        yield return fade.FadeTo(0f, 0.3f);
        ShowBanner("ЯЄЧНИЙ ПЕРЕПОЛОХ", "30 секунд! Яйця з'являються в гніздах і швидко зникають — хапай! Золоте — 3 очки.");
        if (zhora.activeSelf) Say("Жора: Увага, курник! Тренування на швидкість! Ко-ко!", 3f);
        yield return new WaitForSeconds(2.5f);
        rushRunning = true;
        rushEndsAt = Time.time + RushTime;
        nextRushEgg = Time.time;
        nextRushClock = 0f;
        FarmAudio.Play2D("rooster_crow", 0.5f, 1.2f);
    }

    // Called from Update while the game is on.
    void UpdateEggRush()
    {
        if (FarmInput.TasksPressed())
        {
            StartCoroutine(EndEggRush(true));
            return;
        }
        if (!rushRunning) return;
        float left = rushEndsAt - Time.time;
        if (Time.time >= nextRushClock)
        {
            nextRushClock = Time.time + 1f;
            Say("Залишилось " + Mathf.CeilToInt(Mathf.Max(0f, left)) + " с • Очки: " + rushScore, 1.2f);
        }
        if (left <= 0f)
        {
            rushRunning = false;
            StartCoroutine(EndEggRush(false));
            return;
        }
        // eggs that weren't grabbed in time vanish
        for (int i = 0; i < rushEggs.Length; i++)
            if (rushEggs[i] != null && Time.time >= rushEggDies[i]) { StartCoroutine(Puff(rushEggs[i].transform.position, 0.08f)); Destroy(rushEggs[i]); rushEggs[i] = null; }
        if (Time.time >= nextRushEgg)
        {
            nextRushEgg = Time.time + Random.Range(0.5f, 0.9f);
            int start = Random.Range(0, NestsInUse);
            for (int k = 0; k < NestsInUse; k++)
            {
                int nest = (start + k) % NestsInUse;
                if (nestEggs[nest] != null || rushEggs[nest] != null) continue; // a real egg or a game egg is there
                bool golden = Random.value < 0.15f;
                GameObject egg = Instantiate(eggTemplate, world);
                egg.name = "Rush Egg";
                egg.transform.position = nests[nest].position;
                egg.GetComponent<FarmInteractable>().itemId = golden ? "rush_golden" : "rush_egg";
                if (golden)
                    foreach (Renderer r in egg.transform.Find("Model").GetComponentsInChildren<Renderer>()) r.sharedMaterial = VrShapes.LoadMaterial("SunDisc");
                egg.SetActive(true);
                rushEggs[nest] = egg;
                rushEggDies[nest] = Time.time + RushEggLife;
                FarmAudio.Play("pop", nests[nest].position, 0.5f, golden ? 1.6f : 1.3f);
                break;
            }
        }
        if (FarmInput.InteractPressed() && aimedAtRush != null) GrabRushEgg(aimedAtRush);
    }

    FarmInteractable aimedAtRush; // what the laser points at during the game (set in UpdateAim)

    void GrabRushEgg(FarmInteractable egg)
    {
        int nest = System.Array.IndexOf(rushEggs, egg.gameObject);
        if (nest < 0) return;
        rushEggs[nest] = null;
        bool golden = egg.itemId == "rush_golden";
        rushScore += golden ? 3 : 1;
        FarmAudio.Play("pickup", egg.transform.position, 0.7f, golden ? 1.5f : 1.2f);
        Destroy(egg);
        StartCoroutine(FlyToHand(egg.transform, 0.35f));
    }

    IEnumerator EndEggRush(bool quit)
    {
        if (!rushActive) yield break;
        rushRunning = false;
        for (int i = 0; i < rushEggs.Length; i++) if (rushEggs[i] != null) { Destroy(rushEggs[i]); rushEggs[i] = null; }
        if (!quit)
        {
            bool record = rushScore > rushBest;
            if (record) rushBest = rushScore;
            if (rushDay != dayNight.day) { rushDay = dayNight.day; rushRounds = 0; }
            int prize = 0;
            if (rushRounds < 3)
            {
                prize = rushScore / 2 * (IsFairDay ? 2 : 1);
                rushRounds++;
                coins += prize;
            }
            TaskEvent("egg_rush");
            ShowBanner(record ? "НОВИЙ РЕКОРД ПЕРЕПОЛОХУ!" : "ЧАС ВИЙШОВ",
                "Очки: " + rushScore + " (рекорд " + rushBest + ")" + (prize > 0 ? " • приз +" + prize : rushRounds >= 3 ? " • призи на сьогодні скінчились" : ""));
            FarmAudio.Play2D(record ? "success" : "pickup", 0.8f);
            yield return new WaitForSeconds(3f);
        }
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 0.3f);
        PlacePlayer(FarmWorldBuilder.CoopDoorOutside + new Vector3(0f, 0f, -1.2f), 0f);
        rushActive = false;
        FarmInput.MovementLocked = false;
        yield return fade.FadeTo(0f, 0.3f);
        SaveGame();
    }
}
