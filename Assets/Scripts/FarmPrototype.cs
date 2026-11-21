using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The game rules. They live in several files that are all parts of this one class:
//   FarmPrototype.cs        - common things: aiming, using things, inventory, messages, conversations, saving
//   FarmPrototype.Story.cs  - stages 1-2: the fence, Boris, the coop, Zhora, the first hens
//   FarmPrototype.Eggs.cs   - stage 3: eggs, day and night, sleeping
//   FarmPrototype.Shop.cs   - stage 4: Marquis's shop
//   FarmPrototype.Chores.cs - stage 5: feeding the hens, the horseshoes, the chest, the garden, the house
// The whole idea is in docs/GAME_DESIGN.md. The farm itself is built by FarmWorldBuilder.
// VR helpers (hands, teleport, texts in the headset) are in the Vr*.cs files.
public partial class FarmPrototype : MonoBehaviour
{
    // Bump this number whenever FarmWorldBuilder changes. The editor sees that the saved scene
    // has an older version and rebuilds the farm by itself (no manual "Rebuild Scene" needed).
    public const int WorldVersion = 24;
    public const string WorldVersionMarker = "World Version ";

    public const int LastSaveStage = (int)Stage.RouteDone;

    enum Stage
    {
        Intro, Arrive, FindTools, RepairFence, MeetBoris,   // stage 1
        RestoreCoop, MeetZhora, BuyHens,                    // stage 2
        WaitEggs,                                           // stage 3
        MeetMarquis, Trading,                               // stage 4 (Trading: Boris wants a word)
        Horseshoes, BorisSecret, DigChest, OpenChest,       // stage 5
        Garden, RepairHouse, EpisodeDone,
        OpenBarn, BarnOpen,                                 // the barn, cows and goats (FarmPrototype.Barn.cs)
        RoutePacking, RouteReply, RouteDone,
        // New stages go only at the end: saved games remember the stage as a number.
    }

    static readonly Dictionary<string, string> ItemNames = new Dictionary<string, string>
    {
        { "hammer", "Молоток" }, { "nails", "Цвяхи" }, { "saw", "Пилка" }, { "egg", "Яйця" },
        { "planks", "Дошки" }, { "paint", "Фарба" }, { "shovel", "Лопата" }, { "rake", "Граблі" },
        { "grain", "Зерно" }, { "horseshoe", "Підкова" }, { "seeds", "Насіння моркви" },
        { "carrot", "Морква" }, { "can", "Лійка" }, { "barn_key", "Ключ від стодоли" },
        { "milk", "Молоко" }, { "goat_milk", "Козяче молоко" }, { "golden_egg", "Золоте яйце" },
        { "cheese", "Сир" }, { "goat_cheese", "Козячий сир" },
        { "omelet", "Омлет" }, { "carrot_pie", "Морквяний пиріг" }, { "syrnyky", "Сирники" }, { "goat_bake", "Козяча запіканка" },
    };

    Transform world;
    Camera playerCamera;
    DayNight dayNight;
    FarmInteractable aimedAt;
    FarmInteractable lastAimed; // to react only when the aim moves onto something new

    Stage stage;
    int coins;
    bool repairing; // true while a repair animation is playing

    // ---------- inventory: what you carry and how many ----------
    readonly List<string> inventory = new List<string>();
    readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    readonly Dictionary<string, GameObject> itemLooks = new Dictionary<string, GameObject>(); // id -> object with a "Model" child
    int inventoryVersion;

    // Hints over things are shown until you've used that action once (prices always).
    readonly HashSet<string> usedHints = new HashSet<string>();

    // ---------- messages, panels, conversations ----------
    string message = "";
    float messageUntil;
    Transform messageSpeaker; // who says the current message (null = the game itself)
    bool taskPanelOpen;       // J on Mac, B on the right controller
    bool inventoryOpen;       // I on the Mac, Y on the left controller

    string[] dialogue;        // the lines of the conversation going on right now
    int dialogueLine;
    string dialogueName;
    Transform dialogueSpeaker;
    System.Action dialogueDone;
    bool dialogueTaught;      // "(trigger — next)" is shown only in the very first conversation

    string questText = "";
    // Little "transitions": a banner when a new task starts, "+N" when coins come in (shown by VrHud / OnGUI).
    string bannerText = "", bannerTitle = "";
    float bannerAt = -10f, coinDeltaAt = -10f;
    int shownStage = -1, lastCoins = -1, coinDelta;
    int questKey = -1;
    float nextAutoSave;

