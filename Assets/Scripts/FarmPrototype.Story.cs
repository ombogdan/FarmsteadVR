using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stages 1-2 of the story (part of FarmPrototype, see FarmPrototype.cs):
// stage 1: the broken fence -> find hammer, nails and saw -> repair (+50) -> Boris starts talking;
// stage 2: repair the coop (-40) -> Zhora lands on the roof -> buy 2 hens from him (5 each).
public partial class FarmPrototype
{
    const int CoopPrice = 40;
    const int HenPrice = 5;
    static readonly string[] ToolIds = { "hammer", "nails", "saw" };

    // ---------- what the animals say ----------

    static readonly string[] GoatNoises = { "Ме-е.", "Ме-е-е?", "*жує траву*", "Ме." };
    static readonly string[] BorisStory =
    {
        "Ме-е... Тобто — нарешті! Я вже думав, ти ніколи не полагодиш цей паркан.",
        "Я Борис. Головний козел цієї ферми. Ну, і єдиний.",
        "Колись тут господарював дід Панас. Кури, корови, ярмарки щосуботи — ферма аж гула!",
        "Потім дід постарів і поїхав до міста. А ферма потроху розвалилась.",
        "Він казав: «Колись приїде хтось із нашої родини й поверне фермі життя». Схоже, це ти.",
        "Бачиш он той старий курник праворуч? Полагодь його — і в нас знову будуть яйця. Кажуть, 40 монет вистачить.",
    };
    static readonly string[] BorisLater =
    {
        "Маркіз? Хитрий кіт. Але яйця купує чесно. Майже.",
        "Вночі всі сплять. Крім мене: я думаю. Ме-е.",
        "Морква — найкраще, що є на світі. Після цілого паркану.",
    };
    static readonly string[] ZhoraIntro =
    {
        "Так-так-так! Хто це тут ремонтує МІЙ курник без мого наказу?",
        "Дозвольте представитись: Жора. Директор курника. Ну... майже директор: підлеглих поки немає.",
        "Курник без курей — це не курник, а просто будка з дахом!",
        "У мене є знайомі курочки — по 5 монет за кожну. У тебе якраз 10. Береш двох?",
    };
    static readonly string[] ZhoraThanks =
    {
        "Ко-ко-ко! Ряба й Біляночка — найкращі курочки району. Я сам вибирав!",
        "Тепер у мене є підлеглі. Тобто... подруги. Тобто... підлеглі подруги!",
        "Скоро будуть яйця. Кури несуться вдень, у гніздах у курнику. Директор слово дав!",
        "Тільки спершу — обід! Зерно в мішку біля курника, годівниця — перед входом. Ситі курочки несуться вдвічі частіше!",
    };
    static readonly string[] ZhoraLater =
    {
        "Ко-ко! Не заважай, у мене нарада з курочками.",
        "Яйця збирай щодня — гнізда біля задньої стіни курника!",
        "Борис каже, що він тут головний? Ха! Ко-ко-ко!",
    };
    static readonly string[] HenNoises = { "Ко-ко!", "Ко-ко-ко?", "*дзьобає зернятко*" };

    // ---------- the parts of the farm this story uses ----------

    Transform goat;
    Grazer goatGrazer;
    FarmInteractable goatAction;
    GameObject brokenFence, repairedFence;
    FarmInteractable fenceAction;
    GameObject bus;
    GameObject oldCoop, repairedCoop, coopGlow;
    FarmInteractable coopAction;
    Collider coopCollider;
    GameObject zhora;
    Grazer zhoraGrazer;
    FarmInteractable zhoraAction;
    readonly List<GameObject> hens = new List<GameObject>();
    readonly Dictionary<string, GameObject> items = new Dictionary<string, GameObject>();

    int hensBought;
    bool borisStoryStarted;
    float borisStoryAt;
    int goatNoise, borisTalk, zhoraTalk, henTalk;

