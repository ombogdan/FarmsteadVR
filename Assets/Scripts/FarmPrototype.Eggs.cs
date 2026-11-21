using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stage 3 (part of FarmPrototype, see FarmPrototype.cs): eggs, day and night, sleeping.
// - During the day each hen lays an egg every ~35-70 s into a free nest inside the coop.
// - You walk into the coop and take the eggs (they go to the inventory).
// - In the evening the hens and Zhora walk into the coop to sleep, in the morning Zhora crows
//   and everybody comes out. At night you can sleep on the hay by the barn until morning.
public partial class FarmPrototype
{
    Transform[] nests;
    GameObject[] nestEggs;
    GameObject eggTemplate;
    readonly List<Grazer> henGrazers = new List<Grazer>();
    float[] henNextLay;
    bool firstEggLaid;
    float firstEggAt;

    bool coopUpgraded;     // bought from Marquis: 4 more nests and room for 4 more hens
    GameObject coopUpgrade;

    int HenLimit => coopUpgraded ? hens.Count : Mathf.Min(hens.Count, FarmWorldBuilder.HensBeforeUpgrade);
    int NestsInUse => coopUpgraded ? FarmWorldBuilder.NestsUpgraded : FarmWorldBuilder.NestsBase;
    int[] eggsToday;       // how many eggs each hen has laid today (a hen lays 2 a day, 3 when fed)
    int DailyEggs => 2;

    void FindEggParts()
    {
        coopUpgrade = Find("Coop Upgrade");
        nests = new Transform[FarmWorldBuilder.NestsUpgraded]; // 2 rows of 4 by the back wall, then 6 by the right wall
        for (int i = 0; i < nests.Length; i++)
            nests[i] = (i < FarmWorldBuilder.NestsBase ? repairedCoop.transform : coopUpgrade.transform).Find("Nest Spot " + i);
        nestEggs = new GameObject[nests.Length];
        eggTemplate = Find("Egg Template");
        itemLooks["egg"] = eggTemplate;
        itemLooks["golden_egg"] = eggTemplate;
        foreach (GameObject hen in hens) henGrazers.Add(hen.GetComponent<Grazer>());
        henNextLay = new float[hens.Count];
        eggsToday = new int[hens.Count];
        for (int i = 0; i < henNextLay.Length; i++) henNextLay[i] = float.MaxValue; // not laying yet
    }

    // Zhora's thanks are over: the clock starts, the first egg comes soon.
    void StartEggs()
    {
        stage = Stage.WaitEggs;
        dayNight.SetTime(0.06f, 1); // a fresh morning
        dayNight.running = true;
        for (int i = 0; i < hensBought; i++) henNextLay[i] = Time.time + LayInterval();
        firstEggAt = Time.time + 25f; // feeding the hens brings it sooner (FillFeeder)
        Say("Етап 2 пройдено! Тепер на фермі є день і ніч. Нагодуй курочок — зерно в мішку біля курника, годівниця перед входом.", 9f);
        SaveGame();
    }

    // How long until a hen lays her next egg: about every 1.5-2 minutes, at most 2 a day, and only when
    // there's grain in the feeder (FarmPrototype.Chores.cs). Keeps the farm from getting rich in five minutes.
    float LayInterval()
    {
        return Random.Range(80f, 130f);
    }

    void UpdateEggs()
    {
        if (stage < Stage.WaitEggs || !dayNight.running || dayNight.IsNight || hensBought == 0) return;
        if (hungryDays > 0 && firstEggLaid) return; // hungry hens don't lay
        if (!firstEggLaid)
        {
            if (Time.time >= firstEggAt) LayEgg(0);
            return;
        }
        for (int i = 0; i < hensBought; i++)
        {
            if (henGone[i] || Time.time < henNextLay[i] || eggsToday[i] >= DailyEggs) continue;
            henNextLay[i] = Time.time + LayInterval();
            LayEgg(i);
        }
    }

