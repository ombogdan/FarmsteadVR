using System.Collections;
using UnityEngine;

// Stage 5 (part of FarmPrototype, see FarmPrototype.cs): things to do while the hens lay eggs.
// - Every day: feed the hens (grain sack -> feeder by the coop). Fed hens lay twice as often that day.
// - Boris asks you to find grandpa Panas's 5 lucky horseshoes (+2 coins each).
// - For them he tells a secret: a chest buried by the house. Dig it up with a shovel (Marquis sells one).
// - In the chest: coins, carrot seeds and grandpa's letter. Then the garden: clear the beds with a rake,
//   plant, water (the watering can is by the barrel), wait, harvest. Treat Boris to a carrot.
// - Last: repair grandpa's house with planks and paint. The end of episode 1 (the fair comes next).
// Eggs and coins from Marquis pay for the shovel, the rake, the seeds, the planks and the paint.
// The things themselves are built by FarmWorldBuilder.Chores.cs.
public partial class FarmPrototype
{
    const int HorseshoeReward = 2;
    const int ChestCoins = 20;
    const int HouseReward = 30;
    const float GrowSeconds = 75f;     // from watering to a ripe carrot
    const float LidOpenAngle = 100f;

    enum Bed { Weeds, Clear, Planted, Growing, Ripe }

    // Where each horseshoe hides (the same order as in FarmWorldBuilder.Chores.cs).
    static readonly string[] HorseshoeHints =
    {
        "біля автобусної зупинки", "у лісі, біля пенька", "за великою стодолою",
        "над дверима старого будинку", "у курнику, над гніздами",
    };

    static readonly string[] BorisSecretLines =
    {
        "Ме-е! Усі п'ять? Оце так шукач! Дід Панас би пишався.",
        "Одну повішу над курником — на щастя. Хай Жора не зазнається.",
        "А тепер таємниця. Тільки нікому! Особливо Маркізу.",
        "Дід закопав скриню на своєму городі, біля хати. Там, де горбочок землі, праворуч від грядок.",
        "Лопату продає Маркіз. Копай — і побачиш. Ме-е!",
    };
    static readonly string[] GrandpaLetter =
    {
        "«Привіт! Якщо ти читаєш цей лист — значить, Борис тобі довіряє. А він довіряє не кожному!»",
        "«Тут трохи монет і насіння моркви. Найсолодша морква в окрузі росла саме на цих грядках.»",
        "«Прополи грядки граблями, посади, полий — і побачиш. Борис, до речі, моркву просто обожнює.»",
        "«Бережи ферму. Обіймаю, твій дід Панас.»",
    };
    static readonly string[] BorisCarrot =
    {
        "Хрум! Хрум-хрум... Ме-е-е! Оце морква! Точнісінько як у діда Панаса!",
        "Тепер я точно знаю: ти справжній фермер.",
        "Залишилось одне: дідова хата. Вікна забиті, фарба облізла... Сумно.",
        "Купи в Маркіза дошки й фарбу — і повернемо їй життя!",
    };
    static readonly string[] BorisYum =
    {
        "Хрум-хрум! Ме-е-е, смакота!",
        "Хрум! Ще б одну... Ну гаразд, потім.",
        "Морква! Найкращий день у моєму житті. Знову.",
    };
    static readonly string[] BorisFinale =
    {
        "Ме-е-е! Оце так! Хата діда Панаса знову як лялечка!",
        "Паркан цілий, курник повний, город росте... Ферма «Сидорівка» ожила!",
        "Знаєш, дід Панас щосуботи їздив на ярмарок у село. Перегони, змагання, тир...",
        "А Жора вже тренує курочок до якогось «польоту». Мені аж страшно. Ме.",
        "Але це вже зовсім інша історія. До зустрічі на ярмарку!",
    };

    // ---------- the parts of the farm this stage uses ----------

    GameObject grainSack, sackGlow, feeder, feederGrain, feederGlow;
    FarmInteractable[] horseshoes;
    GameObject luckyHorseshoe;
    GameObject mound, moundGlow;
    FarmInteractable moundAction;
    GameObject chest, chestGlow, chestTreasure;
    Transform chestLid;
    Transform[] bedRoots;
    FarmInteractable[] bedActions;
    Renderer[] bedSoil;
    GameObject[] bedWeeds;
    Transform[] bedPlants;
    Material soilDry, soilWet, freshPlaster;
    GameObject houseBoards, houseRepairs, houseGlow;
    FarmInteractable houseAction;

    int fedDay;           // the day the hens were last fed
    int horseshoesFound;  // one bit per horseshoe
    readonly int[] bedState = new int[FarmWorldBuilder.BedCount];
    readonly float[] bedGrowth = new float[FarmWorldBuilder.BedCount];
    int labelsKey = int.MinValue;