    // What we aim with: the camera on the Mac, the right hand in VR (VrPlayer sets these).
    [System.NonSerialized] public Transform aimSource;
    [System.NonSerialized] public float aimDistance = 4f;
    bool aimHasHit;
    bool windowAim; // the laser is on the shop window
    Vector3 aimPoint;
    float aimHitDistance;

    void Awake()
    {
        if (transform.Find("Farm World") == null) BuildWorld();
        world = transform.Find("Farm World");
        playerCamera = GetComponentInChildren<Camera>();
        FindStoryParts();
        FindEggParts();
        FindShopParts();
        FindChoreParts();
        FindBarnParts();
        FindSlingshotParts();
        FindHayParts();
        FindCheeseParts();
        FindKitchenParts();
        FindRouteParts();
        dayNight = gameObject.AddComponent<DayNight>();
        dayNight.onMorning = OnMorning;
        dayNight.onEvening = OnEvening;
        stage = Stage.Intro;
        FarmInput.MovementLocked = true; // until the intro (or loading) ends
    }

    void Start()
    {
        // Wind everywhere, birds singing in the trees.
        List<Transform> trees = new List<Transform>();
        foreach (Transform t in world.GetComponentsInChildren<Transform>())
            if (t.name == "Tree" || t.name == "Pine") trees.Add(t);
        FarmAudio.StartAmbience(trees);

        dayNight.Init(world.Find("Warm Sun").GetComponent<Light>(), playerCamera);

        GameSave.TryLoad(out menuSave);
    }

    IEnumerator PlayIntro()
    {
        IntroSequence intro = gameObject.AddComponent<IntroSequence>();
        string next = FarmInput.HeadsetActive ? "курок — далі" : "E — далі";
        yield return intro.Run(this, playerCamera, next, introPage, RecordIntroPage);
        Destroy(intro);

        FarmInput.MovementLocked = false;
        stage = Stage.Arrive;
        StartBusLeaving();
        string buttons = FarmInput.HeadsetActive ? "X — меню, B — завдання, Y — інвентар" : "Tab — меню, J — завдання, I — інвентар";
        Say("Ось і ферма «Сидорівка»! Ходімо подивимось, що там коїться. (" + buttons + ")", 10f);
        SaveGame();
    }

    // A saved game: no intro, everything is put back the way it was.
    IEnumerator ContinueSavedGame(GameSave save)
    {
        ScreenFade fade = ScreenFade.On(playerCamera);
        fade.Set(1f);
        restoring = true;
        ApplySave(save);
        yield return null; // one frame, so the VR player is ready to be moved
        Vector3 dest = PlayerBody.ClampToFarm(save.feet);
        dest.y = 0f;
        Physics.SyncTransforms();
        hasReturnSpot = save.hasFeet && PlayerBody.CanStand(dest);
        returnSpot = hasReturnSpot ? dest : new Vector3(1.5f, 0f, -4.5f);
        returnYaw = hasReturnSpot ? save.yaw : 0f;
        PlacePlayer(returnSpot, returnYaw);
        yield return null;
        FarmInput.MovementLocked = false;
        restoring = false;
        yield return fade.FadeTo(0f, 0.8f);
        Say("З поверненням на ферму «Сидорівка»! День " + dayNight.day + ", " + dayNight.PartOfDay + ".", 6f);
        StoryAfterLoad();
    }

    // Puts the player somewhere else (the intro and loading use it).
    public void PlacePlayer(Vector3 feet, float yaw)
    {
        VrPlayer vr = playerCamera.GetComponent<VrPlayer>();
        if (vr != null && vr.IsReady)
        {
            vr.PlaceAt(feet, yaw);
            return;
        }
        DesktopPlayer desktop = playerCamera.GetComponent<DesktopPlayer>();
        if (desktop != null) desktop.PlaceAt(feet, yaw);
    }