    bool LayEgg(int henIndex)
    {
        int nest = System.Array.IndexOf(nestEggs, null, 0, NestsInUse); // a free nest
        if (nest < 0) return false;                                    // all nests are full: collect the eggs!
        bool golden = firstEggLaid && stage >= Stage.Trading && Random.value < 0.06f; // rare: a golden egg!
        SpawnEgg(nest, golden);
        eggsToday[henIndex]++;
        FarmAudio.Play("pop", nests[nest].position, 0.7f, 1.3f);
        if (golden)
        {
            FarmAudio.Play("success", nests[nest].position, 0.8f, 1.3f, 6f);
            Say("Ко-ко-КО! У курнику з'явилося ЗОЛОТЕ яйце! Маркіз дасть за нього " + SellPrice("golden_egg") + " монет.", 7f);
        }

        GameObject hen = hens[henIndex];
        string name = hen.GetComponent<FarmInteractable>().itemId;
        if (!firstEggLaid)
        {
            firstEggLaid = true;
            Say(name + " знесла перше яйце! Зайди в курник: гнізда біля задньої стіни, біля яйця світиться маленьке коло.", 12f);
        }
        else if (!MessageVisible)
        {
            SpeakerSays(hen.transform, name + ": Ко-ко-ко! Яйце!", 3f);
        }
        SaveGame();
        return true;
    }

    void SpawnEgg(int nest, bool golden = false)
    {
        GameObject egg = Instantiate(eggTemplate, world);
        egg.name = golden ? "Golden Egg" : "Egg";
        if (golden)
        {
            egg.GetComponent<FarmInteractable>().itemId = "golden_egg";
            egg.GetComponent<FarmInteractable>().label = "взяти золоте яйце";
            Material shine = VrShapes.LoadMaterial("SunDisc");
            foreach (Renderer r in egg.transform.Find("Model").GetComponentsInChildren<Renderer>()) r.sharedMaterial = shine;
        }
        egg.transform.position = nests[nest].position;
        egg.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        egg.SetActive(true);
        nestEggs[nest] = egg;
    }

    void CollectEgg(FarmInteractable egg)
    {
        int nest = System.Array.IndexOf(nestEggs, egg.gameObject);
        if (nest >= 0) nestEggs[nest] = null;
        FarmAudio.Play("pickup", egg.transform.position, 0.8f, 1.2f);
        Highlight(egg, false);
        Transform ring = egg.transform.Find("Glow Ring");
        if (ring != null) ring.gameObject.SetActive(false);
        Destroy(egg); // no longer something to take...
        StartCoroutine(FlyToHand(egg.transform, 0.45f)); // ...it flies into your hand
        bool golden = egg.itemId == "golden_egg";
        AddItem(golden ? "golden_egg" : "egg");
        TaskEvent("egg");
        if (golden) FarmAudio.Play2D("coin", 0.8f, 1.3f);

        if (stage == Stage.WaitEggs) MarquisArrives(); // -> stage 4 (FarmPrototype.Shop.cs)
        else Say(golden ? "Золоте яйце в рюкзаку!" : "Яйце в інвентарі (" + ItemCount("egg") + ").", 3f);
        SaveGame();
    }

    bool[] NestState(bool goldenOnly = false)
    {
        bool[] state = new bool[nests.Length];
        for (int i = 0; i < nests.Length; i++) state[i] = nestEggs[i] != null &&
            (!goldenOnly || nestEggs[i].GetComponent<FarmInteractable>().itemId == "golden_egg");
        return state;
    }

    // ---------- night and morning ----------

    // Zhora and the hens you have.
    IEnumerable<Grazer> ActiveBirds()
    {
        if (zhora.activeSelf) yield return zhoraGrazer;
        for (int i = 0; i < hensBought && i < henGrazers.Count; i++) if (!henGone[i]) yield return henGrazers[i];
    }

