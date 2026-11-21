using System.Collections;
using UnityEngine;

// Little surprises during the day (part of FarmPrototype, see FarmPrototype.cs), so no two days are alike:
// - a runaway hen: now and then a hen flutters over the fence and wanders off (to the road, the field,
//   behind the barn, the forest edge). An orange beacon spins above her. Find her and point at her:
//   she hops back home (+3 coins). Not caught by the evening? She comes back by herself, no reward.
// - golden eggs are in FarmPrototype.Eggs.cs (a 6% chance per egg).
// - a fair day: every 3rd day. Flags along the road, double prizes in the fun games,
//   and Marquis pays a third more for eggs, carrots and milk.
public partial class FarmPrototype
{
    static readonly Vector3[] RunawaySpots =
    {
        new Vector3(-3.5f, 0f, -7.5f),   // by the road, near Marquis's stall
        new Vector3(19.5f, 0f, -4.5f),   // in the field past the slingshot targets
        new Vector3(2f, 0f, 22.6f),      // behind the barn, by the hedge
        new Vector3(-12f, 0f, -2.4f),    // at the forest edge, by the stump
    };
    static readonly string[] RunawayWhere = { "біля дороги, коло лавки Маркіза", "у полі за мішенями", "за великою стодолою", "на узліссі, біля пенька" };

    // ---------- the coop door and the fox ----------
    // The coop has a door. Shut it for the night: around midnight a fox comes by, and through an open door
    // it takes a hen (you hear about it in the morning). In the morning open the door, or the hens stay in.

    Transform coopDoor;
    bool coopDoorOpen = true;
    int foxNightChecked = -1;
    string foxNews = "";

    void UseCoopDoor()
    {
        SetCoopDoor(!coopDoorOpen, true);
        if (coopDoorOpen)
        {
            TaskEvent("open_coop");
            if (dayNight.running && !dayNight.IsNight)
                foreach (Grazer bird in ActiveBirds()) // out they go
                    bird.GoVia(FarmWorldBuilder.CoopDoorInside, FarmWorldBuilder.CoopDoorOutside + new Vector3(Random.Range(-1.5f, 1.5f), 0f, -Random.Range(0f, 1f)));
            Say(dayNight.IsNight ? "Курник відчинено. Уночі краще тримати його зачиненим — лис!" : "Курник відчинено — курочки, гуляйте!", 4f);
        }
        else
        {
            TaskEvent("close_coop");
            Say(dayNight.IsNight || dayNight.time01 > 0.6f ? "Курник зачинено. Лисові тут нічого не світить!" : "Курник зачинено. Не забудь відчинити, щоб курочки могли гуляти.", 4f);
        }
        SaveGame();
    }

    void SetCoopDoor(bool open, bool animate)
    {
        coopDoorOpen = open;
        if (coopDoor == null) return;
        coopDoor.GetComponent<FarmInteractable>().SetLabel(open ? "зачинити курник" : "відчинити курник");
        if (animate) StartCoroutine(SwingCoopDoor(open ? 100f : 0f));
        else coopDoor.localRotation = Quaternion.Euler(0f, open ? 100f : 0f, 0f);
    }