    void Update()
    {
        if (AtMainMenu) { UpdateMainMenu(); return; }
        if (changingScene || restoring) return;
        HandlePanelButtons();
        if (changingScene) return;
        RefreshQuestText();
        WatchForNews();
        UpdateAim();
        if (stage == Stage.Intro) return;

        if (Time.time >= nextAutoSave)
        {
            nextAutoSave = Time.time + 10f;
            SaveGame();
        }
        UpdateEggs();   // hens keep laying even while you talk
        UpdateGarden(); // and carrots keep growing
        UpdateBarn();   // who can be milked
        UpdateCheese(); // the cheese house cooks
        UpdateKitchen(); // and so does the kitchen
        RefreshChoreLabels();
        UpdateSideTasks();
        UpdateEvents();

        // The "Курячий політ" game takes over the trigger while it's on.
        if (slingshotActive)
        {
            UpdateSlingshot();
            return;
        }
        if (raceActive)
        {
            UpdateRace();
            return;
        }
        if (galleryActive)
        {
            UpdateGallery();
            return;
        }
        if (rushActive)
        {
            UpdateEggRush();
            return;
        }

        // While somebody talks, the trigger (anywhere) shows the next sentence.
        if (InDialogue)
        {
            if (FarmInput.InteractPressed()) AdvanceDialogue();
            return;
        }

        UpdateStory();
        UpdateShop();

        if (FarmInput.InteractPressed() && aimedAt != null)
        {
            RememberFarmPosition();
            Interact(aimedAt);
        }
    }

    // A new stage -> "НОВЕ ЗАВДАННЯ" floats up in front of you. More coins -> "+N" jumps off the coin badge.
    void WatchForNews()
    {
        if (stage == Stage.Intro) return;
        if ((int)stage != shownStage)
        {
            if (shownStage >= 0 && questText.Length > 0)
            {
                int end = questText.IndexOfAny(new[] { '.', '!', ':' });
                bannerText = end > 20 && end < 110 ? questText.Substring(0, end + 1) : questText.Length > 110 ? questText.Substring(0, 110) + "…" : questText;
                bannerTitle = "НОВЕ ЗАВДАННЯ";
                bannerAt = Time.time;
                FarmAudio.Play2D("pickup", 0.45f, 0.8f);
            }
            shownStage = (int)stage;
        }
        if (coins != lastCoins)
        {
            if (lastCoins >= 0 && coins > lastCoins)
            {
                coinDelta = coins - lastCoins;
                coinDeltaAt = Time.time;
            }
            lastCoins = coins;
        }
    }

    // A banner floating up in front of you for a few seconds ("НОВЕ ЗАВДАННЯ", "ВИКОНАНО! +5").
    void ShowBanner(string title, string text)
    {
        bannerTitle = title;
        bannerText = text;
        bannerAt = Time.time;
    }

    void HandlePanelButtons()
    {
        if (FarmInput.MenuPressed())
        {
            ReturnToMenu();
            return;
        }
        if (FarmInput.TasksPressed() && !InMinigame)
        {
            taskPanelOpen = !taskPanelOpen;
            if (taskPanelOpen) inventoryOpen = false;
        }
        if (FarmInput.InventoryPressed())
        {
            inventoryOpen = !inventoryOpen;
            if (inventoryOpen) taskPanelOpen = false;
        }
    }