    void OnEvening()
    {
        if (stage < Stage.WaitEggs) return;
        RunawayOnEvening(); // a runaway hen comes home first (FarmPrototype.Events.cs)
        int spot = 0;
        foreach (Grazer bird in ActiveBirds())
        {
            // around the coop (never through its walls), in through the door, to its spot
            bird.GoVia(FarmWorldBuilder.AroundCoop(bird.transform.position,
                FarmWorldBuilder.CoopDoorOutside, FarmWorldBuilder.CoopDoorInside, FarmWorldBuilder.SleepSpot(spot++)));
            bird.SetSleeping(true);
        }
        goatGrazer.SetSleeping(true);
        BarnOnEvening();
        Say("Вечоріє… Кури йдуть спати в курник. Зачини його на ніч — буває лис! А тебе чекає ліжко в будинку діда Панаса.", 8f);
        SaveGame();
    }

    void OnMorning()
    {
        if (stage < Stage.WaitEggs) return;
        foreach (Grazer bird in ActiveBirds())
        {
            bird.SetSleeping(false);
            if (!coopDoorOpen) continue; // the door is shut: they wait inside until you open it
            Vector3 outside = FarmWorldBuilder.CoopDoorOutside + new Vector3(Random.Range(-1.5f, 1.5f), 0f, -Random.Range(0f, 1f));
            bird.GoVia(FarmWorldBuilder.CoopDoorInside, outside);
        }
        goatGrazer.SetSleeping(false);
        for (int i = 0; i < hensBought; i++)
        {
            henNextLay[i] = Time.time + Random.Range(20f, 60f);
            eggsToday[i] = 0; // a new day
        }
        ChoresOnMorning(); // the feeder is empty again
        BarnOnMorning();
        StartCoroutine(MorningCrow());
        SaveGame();
    }

    // Every morning Zhora jumps onto the coop roof and crows for the whole farm to hear.
    IEnumerator MorningCrow()
    {
        string morning = "Ранок! День " + dayNight.day + "." + morningNote + TakeFoxNews() + (coopDoorOpen ? "" : " Відчини курник, щоб курочки вийшли гуляти!");
        if (!zhora.activeSelf)
        {
            Say(morning, 6f);
            yield break;
        }
        yield return new WaitForSeconds(1f);
        Transform rooster = zhora.transform;
        Vector3 front = FarmWorldBuilder.CoopDoorOutside + new Vector3(-0.8f, 0f, -0.3f);
        Vector3 roof = FarmWorldBuilder.CoopPosition + new Vector3(0f, FarmWorldBuilder.CoopRidge, 0f);
        zhoraGrazer.enabled = false; // we move him ourselves for a moment
        rooster.position = front;    // out of the door first
        yield return Hop(rooster, front, roof, 0.8f, 1f);
        for (int k = 0; k < 2; k++)
        {
            FarmAudio.Play("rooster_crow", roof + Vector3.up * 0.5f, 1f, 1f, 25f); // loud: heard all over the farm
            SpeakerSays(rooster, "Жора: КУКУРІКУ-У-У!", 2.2f);
            yield return new WaitForSeconds(2.2f);
        }
        yield return Hop(rooster, roof, front, 0.7f, 0.5f);
        zhoraGrazer.PlaceAt(front); // done: no more walking in and out of the door
        zhoraGrazer.enabled = true;
        Say(morning, 6f);
    }

    void GoToSleep()
    {
        if (!dayNight.running)
        {
            Say("Ще рано спати — справ повно!", 4f);
            return;
        }
        if (!dayNight.IsNight)
        {
            Say("Ще рано спати. Приходь, коли стемніє.", 4f);
            return;
        }
        StartCoroutine(SleepUntilMorning());
    }