    bool FedToday => dayNight != null && fedDay == dayNight.day;

    void FindChoreParts()
    {
        grainSack = Find("Grain Sack");
        sackGlow = grainSack.transform.Find("Glow Ring").gameObject;
        itemLooks["grain"] = grainSack;
        feeder = Find("Feeder");
        feederGrain = feeder.transform.Find("Feeder Grain").gameObject;
        feederGlow = feeder.transform.Find("Glow Ring").gameObject;

        horseshoes = new FarmInteractable[FarmWorldBuilder.HorseshoeCount];
        for (int i = 0; i < horseshoes.Length; i++)
            horseshoes[i] = world.Find("Horseshoe " + i).GetComponent<FarmInteractable>();
        itemLooks["horseshoe"] = horseshoes[0].gameObject;
        luckyHorseshoe = Find("Lucky Horseshoe");

        mound = Find("Garden Mound");
        moundAction = mound.GetComponent<FarmInteractable>();
        moundGlow = mound.transform.Find("Glow Ring").gameObject;
        chest = Find("Buried Chest");
        chestLid = chest.transform.Find("Lid");
        chestGlow = chest.transform.Find("Glow Ring").gameObject;
        chestTreasure = chest.transform.Find("Treasure").gameObject;

        int beds = FarmWorldBuilder.BedCount;
        bedRoots = new Transform[beds];
        bedActions = new FarmInteractable[beds];
        bedSoil = new Renderer[beds];
        bedWeeds = new GameObject[beds];
        bedPlants = new Transform[beds];
        for (int i = 0; i < beds; i++)
        {
            Transform bed = world.Find("Garden Bed " + i);
            bedRoots[i] = bed;
            bedActions[i] = bed.GetComponent<FarmInteractable>();
            bedSoil[i] = bed.Find("Soil").GetComponent<Renderer>();
            bedWeeds[i] = bed.Find("Weeds").gameObject;
            bedPlants[i] = bed.Find("Plants");
        }
        soilDry = Resources.Load<Material>("FarmProps/Soil");
        soilWet = Resources.Load<Material>("FarmProps/WetSoil");
        freshPlaster = Resources.Load<Material>("FarmProps/FreshLime");
        itemLooks["carrot"] = Find("Carrot Look");

        houseBoards = Find("House Boards");
        houseRepairs = Find("House Repairs");
        Transform house = world.Find("Farmhouse");
        houseAction = house.GetComponent<FarmInteractable>();
        houseGlow = house.Find("Glow Ring").gameObject;
    }

    // ---------- the task text ----------

    // Changes whenever something the stage-5 texts and hints depend on changes.
    int ChoreKey()
    {
        int key = inventoryVersion;
        key = key * 31 + horseshoesFound;
        key = key * 31 + (FedToday ? 1 : 0);
        for (int i = 0; i < bedState.Length; i++) key = key * 7 + bedState[i];
        return key;
    }

    string ChoreQuestText()
    {
        switch (stage)
        {
            case Stage.Trading:
                return "Продавай яйця Маркізу (по " + CoinsText(EggPrice) + "). А ще Борис хоче тобі щось розповісти — поговори з ним.";
            case Stage.Horseshoes:
                return "Знайди 5 підков діда Панаса (" + FoundHorseshoes() + "/5). Біля них світяться маленькі кола. Де шукати: " + MissingHorseshoes() + ".";
            case Stage.BorisSecret:
                return "Усі 5 підков знайдено! Віднеси їх Борису.";
            case Stage.DigChest:
                return (Has("shovel") ? "Розкопай лопатою" : "Купи лопату в Маркіза (" + CoinsText(Prices["shovel"]) + ") і розкопай")
                    + " горбочок землі праворуч від грядок, біля хати діда Панаса.";
            case Stage.OpenChest:
                return "Відкрий скриню діда Панаса.";
            case Stage.Garden:
                return "Город діда Панаса — грядки перед хатою. " + GardenStep();
            case Stage.RepairHouse:
                return HouseText();
            case Stage.EpisodeDone:
                return "Перший епізод пройдено! Ярмарок — у наступному етапі гри. А поки: збирай яйця, вирощуй моркву й продавай Маркізу.";
            default:
                return "";
        }
    }

    // One thing to do every day, shown under the main task.
    string DailyChoreText()
    {
        if (hensBought == 0 || !dayNight.running || dayNight.IsNight || FedToday) return "";
        return "\nЩе сьогодні: нагодуй курочок (зерно в мішку біля курника).";
    }

