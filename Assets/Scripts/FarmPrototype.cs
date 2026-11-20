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
    public const int WorldVersion = 10;
    public const string WorldVersionMarker = "World Version ";

    enum Stage
    {
        Intro, Arrive, FindTools, RepairFence, MeetBoris,   // stage 1
        RestoreCoop, MeetZhora, BuyHens,                    // stage 2
        WaitEggs,                                           // stage 3
        MeetMarquis, Trading,                               // stage 4 (Trading: Boris wants a word)
        Horseshoes, BorisSecret, DigChest, OpenChest,       // stage 5
        Garden, RepairHouse, EpisodeDone,
        // New stages go only at the end: saved games remember the stage as a number.
    }

    static readonly Dictionary<string, string> ItemNames = new Dictionary<string, string>
    {
        { "hammer", "Молоток" }, { "nails", "Цвяхи" }, { "saw", "Пилка" }, { "egg", "Яйця" },
        { "planks", "Дошки" }, { "paint", "Фарба" }, { "shovel", "Лопата" }, { "rake", "Граблі" },
        { "grain", "Зерно" }, { "horseshoe", "Підкова" }, { "seeds", "Насіння моркви" },
        { "carrot", "Морква" }, { "can", "Лійка" },
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
    bool taskPanelOpen;       // short press: Tab on the Mac, X on the left controller
    bool inventoryOpen;       // I on the Mac, Y on the left controller
    float menuDownAt = -1f;   // when the task button went down (a long hold = new game)
    const float NewGameHold = 4f;

    string[] dialogue;        // the lines of the conversation going on right now
    int dialogueLine;
    string dialogueName;
    Transform dialogueSpeaker;
    System.Action dialogueDone;
    bool dialogueTaught;      // "(trigger — next)" is shown only in the very first conversation

    string questText = "";
    int questKey = -1;
    float nextAutoSave;

    // What we aim with: the camera on the Mac, the right hand in VR (VrPlayer sets these).
    [System.NonSerialized] public Transform aimSource;
    [System.NonSerialized] public float aimDistance = 4f;
    bool aimHasHit;
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

        if (GameSave.TryLoad(out GameSave save) && save.stage > (int)Stage.Intro)
            StartCoroutine(ContinueSavedGame(save));
        else
            StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        IntroSequence intro = gameObject.AddComponent<IntroSequence>();
        string next = FarmInput.HeadsetActive ? "курок — далі" : "E — далі";
        yield return intro.Run(this, playerCamera, next);
        Destroy(intro);

        FarmInput.MovementLocked = false;
        stage = Stage.Arrive;
        StartBusLeaving();
        string buttons = FarmInput.HeadsetActive ? "X — завдання, Y — інвентар" : "Tab — завдання, I — інвентар";
        Say("Ось і ферма «Сидорівка»! Ходімо подивимось, що там коїться. (" + buttons + ")", 10f);
        SaveGame();
    }

    // A saved game: no intro, everything is put back the way it was.
    IEnumerator ContinueSavedGame(GameSave save)
    {
        ScreenFade fade = ScreenFade.On(playerCamera);
        fade.Set(1f);
        ApplySave(save);
        yield return null; // one frame, so the VR player is ready to be moved
        PlacePlayer(new Vector3(1.5f, 0f, -4.5f), 0f);
        yield return null;
        FarmInput.MovementLocked = false;
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
        HandlePanelButtons();
        RefreshQuestText();
        UpdateAim();
        if (stage == Stage.Intro) return;

        if (Time.time >= nextAutoSave)
        {
            nextAutoSave = Time.time + 20f;
            SaveGame();
        }
        UpdateEggs();   // hens keep laying even while you talk
        UpdateGarden(); // and carrots keep growing
        RefreshChoreLabels();

        // While somebody talks, the trigger (anywhere) shows the next sentence.
        if (InDialogue)
        {
            if (FarmInput.InteractPressed()) AdvanceDialogue();
            return;
        }

        UpdateStory();
        UpdateShop();

        if (FarmInput.InteractPressed() && aimedAt != null)
            Interact(aimedAt);
    }

    // Short press of X/Tab: tasks. Y/I: inventory. Holding X/Tab for 4 seconds: a new game.
    void HandlePanelButtons()
    {
        if (FarmInput.MenuPressed()) menuDownAt = Time.time;
        if (menuDownAt >= 0f && FarmInput.MenuHeld() && stage != Stage.Intro)
        {
            float held = Time.time - menuDownAt;
            if (held > 0.8f && !InDialogue)
                Say("Нова гра через " + Mathf.CeilToInt(NewGameHold - held) + "… (відпусти, щоб скасувати)", 0.2f);
            if (held >= NewGameHold) NewGame();
        }
        if (FarmInput.MenuReleased())
        {
            if (menuDownAt >= 0f && Time.time - menuDownAt < 0.8f)
            {
                taskPanelOpen = !taskPanelOpen;
                if (taskPanelOpen) inventoryOpen = false;
            }
            menuDownAt = -1f;
        }
        if (FarmInput.InventoryPressed())
        {
            inventoryOpen = !inventoryOpen;
            if (inventoryOpen) taskPanelOpen = false;
        }
    }

    void NewGame()
    {
        GameSave.Delete();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateAim()
    {
        aimedAt = null;
        aimHasHit = false;
        Transform source = aimSource != null ? aimSource : (playerCamera != null ? playerCamera.transform : null);
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
            case FarmInteractable.Kind.MenuRow: PressMenu(item.itemId); break;
            case FarmInteractable.Kind.GrainSack: TakeGrain(); break;
            case FarmInteractable.Kind.Feeder: FillFeeder(); break;
            case FarmInteractable.Kind.Horseshoe: PickHorseshoe(item); break;
            case FarmInteractable.Kind.Mound: DigMound(); break;
            case FarmInteractable.Kind.Chest: OpenChest(); break;
            case FarmInteractable.Kind.Bed: UseBed(item); break;
            case FarmInteractable.Kind.House: UseHouse(); break;
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

    void SaveGame()
    {
        if (stage == Stage.Intro) return;
        GameSave save = new GameSave
        {
            stage = (int)stage,
            coins = coins,
            hensBought = hensBought,
            day = dayNight.day,
            time01 = dayNight.time01,
            nests = NestState(),
            fedDay = fedDay,
            coopUpgraded = coopUpgraded,
            horseshoes = horseshoesFound,
            beds = (int[])bedState.Clone(),
            bedGrowth = (float[])bedGrowth.Clone(),
        };
        save.itemIds = inventory.ToArray();
        save.itemCounts = new int[inventory.Count];
        for (int i = 0; i < inventory.Count; i++) save.itemCounts[i] = counts[inventory[i]];
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
        ApplyEggState(save);
        ApplyShopState();
        ApplyChoreState(save);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveGame(); // on the Quest: the headset was taken off or the app went to the background
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
        if (key == questKey) return;
        questKey = key;
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
                    : "Нагодуй курочок: набери зерна з мішка біля курника й насип у годівницю. Потім збери яйце в гніздах у курнику.";
                break;
            case Stage.MeetMarquis: questText = "Віднеси яйця коту Маркізу — його лавка біля дороги, зліва від стежки."; break;
            default: questText = ChoreQuestText(); break; // stage 5 (FarmPrototype.Chores.cs)
        }
        if (stage >= Stage.MeetMarquis) questText += DailyChoreText();
        if (dayNight != null && dayNight.running && night)
            questText += "\nЗараз ніч: можна поспати в ліжку в будинку діда Панаса.";
    }

    // ---------- read by VrHud, VrHands and the Mac screen ----------

    public int Coins => coins;
    public string Message => message;
    public bool MessageVisible => Time.time < messageUntil; // messages fade after a few seconds
    public Transform MessageSpeaker => messageSpeaker;
    public bool TaskPanelOpen => taskPanelOpen;
    public bool InventoryOpen => inventoryOpen;
    public IReadOnlyList<string> Inventory => inventory;
    public int InventoryVersion => inventoryVersion;
    public bool AimedAtSomething => aimedAt != null;
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

    public string ItemName(string id)
    {
        return ItemNames.TryGetValue(id, out string name) ? name : id;
    }

    // "Яйця ×3" for things you have more than one of.
    public string ItemLine(string id)
    {
        int n = ItemCount(id);
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
        if (FarmInput.HeadsetActive || stage == Stage.Intro) return; // in VR, VrHud shows this instead

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        GUIStyle body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = Color.white } };
        GUIStyle prompt = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };

        // Coins (and the day) in the top-right corner.
        GUI.Box(new Rect(Screen.width - 236, 16, 220, 64), "");
        GUI.Label(new Rect(Screen.width - 226, 20, 210, 34), "Монети: " + coins, title);
        GUI.Label(new Rect(Screen.width - 226, 52, 210, 24), DayText, body);

        if (taskPanelOpen)
        {
            GUI.Box(new Rect(16, 16, 460, 290), "");
            GUI.Label(new Rect(28, 24, 420, 30), "Завдання", title);
            GUI.Label(new Rect(28, 58, 430, 180), questText, body);
            GUI.Label(new Rect(28, 242, 430, 60), "WASD — рух • миша — погляд • E — дія • I — інвентар\nTab — закрити • затисни Tab на 4 с — нова гра", body);
        }
        else if (inventoryOpen)
        {
            string list = inventory.Count == 0 ? "(поки порожньо)" : "";
            foreach (string id in inventory) list += "• " + ItemLine(id) + "\n";
            GUI.Box(new Rect(16, 16, 300, 220), "");
            GUI.Label(new Rect(28, 24, 270, 30), "Інвентар", title);
            GUI.Label(new Rect(28, 58, 270, 140), list, body);
            GUI.Label(new Rect(28, 200, 270, 30), "I — закрити", body);
        }
        else GUI.Label(new Rect(20, 16, 400, 28), "Tab — завдання   •   I — інвентар", body);

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