    // Fade to black, the night passes, fade back in.
    IEnumerator SleepUntilMorning()
    {
        FarmInput.MovementLocked = true;
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 1f);
        int spot = 0;
        foreach (Grazer bird in ActiveBirds()) bird.PlaceAt(FarmWorldBuilder.SleepSpot(spot++)); // everyone is in bed
        FoxVisit(true); // the night passes: the fox still comes by
        dayNight.SkipToMorning();
        TaskEvent("sleep");
        yield return new WaitForSeconds(0.6f);
        FarmInput.MovementLocked = false;
        yield return fade.FadeTo(0f, 1f);
    }

    // ---------- loading a saved game ----------

    // Bought from Marquis: a second trough appears next to the feeder (20 kg in all).
    IEnumerator UpgradeFeeder()
    {
        feederUpgraded = true; // ownership is saved with the purchase, before the animation yields
        StartCoroutine(HammerKnocks(FarmWorldBuilder.FeederPosition + new Vector3(1.2f, 0.4f, 0f), 3, 0.2f));
        yield return new WaitForSeconds(0.6f);
        Transform extension = feeder.transform.Find("Feeder Extension");
        extension.gameObject.SetActive(true);
        StartCoroutine(Puff(extension.position + Vector3.up * 0.2f, 0.5f));
        FarmAudio.Play("success", extension.position + Vector3.up, 0.9f);
        ShowFeederGrain();
        Say("Велика годівниця готова: тепер вміщує " + FeederCapacity + " кг зерна!", 6f);
        SaveGame();
    }

    // Bought from Marquis: hammers knock, the upper shelf with 4 nests, a ladder and a window pop in.
    IEnumerator UpgradeCoop()
    {
        coopUpgraded = true; // ownership is saved with the purchase, before the animation yields
        repairing = true;
        Say("Стук-стук! Курник розширюється…", 4f);
        StartCoroutine(HammerKnocks(FarmWorldBuilder.CoopDoorOutside + Vector3.up * 1.6f, 4, 0.2f));
        for (int k = 0; k < 4; k++)
        {
            FarmAudio.Play(k % 2 == 0 ? "knock_1" : "knock_2", FarmWorldBuilder.CoopPosition + Vector3.up * 1.5f, 1f, Random.Range(0.95f, 1.05f));
            yield return new WaitForSeconds(0.2f);
        }
        yield return PopIn(coopUpgrade, 0.08f);
        FarmAudio.Play("success", FarmWorldBuilder.CoopPosition + Vector3.up * 1.5f, 1f);
        repairing = false;
        if (zhora.activeSelf)
            SpeakerSays(zhora.transform, "Жора: Ко-ко! Новий поверх! Тепер у моєму курнику місця ще для 4 курочок. Директор задоволений!", 7f);
        else
            Say("Курник розширено: +4 гнізда і місце ще для 4 курочок!", 6f);
        SaveGame();
    }

    void ApplyEggState(GameSave save)
    {
        coopUpgrade.SetActive(coopUpgraded);
        for (int i = 0; i < NestsInUse && i < save.nests.Length; i++)
            if (save.nests[i]) SpawnEgg(i, save.goldenNests != null && i < save.goldenNests.Length && save.goldenNests[i]);
        firstEggLaid = stage > Stage.WaitEggs || ItemCount("egg") > 0 || System.Array.Exists(nestEggs, egg => egg != null);
        firstEggAt = Time.time + (save.laySeconds == null ? 12f : save.firstEggSeconds);
        for (int i = 0; i < hensBought; i++)
        {
            float delay = save.laySeconds != null && i < save.laySeconds.Length ? save.laySeconds[i] : Random.Range(10f, 40f);
            henNextLay[i] = Time.time + delay;
            if (save.dailyEggs != null && i < save.dailyEggs.Length) eggsToday[i] = save.dailyEggs[i];
        }
        if (dayNight.running && dayNight.IsNight)
        {
            int spot = 0;
            foreach (Grazer bird in ActiveBirds())
            {
                bird.PlaceAt(FarmWorldBuilder.SleepSpot(spot++));
                bird.SetSleeping(true);
            }
            goatGrazer.SetSleeping(true);
        }
    }
}
