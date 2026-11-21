using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Little animations that make actions feel real (part of FarmPrototype, see FarmPrototype.cs):
// - a copy of the tool appears and does the job: the shovel digs, the rake rakes,
//   the watering can pours, the hammer knocks during repairs;
// - bits fly: earth, grain, water drops, dust puffs when something is built;
// - what you take (eggs, carrots) flies into your hand.
// Everything made here is temporary and is removed when its animation ends.
public partial class FarmPrototype
{
    // ---------- tools ----------

    // A copy of what an item looks like (its "Model" part), to animate. Null if we don't know its look.
    Transform ToolCopy(string id)
    {
        if (!itemLooks.TryGetValue(id, out GameObject look) || look == null) return null;
        Transform model = look.transform.Find("Model");
        if (model == null) return null;
        Transform copy = Instantiate(model.gameObject).transform;
        copy.name = "Animated " + id;
        copy.gameObject.SetActive(true);
        foreach (Transform part in copy.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = VrShapes.IgnoreRaycastLayer;
        foreach (Collider c in copy.GetComponentsInChildren<Collider>(true)) Destroy(c);
        copy.localScale = Vector3.one;
        return copy;
    }

    // Which way is "from the player to there", flat on the ground.
    Vector3 AwayFromPlayer(Vector3 spot)
    {
        Vector3 away = spot - playerCamera.transform.position;
        away.y = 0f;
        return away.sqrMagnitude > 0.001f ? away.normalized : Vector3.forward;
    }

    // The shovel goes into the earth, levers it out and throws it, "scoops" times.
    IEnumerator DigWithShovel(Vector3 spot, int scoops)
    {
        Transform shovel = ToolCopy("shovel");
        Vector3 away = AwayFromPlayer(spot);
        Quaternion facing = Quaternion.LookRotation(away);
        Vector3 blade = spot - away * 0.15f;
        for (int k = 0; k < scoops; k++)
        {
            FarmAudio.Play("dig", spot + Vector3.up * 0.3f, 1f, Random.Range(0.9f, 1.1f));
            for (float t = 0f; t < 0.55f; t += Time.deltaTime)
            {
                float push = Mathf.Clamp01(t / 0.2f);              // blade goes in
                float lever = Mathf.Clamp01((t - 0.2f) / 0.2f);    // handle pulled back, earth thrown
                if (shovel != null)
                {
                    shovel.position = blade + Vector3.up * (0.15f - 0.25f * push + 0.3f * lever);
                    shovel.rotation = facing * Quaternion.Euler(-20f - 40f * lever, 0f, 0f);
                }
                if (lever > 0f && lever < 0.15f && t - Time.deltaTime < 0.2f)
                    Burst(spot + Vector3.up * 0.15f, "FarmProps/Soil", 10, 0.06f, -away * 1.2f + Vector3.up * 2.2f, 0.8f);
                yield return null;
            }
        }
        if (shovel != null) Destroy(shovel.gameObject);
    }

    // The rake pulls across the bed towards you a few times.
    IEnumerator RakeBed(Vector3 spot)
    {
        Transform rake = ToolCopy("rake");
        Vector3 away = AwayFromPlayer(spot);
        Quaternion facing = Quaternion.LookRotation(away);
        for (int k = 0; k < 3; k++)
        {
            FarmAudio.Play("rustle", spot + Vector3.up * 0.2f, 0.9f, Random.Range(0.9f, 1.15f));
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                float pull = t / 0.35f;
                if (rake != null)
                {
                    rake.position = spot + away * Mathf.Lerp(0.55f, -0.25f, pull) + Vector3.up * (0.02f + 0.05f * Mathf.Sin(pull * Mathf.PI));
                    rake.rotation = facing * Quaternion.Euler(-40f, 0f, 0f);
                }
                yield return null;
            }
            Burst(spot - away * 0.2f, "CountryArt/GrassBlade", 4, 0.04f, Vector3.up * 1.5f - away * 0.6f, 0.6f);
        }
        if (rake != null) Destroy(rake.gameObject);
    }

