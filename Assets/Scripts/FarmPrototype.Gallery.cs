using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fun: "Тир Маркіза" (part of FarmPrototype, see FarmPrototype.cs). 30 seconds at Marquis's booth by
// the road: aim with your hand (the mouse on the Mac), press the trigger (E) and a pebble flies.
//   a can 1 point, the golden can 3, a wooden duck sliding on the rail 2.
// Knocked-down things fly off spinning and come back a moment later. The best score is kept;
// the first 3 rounds a day pay half the score in coins. The booth: FarmWorldBuilder.Chores.cs.
public partial class FarmPrototype
{
    const float GalleryTime = 30f;
    const int PaidGalleryRoundsPerDay = 3;

    class GalleryTarget
    {
        public Transform look;
        public Vector3 home;
        public int points;
        public float radius;
        public bool duck, down;
        public float speed, phase;
    }

    bool galleryActive, galleryRunning;
    float galleryEndsAt, nextShotAt, nextClockAt;
    int galleryScore, galleryBest, galleryRounds, galleryDay;
    readonly List<GalleryTarget> galleryTargets = new List<GalleryTarget>();

    void UseGallery()
    {
        if (stage < Stage.Trading)
        {
            Say("«Тир Маркіза»: зачинено. Маркіз відкриє його, коли ви познайомитесь.", 5f);
            return;
        }
        if (dayNight.IsNight)
        {
            Say("Тир зачинено до ранку. Мур.", 4f);
            return;
        }
        StartCoroutine(StartGallery());
    }