    void FindStoryParts()
    {
        brokenFence = world.Find("Broken Fence").gameObject;
        repairedFence = world.Find("Repaired Fence").gameObject;
        fenceAction = brokenFence.GetComponent<FarmInteractable>();
        goat = world.Find("Goat Boris");
        goatGrazer = goat.GetComponent<Grazer>();
        goatAction = goat.GetComponent<FarmInteractable>();
        bus = Find("Bus");
        oldCoop = Find("Old Coop");
        repairedCoop = Find("Repaired Coop");
        Transform coop = world.Find("Chicken Coop");
        coopAction = coop.GetComponent<FarmInteractable>();
        coopCollider = coop.GetComponent<Collider>();
        coopGlow = coop.Find("Glow Ring").gameObject;
        zhora = Find("Zhora");
        zhoraGrazer = zhora.GetComponent<Grazer>();
        zhoraAction = zhora.GetComponent<FarmInteractable>();
        foreach (FarmInteractable thing in world.GetComponentsInChildren<FarmInteractable>(true))
        {
            if (thing.kind == FarmInteractable.Kind.Item)
            {
                items[thing.itemId] = thing.gameObject;
                itemLooks[thing.itemId] = thing.gameObject;
            }
            if (thing.kind == FarmInteractable.Kind.Hen) hens.Add(thing.gameObject);
        }
        SetToolGlow(false);
        coopGlow.SetActive(false);
    }

    GameObject Find(string name)
    {
        Transform found = world.Find(name);
        return found != null ? found.gameObject : null;
    }

    void UpdateStory()
    {
        // Coming close to the broken fence for the first time explains what to do.
        if (stage == Stage.Arrive && PlayerDistance(brokenFence.transform) < 5f)
            SeeBrokenFence();

        if (stage == Stage.MeetBoris && !borisStoryStarted && Time.time >= borisStoryAt)
            StartBorisStory();
    }

    void StartBusLeaving()
    {
        if (bus != null && bus.activeSelf) StartCoroutine(BusLeaves());
    }

    // The bus honks and drives away along the road.
    IEnumerator BusLeaves()
    {
        Transform b = bus.transform;
        FarmAudio.Play("bus_horn", b.position + Vector3.up * 1.5f, 1f, 1f, 4f);
        FarmAudio.PlayLoopOn("bus_engine", b, 0.9f, 4f);
        yield return new WaitForSeconds(1f);
        Vector3 start = b.position;
        Transform[] parts = b.GetComponentsInChildren<Transform>();
        var wheels = System.Array.FindAll(parts, part => part.name == "Wheel hub pivot");
        for (float t = 0f; t < 9f; t += Time.deltaTime)
        {
            float distance = 0.9f * t * t;
            b.position = start + b.forward * distance;
            for (int i = 0; i < wheels.Length; i++)
            {
                float angle = distance / 0.503f * Mathf.Rad2Deg;
                float side = Mathf.Sign(wheels[i].localPosition.x);
                wheels[i].localRotation = Quaternion.Euler(0f, side < 0f ? 180f : 0f, 0f) *
                    Quaternion.AngleAxis(angle * side, Vector3.right);
            }
            yield return null;
        }
        bus.SetActive(false);
    }

    // ---------- talking to the animals ----------

    void TalkToGoat()
    {
        if (stage < Stage.MeetBoris) // before the repair he is "just a goat"
            SpeakerSays(goat, GoatNoises[goatNoise++ % GoatNoises.Length], 3f);
        else if (stage == Stage.MeetBoris)
            StartBorisStory();
        else if (goatGrazer.Sleeping)
            SpeakerSays(goat, "Борис: Хр-р... Ме... хр-р-р...", 3f);
        else if (stage == Stage.RestoreCoop)
            SpeakerSays(goat, "Борис: Курник он там, праворуч. Полагодь його — і буде нам яєчня! Тобто... яйця.", 7f);
        else if (stage < Stage.WaitEggs)
            SpeakerSays(goat, "Борис: Цей Жора думає, що він тут головний. Ха! Головний тут я. Ме-е.", 7f);
        else if (stage == Stage.WaitEggs)
            SpeakerSays(goat, "Борис: Нагодуй курочок — зерно в мішку біля курника. А яйця шукай у гніздах біля задньої стіни.", 7f);
        else if (!BorisChores()) // stage 5: horseshoes, the secret, carrots (FarmPrototype.Chores.cs)
            SpeakerSays(goat, "Борис: " + BorisLater[borisTalk++ % BorisLater.Length], 7f);
    }