    void UpdateAim()
    {
        aimedAt = null;
        aimHasHit = false;
        windowAim = false;
        Transform source = aimSource != null ? aimSource : (playerCamera != null ? playerCamera.transform : null);
        aimedAtRush = null;
        if (slingshotActive || raceActive || galleryActive) return; // the fun games handle the trigger themselves
        if (source != null && shopWindow != null && shopWindow.IsOpen)
        {
            // The shop window is open: the laser only talks to it, nothing behind it gets in the way.
            windowAim = shopWindow.Aim(source.position, source.forward, out Vector3 onWindow);
            if (windowAim)
            {
                aimHasHit = true;
                aimPoint = onWindow;
                aimHitDistance = Vector3.Distance(source.position, onWindow);
            }
            Highlight(lastAimed, false);
            lastAimed = null;
            return;
        }
        RaycastHit hit;
        if (source != null && Physics.Raycast(source.position, source.forward, out hit, aimDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            aimHasHit = true;
            aimPoint = hit.point;
            aimHitDistance = hit.distance;
            aimedAt = hit.collider.GetComponentInParent<FarmInteractable>();
        }

        // During the intro, repairs and conversations nothing else can be used.
        if (repairing || stage == Stage.Intro || InDialogue) aimedAt = null;
        if (rushActive) aimedAtRush = aimedAt != null && aimedAt.name == "Rush Egg" ? aimedAt : null; // the egg rush game

        if (aimedAt != lastAimed)
        {
            Highlight(lastAimed, false); // the orange circle shines only while you point at it
            Highlight(aimedAt, true);
            if (aimedAt != null) FarmAudio.Play2D("tick", 0.4f);
        }
        lastAimed = aimedAt;
    }

    static void Highlight(FarmInteractable thing, bool on)
    {
        if (thing == null) return;
        PickupGlow glow = thing.GetComponentInChildren<PickupGlow>(true);
        if (glow != null) glow.highlighted = on;
    }

    void Interact(FarmInteractable item)
    {
        // Visible in Unity's Console, and on the Quest with: adb logcat -s Unity
        Debug.Log("[Farm] Interact: " + item.kind + " " + item.itemId + ", stage " + stage);
        usedHints.Add(item.label); // this hint won't show again (a new action = a new label = a new hint)
        switch (item.kind)
        {
            case FarmInteractable.Kind.Goat: TalkToGoat(); break;
            case FarmInteractable.Kind.Fence: UseFence(); break;
            case FarmInteractable.Kind.Item: PickUp(item); break;
            case FarmInteractable.Kind.Coop: UseCoop(); break;
            case FarmInteractable.Kind.Rooster: TalkToZhora(); break;
            case FarmInteractable.Kind.Hen: TalkToHen(item); break;
            case FarmInteractable.Kind.Egg: CollectEgg(item); break;
            case FarmInteractable.Kind.Sleep: GoToSleep(); break;
            case FarmInteractable.Kind.Cat: TalkToMarquis(); break;
            case FarmInteractable.Kind.ShopItem: OpenShop(false); break; // the goods on the counter: the BUY menu
            case FarmInteractable.Kind.SellBasket: OpenShop(true); break; // the basket: the SELL menu
            case FarmInteractable.Kind.GrainSack: TakeGrain(); break;
            case FarmInteractable.Kind.Storage: OpenStorage(); break;
            case FarmInteractable.Kind.Well: UseWell(); break;
            case FarmInteractable.Kind.CheesePress: UseCheesePress(); break;
            case FarmInteractable.Kind.Kitchen: UseKitchen(); break;
            case FarmInteractable.Kind.CoopDoor: UseCoopDoor(); break;
            case FarmInteractable.Kind.HayStack: TakeHay(); break;
            case FarmInteractable.Kind.Trough: FillTrough(); break;
            case FarmInteractable.Kind.Slingshot: UseSlingshot(); break;
            case FarmInteractable.Kind.Race: UseRaceSign(); break;
            case FarmInteractable.Kind.Gallery: UseGallery(); break;
            case FarmInteractable.Kind.RoutePost: UseRoutePost(); break;
            case FarmInteractable.Kind.EggRush: UseEggRush(); break;
            case FarmInteractable.Kind.Feeder: FillFeeder(); break;
            case FarmInteractable.Kind.Horseshoe: PickHorseshoe(item); break;
            case FarmInteractable.Kind.Mound: DigMound(); break;
            case FarmInteractable.Kind.Chest: OpenChest(); break;
            case FarmInteractable.Kind.Bed: UseBed(item); break;
            case FarmInteractable.Kind.House: UseHouse(); break;
            case FarmInteractable.Kind.Barn: UseBarn(); break;
            case FarmInteractable.Kind.Animal: TalkToAnimal(item); break;
        }
    }

    // ---------- inventory ----------

    void AddItem(string id, int amount = 1)
    {
        if (!counts.ContainsKey(id))
        {
            counts[id] = 0;
            inventory.Add(id);
        }
        counts[id] += amount;
        inventoryVersion++;
    }

    void RemoveItem(string id, int amount)
    {
        if (!counts.ContainsKey(id)) return;
        counts[id] -= amount;
        if (counts[id] <= 0)
        {
            counts.Remove(id);
            inventory.Remove(id);
        }
        inventoryVersion++;
    }

    bool Has(string id)
    {
        return counts.ContainsKey(id);
    }

    // ---------- conversations ----------

    bool InDialogue => dialogue != null;

    // The speaker stops and looks at you; each trigger press shows the next sentence.
    void StartDialogue(Transform speaker, string name, string[] lines, System.Action done)
    {
        dialogue = lines;
        dialogueLine = -1;
        dialogueName = name;
        dialogueSpeaker = speaker;
        dialogueDone = done;
        Grazer grazer = speaker.GetComponent<Grazer>();
        if (grazer != null) grazer.Face(playerCamera.transform);
        AdvanceDialogue();
    }

    void AdvanceDialogue()
    {
        if (dialogueLine >= 0)
        {
            dialogueTaught = true;
            FarmAudio.Play2D("tick", 0.3f);
        }
        dialogueLine++;
        if (dialogueLine >= dialogue.Length)
        {
            EndDialogue();
            return;
        }
        string text = dialogueName + ": " + dialogue[dialogueLine];
        if (!dialogueTaught) text += "\n\n(" + (FarmInput.HeadsetActive ? "курок" : "E") + " — далі)";
        message = text;
        messageSpeaker = dialogueSpeaker;
        messageUntil = float.MaxValue; // stays until you press for the next sentence
    }

    void EndDialogue()
    {
        Grazer grazer = dialogueSpeaker != null ? dialogueSpeaker.GetComponent<Grazer>() : null;
        if (grazer != null) grazer.StopFacing();
        System.Action done = dialogueDone;
        dialogue = null;
        dialogueSpeaker = null;
        dialogueDone = null;
        messageUntil = 0f;
        messageSpeaker = null;
        if (done != null) done();
    }

    // ---------- messages ----------

    void SpeakerSays(Transform speaker, string text, float seconds)
    {
        if (InDialogue) return; // never interrupt a conversation
        Say(text, seconds);
        messageSpeaker = speaker; // in VR the words appear in a bubble above the speaker
    }

    void Say(string text, float seconds = 8f)
    {
        if (InDialogue) return;
        message = text;
        messageSpeaker = null;
        messageUntil = Time.time + seconds;
    }

    // "1 монета", "2 монети", "5 монет" - Ukrainian needs three forms.
    static string Plural(int n, string one, string few, string many)
    {
        int lastTwo = n % 100, last = n % 10;
        if (lastTwo >= 11 && lastTwo <= 14) return n + " " + many;
        if (last == 1) return n + " " + one;
        if (last >= 2 && last <= 4) return n + " " + few;
        return n + " " + many;
    }

    static string CoinsText(int n) => Plural(n, "монета", "монети", "монет");

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    float PlayerDistance(Transform thing)
    {
        return FlatDistance(playerCamera.transform.position, thing.position);
    }

    // ---------- saving ----------

    void RememberFarmPosition()
    {
        if (!FarmInput.MovementLocked && !slingshotActive && !raceActive && !galleryActive && !rushActive)
        {
            Vector3 pos = playerCamera.transform.position;
            returnSpot = new Vector3(pos.x, 0f, pos.z);
            returnYaw = playerCamera.transform.eulerAngles.y;
            hasReturnSpot = true;
        }
    }

    void SaveGame()
    {
        if (AtMainMenu || changingScene || restoring) return;
        if (stage == Stage.Intro)
        {
            new GameSave { introPage = introPage }.Store();
            return;
        }
        RememberFarmPosition();
        GameSave save = new GameSave
        {
            stage = (int)stage,
            feet = returnSpot, yaw = returnYaw, hasFeet = hasReturnSpot,
            coins = coins,
            hensBought = hensBought,
            day = dayNight.day,
            time01 = dayNight.time01,
            nests = NestState(),
            goldenNests = NestState(true),
            dailyEggs = eggsToday,
            laySeconds = System.Array.ConvertAll(henNextLay, next => Mathf.Max(0f, next - Time.time)),
            firstEggSeconds = Mathf.Max(0f, firstEggAt - Time.time),
            foxNightChecked = foxNightChecked, foxNews = foxNews,
            cowPetted = cowPettedDay, goatPetted = goatPettedDay,
            routeSentDay = routeSentDay,
            bigHungry = bigHungry,
            feederKg = feederKg,
            storageKg = storageKg,
            hungryDays = hungryDays,
            henGone = (bool[])henGone.Clone(),
            flightBest = bestScore,
            flightPaid = roundsDay == dayNight.day ? roundsToday : 0,
            racePaid = racesDay == dayNight.day ? racesToday : 0,
            galleryPaid = galleryDay == dayNight.day ? galleryRounds : 0,
            rushPaid = rushDay == dayNight.day ? rushRounds : 0,
            canWater = canWater,
            galleryBest = galleryBest,
            rushBest = rushBest,
            cheeseBuilt = cheeseBuilt,
            kitchenBuilt = kitchenBuilt,
            cooking = cooking,
            cookProgress = cookProgress,
            coopDoorOpen = coopDoorOpen,
            feederUpgraded = feederUpgraded,
            barnUpgraded = barnUpgraded,
            cheeseBatch = cheeseBatch,
            cheeseProgress = cheeseProgress,
            barnHayKg = barnHayKg,
            storageHayKg = storageHayKg,
            coopUpgraded = coopUpgraded,
            cowsBought = cowsBought,
            goatsBought = goatsBought,
            horseshoes = horseshoesFound,
            beds = (int[])bedState.Clone(),
            bedGrowth = (float[])bedGrowth.Clone(),
        };
        save.itemIds = inventory.ToArray();
        save.itemCounts = new int[inventory.Count];
        for (int i = 0; i < inventory.Count; i++) save.itemCounts[i] = counts[inventory[i]];
        SaveTasks(save);
        save.cowMilked = (int[])cowMilkedDay.Clone();
        save.goatMilked = (int[])goatMilkedDay.Clone();
        save.usedHints = new string[usedHints.Count];
        usedHints.CopyTo(save.usedHints);
        save.Store();
    }

    void ApplySave(GameSave save)
    {
        stage = (Stage)save.stage;
        coins = save.coins;
        coopUpgraded = save.coopUpgraded;
        hensBought = Mathf.Min(save.hensBought, hens.Count);
        for (int i = 0; i < save.itemIds.Length && i < save.itemCounts.Length; i++) AddItem(save.itemIds[i], save.itemCounts[i]);
        foreach (string hint in save.usedHints) usedHints.Add(hint);
        dialogueTaught = stage > Stage.MeetBoris;
        dayNight.SetTime(save.time01, save.day);
        dayNight.running = stage >= Stage.WaitEggs;
        ApplyStoryState();
        foxNightChecked = save.foxNightChecked;
        foxNews = save.foxNews ?? "";
        ApplyEggState(save);
        ApplyShopState();
        ApplyChoreState(save);
        routeSentDay = save.routeSentDay;
        barnHayKg = save.barnHayKg;
        storageHayKg = save.storageHayKg;
        bigHungry = save.bigHungry;
        ApplyBarnState(save);
        LoadTasks(save);
        roundsDay = racesDay = galleryDay = rushDay = dayNight.day;
        roundsToday = save.flightPaid;
        racesToday = save.racePaid;
        galleryRounds = save.galleryPaid;
        rushRounds = save.rushPaid;
        bestScore = save.flightBest;
        canWater = save.canWater;
        galleryBest = save.galleryBest;
        rushBest = save.rushBest;
        feederUpgraded = save.feederUpgraded;
        barnUpgraded = save.barnUpgraded;
        ShowFeederGrain();
        ApplyCheeseState(save);
        ApplyKitchenState(save);
        SetCoopDoor(save.coopDoorOpen, false);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveGame(); // on the Quest: the headset was taken off or the app went to the background
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused) SaveGame();
    }

