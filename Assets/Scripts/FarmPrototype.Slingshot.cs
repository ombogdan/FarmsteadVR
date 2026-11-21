using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fun: "Курячий політ" (part of FarmPrototype, see FarmPrototype.cs). A big slingshot in the field by
// the road launches your hens (in little helmets) at three hay targets. A round is 5 shots.
//   aim  - with your hand (the laser) on the Quest, with the mouse on the Mac; dots show the flight;
//   hold the trigger (E) - the power bar runs up and down and the slingshot stretches;
//   let go - the hen flies! Bullseye 5, red ring 3, white ring 2, hay 1, the grass 0.
// The best score is kept; the first 3 rounds a day pay coins (half the score). Hens love it.
// The slingshot and the targets are built in FarmWorldBuilder.Chores.cs (ChickenFlight).
public partial class FarmPrototype
{
    const int ShotsPerRound = 5;
    const int PaidRoundsPerDay = 3;

    bool slingshotActive;
    Transform slingshot, pouch, tipL, tipR, bandL, bandR;
    Vector3 pouchRest;
    readonly List<Transform> aimDots = new List<Transform>();
    int shotsLeft, roundScore, bestScore, roundsToday, roundsDay;
    bool charging, flying;
    float chargeStart;
    Transform pilot;

    void FindSlingshotParts()
    {
        slingshot = world.Find("Slingshot");
        pouch = slingshot.Find("Pouch");
        tipL = slingshot.Find("Tip L");
        tipR = slingshot.Find("Tip R");
        bandL = slingshot.Find("Band L");
        bandR = slingshot.Find("Band R");
        pouchRest = pouch.localPosition;
        StretchBands();
    }

    void UseSlingshot()
    {
        if (AliveHens == 0 || stage < Stage.Trading)
        {
            Say("«Курячий політ»! Рогатка для курочок-пілотів. Спершу заведи курочок і познайомся з Маркізом.", 6f);
            return;
        }
        if (dayNight.IsNight)
        {
            Say("Курочки-пілоти сплять. Політи — вдень!", 4f);
            return;
        }
        StartCoroutine(StartFlightGame());
    }