    void TalkToZhora()
    {
        if (stage == Stage.BuyHens) BuyHen();
        else if (zhoraGrazer.Sleeping) SpeakerSays(zhora.transform, "Жора: Хр-р... Ко... хр-р-р...", 3f);
        else SpeakerSays(zhora.transform, "Жора: " + ZhoraLater[zhoraTalk++ % ZhoraLater.Length], 5f);
    }

    void TalkToHen(FarmInteractable hen)
    {
        Grazer grazer = hen.GetComponent<Grazer>();
        string sound = grazer != null && grazer.Sleeping ? "Хр-р-р..." : HenNoises[henTalk++ % HenNoises.Length];
        SpeakerSays(hen.transform, hen.itemId + ": " + sound, 3f);
    }

    // ---------- stage 1: the fence ----------

    void UseFence()
    {
        if (repairing) return;
        if (stage == Stage.RepairFence)
        {
            StartCoroutine(RepairFence());
            return;
        }
        if (stage == Stage.Arrive)
        {
            SeeBrokenFence();
            return;
        }
        FarmAudio.Play2D("nope", 0.6f);
        Say("Ще бракує: " + MissingTools() + ".", 6f);
    }

    void SeeBrokenFence()
    {
        stage = Stage.FindTools;
        SetToolGlow(true);
        FarmAudio.Play2D("pop", 0.7f);
        if (HasAllTools()) // picked everything up before even seeing the fence
        {
            ToolsReady("Паркан розвалився, але в тебе вже є всі інструменти!");
            return;
        }
        Say("Паркан розвалився! Щоб його полагодити, потрібні молоток, цвяхи й пилка. "
            + "Пошукай їх навколо — біля них світяться маленькі помаранчеві кола.", 12f);
        SaveGame();
    }

    void PickUp(FarmInteractable item)
    {
        if (Has(item.itemId)) return;
        AddItem(item.itemId);
        FarmAudio.Play("pickup", item.transform.position + Vector3.up * 0.3f);
        Highlight(item, false);
        item.gameObject.SetActive(false);
        string name = ItemName(item.itemId);

        if (System.Array.IndexOf(ToolIds, item.itemId) < 0) // not a fence tool (the watering can)
        {
            Say(item.itemId == "can" ? "Лійка в тебе! Тепер можна поливати грядки." : name + " — у твоєму інвентарі.", 5f);
            SaveGame();
            return;
        }
        if (HasAllTools() && stage == Stage.FindTools)
        {
            ToolsReady(name + " у тебе!");
            return;
        }
        string button = FarmInput.HeadsetActive ? "Y" : "I";
        Say(name + " — у твоєму інвентарі (" + ToolCount() + "/3). " + button + " — відкрити інвентар.", 7f);
        SaveGame();
    }

    void ToolsReady(string start)
    {
        stage = Stage.RepairFence;
        fenceAction.SetLabel("полагодити паркан (молоток, цвяхи, пилка)");
        Say(start + " Усі інструменти зібрано — тепер до паркана!", 8f);
        SaveGame();
    }

    IEnumerator RepairFence()
    {
        repairing = true;
        Say("Стук-стук-стук! Паркан відновлюється…", 4f);
        yield return ShakeAway(brokenFence);
        yield return PopIn(repairedFence, 0.25f);

        FarmAudio.Play("success", repairedFence.transform.position + Vector3.up, 1f);
        FarmAudio.Play2D("coin", 0.8f);
        coins += 50;
        Say("Паркан як новий! +50 монет.", 4f);
        repairing = false;

        // Boris walks up to the new fence and starts talking by himself.
        stage = Stage.MeetBoris;
        goatAction.SetLabel("поговорити з Борисом");
        goatGrazer.GoTo(FarmWorldBuilder.BrokenFenceCenter + new Vector3(0f, 0f, 1.4f));
        borisStoryStarted = false;
        borisStoryAt = Time.time + 3.5f;
        SaveGame();
    }