    void OnApplicationQuit()
    {
        SaveGame();
    }

    // ---------- the task text ----------

    // Rebuilt only when something it shows has changed.
    void RefreshQuestText()
    {
        bool night = dayNight != null && dayNight.IsNight;
        int key = (int)stage;
        key = key * 31 + ToolCount();
        key = key * 31 + hensBought;
        key = key * 31 + (night ? 1 : 0);
        key = key * 31 + ChoreKey(); // inventory, horseshoes, feeding, the garden (FarmPrototype.Chores.cs)
        key = key * 31 + dayNight.day;
        key = key * 31 + cowsBought;
        key = key * 31 + goatsBought;
        key = key * 31 + Mathf.FloorToInt(barnHayKg);
        key = key * 31 + Mathf.FloorToInt(storageHayKg);
        key = key * 31 + tasksVersion; // side jobs (FarmPrototype.Tasks.cs)
        if (key == questKey) return;
        questKey = key;
        ShowRoutePost();
        switch (stage)
        {
            case Stage.Intro: questText = ""; break;
            case Stage.Arrive: questText = "Дійди до ферми «Сидорівка»."; break;
            case Stage.FindTools: questText = "Знайди інструменти для паркана (" + ToolCount() + "/3). Ще треба: " + MissingTools() + ". Біля них світяться маленькі помаранчеві кола."; break;
            case Stage.RepairFence: questText = "Усі інструменти є! Полагодь розвалений паркан біля воріт."; break;
            case Stage.MeetBoris: questText = "Послухай козла Бориса."; break;
            case Stage.RestoreCoop: questText = "Відремонтуй старий курник праворуч у дворі (" + CoinsText(CoopPrice) + ")."; break;
            case Stage.MeetZhora: questText = "Познайомся з півнем Жорою."; break;
            case Stage.BuyHens: questText = "Купи 2 курочки у Жори, по " + CoinsText(HenPrice) + " (куплено " + hensBought + "/2)."; break;
            case Stage.WaitEggs:
                questText = FedToday
                    ? "Курочки ситі! Зайди в курник і збери яйце в гніздах біля задньої стіни."
                    : "Курочки голодні: набери зерна на складі (сарай біля стодоли) і досип у годівницю перед курником.";
                break;
            case Stage.MeetMarquis: questText = "Віднеси яйця коту Маркізу — його лавка біля дороги, зліва від стежки."; break;
            case Stage.RoutePacking: case Stage.RouteReply: case Stage.RouteDone: questText = RouteQuestText(); break;
            default: questText = ChoreQuestText(); break; // stage 5 (FarmPrototype.Chores.cs)
        }
        if (stage >= Stage.MeetMarquis) questText += DailyChoreText();
        questText += SideTasksText();
        if (dayNight != null && dayNight.running && night)
            questText += "\nЗараз ніч: можна поспати в ліжку в будинку діда Панаса.";
    }