    // The next step in the garden, so there is always one clear thing to do.
    string GardenStep()
    {
        if (Has("carrot")) return "У тебе є морква — пригости нею Бориса!";
        int weeds = CountBeds(Bed.Weeds), clear = CountBeds(Bed.Clear), planted = CountBeds(Bed.Planted);
        if (CountBeds(Bed.Ripe) > 0) return "Морква достигла — збери її і пригости Бориса!";
        if (planted > 0)
            return Has("can") ? "Полий посаджене з лійки." : "Візьми лійку біля бочки з водою (біля хати) і полий посаджене.";
        if (clear > 0 && Has("seeds")) return "Посади насіння моркви в прополоті грядки (насінин: " + ItemCount("seeds") + ").";
        if (weeds > 0 && Has("rake")) return "Прополи грядки граблями (готово " + (FarmWorldBuilder.BedCount - weeds) + "/" + FarmWorldBuilder.BedCount + ").";
        if (CountBeds(Bed.Growing) > 0) return "Морква росте. А поки — збирай яйця!";
        if (weeds > 0) return "Грядки заросли бур'янами. Купи граблі в Маркіза (" + CoinsText(Prices["rake"]) + ") і прополи їх.";
        return "Купи насіння моркви в Маркіза (" + SeedsInPack + " штуки — " + CoinsText(Prices["seeds"]) + ") і посади.";
    }

    string HouseText()
    {
        string need = "";
        if (!Has("planks")) need = "дошки (" + CoinsText(Prices["planks"]) + ")";
        if (!Has("paint")) need += (need.Length > 0 ? " і " : "") + "фарбу (" + CoinsText(Prices["paint"]) + ")";
        return need.Length == 0
            ? "Дошки й фарба є! Підійди до будинку діда Панаса і відремонтуй його."
            : "Відремонтуй будинок діда Панаса: купи в Маркіза " + need + ".";
    }

    // What the hints over things say, and which orange circles shine. Updated only when something changed.
    void RefreshChoreLabels()
    {
        bool night = dayNight.IsNight;
        int key = ChoreKey();
        key = key * 31 + (int)stage;
        key = key * 31 + hensBought;
        key = key * 31 + (night ? 1 : 0) + (dayNight.running ? 2 : 0);
        if (key == labelsKey) return;
        labelsKey = key;

        bool hungry = stage >= Stage.WaitEggs && hensBought > 0 && dayNight.running && !night && !FedToday;
        sackGlow.SetActive(hungry && !Has("grain"));
        feederGlow.SetActive(hungry && Has("grain"));
        if (stage > Stage.RepairFence)
            goatAction.SetLabel(stage >= Stage.Garden && Has("carrot") ? "пригостити Бориса морквою" : "поговорити з Борисом");
        moundAction.SetLabel(stage == Stage.DigChest && Has("shovel") ? "копати лопатою" : "оглянути горбочок землі");
        for (int i = 0; i < bedActions.Length; i++) bedActions[i].SetLabel(BedLabel(i));
        if (items.TryGetValue("can", out GameObject can))
        {
            Transform ring = can.transform.Find("Glow Ring");
            if (ring != null) ring.gameObject.SetActive(stage >= Stage.Garden);
        }
        houseAction.SetLabel(HouseLabel());
        houseGlow.SetActive(stage == Stage.RepairHouse && Has("planks") && Has("paint"));
        marquisAction.SetLabel(stage >= Stage.Trading ? "лавка Маркіза: купити" : "поговорити з Маркізом");
    }

    string BedLabel(int i)
    {
        if (stage < Stage.Garden) return "оглянути грядку";
        switch ((Bed)bedState[i])
        {
            case Bed.Weeds: return Has("rake") ? "прополоти граблями" : "прополоти (потрібні граблі)";
            case Bed.Clear: return Has("seeds") ? "посадити моркву" : "посадити (потрібне насіння)";
            case Bed.Planted: return Has("can") ? "полити з лійки" : "полити (потрібна лійка)";
            case Bed.Growing: return "подивитись, як росте морква";
            default: return "зібрати моркву";
        }
    }

    string HouseLabel()
    {
        if (stage >= Stage.EpisodeDone) return "поспати вдома";
        if (stage < Stage.RepairHouse) return "оглянути старий будинок";
        return Has("planks") && Has("paint") ? "відремонтувати будинок (дошки + фарба)" : "відремонтувати будинок (потрібні дошки й фарба)";
    }

    // A tip that is shown only the very first time.
    void SayOnce(string key, string text, float seconds)
    {
        if (usedHints.Contains(key)) return;
        usedHints.Add(key);
        Say(text, seconds);
    }

    // ---------- feeding the hens (every day) ----------