    // The watering can tilts over the bed, water drops fall, it moves along the bed and back up.
    IEnumerator WaterBed(Vector3 soilMiddle, float bedLength)
    {
        Transform can = ToolCopy("can");
        MeshFilter shape = can != null ? can.GetComponentInChildren<MeshFilter>() : null;
        Vector3 away = AwayFromPlayer(soilMiddle);
        Vector3 along = Vector3.forward * (bedLength / 2f - 0.25f);
        FarmAudio.Play("water_pour", soilMiddle + Vector3.up * 0.4f, 0.9f);
        const float time = 1.4f;
        float nextDrop = 0f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float tilt = Mathf.SmoothStep(0f, 1f, Mathf.Min(t / 0.25f, (time - t) / 0.25f));
            Vector3 over = soilMiddle + Vector3.Lerp(-along, along, t / time) + Vector3.up * 0.5f - away * 0.25f;
            Vector3 tip = over;
            if (shape != null)
            {
                // Turn the can so its spout looks at the bed, then tip it forward.
                Bounds b = shape.sharedMesh.bounds;
                float spoutSide = Mathf.Sign(b.center.x);
                can.rotation = Quaternion.identity;
                can.position = over;
                Vector3 spout = shape.transform.TransformDirection(new Vector3(spoutSide, 0f, 0f));
                spout.y = 0f;
                can.rotation = Quaternion.FromToRotation(spout.normalized, away) * Quaternion.identity;
                can.rotation = Quaternion.AngleAxis(40f * tilt, Vector3.Cross(Vector3.up, away)) * can.rotation;
                tip = shape.transform.TransformPoint(new Vector3(b.center.x + spoutSide * b.extents.x, b.max.y - 0.02f, 0f));
            }
            if (tilt > 0.6f && t >= nextDrop)
            {
                nextDrop = t + 0.03f;
                Burst(tip, "FarmMaterials/Glass", 2, 0.025f, Vector3.down * 0.6f + away * 0.4f, 0.3f);
            }
            yield return null;
        }
        if (can != null) Destroy(can.gameObject);
    }

    // The hammer knocks a few times at the closest point of something being repaired.
    IEnumerator HammerKnocks(Vector3 point, int knocks, float every)
    {
        Transform hammer = ToolCopy("hammer");
        if (hammer == null) yield break;
        Transform grip = new GameObject("Hammer Grip").transform; // the end of the handle, where a hand would hold it
        hammer.SetParent(grip, false);
        hammer.localPosition = new Vector3(0.18f, 0f, 0f);
        hammer.localRotation = Quaternion.identity;
        Vector3 toThing = point - playerCamera.transform.position;
        toThing.y = 0f;
        Quaternion facing = Quaternion.LookRotation(toThing.sqrMagnitude > 0.001f ? toThing.normalized : Vector3.forward) * Quaternion.Euler(0f, -90f, 0f);
        grip.position = point - toThing.normalized * 0.32f + Vector3.up * 0.05f;
        for (float t = 0f; t < knocks * every; t += Time.deltaTime)
        {
            float cycle = (t % every) / every; // up, then down fast onto the thing
            float angle = cycle < 0.7f ? Mathf.Lerp(-5f, 55f, cycle / 0.7f) : Mathf.Lerp(55f, -5f, (cycle - 0.7f) / 0.3f);
            grip.rotation = facing * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        Destroy(grip.gameObject);
    }

    // ---------- flying bits ----------

    // A handful of little balls thrown out (earth, grain, water drops, grass). They fall and vanish.
    void Burst(Vector3 from, string material, int count, float size, Vector3 push, float spread)
    {
        StartCoroutine(BurstRoutine(from, material, count, size, push, spread));
    }

    IEnumerator BurstRoutine(Vector3 from, string material, int count, float size, Vector3 push, float spread)
    {
        Material look = Resources.Load<Material>(material);
        var bits = new List<Transform>();
        var speed = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            Transform bit = VrShapes.Create(PrimitiveType.Sphere, "Bit", null, "Paper").transform;
            if (look != null) bit.GetComponent<Renderer>().sharedMaterial = look;
            bit.position = from + Random.insideUnitSphere * size;
            bit.localScale = Vector3.one * size * Random.Range(0.6f, 1.2f);
            bits.Add(bit);
            speed.Add(push + Random.insideUnitSphere * spread);
        }
        float ground = 0.01f;
        for (float t = 0f; t < 0.9f; t += Time.deltaTime)
        {
            for (int i = 0; i < bits.Count; i++)
            {
                speed[i] += Vector3.down * 9.8f * Time.deltaTime;
                Vector3 p = bits[i].position + speed[i] * Time.deltaTime;
                if (p.y < ground) { p.y = ground; speed[i] = Vector3.zero; }
                bits[i].position = p;
                if (t > 0.6f) bits[i].localScale *= 0.9f;
            }
            yield return null;
        }
        foreach (Transform bit in bits) Destroy(bit.gameObject);
    }

    // A small cloud of dust that puffs up and fades (when a part of a building lands).
    IEnumerator Puff(Vector3 at, float size)
    {
        Material dust = VrShapes.LoadMaterial("Paper");
        var bits = new List<Transform>();
        var dirs = new List<Vector3>();
        for (int i = 0; i < 6; i++)
        {
            Transform bit = VrShapes.Create(PrimitiveType.Sphere, "Dust", null, "Paper").transform;
            if (dust != null) bit.GetComponent<Renderer>().sharedMaterial = dust;
            Vector3 dir = Quaternion.Euler(0f, i * 60f + Random.Range(-15f, 15f), 0f) * Vector3.forward;
            bits.Add(bit);
            dirs.Add(dir);
        }
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            float k = t / 0.6f;
            for (int i = 0; i < bits.Count; i++)
            {
                bits[i].position = at + dirs[i] * size * (0.3f + 1.2f * k) + Vector3.up * size * 0.6f * k;
                bits[i].localScale = Vector3.one * size * 0.5f * Mathf.Sin(Mathf.PI * k);
            }
            yield return null;
        }
        foreach (Transform bit in bits) Destroy(bit.gameObject);
    }

    // Little red hearts float up and fade (when you pet an animal).
    void Hearts(Vector3 at)
    {
        StartCoroutine(HeartsRoutine(at));
    }

    IEnumerator HeartsRoutine(Vector3 at)
    {
        var hearts = new List<Transform>();
        for (int i = 0; i < 4; i++)
        {
            Transform heart = new GameObject("Heart").transform;
            foreach (float side in new[] { -1f, 1f }) // two balls and a point make a heart
            {
                Transform lobe = VrShapes.Create(PrimitiveType.Sphere, "Lobe", heart, "ToolboxRed").transform;
                lobe.localPosition = new Vector3(side * 0.025f, 0.02f, 0f);
                lobe.localScale = Vector3.one * 0.055f;
            }
            Transform tip = VrShapes.Create(PrimitiveType.Cube, "Tip", heart, "ToolboxRed").transform;
            tip.localPosition = new Vector3(0f, -0.005f, 0f);
            tip.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tip.localScale = Vector3.one * 0.045f;
            heart.position = at + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0f, 0.15f), Random.Range(-0.2f, 0.2f));
            hearts.Add(heart);
        }
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            for (int i = 0; i < hearts.Count; i++)
            {
                hearts[i].position += Vector3.up * Time.deltaTime * (0.35f + i * 0.05f);
                hearts[i].rotation = Quaternion.LookRotation(hearts[i].position - playerCamera.transform.position);
                hearts[i].localScale = Vector3.one * Mathf.Sin(Mathf.Clamp01(t / 1.4f) * Mathf.PI) * 1.2f;
            }
            yield return null;
        }
        foreach (Transform heart in hearts) Destroy(heart.gameObject);
    }

    // ---------- things flying into your hand ----------

    Vector3 HandPosition()
    {
        if (aimSource != null && FarmInput.HeadsetActive) return aimSource.position;
        return playerCamera.transform.position + Vector3.down * 0.35f + playerCamera.transform.forward * 0.25f;
    }

    // It jumps up a little, flies to your hand in an arc, shrinks and disappears.
    IEnumerator FlyToHand(Transform thing, float seconds)
    {
        foreach (Collider c in thing.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        Vector3 start = thing.position;
        Vector3 size = thing.localScale;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = t / seconds;
            Vector3 hand = HandPosition();
            thing.position = Vector3.Lerp(start, hand, k * k) + Vector3.up * Mathf.Sin(Mathf.PI * k) * 0.35f;
            thing.localScale = size * Mathf.Lerp(1f, 0.3f, k);
            yield return null;
        }
        Destroy(thing.gameObject);
    }
}