    // ---------- read by VrHud, VrHands and the Mac screen ----------

    public int Coins => coins;
    public string BannerText => bannerText;
    public string BannerTitle => bannerTitle;
    public float BannerAge => Time.time - bannerAt;
    public float BannerStamp => bannerAt; // changes every time a new banner is shown
    public int CoinDelta => coinDelta;
    public float CoinDeltaAge => Time.time - coinDeltaAt;
    public string Message => message;
    public bool MessageVisible => Time.time < messageUntil; // messages fade after a few seconds
    public Transform MessageSpeaker => messageSpeaker;
    public bool InMinigame => slingshotActive || raceActive || galleryActive || rushActive;
    public bool TaskPanelOpen => taskPanelOpen;
    public bool InventoryOpen => inventoryOpen;
    public IReadOnlyList<string> Inventory => inventory;
    public int InventoryVersion => inventoryVersion;
    public bool AimedAtSomething => (AtMainMenu && titleBoard != null && titleBoard.Hovering) || aimedAt != null || (windowAim && shopWindow.Hovering);
    public string AimedLabel => aimedAt != null && (aimedAt.alwaysHint || !usedHints.Contains(aimedAt.label)) ? aimedAt.label : null;
    public bool AimHasHit => aimHasHit;
    public Vector3 AimPoint => aimPoint;
    public float AimHitDistance => aimHitDistance;
    public string CurrentQuestText() => questText;
    public string DayText => dayNight != null && dayNight.running ? "День " + dayNight.day + " • " + dayNight.PartOfDay : "";