    void TakeGrain()
    {
        if (stage < Stage.WaitEggs)
        {
            Say("Мішок зерна. Ним годують курей — але курей тут поки немає.", 5f);
            return;
        }
        if (Has("grain"))
        {
            Say("Зерно вже в тебе. Насип його в годівницю перед курником.", 5f);
            return;
        }
        AddItem("grain");
        FarmAudio.Play("grain_pour", grainSack.transform.position + Vector3.up * 0.4f, 0.6f, 1.2f);
        Say(FedToday ? "Зерно в тебе. Сьогодні курочки вже ситі — насиплеш завтра." : "Зерно в тебе! Тепер насип його в годівницю перед курником.", 5f);
        SaveGame();
    }

    void FillFeeder()
    {
        if (stage < Stage.WaitEggs)
        {
            Say("Годівниця для курей. Поки що порожня.", 4f);
            return;
        }
        if (FedToday)
        {
            Say("Курочки сьогодні вже ситі. Завтра — знову!", 4f);
            return;
        }
        if (dayNight.IsNight)
        {
            Say("Курочки сплять. Нагодуєш їх зранку.", 4f);
            return;
        }
        if (!Has("grain"))
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Спершу набери зерна з мішка біля курника.", 5f);
            return;
        }
        RemoveItem("grain", 1);
        fedDay = dayNight.day;
        feederGrain.SetActive(true);
        FarmAudio.Play("grain_pour", feeder.transform.position + Vector3.up * 0.3f, 0.9f);
        for (int k = 0; k < 6; k++) // grain pours along the trough
            Burst(feeder.transform.position + new Vector3(-0.45f + k * 0.18f, 0.75f, 0f), "FarmProps/Grain", 5, 0.02f, Vector3.down * 0.5f, 0.25f);
        CallHensToFeeder();
        for (int i = 0; i < hensBought; i++) henNextLay[i] = Mathf.Min(henNextLay[i], Time.time + Random.Range(6f, 16f));
        if (!firstEggLaid) firstEggAt = Mathf.Min(firstEggAt, Time.time + 7f);
        if (zhora.activeSelf)
            SpeakerSays(zhora.transform, "Жора: Ко-ко-ко! Обід! Ситі курочки сьогодні несуться вдвічі частіше!", 5f);
        else
            Say("Курочки ситі! Сьогодні вони несуться вдвічі частіше.", 5f);
        SaveGame();
    }

    // The hens run to the feeder and peck there for a while.
    void CallHensToFeeder()
    {
        Vector3 feederSpot = FarmWorldBuilder.FeederPosition;
        for (int i = 0; i < hensBought && i < henGrazers.Count; i++)
        {
            float x = -0.45f + (i % 3) * 0.45f;
            float z = i < 3 ? -0.42f : 0.42f; // three in front, the rest behind
            Vector3 seat = feederSpot + new Vector3(x, 0f, z);
            henGrazers[i].EatAt(Random.Range(9f, 15f), FarmWorldBuilder.AroundCoop(henGrazers[i].transform.position, seat));
        }
    }

    void ChoresOnMorning()
    {
        feederGrain.SetActive(false); // eaten: today the hens are hungry again
    }

    // ---------- Boris: horseshoes, the secret, carrots ----------

    // Returns false when Boris has nothing special to say at this stage.
    bool BorisChores()
    {
        if (stage >= Stage.Garden && Has("carrot"))
        {
            FeedBorisCarrot();
            return true;
        }
        switch (stage)
        {
            case Stage.Trading:
                StartHorseshoeQuest();
                return true;
            case Stage.Horseshoes:
                SpeakerSays(goat, "Борис: Знайдено " + FoundHorseshoes() + " з 5. Шукай ще! Підказки — у завданнях.", 6f);
                return true;
            case Stage.BorisSecret:
                StartBorisSecret();
                return true;
            case Stage.DigChest:
            case Stage.OpenChest:
                SpeakerSays(goat, "Борис: Горбочок землі — праворуч від грядок, біля хати діда. Копай! Ме-е.", 6f);
                return true;
            case Stage.Garden:
                SpeakerSays(goat, "Борис: Морква... Я мрію про моркву. Посади її, га? Ме-е.", 5f);
                return true;
            case Stage.RepairHouse:
                SpeakerSays(goat, "Борис: Дошки й фарба — у Маркіза. Хата діда Панаса чекає!", 5f);
                return true;
            default:
                return false;
        }
    }

    void StartHorseshoeQuest()
    {
        string button = FarmInput.HeadsetActive ? "X" : "Tab";
        string[] lines =
        {
            "Ме-е! Ось і ти. Слухай, є справа. Секретна.",
            "Дід Панас вірив, що підкова приносить щастя. І ховав їх по всій фермі — аж п'ять штук!",
            "Знайди всі п'ять — і я відкрию тобі дідову таємницю. Велику таємницю. Ме.",
            "Де шукати — дивись у завданнях (" + button + "). Біля підков світяться маленькі кола.",
        };
        StartDialogue(goat, "Борис", lines, () =>
        {
            stage = Stage.Horseshoes;
            ShowHorseshoes();
            Say("Нове завдання: знайди 5 підков діда Панаса. Підказки — у завданнях (" + button + ").", 8f);
            SaveGame();
        });
    }

    bool FoundHorseshoe(int i) => (horseshoesFound & (1 << i)) != 0;

    int FoundHorseshoes()
    {
        int count = 0;
        for (int i = 0; i < FarmWorldBuilder.HorseshoeCount; i++)
            if (FoundHorseshoe(i)) count++;
        return count;
    }

    string MissingHorseshoes()
    {
        System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();
        for (int i = 0; i < HorseshoeHints.Length; i++)
            if (!FoundHorseshoe(i)) missing.Add(HorseshoeHints[i]);
        return string.Join("; ", missing);
    }

    void ShowHorseshoes()
    {
        for (int i = 0; i < horseshoes.Length; i++)
        {
            bool show = stage == Stage.Horseshoes && !FoundHorseshoe(i);
            horseshoes[i].gameObject.SetActive(show);
            if (show) horseshoes[i].transform.Find("Glow Ring").gameObject.SetActive(true);
        }
    }

    void PickHorseshoe(FarmInteractable shoe)
    {
        int i = int.Parse(shoe.itemId);
        if (FoundHorseshoe(i)) return;
        horseshoesFound |= 1 << i;
        AddItem("horseshoe");
        coins += HorseshoeReward;
        FarmAudio.Play("pickup", shoe.transform.position + Vector3.up * 0.2f);
        FarmAudio.Play2D("coin", 0.6f);
        Highlight(shoe, false);
        shoe.gameObject.SetActive(false);
        int found = FoundHorseshoes();
        if (found >= FarmWorldBuilder.HorseshoeCount)
        {
            stage = Stage.BorisSecret;
            Say("Усі п'ять підков діда Панаса! +" + CoinsText(HorseshoeReward) + ". Віднеси їх Борису.", 8f);
        }
        else Say("Підкова діда Панаса! (" + found + "/5) +" + CoinsText(HorseshoeReward) + ".", 5f);
        SaveGame();
    }

    void StartBorisSecret()
    {
        StartDialogue(goat, "Борис", BorisSecretLines, () =>
        {
            RemoveItem("horseshoe", ItemCount("horseshoe"));
            luckyHorseshoe.SetActive(true); // over the coop door, for luck
            stage = Stage.DigChest;
            moundGlow.SetActive(true);
            Say(Has("shovel")
                ? "Нове завдання: розкопай горбочок землі біля хати діда Панаса."
                : "Нове завдання: купи лопату в Маркіза і розкопай горбочок землі біля хати діда Панаса.", 8f);
            SaveGame();
        });
    }

    void FeedBorisCarrot()
    {
        RemoveItem("carrot", 1);
        FarmAudio.Play("crunch", goat.position + Vector3.up * 0.6f, 1f, 1f, 3f);
        if (stage == Stage.Garden)
        {
            stage = Stage.RepairHouse; // right away, so the carrot is never eaten twice for this
            SaveGame();
            StartDialogue(goat, "Борис", BorisCarrot, () =>
                Say("Нове завдання: відремонтуй будинок діда Панаса. Потрібні дошки й фарба — їх продає Маркіз.", 8f));
            return;
        }
        SpeakerSays(goat, "Борис: " + BorisYum[borisTalk++ % BorisYum.Length], 4f);
        SaveGame();
    }

    // Boris trots over to you (for the big news at the end).
    IEnumerator BorisComesOver()
    {
        Vector3 player = playerCamera.transform.position;
        Vector3 away = goat.position - player;
        away.y = 0f;
        Vector3 target = new Vector3(player.x, goat.position.y, player.z) + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward) * 1.7f;
        float normalSpeed = goatGrazer.speed;
        goatGrazer.speed = 1.3f;
        goatGrazer.GoTo(target);
        for (float t = 0f; t < 9f && PlayerDistance(goat) > 2.3f; t += Time.deltaTime) yield return null;
        goatGrazer.speed = normalSpeed;
    }

    // ---------- the buried chest ----------

    void DigMound()
    {
        if (repairing) return;
        if (stage < Stage.DigChest)
        {
            Say("Горбочок землі. Здається, тут колись щось закопали…", 5f);
            return;
        }
        if (!Has("shovel"))
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Тут треба копати. Лопату продає Маркіз — " + CoinsText(Prices["shovel"]) + ".", 6f);
            return;
        }
        StartCoroutine(DigUpChest());
    }

    // Three shovels of earth, the mound shrinks, and the chest comes up out of the ground.
    IEnumerator DigUpChest()
    {
        repairing = true;
        moundGlow.SetActive(false);
        Transform heap = mound.transform;
        Vector3 place = heap.localPosition;
        Vector3 size = heap.localScale;
        StartCoroutine(DigWithShovel(heap.position, 3)); // the shovel digs (3 scoops, 0.55 s each)...
        for (float t = 0f; t < 1.65f; t += Time.deltaTime)  // ...and the mound gets smaller
        {
            heap.localScale = new Vector3(size.x, size.y * (1f - 0.75f * t / 1.65f), size.z);
            yield return null;
        }
        heap.localPosition = place;
        heap.localScale = size;
        mound.SetActive(false);
        yield return RiseFromGround(chest.transform, 0.9f);
        FarmAudio.Play("success", chest.transform.position + Vector3.up * 0.5f, 1f);
        stage = Stage.OpenChest;
        chestGlow.SetActive(true);
        repairing = false;
        Say("Скриня діда Панаса! Відкрий її.", 5f);
        SaveGame();
    }

    static IEnumerator RiseFromGround(Transform thing, float seconds)
    {
        Vector3 end = thing.position;
        Vector3 start = end + Vector3.down * 0.55f;
        thing.position = start;
        thing.gameObject.SetActive(true);
        FarmAudio.Play("pop", end + Vector3.up * 0.3f, 0.8f, 0.8f);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            thing.position = Vector3.LerpUnclamped(start, end, EaseOutBack(t / seconds));
            yield return null;
        }
        thing.position = end;
    }

    void OpenChest()
    {
        if (repairing) return;
        if (stage != Stage.OpenChest)
        {
            Say("Скриня порожня: усе, що в ній було, вже в тебе.", 4f);
            return;
        }
        StartCoroutine(OpenChestRoutine());
    }

    IEnumerator OpenChestRoutine()
    {
        repairing = true;
        chestGlow.SetActive(false);
        FarmAudio.Play("knock_2", chestLid.position, 0.8f, 0.7f);
        for (float t = 0f; t < 0.7f; t += Time.deltaTime)
        {
            chestLid.localRotation = Quaternion.Euler(LidOpenAngle * EaseOutBack(t / 0.7f), 0f, 0f);
            yield return null;
        }
        chestLid.localRotation = Quaternion.Euler(LidOpenAngle, 0f, 0f);
        FarmAudio.Play("success", chest.transform.position + Vector3.up * 0.5f, 0.8f, 1.1f);
        FarmAudio.Play2D("coin", 0.8f);
        coins += ChestCoins;
        AddItem("seeds", SeedsInPack);
        stage = Stage.Garden; // the reward is saved right away, so it's never given twice
        SaveGame();
        Say("+" + CoinsText(ChestCoins) + " і " + SeedsInPack + " насінини моркви! А ще — лист від діда Панаса…", 4f);
        yield return new WaitForSeconds(1.4f);
        chestTreasure.SetActive(false);
        FarmAudio.Play("pickup", chest.transform.position + Vector3.up * 0.4f, 0.8f);
        repairing = false;
        if (!InDialogue)
            StartDialogue(chest.transform, "Лист діда Панаса", GrandpaLetter, () =>
                Say("Нове завдання: город діда Панаса — грядки перед хатою. Що робити — у завданнях.", 8f));
    }

    // ---------- the garden ----------

    int CountBeds(Bed state)
    {
        int count = 0;
        foreach (int s in bedState)
            if (s == (int)state) count++;
        return count;
    }

    void UseBed(FarmInteractable item)
    {
        if (repairing) return;
        int i = int.Parse(item.itemId);
        if (stage < Stage.Garden)
        {
            Say("Стара грядка діда Панаса, вся в бур'янах.", 4f);
            return;
        }
        Vector3 at = bedRoots[i].position + Vector3.up * 0.3f;
        switch ((Bed)bedState[i])
        {
            case Bed.Weeds:
                if (!Has("rake"))
                {
                    FarmAudio.Play2D("nope", 0.6f);
                    Say("Тут самі бур'яни! Прополоти їх можна граблями — Маркіз продає, " + CoinsText(Prices["rake"]) + ".", 6f);
                    return;
                }
                StartCoroutine(Weed(i));
                return;
            case Bed.Clear:
                if (!Has("seeds"))
                {
                    FarmAudio.Play2D("nope", 0.6f);
                    Say("Немає насіння. Маркіз продає " + SeedsInPack + " насінини моркви за " + CoinsText(Prices["seeds"]) + ".", 6f);
                    return;
                }
                RemoveItem("seeds", 1);
                bedState[i] = (int)Bed.Planted;
                FarmAudio.Play("dig", at, 0.5f, 1.3f);
                FarmAudio.Play("pop", at, 0.6f, 1.4f);
                foreach (Transform plant in bedPlants[i]) // a seed drops into each hole, a little earth jumps
                {
                    Vector3 hole = plant.position;
                    Burst(hole + Vector3.up * 0.35f, "CountryArt/Seed", 1, 0.025f, Vector3.down, 0.05f);
                    Burst(hole + Vector3.up * 0.02f, "FarmProps/Soil", 5, 0.03f, Vector3.up * 1.2f, 0.5f);
                }
                SayOnce("#planted", "Посаджено! Тепер полий грядку з лійки.", 5f);
                break;
            case Bed.Planted:
                if (!Has("can"))
                {
                    FarmAudio.Play2D("nope", 0.6f);
                    Say("Треба полити. Лійка стоїть біля бочки з водою, біля хати.", 5f);
                    return;
                }
                bedState[i] = (int)Bed.Growing;
                bedGrowth[i] = 0f;
                StartCoroutine(WaterBed(bedRoots[i].position + Vector3.up * 0.38f, 2.2f)); // the can pours along the bed
                SayOnce("#watered", "Полито! Морква росте — зазирни трохи згодом. А поки збирай яйця.", 6f);
                break;
            case Bed.Growing:
                Say("Морква росте… " + Mathf.RoundToInt(bedGrowth[i] * 100f) + "%", 3f);
                return;
            case Bed.Ripe:
                bedState[i] = (int)Bed.Clear;
                AddItem("carrot", FarmWorldBuilder.CarrotsPerBed);
                foreach (Transform plant in bedPlants[i]) // the carrots come out of the earth and fly into your hand
                {
                    Transform pulled = Instantiate(plant.gameObject, plant.position, plant.rotation).transform;
                    pulled.localScale = plant.lossyScale;
                    StartCoroutine(FlyToHand(pulled, 0.6f));
                }
                Burst(at + Vector3.up * 0.08f, "FarmProps/Soil", 8, 0.03f, Vector3.up * 1.6f, 0.6f);
                FarmAudio.Play("rustle", at, 0.8f, 1.2f);
                FarmAudio.Play("pickup", at, 0.8f);
                Say("+" + Plural(FarmWorldBuilder.CarrotsPerBed, "морквина", "морквини", "морквин") + "! "
                    + (stage == Stage.Garden ? "Пригости Бориса!" : "Маркіз купує по " + CoinsText(CarrotPrice) + "."), 5f);
                break;
        }
        ShowBed(i);
        SaveGame();
    }

    // The weeds shake, rustle and disappear.
    IEnumerator Weed(int i)
    {
        repairing = true;
        Transform weeds = bedWeeds[i].transform;
        StartCoroutine(RakeBed(bedRoots[i].position + Vector3.up * 0.38f)); // the rake pulls 3 times (~1 s)
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            weeds.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 40f) * 8f);
            yield return null;
        }
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            weeds.localScale = Vector3.one * (1f - t / 0.25f);
            yield return null;
        }
        weeds.localRotation = Quaternion.identity;
        weeds.localScale = Vector3.one;
        bedState[i] = (int)Bed.Clear;
        ShowBed(i);
        repairing = false;
        SayOnce("#weeded", "Прополото! Тепер можна садити моркву.", 4f);
        SaveGame();
    }

    void UpdateGarden()
    {
        if (stage < Stage.Garden) return;
        for (int i = 0; i < bedState.Length; i++)
        {
            if (bedState[i] != (int)Bed.Growing) continue;
            bedGrowth[i] += Time.deltaTime / GrowSeconds;
            if (bedGrowth[i] < 1f)
            {
                SetGrowth(i, bedGrowth[i]);
                continue;
            }
            bedGrowth[i] = 1f;
            bedState[i] = (int)Bed.Ripe;
            ShowBed(i);
            FarmAudio.Play("pop", bedRoots[i].position + Vector3.up * 0.3f, 0.7f, 1.2f);
            if (!MessageVisible) Say("Морква на грядці достигла! Збирай урожай.", 5f);
            SaveGame();
        }
    }

    // Weeds, sprouts or carrots, dry or wet soil - whatever the bed's state is.
    void ShowBed(int i)
    {
        Bed state = (Bed)bedState[i];
        bedWeeds[i].SetActive(state == Bed.Weeds);
        bool plants = state >= Bed.Planted;
        bedPlants[i].gameObject.SetActive(plants);
        Material soil = state == Bed.Growing ? soilWet : soilDry;
        if (soil != null) bedSoil[i].sharedMaterial = soil;
        if (plants) SetGrowth(i, state == Bed.Planted ? 0f : state == Bed.Ripe ? 1f : bedGrowth[i]);
    }

    // The green tops grow from tiny sprouts to full size; the orange root shows when it's ripe.
    void SetGrowth(int i, float growth)
    {
        float size = Mathf.Lerp(0.12f, 1f, growth) * 1.15f;
        foreach (Transform plant in bedPlants[i])
        {
            Transform leaves = plant.Find("Leaves");
            Transform root = plant.Find("Root");
            if (leaves != null) leaves.localScale = Vector3.one * size;
            if (root != null) root.gameObject.SetActive(growth >= 1f);
        }
    }

    // ---------- grandpa's house ----------

    void UseHouse()
    {
        if (repairing) return;
        if (stage >= Stage.EpisodeDone)
        {
            GoToSleep(); // the repaired house is your home now
            return;
        }
        if (stage < Stage.RepairHouse)
        {
            Say("Старий будинок діда Панаса: вікна забиті дошками, фарба облізла... Але заходь — усередині є ліжко, вночі тут можна поспати.", 6f);
            return;
        }
        if (!Has("planks") || !Has("paint"))
        {
            FarmAudio.Play2D("nope", 0.6f);
            string have = Has("planks") ? " Дошки вже є." : Has("paint") ? " Фарба вже є." : "";
            Say("Для ремонту потрібні дошки й фарба — їх продає Маркіз." + have, 6f);
            return;
        }
        RemoveItem("planks", 1);
        RemoveItem("paint", 1);
        StartCoroutine(RepairHouseRoutine());
    }

    IEnumerator RepairHouseRoutine()
    {
        repairing = true;
        houseGlow.SetActive(false);
        Say("Стук-стук! Шурх-шурх пензлем… Будинок оживає!", 5f);
        yield return ShakeAway(houseBoards);
        Vector3 front = FarmWorldBuilder.HousePosition + new Vector3(0f, 2.5f, -3.2f);
        FarmAudio.Play("whoosh", front, 1f, 0.8f, 6f);
        PaintHouse();
        yield return PopIn(houseRepairs, 0.18f);
        FarmAudio.Play("success", front, 1f);
        FarmAudio.Play2D("coin", 0.8f);
        coins += HouseReward;
        stage = Stage.EpisodeDone;
        repairing = false;
        Say("Будинок діда Панаса як новий! +" + CoinsText(HouseReward) + ". Тепер тут можна ночувати.", 6f);
        SaveGame();
        yield return new WaitForSeconds(1.5f);
        yield return BorisComesOver();
        if (!InDialogue)
            StartDialogue(goat, "Борис", BorisFinale, () =>
                Say("Кінець першого епізоду! Ярмарок — у наступному етапі гри. А поки — збирай яйця, вирощуй моркву й продавай Маркізу.", 10f));
    }

    // Fresh paint: the old cracked plaster of the house becomes a clean, light one.
    void PaintHouse()
    {
        Transform house = world.Find("Static/Old Farmhouse");
        if (house == null || freshPlaster == null) return;
        foreach (MeshRenderer r in house.GetComponentsInChildren<MeshRenderer>())
        {
            Material old = r.sharedMaterial;
            if (old != null && (old.name == "Lime" || old.name == "House")) r.sharedMaterial = freshPlaster;
        }
    }

    // ---------- loading a saved game ----------

    void ApplyChoreState(GameSave save)
    {
        fedDay = save.fedDay;
        horseshoesFound = save.horseshoes;
        for (int i = 0; i < bedState.Length; i++)
        {
            bedState[i] = save.beds != null && i < save.beds.Length ? Mathf.Clamp(save.beds[i], 0, (int)Bed.Ripe) : 0;
            bedGrowth[i] = save.bedGrowth != null && i < save.bedGrowth.Length ? save.bedGrowth[i] : 0f;
        }

        feederGrain.SetActive(FedToday);
        ShowHorseshoes();
        luckyHorseshoe.SetActive(stage > Stage.BorisSecret);
        mound.SetActive(stage <= Stage.DigChest);
        moundGlow.SetActive(stage == Stage.DigChest);
        chest.SetActive(stage >= Stage.OpenChest);
        chestGlow.SetActive(stage == Stage.OpenChest);
        chestTreasure.SetActive(stage <= Stage.OpenChest);
        chestLid.localRotation = stage > Stage.OpenChest ? Quaternion.Euler(LidOpenAngle, 0f, 0f) : Quaternion.identity;
        for (int i = 0; i < bedState.Length; i++) ShowBed(i);
        if (Has("can") && items.TryGetValue("can", out GameObject can)) can.SetActive(false);
        bool repaired = stage >= Stage.EpisodeDone;
        houseBoards.SetActive(!repaired);
        houseRepairs.SetActive(repaired);
        if (repaired) PaintHouse();
        labelsKey = int.MinValue; // hints and circles get refreshed on the next frame
    }
}
