using System.Collections;
using UnityEngine;

// Fun: "Перегони" (part of FarmPrototype, see FarmPrototype.cs). Boris, Zhora and Marquis race along
// the road from СТАРТ to ФІНІШ. You stand on the wooden stand by the road (no riding: no dizziness)
// and spur Boris on:
//   a ring pulses above Boris to a beat; press the trigger (E) right when it's GREEN -> a big dash,
//   at another moment -> a small push; mash the button -> Boris trips over his own legs.
// Zhora sprints and then has to catch his breath; Marquis runs steadily and dashes at the end.
// Prizes: 1st place 10 coins, 2nd place 4 (the first 3 races a day). The track: FarmWorldBuilder.Chores.cs.
public partial class FarmPrototype
{
    const float BeatPeriod = 0.5f, BeatWindow = 0.13f;
    const float BorisBaseSpeed = 2.2f;
    const int PaidRacesPerDay = 3;
    static readonly string[] RacerNames = { "Борис", "Жора", "Маркіз" };

    bool raceActive, raceRunning;
    float raceStartedAt, lastPress = -10f, stumbleUntil, zhoraPhaseUntil;
    bool zhoraSprinting;
    readonly float[] racerX = new float[3], racerSpeed = new float[3];
    readonly Transform[] racers = new Transform[3];
    Transform beatRing, catRunner;
    Renderer beatLook;
    Material beatIdle, beatHot;
    Vector3 borisHome, zhoraHome;
    int racesToday, racesDay, greatPresses;

    void UseRaceSign()
    {
        if (stage < Stage.Trading || !zhora.activeSelf)
        {
            Say("«Перегони»: Борис проти Жори й Маркіза! Відкриються, коли познайомишся з Маркізом.", 6f);
            return;
        }
        if (dayNight.IsNight)
        {
            Say("Учасники перегонів сплять. Приходь удень!", 4f);
            return;
        }
        StartCoroutine(StartRace());
    }

    IEnumerator StartRace()
    {
        raceActive = true;
        FarmInput.MovementLocked = true;
        CloseShop();
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 0.3f);

        PlacePlayer(FarmWorldBuilder.RaceStandSpot + Vector3.up * 0.2f, 180f); // on the stand, facing the road
        borisHome = goat.position;
        zhoraHome = zhora.transform.position;
        goatGrazer.enabled = false;
        zhoraGrazer.enabled = false;
        catRunner = Instantiate(marquis, world).transform; // a running copy of Marquis (the real one minds the shop)
        catRunner.gameObject.SetActive(true);
        foreach (Collider c in catRunner.GetComponentsInChildren<Collider>(true)) Destroy(c);
        Destroy(catRunner.GetComponent<FarmInteractable>());
        racers[0] = goat;
        racers[1] = zhora.transform;
        racers[2] = catRunner;
        for (int i = 0; i < 3; i++)
        {
            racerX[i] = FarmWorldBuilder.RaceStartX - 0.8f;
            racerSpeed[i] = 0f;
            racers[i].position = new Vector3(racerX[i], 0f, FarmWorldBuilder.RaceLanes[i]);
            racers[i].rotation = Quaternion.LookRotation(Vector3.left); // noses (-Z) towards the finish
        }
        beatRing = VrShapes.Create(PrimitiveType.Sphere, "Beat Ring", null, "Glow").transform;
        beatLook = beatRing.GetComponent<Renderer>();
        beatIdle = VrShapes.LoadMaterial("Glow");
        beatHot = VrShapes.LoadMaterial("Leaves");
        greatPresses = 0;
        yield return fade.FadeTo(0f, 0.3f);