    public int ItemCount(string id)
    {
        return counts.TryGetValue(id, out int n) ? n : 0;
    }

    // What a thing in the backpack is for (a short hint under it).
    public string ItemHint(string id)
    {
        switch (id)
        {
            case "hammer": case "nails": case "saw": return "лагодити";
            case "shovel": return "копати";
            case "rake": return "полоти грядки";
            case "can": return "поливати";
            case "seeds": return "садити";
            case "grain": return "годувати курей";
            case "planks": case "paint": return "ремонт будинку";
            case "horseshoe": return "для Бориса";
            case "barn_key": return "відімкнути стодолу";
            case "carrot": return "продати / Борису";
            default: return "продати Маркізу"; // eggs, milk
        }
    }

    public string ItemName(string id)
    {
        return ItemNames.TryGetValue(id, out string name) ? name : id;
    }

    // "Яйця ×3" for things you have more than one of.
    public string ItemLine(string id)
    {
        int n = ItemCount(id);
        if (id == "can") return "Лійка (вода " + canWater + "/" + CanCapacity + ")";
        if (id == "grain") return "Зерно, " + n + " кг";
        if (id == "hay") return "Сіно, " + n + " кг";
        return n > 1 ? ItemName(id) + " ×" + n : ItemName(id);
    }