    IEnumerator SwingCoopDoor(float to)
    {
        FarmAudio.Play(to > 0f ? "creak" : "knock_1", coopDoor.position + Vector3.up, 0.6f, 1.5f, 3f);
        float from = coopDoor.localEulerAngles.y;
        if (from > 180f) from -= 360f;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            coopDoor.localRotation = Quaternion.Euler(0f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / 0.5f)), 0f);
            yield return null;
        }
        coopDoor.localRotation = Quaternion.Euler(0f, to, 0f);
    }

    // Around midnight the fox comes by.
    // force: you went to sleep, so the night passes at once (the fox still comes).
    void FoxVisit(bool force = false)
    {
        if (!dayNight.running || !dayNight.IsNight || (!force && dayNight.time01 < 0.87f) || foxNightChecked == dayNight.day) return;
        foxNightChecked = dayNight.day;
        if (stage < Stage.Trading || AliveHens == 0 || Random.value > 0.6f) return; // not every night
        if (!coopDoorOpen)
        {
            foxNews = " Вночі навколо курника ходив лис, але двері були зачинені. Молодець!";
            return;
        }
        int last = -1;
        for (int i = 0; i < hensBought && i < hens.Count; i++) if (!henGone[i]) last = i;
        if (last < 0) return;
        henGone[last] = true;
        hens[last].SetActive(false);
        foxNews = " Ой лихо: вночі приходив лис і забрав курочку " + hens[last].GetComponent<FarmInteractable>().itemId + "! Зачиняй курник на ніч.";
        SaveGame();
    }

    // The fox's news goes into the morning message (MorningCrow).
    string TakeFoxNews()
    {
        string news = foxNews;
        foxNews = "";
        return news;
    }

    int runaway = -1;            // which hen is out (index in hens), -1 = nobody
    float nextRunawayCheck = 60f;
    Vector2 runawayAreaMin, runawayAreaMax; // where she grazed before (to give it back)
    Transform runawayBeacon;

    bool IsFairDay => dayNight != null && dayNight.running && stage >= Stage.Trading && dayNight.day % 3 == 0;
    int fairShownFor = -1;
    GameObject fairFlags;

    // What Marquis pays today (a third more on a fair day).
    int SellPrice(string id) => IsFairDay ? (4 * SellPrices[id] + 2) / 3 : SellPrices[id];

    void UpdateFair()
    {
        if (fairFlags == null) fairFlags = Find("Fair Flags");
        bool fair = IsFairDay;
        if (fairFlags != null && fairFlags.activeSelf != fair) fairFlags.SetActive(fair);
        if (fair && fairShownFor != dayNight.day && !dayNight.IsNight && dayNight.time01 > 0.03f)
        {
            fairShownFor = dayNight.day;
            ShowBanner("ЯРМАРОК У СИДОРІВЦІ!", "Сьогодні подвійні призи в «Курячому польоті», «Перегонах» і «Тирі», а Маркіз купує все на третину дорожче!");
            FarmAudio.Play2D("success", 0.7f);
        }
    }

    void UpdateEvents()
    {
        UpdateFair();
        FoxVisit();
        if (runawayBeacon != null && runaway >= 0)
        {
            Transform hen = hens[runaway].transform;
            runawayBeacon.position = hen.position + Vector3.up * (0.85f + Mathf.Sin(Time.time * 4f) * 0.06f);
            runawayBeacon.Rotate(0f, 180f * Time.deltaTime, 0f);
        }
        if (Time.time < nextRunawayCheck) return;
        nextRunawayCheck = Time.time + 60f;
        if (runaway >= 0 || stage < Stage.Trading || !dayNight.running || dayNight.IsNight || dayNight.time01 > 0.6f) return;
        if (AliveHens < 2 || Random.value > 0.15f) return;
        StartCoroutine(HenRunsAway());
    }

    IEnumerator HenRunsAway()
    {
        int pick = -1;
        for (int tries = 0; tries < 10 && pick < 0; tries++)
        {
            int i = Random.Range(0, hensBought);
            if (!henGone[i] && hens[i].activeSelf) pick = i;
        }
        if (pick < 0) yield break;
        runaway = pick;
        int where = Random.Range(0, RunawaySpots.Length);
        Transform hen = hens[pick].transform;
        Grazer grazer = henGrazers[pick];
        runawayAreaMin = grazer.areaMin;
        runawayAreaMax = grazer.areaMax;
        grazer.enabled = false;
        Vector3 spot = RunawaySpots[where] + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
        yield return Hop(hen, hen.position, spot, 1.4f, 2.2f); // over the fence!
        grazer.PlaceAt(spot);
        grazer.areaMin = new Vector2(spot.x - 1.2f, spot.z - 1.2f);
        grazer.areaMax = new Vector2(spot.x + 1.2f, spot.z + 1.2f);
        grazer.enabled = true;
        runawayBeacon = VrShapes.Create(PrimitiveType.Sphere, "Runaway Beacon", null, "Glow").transform;
        runawayBeacon.localScale = new Vector3(0.12f, 0.2f, 0.12f);
        string name = hens[pick].GetComponent<FarmInteractable>().itemId;
        ShowBanner("КУРКА-ВТІКАЧКА!", name + " перелетіла через паркан і втекла " + RunawayWhere[where] + ". Знайди її до вечора!");
        FarmAudio.Play2D("nope", 0.5f, 1.4f);
    }

    // Pointing at the runaway hen: she hops back over the fence to the coop.
    bool CatchRunaway(FarmInteractable hen)
    {
        if (runaway < 0 || hens[runaway] != hen.gameObject) return false;
        StartCoroutine(BringRunawayHome(true));
        return true;
    }

    IEnumerator BringRunawayHome(bool caught)
    {
        int index = runaway;
        runaway = -1;
        if (runawayBeacon != null) Destroy(runawayBeacon.gameObject);
        Transform hen = hens[index].transform;
        Grazer grazer = henGrazers[index];
        grazer.enabled = false;
        if (caught)
        {
            string name = hens[index].GetComponent<FarmInteractable>().itemId;
            SpeakerSays(hen, name + ": Ко-ко! Я тільки на хвилинку!", 2f);
            coins += 3;
            TaskEvent("catch_hen");
            FarmAudio.Play2D("coin", 0.7f);
            Say("Спіймано! " + name + " повертається додому. +3 монети.", 4f);
            yield return new WaitForSeconds(0.6f);
        }
        Vector3 home = FarmWorldBuilder.CoopDoorOutside + new Vector3(Random.Range(-1f, 1f), 0f, -0.5f);
        yield return Hop(hen, hen.position, home, 1.4f, 2.2f);
        grazer.areaMin = runawayAreaMin;
        grazer.areaMax = runawayAreaMax;
        grazer.PlaceAt(home);
        grazer.enabled = true;
    }

    // Evening: a hen that's still out comes home by herself (before everyone goes to sleep).
    void RunawayOnEvening()
    {
        if (runaway < 0) return;
        Say("Втікачка повернулася додому сама — цього разу без нагороди.", 5f);
        int index = runaway;
        runaway = -1;
        if (runawayBeacon != null) Destroy(runawayBeacon.gameObject);
        Grazer grazer = henGrazers[index];
        grazer.areaMin = runawayAreaMin;
        grazer.areaMax = runawayAreaMax;
        grazer.PlaceAt(FarmWorldBuilder.CoopDoorOutside);
    }
}