    // A quick fade, and you stand behind the slingshot looking at the targets.
    IEnumerator StartFlightGame()
    {
        slingshotActive = true;
        FarmInput.MovementLocked = true;
        CloseShop();
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 0.3f);
        PlacePlayer(FarmWorldBuilder.SlingshotPosition + new Vector3(-1.4f, 0f, 0f), 90f);
        shotsLeft = ShotsPerRound;
        roundScore = 0;
        LoadPilot();
        yield return fade.FadeTo(0f, 0.3f);
        string how = FarmInput.HeadsetActive ? "Цілься рукою, затисни курок — і відпусти!" : "Цілься мишею, затисни E — і відпусти!";
        ShowBanner("КУРЯЧИЙ ПОЛІТ", ShotsPerRound + " курочок-пілотів. " + how);
        if (zhora.activeSelf) Say("Жора: Пілоти, шоломи вдягнули? На старт! Ко-ко!", 4f);
    }

    // The next hen sits in the pouch, in a red helmet.
    void LoadPilot()
    {
        var alive = new List<GameObject>();
        for (int i = 0; i < hensBought && i < hens.Count; i++) if (!henGone[i]) alive.Add(hens[i]);
        GameObject source = alive[Random.Range(0, alive.Count)];
        pilot = new GameObject("Pilot " + source.GetComponent<FarmInteractable>().itemId).transform;
        foreach (Transform part in source.transform)
        {
            if (part.name == "Glow Ring") continue;
            Transform copy = Instantiate(part.gameObject, pilot).transform;
            copy.localPosition = part.localPosition;
            copy.localRotation = part.localRotation;
            copy.localScale = part.localScale;
        }
        Transform helmet = VrShapes.Create(PrimitiveType.Sphere, "Helmet", pilot, "ToolboxRed").transform;
        helmet.localPosition = new Vector3(0f, 0.47f, -0.12f);
        helmet.localScale = new Vector3(0.15f, 0.1f, 0.16f);
        foreach (Transform t in pilot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = VrShapes.IgnoreRaycastLayer;
        pilot.name += " (" + source.GetComponent<FarmInteractable>().itemId + ")";
        pilotName = source.GetComponent<FarmInteractable>().itemId;
        SitInPouch();
    }

    string pilotName;

    void SitInPouch()
    {
        if (pilot == null) return;
        pilot.position = pouch.position + Vector3.down * 0.25f;
        pilot.rotation = Quaternion.LookRotation(-slingshot.forward); // beak (-Z) towards the targets
    }

    // Called from Update while the game is on (instead of the normal farm actions).
    void UpdateSlingshot()
    {
        if (flying || pilot == null) return;
        Transform source = aimSource != null ? aimSource : playerCamera.transform;
        Vector3 aim = ClampAim(source.forward);
        float power = charging ? Mathf.PingPong((Time.time - chargeStart) / 0.9f, 1f) : 0.35f;

        // The pouch is pulled back (with the hen in it) as the power grows; the bands stretch.
        pouch.position = slingshot.TransformPoint(pouchRest) - aim * (0.15f + 0.45f * power);
        StretchBands();
        SitInPouch();
        ShowAimDots(pouch.position, aim * LaunchSpeed(power));

        if (FarmInput.InteractPressed())
        {
            charging = true;
            chargeStart = Time.time;
            FarmAudio.Play2D("tick", 0.5f, 0.8f);
        }
        if (charging && FarmInput.InteractReleased())
        {
            charging = false;
            StartCoroutine(Fly(aim * LaunchSpeed(power), power));
        }
        if (FarmInput.TasksPressed()) StartCoroutine(EndFlightGame(true)); // B / J: leave the round
    }

    static float LaunchSpeed(float power) => Mathf.Lerp(6.5f, 14.5f, power);

    // Keep the shot going roughly towards the targets and a bit upwards.
    Vector3 ClampAim(Vector3 wanted)
    {
        Vector3 ahead = slingshot.forward;
        Vector3 flat = Vector3.ProjectOnPlane(wanted, Vector3.up);
        if (flat.sqrMagnitude < 0.001f) flat = ahead;
        float yaw = Mathf.Clamp(Vector3.SignedAngle(ahead, flat, Vector3.up), -35f, 35f);
        Vector3 across = Quaternion.AngleAxis(yaw, Vector3.up) * ahead;
        float pitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(wanted.normalized.y, -1f, 1f)) * Mathf.Rad2Deg + 20f, 8f, 55f) * Mathf.Deg2Rad;
        return across * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch); // a bit upwards, so you can lob it
    }

    void StretchBands()
    {
        Band(bandL, tipL.position, pouch.position);
        Band(bandR, tipR.position, pouch.position);
    }

    static void Band(Transform band, Vector3 from, Vector3 to)
    {
        band.position = (from + to) / 2f;
        band.rotation = Quaternion.LookRotation(to - from);
        band.localScale = new Vector3(band.localScale.x, band.localScale.y, Vector3.Distance(from, to) / Mathf.Max(0.001f, band.parent.lossyScale.z));
    }

    // Yellow dots along where the hen would fly (only the first part: it's a game, not a calculator).
    void ShowAimDots(Vector3 from, Vector3 velocity)
    {
        while (aimDots.Count < 12)
        {
            Transform dot = VrShapes.Create(PrimitiveType.Sphere, "Aim Dot", null, "Yellow").transform;
            dot.localScale = Vector3.one * 0.06f;
            aimDots.Add(dot);
        }
        for (int i = 0; i < aimDots.Count; i++)
        {
            float t = (i + 1) * 0.06f;
            Vector3 p = from + velocity * t + 0.5f * Physics.gravity * t * t;
            aimDots[i].position = p;
            aimDots[i].gameObject.SetActive(true);
        }
    }

    void HideAimDots()
    {
        foreach (Transform dot in aimDots) if (dot != null) dot.gameObject.SetActive(false);
    }

    // The flight: an arc, the hen spinning, a few feathers; then the score where she lands.
    IEnumerator Fly(Vector3 velocity, float power)
    {
        flying = true;
        HideAimDots();
        FarmAudio.Play("whoosh", pouch.position, 1f, 0.8f + power * 0.4f, 4f);
        FarmAudio.Play("knock_1", pouch.position, 0.6f, 1.6f);
        SpeakerSays(pilot, pilotName + ": Ко-ко-ко-о-о!", 1.5f);
        Vector3 p = pouch.position;
        pouch.localPosition = pouchRest; // the slingshot snaps back
        StretchBands();
        float spin = Random.Range(360f, 720f) * (Random.value < 0.5f ? -1f : 1f);
        float nextFeather = 0f;
        for (float t = 0f; t < 5f; t += Time.deltaTime)
        {
            velocity += Physics.gravity * Time.deltaTime;
            p += velocity * Time.deltaTime;
            if (p.y <= 0.05f) { p.y = 0.05f; break; }
            pilot.position = p;
            pilot.rotation = Quaternion.LookRotation(velocity.normalized) * Quaternion.Euler(0f, 180f, spin * t);
            if (t >= nextFeather)
            {
                nextFeather = t + 0.12f;
                Burst(p + Vector3.up * 0.2f, "FarmMaterials/Paper", 1, 0.03f, -velocity * 0.1f, 0.3f);
            }
            yield return null;
        }
        pilot.position = p;
        pilot.rotation = Quaternion.LookRotation(-new Vector3(velocity.x, 0f, velocity.z).normalized);
        int points = ScoreAt(p, out string words);
        roundScore += points;
        shotsLeft--;
        StartCoroutine(Puff(p + Vector3.up * 0.1f, 0.5f));
        FarmAudio.Play(points >= 5 ? "success" : points > 0 ? "coin" : "pop", p + Vector3.up * 0.5f, 1f, 1f, 10f);
        SpeakerSays(pilot, (points > 0 ? "+" + points + "! " : "") + words, 2.2f);
        // a happy little hop, then she runs off home (hens love this)
        Vector3 ground = p;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            pilot.position = ground + Vector3.up * Mathf.Sin(t / 0.5f * Mathf.PI) * 0.35f;
            yield return null;
        }
        yield return new WaitForSeconds(1.2f);
        Destroy(pilot.gameObject);
        pilot = null;
        flying = false;
        if (shotsLeft > 0) LoadPilot();
        else StartCoroutine(EndFlightGame(false));
    }

    int ScoreAt(Vector3 landed, out string words)
    {
        float best = float.MaxValue;
        foreach (Vector3 target in FarmWorldBuilder.TargetSpots)
            best = Mathf.Min(best, FlatDistance(landed, target));
        if (best <= 0.35f) { words = "В ЯБЛУЧКО!"; return 5; }
        if (best <= 0.65f) { words = "Червоне кільце!"; return 3; }
        if (best <= 1.0f) { words = "Біле кільце!"; return 2; }
        if (best <= 1.4f) { words = "М'якенько в сіно!"; return 1; }
        words = "Мимо... але політ гарний!";
        return 0;
    }

    // Results, the prize, a fade, and you're back by the slingshot (free to walk).
    IEnumerator EndFlightGame(bool quit)
    {
        if (!slingshotActive) yield break;
        HideAimDots();
        charging = false;
        if (pilot != null && !flying) { Destroy(pilot.gameObject); pilot = null; }
        bool record = !quit && roundScore > bestScore;
        if (record) bestScore = roundScore;
        int prize = 0;
        if (roundsDay != dayNight.day) { roundsDay = dayNight.day; roundsToday = 0; }
        if (!quit && roundsToday < PaidRoundsPerDay)
        {
            prize = roundScore / 2 * (IsFairDay ? 2 : 1);
            roundsToday++;
            coins += prize;
        }
        if (!quit)
        {
            TaskEvent("fun");
            ShowBanner(record ? "НОВИЙ РЕКОРД!" : "ПОЛІТ ЗАВЕРШЕНО",
                "Очки: " + roundScore + " (рекорд " + bestScore + ")" + (prize > 0 ? " • приз +" + prize : roundsToday >= PaidRoundsPerDay ? " • призи на сьогодні скінчились" : ""));
            FarmAudio.Play2D(record ? "success" : "pickup", 0.8f);
            yield return new WaitForSeconds(2.5f);
        }
        while (flying) yield return null;
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 0.3f);
        pouch.localPosition = pouchRest;
        StretchBands();
        slingshotActive = false;
        FarmInput.MovementLocked = false;
        yield return fade.FadeTo(0f, 0.3f);
        SaveGame();
    }
}