        string button = FarmInput.HeadsetActive ? "курок" : "E";
        ShowBanner("ПЕРЕГОНИ", "Тисни " + button + " в такт, коли коло над Борисом ЗЕЛЕНЕ! Не поспішай — спіткнеться.");
        yield return new WaitForSeconds(3f);
        for (int n = 3; n >= 1; n--)
        {
            ShowBanner(n.ToString(), "");
            FarmAudio.Play2D("tick", 0.9f, 0.8f);
            yield return new WaitForSeconds(0.8f);
        }
        ShowBanner("СТАРТ!", "Жени Бориса!");
        FarmAudio.Play2D("bus_horn", 0.5f, 1.2f);
        raceRunning = true;
        raceStartedAt = Time.time;
        zhoraPhaseUntil = Time.time;
    }

    // Called from Update while a race is on (instead of the normal farm actions).
    void UpdateRace()
    {
        if (FarmInput.TasksPressed() && raceActive)
        {
            StartCoroutine(EndRace(-1)); // B / J: leave the round
            return;
        }
        if (!raceRunning) return;
        float dt = Time.deltaTime;
        float beat = Mathf.Repeat(Time.time - raceStartedAt, BeatPeriod);
        bool inWindow = Mathf.Min(beat, BeatPeriod - beat) <= BeatWindow;

        // The ring above Boris: jumps big on every beat, green while it's the right moment.
        beatRing.position = goat.position + Vector3.up * 1.35f;
        float pulse = 1f + 0.6f * Mathf.Exp(-beat * 9f);
        beatRing.localScale = new Vector3(0.32f * pulse, 0.02f, 0.32f * pulse);
        beatLook.sharedMaterial = inWindow ? beatHot : beatIdle;

        // Boris: that's you.
        float boris = racerSpeed[0];
        if (FarmInput.InteractPressed())
        {
            if (Time.time - lastPress < 0.22f)
            {
                stumbleUntil = Time.time + 0.6f;
                FarmAudio.Play2D("nope", 0.6f);
                SpeakerSays(goat, "Борис: Ой-ой, ноги заплуталися!", 1f);
            }
            else if (inWindow)
            {
                boris += 2.2f;
                FarmAudio.Play2D("tick", 0.6f, 1.5f);
                if (++greatPresses % 4 == 0) SpeakerSays(goat, "Борис: Ме-е-е! Лечу!", 0.8f);
            }
            else boris += 0.8f;
            lastPress = Time.time;
        }
        boris = Time.time < stumbleUntil ? 0.8f : Mathf.Min(6.5f, Mathf.MoveTowards(boris, BorisBaseSpeed, 2f * dt));
        racerSpeed[0] = boris;

        // Zhora: a sprint, then catching his breath.
        if (Time.time > zhoraPhaseUntil)
        {
            zhoraSprinting = !zhoraSprinting;
            zhoraPhaseUntil = Time.time + (zhoraSprinting ? Random.Range(1f, 1.4f) : Random.Range(0.8f, 1.2f));
        }
        racerSpeed[1] = Mathf.MoveTowards(racerSpeed[1], zhoraSprinting ? 5f : 2.1f, 8f * dt);
        // Marquis: steady, and a sneaky dash near the finish.
        racerSpeed[2] = Mathf.MoveTowards(racerSpeed[2], racerX[2] > FarmWorldBuilder.RaceFinishX - 8f ? 4.7f : 3.25f, 4f * dt);

        int winner = -1;
        for (int i = 0; i < 3; i++)
        {
            racerX[i] += racerSpeed[i] * dt;
            float hop = i == 2 ? Mathf.Abs(Mathf.Sin(racerX[i] * 5f)) * 0.08f : 0f; // the cat bounces along
            racers[i].position = new Vector3(racerX[i], hop, FarmWorldBuilder.RaceLanes[i]);
            if (racerX[i] >= FarmWorldBuilder.RaceFinishX && winner < 0) winner = i;
        }
        if (winner >= 0)
        {
            raceRunning = false;
            StartCoroutine(EndRace(winner));
        }
    }

    // The results, a prize, then everybody goes home.
    IEnumerator EndRace(int winner)
    {
        raceRunning = false;
        if (winner >= 0)
        {
            int place = 1 + (racerX[1] > racerX[0] ? 1 : 0) + (racerX[2] > racerX[0] ? 1 : 0);
            if (racesDay != dayNight.day) { racesDay = dayNight.day; racesToday = 0; }
            int prize = 0;
            if (racesToday < PaidRacesPerDay)
            {
                prize = (place == 1 ? 10 : place == 2 ? 4 : 0) * (IsFairDay ? 2 : 1);
                racesToday++;
                coins += prize;
            }
            TaskEvent("race");
            if (place == 1) TaskEvent("race_win");
            ShowBanner(winner == 0 ? "БОРИС ПЕРЕМІГ!" : RacerNames[winner].ToUpper() + " ПЕРЕМІГ",
                "Борис — " + place + " місце" + (prize > 0 ? " • приз +" + prize : racesToday >= PaidRacesPerDay ? " • призи на сьогодні скінчились" : ""));
            FarmAudio.Play2D(place == 1 ? "success" : "pickup", 0.9f);
            string line = winner == 0 ? "Борис: Ме-е-е! Я найшвидший козел у Сидорівці!"
                : winner == 1 ? "Жора: Ко-ко! Директори не програють!"
                : "Маркіз: Мур-р. Хитрість — теж швидкість.";
            SpeakerSays(racers[winner], line, 3f);
            yield return new WaitForSeconds(3.5f);
        }
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 0.3f);
        if (catRunner != null) Destroy(catRunner.gameObject);
        if (beatRing != null) Destroy(beatRing.gameObject);
        goat.position = borisHome;
        zhora.transform.position = zhoraHome;
        goatGrazer.PlaceAt(borisHome);
        zhoraGrazer.PlaceAt(zhoraHome);
        goatGrazer.enabled = true;
        zhoraGrazer.enabled = true;
        PlacePlayer(new Vector3(FarmWorldBuilder.RaceStartX - 1.2f, 0f, -10.4f), 0f);
        raceActive = false;
        FarmInput.MovementLocked = false;
        yield return fade.FadeTo(0f, 0.3f);
        SaveGame();
    }
}