    // The look of an item (its "Model" part), used to show small copies in the inventory.
    public GameObject ItemModel(string id)
    {
        if (!itemLooks.TryGetValue(id, out GameObject item) || item == null) return null;
        Transform model = item.transform.Find("Model");
        return model != null ? model.gameObject : null;
    }

    void OnGUI()
    {
        if (FarmInput.HeadsetActive) return;
        if (AtMainMenu) { DrawMainMenu(); return; }
        if (stage == Stage.Intro) return; // in VR, VrHud shows this instead

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        GUIStyle body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = Color.white } };
        GUIStyle prompt = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };

        // Coins (and the day) in the top-right corner.
        GUI.Box(new Rect(Screen.width - 236, 16, 220, 64), "");
        GUI.Label(new Rect(Screen.width - 226, 20, 210, 34), "Монети: " + coins, title);
        GUI.Label(new Rect(Screen.width - 226, 52, 210, 24), DayText, body);

        if (taskPanelOpen)
        {
            GUI.Box(new Rect(16, 16, 480, 400), "");
            GUI.Label(new Rect(28, 24, 440, 30), "Завдання", title);
            GUI.Label(new Rect(28, 58, 450, 290), questText, body);
            GUI.Label(new Rect(28, 352, 450, 60), "WASD — рух • миша — погляд • E — дія • I — інвентар\nJ — закрити • Tab — головне меню", body);
        }
        else if (inventoryOpen)
        {
            string list = inventory.Count == 0 ? "(поки порожньо)" : "";
            foreach (string id in inventory) list += "• " + ItemLine(id) + "  — " + ItemHint(id) + "\n";
            GUI.Box(new Rect(16, 16, 380, 330), "");
            GUI.Label(new Rect(28, 24, 350, 30), "Рюкзак", title);
            GUI.Label(new Rect(28, 58, 350, 250), list, body);
            GUI.Label(new Rect(28, 310, 350, 30), "I — закрити", body);
        }
        else GUI.Label(new Rect(20, 16, 500, 28), "Tab — меню   •   J — завдання   •   I — інвентар", body);

        // The "new task" banner (fades in and out at the top) and "+N" rising from the coins box.
        if (BannerAge < 3.5f)
        {
            float a = Mathf.Clamp01(Mathf.Min(BannerAge / 0.3f, (3.5f - BannerAge) / 0.5f));
            Color was = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            float drop = (1f - a) * 30f;
            Rect banner = new Rect(Screen.width / 2f - 300, 70 - drop, 600, 84);
            GUI.Box(banner, "");
            GUI.Box(banner, "");
            GUI.Label(new Rect(banner.x, banner.y + 6, 600, 28), bannerTitle, new GUIStyle(prompt) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.85f, 0.3f) } });
            GUI.Label(new Rect(banner.x + 16, banner.y + 34, 568, 46), bannerText, new GUIStyle(body) { alignment = TextAnchor.UpperCenter });
            GUI.color = was;
        }
        if (CoinDeltaAge < 1.4f)
        {
            float k = CoinDeltaAge / 1.4f;
            Color was = GUI.color;
            GUI.color = new Color(1f, 0.85f, 0.25f, 1f - k * k);
            GUI.Label(new Rect(Screen.width - 230, 84 - k * 50, 200, 34), "+" + coinDelta, title);
            GUI.color = was;
        }

        if (MessageVisible)
        {
            GUI.Box(new Rect(16, Screen.height - 110, Mathf.Min(650, Screen.width - 32), 90), "");
            GUI.Label(new Rect(30, Screen.height - 100, Mathf.Min(620, Screen.width - 60), 70), message, body);
        }

        GUI.Label(new Rect(Screen.width / 2f - 12, Screen.height / 2f - 16, 24, 32), "+", prompt);
        string label = AimedLabel;
        if (label != null)
        {
            GUI.Box(new Rect(Screen.width / 2f - 220, Screen.height / 2f + 32, 440, 42), "");
            GUI.Label(new Rect(Screen.width / 2f - 216, Screen.height / 2f + 36, 432, 34), "E — " + label, prompt);
        }
    }

    // Builds the farm (the editor calls this; see FarmSceneInitializer).
    public void BuildWorld()
    {
        if (transform.Find("Farm World") != null) return;
        GameObject root = new GameObject("Farm World");
        root.transform.SetParent(transform, false);
        new GameObject(WorldVersionMarker + WorldVersion).transform.SetParent(root.transform, false);
        FarmWorldBuilder.Build(root.transform);
    }
}