    void StartBorisStory()
    {
        if (borisStoryStarted) return;
        borisStoryStarted = true;
        StartDialogue(goat, "Борис", BorisStory, () =>
        {
            stage = Stage.RestoreCoop;
            ShowCoopOffer();
            Say("Нове завдання: відремонтуй старий курник праворуч у дворі (" + CoinsText(CoopPrice) + "). Біля нього світиться коло.", 8f);
            SaveGame();
        });
    }

    // ---------- stage 2: the coop, Zhora and the first hens ----------

    void ShowCoopOffer()
    {
        coopGlow.SetActive(true);
        coopAction.SetLabel("відремонтувати курник — " + CoinsText(CoopPrice));
        coopAction.alwaysHint = true; // a price: always shown
    }

    void UseCoop()
    {
        if (repairing) return;
        if (stage < Stage.RestoreCoop)
        {
            Say("Старий курник ледве тримається: дах провалився, дошки відпадають...", 5f);
            return;
        }
        if (coins < CoopPrice)
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("На ремонт курника потрібно " + CoinsText(CoopPrice) + ", а в тебе " + CoinsText(coins) + ".", 5f);
            return;
        }
        coins -= CoopPrice;
        FarmAudio.Play2D("coin", 0.6f);
        StartCoroutine(RepairCoop());
    }

    IEnumerator RepairCoop()
    {
        repairing = true;
        coopGlow.SetActive(false);
        coopAction.alwaysHint = false;
        Say("Стук-стук! Курник ремонтується… −" + CoinsText(CoopPrice) + ".", 5f);
        yield return ShakeAway(oldCoop);
        coopCollider.enabled = false; // now you can walk in through the doorway
        yield return PopIn(repairedCoop, 0.1f);
        FarmAudio.Play("success", FarmWorldBuilder.CoopPosition + Vector3.up * 1.5f, 1f);
        Say("Курник як новий! Туди можна зайти.", 3f);
        repairing = false;

        stage = Stage.MeetZhora;
        SaveGame();
        yield return new WaitForSeconds(1.2f);
        yield return ZhoraArrives();
        StartDialogue(zhora.transform, "Жора", ZhoraIntro, ZhoraIntroDone);
    }

    void ZhoraIntroDone()
    {
        stage = Stage.BuyHens;
        zhoraAction.SetLabel("купити курочку — " + CoinsText(HenPrice));
        zhoraAction.alwaysHint = true;
        string how = FarmInput.HeadsetActive ? "Наведи промінь на Жору і натисни курок" : "Наведи приціл на Жору і натисни E";
        Say(how + ", щоб купити курочку.", 7f);
        SaveGame();
    }

    // Zhora jumps from behind the coop onto its roof, crows, and hops down in front of it.
    IEnumerator ZhoraArrives()
    {
        Vector3 behind = FarmWorldBuilder.CoopPosition + new Vector3(0.6f, 0f, 2.7f);
        Vector3 roof = FarmWorldBuilder.CoopPosition + new Vector3(0f, FarmWorldBuilder.CoopRidge, 0f);
        Vector3 front = FarmWorldBuilder.CoopDoorOutside + new Vector3(-0.6f, 0f, 0.3f);
        Transform rooster = zhora.transform;
        zhora.SetActive(true);
        zhoraGrazer.enabled = false; // we move him ourselves for a moment
        rooster.position = behind;
        yield return Hop(rooster, behind, roof, 0.8f, 1f);
        FarmAudio.Play("rooster_crow", roof + Vector3.up * 0.5f, 1f, 1f, 8f);
        SpeakerSays(rooster, "Жора: КУКУРІКУ-У-У!", 2.5f);
        yield return new WaitForSeconds(1.8f);
        yield return Hop(rooster, roof, front, 0.7f, 0.5f);
        zhoraGrazer.enabled = true;
    }

    static IEnumerator Hop(Transform bird, Vector3 from, Vector3 to, float seconds, float height)
    {
        Vector3 flat = new Vector3(to.x - from.x, 0f, to.z - from.z);
        if (flat.sqrMagnitude > 0.001f) bird.rotation = Quaternion.LookRotation(-flat.normalized, Vector3.up); // beak forward
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = t / seconds;
            bird.position = Vector3.Lerp(from, to, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * height);
            yield return null;
        }
        bird.position = to;
    }

    void BuyHen()
    {
        if (hensBought >= 2)
        {
            SpeakerSays(zhora.transform, "Жора: Ко-ко! Більше курочок у мене немає.", 4f);
            return;
        }
        if (coins < HenPrice)
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Курочка коштує " + CoinsText(HenPrice) + ", а в тебе " + CoinsText(coins) + ".", 5f);
            return;
        }
        coins -= HenPrice;
        FarmAudio.Play2D("coin", 0.6f);
        int number = hensBought++;
        GameObject hen = hens[number];
        StartCoroutine(HenAppears(hen, zhora.transform.position + new Vector3(number == 0 ? 0.6f : -0.6f, 0f, -0.3f)));
        string name = hen.GetComponent<FarmInteractable>().itemId;
        if (hensBought < 2)
            SpeakerSays(zhora.transform, "Жора: Чудовий вибір! Це " + name + ". Ще одна — " + CoinsText(HenPrice) + ".", 6f);
        else
            StartCoroutine(AfterLastHen(name));
        SaveGame();
    }

    // A hen pops out with a little bounce.
    IEnumerator HenAppears(GameObject hen, Vector3 position)
    {
        Transform t = hen.transform;
        Vector3 size = t.localScale;
        t.position = position;
        t.localScale = Vector3.zero;
        hen.SetActive(true);
        FarmAudio.Play("pop", t.position + Vector3.up * 0.3f, 1f, 1.2f);
        FarmAudio.Play("pickup", t.position + Vector3.up * 0.3f, 0.7f);
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.45f)
        {
            t.localScale = size * EaseOutBack(k);
            yield return null;
        }
        t.localScale = size;
    }

    IEnumerator AfterLastHen(string name)
    {
        SpeakerSays(zhora.transform, "Жора: Це " + name + "!", 2f);
        yield return new WaitForSeconds(1.5f);
        zhoraAction.alwaysHint = false;
        zhoraAction.SetLabel("поговорити з Жорою");
        StartDialogue(zhora.transform, "Жора", ZhoraThanks, StartEggs); // -> stage 3 (FarmPrototype.Eggs.cs)
    }

    // ---------- repair animations (the fence and the coop) ----------

    // The old thing shakes with three hammer knocks, then disappears.
    IEnumerator ShakeAway(GameObject old)
    {
        Transform t = old.transform;
        Vector3 start = t.localPosition;
        const float shakeTime = 0.6f;
        float nextKnock = 0f;
        int knocks = 0;
        for (float time = 0f; time < shakeTime; time += Time.deltaTime)
        {
            if (time >= nextKnock && knocks < 3)
            {
                FarmAudio.Play(knocks % 2 == 0 ? "knock_1" : "knock_2", t.position + Vector3.up, 1f, Random.Range(0.95f, 1.05f));
                knocks++;
                nextKnock += 0.2f;
            }
            float strength = 1f - time / shakeTime;
            t.localPosition = start + new Vector3(Mathf.Sin(time * 70f) * 0.05f * strength, 0f, 0f);
            yield return null;
        }
        t.localPosition = start;
        old.SetActive(false);
    }

    // The new thing's parts pop up one after another, each with a "pop", a bit higher each time.
    IEnumerator PopIn(GameObject fresh, float delayBetweenParts)
    {
        Transform root = fresh.transform;
        int count = root.childCount;
        Vector3[] finalPosition = new Vector3[count];
        Vector3[] finalScale = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Transform part = root.GetChild(i);
            finalPosition[i] = part.localPosition;
            finalScale[i] = part.localScale;
            part.localScale = Vector3.zero;
        }
        fresh.SetActive(true);

        const float popTime = 0.45f;
        float total = delayBetweenParts * (count - 1) + popTime;
        bool[] popped = new bool[count];
        for (float t = 0f; t < total; t += Time.deltaTime)
        {
            for (int i = 0; i < count; i++)
            {
                if (!popped[i] && t >= i * delayBetweenParts)
                {
                    popped[i] = true;
                    if (i % 2 == 0) // not every part, so it doesn't turn into noise
                        FarmAudio.Play("pop", root.GetChild(i).position, 0.8f, 0.9f + 0.4f * i / Mathf.Max(1, count - 1));
                }
                float pop = EaseOutBack(Mathf.Clamp01((t - i * delayBetweenParts) / popTime));
                Transform part = root.GetChild(i);
                part.localScale = finalScale[i] * pop;
                part.localPosition = finalPosition[i] + Vector3.down * (1f - Mathf.Min(pop, 1f)) * 0.8f;
            }
            yield return null;
        }
        for (int i = 0; i < count; i++)
        {
            root.GetChild(i).localScale = finalScale[i];
            root.GetChild(i).localPosition = finalPosition[i];
        }
    }

    // 0 -> 1 curve that overshoots a bit near the end (a "bouncy" pop).
    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = x - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    // ---------- tools ----------

    void SetToolGlow(bool on)
    {
        foreach (string id in ToolIds) // only the fence tools (the watering can has its own time)
        {
            if (!items.TryGetValue(id, out GameObject item)) continue;
            Transform ring = item.transform.Find("Glow Ring");
            if (ring != null) ring.gameObject.SetActive(on);
        }
    }

    bool HasAllTools()
    {
        return ToolCount() == ToolIds.Length;
    }

    int ToolCount()
    {
        int count = 0;
        foreach (string id in ToolIds)
            if (Has(id)) count++;
        return count;
    }

    string MissingTools()
    {
        List<string> missing = new List<string>();
        foreach (string id in ToolIds)
            if (!Has(id)) missing.Add(ItemName(id).ToLower());
        return string.Join(", ", missing);
    }

    // ---------- loading a saved game ----------

    void ApplyStoryState()
    {
        if (stage > Stage.Arrive && bus != null) bus.SetActive(false);
        foreach (string id in ToolIds)
            if (Has(id) && items.ContainsKey(id)) items[id].SetActive(false);
        SetToolGlow(stage == Stage.FindTools);
        if (stage == Stage.RepairFence) fenceAction.SetLabel("полагодити паркан (молоток, цвяхи, пилка)");
        if (stage > Stage.RepairFence)
        {
            brokenFence.SetActive(false);
            repairedFence.SetActive(true);
            goatAction.SetLabel("поговорити з Борисом");
        }
        if (stage == Stage.MeetBoris)
        {
            borisStoryStarted = false;
            borisStoryAt = Time.time + 3f;
        }
        if (stage == Stage.RestoreCoop) ShowCoopOffer();
        if (stage > Stage.RestoreCoop)
        {
            oldCoop.SetActive(false);
            repairedCoop.SetActive(true);
            coopCollider.enabled = false;
        }
        if (stage >= Stage.MeetZhora)
        {
            zhora.SetActive(true);
            zhora.transform.position = FarmWorldBuilder.CoopDoorOutside + new Vector3(-0.6f, 0f, 0.3f);
        }
        if (stage == Stage.BuyHens)
        {
            zhoraAction.SetLabel("купити курочку — " + CoinsText(HenPrice));
            zhoraAction.alwaysHint = true;
        }
        for (int i = 0; i < hensBought; i++)
        {
            hens[i].SetActive(true);
            hens[i].transform.position = FarmWorldBuilder.CoopDoorOutside + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-1.5f, 0f));
        }
    }

    // Conversations that were cut off when the game was closed start again.
    void StoryAfterLoad()
    {
        if (stage == Stage.MeetZhora) StartCoroutine(ResumeZhoraIntro());
    }

    IEnumerator ResumeZhoraIntro()
    {
        yield return new WaitForSeconds(2f);
        StartDialogue(zhora.transform, "Жора", ZhoraIntro, ZhoraIntroDone);
    }
}
