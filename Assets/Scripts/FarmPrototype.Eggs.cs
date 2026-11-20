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
    int NestsInUse => coopUpgraded ? nests.Length : FarmWorldBuilder.NestCount;

    void FindEggParts()
    {
        coopUpgrade = Find("Coop Upgrade");
        nests = new Transform[FarmWorldBuilder.NestCount * 2]; // the first 4 below, 4 more on the upper shelf
        for (int i = 0; i < nests.Length; i++)
            nests[i] = (i < FarmWorldBuilder.NestCount ? repairedCoop.transform : coopUpgrade.transform).Find("Nest Spot " + i);
        nestEggs = new GameObject[nests.Length];
        eggTemplate = Find("Egg Template");
        itemLooks["egg"] = eggTemplate;
        foreach (GameObject hen in hens) henGrazers.Add(hen.GetComponent<Grazer>());
        henNextLay = new float[hens.Count];
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

    // How long until a hen lays her next egg: fed hens (FarmPrototype.Chores.cs) lay twice as often.
    float LayInterval()
    {
        return FedToday ? Random.Range(18f, 35f) : Random.Range(35f, 70f);
    }

    void UpdateEggs()
    {
        if (stage < Stage.WaitEggs || !dayNight.running || dayNight.IsNight || hensBought == 0) return;
        if (!firstEggLaid)
        {
            if (Time.time >= firstEggAt) LayEgg(0);
            return;
        }
        for (int i = 0; i < hensBought; i++)
        {
            if (Time.time < henNextLay[i]) continue;
            henNextLay[i] = Time.time + LayInterval();
            LayEgg(i);
        }
    }

    void LayEgg(int henIndex)
    {
        int nest = System.Array.IndexOf(nestEggs, null, 0, NestsInUse); // a free nest
        if (nest < 0) return;                                          // all nests are full: collect the eggs!
        SpawnEgg(nest);
        FarmAudio.Play("pop", nests[nest].position, 0.7f, 1.3f);

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
    }

    void SpawnEgg(int nest)
    {
        GameObject egg = Instantiate(eggTemplate, world);
        egg.name = "Egg";
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
        AddItem("egg");

        if (stage == Stage.WaitEggs) MarquisArrives(); // -> stage 4 (FarmPrototype.Shop.cs)
        else Say("Яйце в інвентарі (" + ItemCount("egg") + ").", 3f);
        SaveGame();
    }

    bool[] NestState()
    {
        bool[] state = new bool[nests.Length];
        for (int i = 0; i < nests.Length; i++) state[i] = nestEggs[i] != null;
        return state;
    }

    // ---------- night and morning ----------

    // Zhora and the hens you have.
    IEnumerable<Grazer> ActiveBirds()
    {
        if (zhora.activeSelf) yield return zhoraGrazer;
        for (int i = 0; i < hensBought && i < henGrazers.Count; i++) yield return henGrazers[i];
    }

    void OnEvening()
    {
        if (stage < Stage.WaitEggs) return;
        int spot = 0;
        foreach (Grazer bird in ActiveBirds())
        {
            // around the coop (never through its walls), in through the door, to its spot
            bird.GoVia(FarmWorldBuilder.AroundCoop(bird.transform.position,
                FarmWorldBuilder.CoopDoorOutside, FarmWorldBuilder.CoopDoorInside, FarmWorldBuilder.SleepSpot(spot++)));
            bird.SetSleeping(true);
        }
        goatGrazer.SetSleeping(true);
        Say("Вечоріє… Кури йдуть спати в курник. А тебе чекає ліжко в будинку діда Панаса.", 8f);
        SaveGame();
    }

    void OnMorning()
    {
        if (stage < Stage.WaitEggs) return;
        foreach (Grazer bird in ActiveBirds())
        {
            Vector3 outside = FarmWorldBuilder.CoopDoorOutside + new Vector3(Random.Range(-1.5f, 1.5f), 0f, -Random.Range(0f, 1f));
            bird.GoVia(FarmWorldBuilder.CoopDoorInside, outside);
            bird.SetSleeping(false);
        }
        goatGrazer.SetSleeping(false);
        for (int i = 0; i < hensBought; i++) henNextLay[i] = Time.time + Random.Range(8f, 30f);
        ChoresOnMorning(); // the feeder is empty again
        StartCoroutine(MorningCrow());
        SaveGame();
    }

    // Every morning Zhora jumps onto the coop roof and crows for the whole farm to hear.
    IEnumerator MorningCrow()
    {
        string morning = "Ранок! День " + dayNight.day + ". Курочки голодні — насип зерна в годівницю.";
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
        dayNight.SkipToMorning();
        yield return new WaitForSeconds(0.6f);
        FarmInput.MovementLocked = false;
        yield return fade.FadeTo(0f, 1f);
    }

    // ---------- loading a saved game ----------

    // Bought from Marquis: hammers knock, the upper shelf with 4 nests, a ladder and a window pop in.
    IEnumerator UpgradeCoop()
    {
        repairing = true;
        Say("Стук-стук! Курник розширюється…", 4f);
        StartCoroutine(HammerKnocks(FarmWorldBuilder.CoopDoorOutside + Vector3.up * 1.6f, 4, 0.2f));
        for (int k = 0; k < 4; k++)
        {
            FarmAudio.Play(k % 2 == 0 ? "knock_1" : "knock_2", FarmWorldBuilder.CoopPosition + Vector3.up * 1.5f, 1f, Random.Range(0.95f, 1.05f));
            yield return new WaitForSeconds(0.2f);
        }
        coopUpgraded = true;
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
            if (save.nests[i]) SpawnEgg(i);
        firstEggLaid = stage > Stage.WaitEggs || ItemCount("egg") > 0 || System.Array.Exists(nestEggs, egg => egg != null);
        firstEggAt = Time.time + 12f;
        for (int i = 0; i < hensBought; i++) henNextLay[i] = Time.time + Random.Range(10f, 40f);
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