    IEnumerator StartGallery()
    {
        galleryActive = true;
        FarmInput.MovementLocked = true;
        CloseShop();
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 0.3f);
        PlacePlayer(FarmWorldBuilder.GalleryPosition + new Vector3(0f, 0f, 1.95f), 180f); // at the counter, facing the shelf
        MakeGalleryTargets();
        galleryScore = 0;
        yield return fade.FadeTo(0f, 0.3f);
        ShowBanner("ТИР МАРКІЗА", "30 секунд! Цілься й тисни " + (FarmInput.HeadsetActive ? "курок" : "E") + ". Бляшанка 1, качечка 2, золота бляшанка 3.");
        Say("Маркіз: Мур! Ну-ка, покажи, який ти влучний!", 3f);
        yield return new WaitForSeconds(2.5f);
        galleryRunning = true;
        galleryEndsAt = Time.time + GalleryTime;
        nextClockAt = 0f;
        FarmAudio.Play2D("bus_horn", 0.35f, 1.6f);
    }

    // 6 cans on the shelf (one of them golden) and 3 ducks on the rail above.
    void MakeGalleryTargets()
    {
        ClearGalleryTargets();
        Vector3 booth = FarmWorldBuilder.GalleryPosition;
        int golden = Random.Range(0, 6);
        for (int i = 0; i < 6; i++)
        {
            Transform can = VrShapes.Create(PrimitiveType.Cube, "Can", null, i == golden ? "Yellow" : "Metal").transform;
            can.localScale = new Vector3(0.1f, 0.16f, 0.1f);
            Vector3 home = new Vector3(booth.x - 1.4f + i * 0.56f, FarmWorldBuilder.GalleryShelfY + 0.08f, FarmWorldBuilder.GalleryShelfZ);
            can.position = home;
            galleryTargets.Add(new GalleryTarget { look = can, home = home, points = i == golden ? 3 : 1, radius = 0.1f });
        }
        for (int i = 0; i < 3; i++)
        {
            Transform duck = new GameObject("Duck").transform;
            Transform body = VrShapes.Create(PrimitiveType.Sphere, "Duck Body", duck, "Yellow").transform;
            body.localScale = new Vector3(0.22f, 0.13f, 0.08f);
            Transform head = VrShapes.Create(PrimitiveType.Sphere, "Duck Head", duck, "Yellow").transform;
            head.localPosition = new Vector3(0.08f, 0.1f, 0f);
            head.localScale = Vector3.one * 0.09f;
            Transform beak = VrShapes.Create(PrimitiveType.Cube, "Duck Beak", duck, "ToolboxRed").transform;
            beak.localPosition = new Vector3(0.14f, 0.1f, 0f);
            beak.localScale = new Vector3(0.05f, 0.02f, 0.03f);
            Vector3 home = new Vector3(booth.x, FarmWorldBuilder.GalleryRailY, FarmWorldBuilder.GalleryShelfZ - 0.12f); // just in front of the back board
            galleryTargets.Add(new GalleryTarget { look = duck, home = home, points = 2, radius = 0.14f, duck = true, speed = 0.7f + i * 0.25f, phase = i * 1.3f });
        }
    }

    void ClearGalleryTargets()
    {
        foreach (GalleryTarget t in galleryTargets) if (t.look != null) Destroy(t.look.gameObject);
        galleryTargets.Clear();
    }

    // Called from Update while the gallery is on (instead of the normal farm actions).
    void UpdateGallery()
    {
        if (FarmInput.TasksPressed())
        {
            StartCoroutine(EndGallery(true));
            return;
        }
        // ducks slide left and right along the rail
        foreach (GalleryTarget t in galleryTargets)
        {
            if (!t.duck || t.down) continue;
            float x = Mathf.PingPong(Time.time * t.speed + t.phase, 3f) - 1.5f;
            t.look.position = t.home + Vector3.right * x;
            t.look.rotation = Quaternion.Euler(0f, Mathf.Repeat(Time.time * t.speed + t.phase, 6f) < 3f ? 0f : 180f, 0f);
        }
        if (!galleryRunning) return;
        float left = galleryEndsAt - Time.time;
        if (Time.time >= nextClockAt)
        {
            nextClockAt = Time.time + 1f;
            Say("Залишилось " + Mathf.CeilToInt(Mathf.Max(0f, left)) + " с • Очки: " + galleryScore, 1.2f);
            if (left <= 5.5f && left > 0f) FarmAudio.Play2D("tick", 0.5f, 1.3f);
        }
        if (left <= 0f)
        {
            galleryRunning = false;
            StartCoroutine(EndGallery(false));
            return;
        }
        if (FarmInput.InteractPressed() && Time.time >= nextShotAt)
        {
            nextShotAt = Time.time + 0.25f;
            Transform source = aimSource != null ? aimSource : playerCamera.transform;
            Shoot(source.position, source.forward);
        }
    }

    // A pebble flies along the laser; the first standing thing close enough to its path is hit.
    void Shoot(Vector3 from, Vector3 direction)
    {
        GalleryTarget hit = null;
        float nearest = 20f;
        foreach (GalleryTarget t in galleryTargets)
        {
            if (t.down) continue;
            Vector3 toTarget = t.look.position - from;
            float along = Vector3.Dot(toTarget, direction);
            if (along <= 0f || along >= nearest) continue;
            float off = (toTarget - direction * along).magnitude;
            if (off <= t.radius) { hit = t; nearest = along; }
        }
        Vector3 end = hit != null ? hit.look.position : from + direction * 8f;
        StartCoroutine(Pebble(HandPosition(), end));
        FarmAudio.Play2D("whoosh", 0.35f, 1.8f);
        if (hit == null) return;
        hit.down = true;
        galleryScore += hit.points;
        FarmAudio.Play(Random.value < 0.5f ? "knock_1" : "knock_2", hit.look.position, 1f, 1.6f + hit.points * 0.1f);
        if (hit.points >= 3) FarmAudio.Play2D("coin", 0.7f);
        StartCoroutine(KnockDown(hit, direction));
    }

    IEnumerator Pebble(Vector3 from, Vector3 to)
    {
        Transform pebble = VrShapes.Create(PrimitiveType.Sphere, "Pebble", null, "DarkWood").transform;
        pebble.localScale = Vector3.one * 0.035f;
        for (float t = 0f; t < 0.14f; t += Time.deltaTime)
        {
            pebble.position = Vector3.Lerp(from, to, t / 0.14f);
            yield return null;
        }
        Destroy(pebble.gameObject);
    }

    // The thing jumps back, spins and falls; after a moment it stands up again.
    IEnumerator KnockDown(GalleryTarget t, Vector3 direction)
    {
        yield return new WaitForSeconds(0.13f);
        if (t.look == null) yield break;
        Vector3 velocity = direction.normalized * 2.5f + Vector3.up * 2f;
        Vector3 spin = Random.insideUnitSphere * 720f;
        for (float k = 0f; k < 0.7f && t.look != null; k += Time.deltaTime)
        {
            velocity += Physics.gravity * Time.deltaTime;
            t.look.position += velocity * Time.deltaTime;
            t.look.Rotate(spin * Time.deltaTime, Space.World);
            yield return null;
        }
        if (t.look == null) yield break;
        t.look.gameObject.SetActive(false);
        yield return new WaitForSeconds(t.duck ? 1.2f : 1.8f);
        if (t.look == null) yield break;
        t.look.position = t.home;
        t.look.rotation = Quaternion.identity;
        t.look.gameObject.SetActive(true);
        StartCoroutine(Puff(t.home, 0.15f));
        t.down = false;
    }

    IEnumerator EndGallery(bool quit)
    {
        if (!galleryActive) yield break;
        galleryRunning = false;
        if (!quit)
        {
            bool record = galleryScore > galleryBest;
            if (record) galleryBest = galleryScore;
            if (galleryDay != dayNight.day) { galleryDay = dayNight.day; galleryRounds = 0; }
            int prize = 0;
            if (galleryRounds < PaidGalleryRoundsPerDay)
            {
                prize = galleryScore / 2 * (IsFairDay ? 2 : 1);
                galleryRounds++;
                coins += prize;
            }
            TaskEvent("gallery");
            ShowBanner(record ? "НОВИЙ РЕКОРД ТИРУ!" : "ЧАС ВИЙШОВ",
                "Очки: " + galleryScore + " (рекорд " + galleryBest + ")" + (prize > 0 ? " • приз +" + prize : galleryRounds >= PaidGalleryRoundsPerDay ? " • призи на сьогодні скінчились" : ""));
            FarmAudio.Play2D(record ? "success" : "pickup", 0.8f);
            SpeakerSays(marquis.transform, galleryScore >= 20 ? "Маркіз: Мур-р! Снайпер! Сам би краще не зміг. Майже." : "Маркіз: Мур. Непогано! Приходь ще.", 3f);
            yield return new WaitForSeconds(3f);
        }
        ScreenFade fade = ScreenFade.On(playerCamera);
        yield return fade.FadeTo(1f, 0.3f);
        ClearGalleryTargets();
        galleryActive = false;
        FarmInput.MovementLocked = false;
        yield return fade.FadeTo(0f, 0.3f);
        SaveGame();
    }
}
