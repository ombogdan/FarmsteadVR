using System.Text;
using UnityEngine;

// Everything you read in the headset. Nothing hangs in front of your eyes all the time:
// - Coins: a small badge with a yellow coin on your LEFT hand, like a watch. Raise the hand to look.
// - B on the right controller: the task panel above your left hand.
// - Y on the left controller: the inventory above your left hand, with little copies of your items.
// - Boris speaks in a bubble above his head; other messages float briefly in front of you and fade.
// - When the laser points at something usable, a hint appears above it.
// VrPlayer adds this script to the camera and calls Init.
public class VrHud : MonoBehaviour
{
    FarmPrototype farm;
    Transform head;
    Transform leftHand;

    TextPlate message, bubble, task, inventory, hint, coins, banner, coinPop;
    float lastBannerStamp = -1f;
    int lastPop = -1;
    Transform coinIcon;
    Transform itemModels;

    string lastMessage, wrappedMessage;
    string lastTask, taskText;
    int lastTaskCoins = -1;
    string lastLabel, hintText;
    int shownCoins = -1;
    string coinsText;
    float coinPulse;
    int shownInventoryVersion = -1;
    string lastDay;
    Vector3 messagePosition;
    Vector3 bannerPlace;
    float bannerPlacedFor = -1f;
    float backpackOpenedAt;
    readonly System.Collections.Generic.List<Transform> backpackIcons = new System.Collections.Generic.List<Transform>();
    Font hudFont;
    string bannerWrapped;
    bool messagePlaced;
    bool ready; // false if Start failed: then we stop instead of repeating the error every frame

    public void Init(Transform leftHand)
    {
        this.leftHand = leftHand;
    }

    void Start()
    {
        farm = GetComponentInParent<FarmPrototype>();
        head = transform;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hudFont = font;

        message = new TextPlate("VR Message", font, 52, 0.005f, TextAlignment.Left);
        bubble = new TextPlate("VR Speech Bubble", font, 52, 0.005f, TextAlignment.Left);
        task = new TextPlate("VR Task Panel", font, 40, 0.0028f, TextAlignment.Left);
        inventory = new TextPlate("VR Inventory", font, 40, 0.0028f, TextAlignment.Left);
        hint = new TextPlate("VR Hint", font, 44, 0.005f, TextAlignment.Center);
        coins = new TextPlate("VR Coins", font, 44, 0.0028f, TextAlignment.Left);
        banner = new TextPlate("VR New Task", font, 52, 0.005f, TextAlignment.Center, "DarkWood");
        coinPop = new TextPlate("VR Coin Pop", font, 52, 0.004f, TextAlignment.Center, null, new Color(1f, 0.85f, 0.25f));
        banner.Hide();
        coinPop.Hide();
        coinIcon = VrShapes.Create(PrimitiveType.Sphere, "Coin", coins.Root, "Yellow").transform;
        coinIcon.localScale = new Vector3(0.026f, 0.026f, 0.006f); // a flat yellow disk
        itemModels = new GameObject("VR Inventory Items").transform;

        message.Hide();
        bubble.Hide();
        task.Hide();
        inventory.Hide();
        hint.Hide();
        itemModels.gameObject.SetActive(false);
        ready = true;
    }

    void LateUpdate()
    {
        if (!ready || farm == null) return;
        UpdateMessages();
        UpdateHandItems();
        UpdateHint();
        UpdateTransitions();
    }

    // "НОВЕ ЗАВДАННЯ" pops up in front of you (grows with a bounce, floats up, shrinks away),
    // and "+N" jumps up from the coin badge on your wrist.
    void UpdateTransitions()
    {
        float age = farm.BannerAge;
        if (age < 3.5f)
        {
            if (farm.BannerStamp != lastBannerStamp)
            {
                lastBannerStamp = farm.BannerStamp;
                bannerWrapped = farm.BannerText.Length > 0 ? farm.BannerTitle + "\n\n" + TextPlate.Wrap(farm.BannerText, 34) : farm.BannerTitle;
            }
            Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
            if (age < 0.05f || !banner.Root.gameObject.activeSelf || farm.BannerStamp != bannerPlacedFor) { bannerPlace = head.position + flat.normalized * 1.6f + Vector3.up * 0.25f; bannerPlacedFor = farm.BannerStamp; }
            float grow = Mathf.Clamp01(age / 0.35f);
            float bounce = 1f + 2.70158f * Mathf.Pow(grow - 1f, 3f) + 1.70158f * Mathf.Pow(grow - 1f, 2f);
            float shrink = Mathf.Clamp01((3.5f - age) / 0.35f);
            banner.Show(bannerWrapped, bannerPlace + Vector3.up * age * 0.03f, head, Mathf.Max(0.01f, bounce * shrink));
        }
        else banner.Hide();

        float pop = farm.CoinDeltaAge;
        if (pop < 1.3f && leftHand != null)
        {
            if (farm.CoinDelta != lastPop) lastPop = farm.CoinDelta;
            Vector3 wrist = leftHand.position + leftHand.up * 0.045f - leftHand.forward * 0.07f;
            coinPop.Show("+" + lastPop, wrist + Vector3.up * (0.05f + 0.15f * pop), head, 1f + 0.5f * Mathf.Sin(Mathf.Min(1f, pop * 3f) * Mathf.PI));
        }
        else coinPop.Hide();
    }

    // Boris talks in a bubble above his head; other messages float in front of you.
    void UpdateMessages()
    {
        if (!farm.MessageVisible)
        {
            message.Hide();
            bubble.Hide();
            messagePlaced = false;
            return;
        }
        if (!ReferenceEquals(farm.Message, lastMessage))
        {
            lastMessage = farm.Message;
            wrappedMessage = TextPlate.Wrap(lastMessage, 36);
        }

        Transform speaker = farm.MessageSpeaker;
        if (speaker != null)
        {
            // Just above the speaker's head (whatever its size), pulled a little towards you.
            Collider body = speaker.GetComponent<Collider>();
            float top = body != null ? body.bounds.max.y : speaker.position.y + 1f;
            Vector3 above = new Vector3(speaker.position.x, top + 0.45f, speaker.position.z);
            Vector3 toHead = head.position - above;
            toHead.y = 0f;
            if (toHead.sqrMagnitude > 0.01f) above += toHead.normalized * 0.3f;
            float distance = Vector3.Distance(head.position, above);
            bubble.Show(wrappedMessage, above, head, Mathf.Clamp(distance / 2f, 1f, 2.2f));
            message.Hide();
            return;
        }

        // Slightly below your gaze, slowly following the head (not glued to the face).
        Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
        Vector3 target = head.position + flat.normalized * 1.4f + Vector3.down * 0.25f;
        messagePosition = messagePlaced ? Vector3.Lerp(messagePosition, target, Time.deltaTime * 2.5f) : target;
        messagePlaced = true;
        message.Show(wrappedMessage, messagePosition, head);
        bubble.Hide();
    }

    // Coin badge on the left hand (always), and the task or inventory panel above it.
    void UpdateHandItems()
    {
        if (leftHand == null) return;

        if (farm.Coins != shownCoins)
        {
            if (shownCoins >= 0) coinPulse = 1f; // the badge "jumps" when you earn coins
            shownCoins = farm.Coins;
            coinsText = farm.Coins.ToString();
        }
        coinPulse = Mathf.Max(0f, coinPulse - Time.deltaTime * 2f);
        Vector3 wrist = leftHand.position + leftHand.up * 0.045f - leftHand.forward * 0.07f;
        coins.Show(coinsText, wrist, head, 1f + 0.6f * coinPulse, extraLeft: 0.03f);
        Bounds amount = coins.TextBounds;
        coinIcon.localPosition = new Vector3(amount.min.x - 0.02f, amount.center.y, -0.003f);

        Vector3 abovePanel = leftHand.position + Vector3.up * 0.17f;

        if (farm.TaskPanelOpen)
        {
            string quest = farm.CurrentQuestText();
            string day = farm.DayText;
            if (!ReferenceEquals(quest, lastTask) || farm.Coins != lastTaskCoins || day != lastDay)
            {
                lastTask = quest;
                lastTaskCoins = farm.Coins;
                lastDay = day;
                string dayLine = day.Length > 0 ? day + "\n" : "";
                taskText = dayLine + "Монети: " + farm.Coins + "\n\n" + TextPlate.Wrap(quest, 30)
                    + "\n\nB — закрити\nX — головне меню";
            }
            task.Show(taskText, abovePanel, head);
        }
        else task.Hide();

        if (farm.InventoryOpen)
        {
            // The backpack: a panel of cells above your left hand, a turning model of each thing in its cell.
            if (farm.InventoryVersion != shownInventoryVersion) RebuildInventory();
            inventory.Hide();
            if (!itemModels.gameObject.activeSelf)
            {
                itemModels.gameObject.SetActive(true);
                backpackOpenedAt = Time.time;
            }
            itemModels.position = abovePanel + Vector3.up * 0.12f;
            itemModels.rotation = Quaternion.LookRotation(itemModels.position - head.position, Vector3.up);
            float open = Mathf.Clamp01((Time.time - backpackOpenedAt) / 0.25f);
            itemModels.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, 1f - (1f - open) * (1f - open));
            foreach (Transform icon in backpackIcons) if (icon != null) icon.Rotate(0f, 50f * Time.deltaTime, 0f, Space.Self);
        }
        else
        {
            inventory.Hide();
            itemModels.gameObject.SetActive(false);
        }
    }

    void RebuildInventory()
    {
        shownInventoryVersion = farm.InventoryVersion;
        foreach (Transform old in itemModels) Destroy(old.gameObject);
        backpackIcons.Clear();
        itemModels.localScale = Vector3.one;
        itemModels.rotation = Quaternion.identity;

        int count = farm.Inventory.Count;
        int columns = Mathf.Clamp(count, 1, 4);
        int rows = Mathf.Max(1, Mathf.CeilToInt(count / 4f));
        const float cellWidth = 0.085f, cellHeight = 0.105f;
        float width = columns * cellWidth + 0.03f, height = rows * cellHeight + 0.09f;
        Board(new Vector3(0f, 0.04f - height / 2f, 0.01f), new Vector3(width, height, 0.004f), "DarkWood");
        Text("Рюкзак", new Vector3(0f, 0.02f, 0f), 0.0022f, Color.white);
        for (int i = 0; i < count; i++)
        {
            string id = farm.Inventory[i];
            float x = (i % columns - (columns - 1) / 2f) * cellWidth;
            float y = -0.035f - cellHeight / 2f - (i / columns) * cellHeight;
            Board(new Vector3(x, y, 0.006f), new Vector3(cellWidth - 0.008f, cellHeight - 0.008f, 0.003f), "Eyes");
            Text(farm.ItemLine(id), new Vector3(x, y - 0.028f, -0.002f), 0.0014f, Color.white);
            Text(farm.ItemHint(id), new Vector3(x, y - 0.042f, -0.002f), 0.0012f, new Color(0.75f, 0.75f, 0.75f));
            GameObject source = farm.ItemModel(id);
            if (source == null) continue;
            Transform copy = Instantiate(source, itemModels).transform;
            copy.gameObject.SetActive(true);
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = VrShapes.IgnoreRaycastLayer;
            copy.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            copy.localScale = Vector3.one;
            Bounds b = new Bounds(copy.position, Vector3.zero);
            bool first = true;
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>())
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
            copy.localScale = Vector3.one * (size > 0.001f ? 0.045f / size : 1f);
            copy.localPosition = new Vector3(x, y + 0.012f, -0.025f);
            backpackIcons.Add(copy);
        }
        if (count == 0) Text("(поки порожньо)", new Vector3(0f, -0.07f, 0f), 0.0016f, new Color(0.75f, 0.75f, 0.75f));
        Text("Y — закрити", new Vector3(0f, 0.06f - height, 0f), 0.0014f, new Color(0.75f, 0.75f, 0.75f));
    }

    void Board(Vector3 at, Vector3 size, string material)
    {
        Transform board = VrShapes.Create(PrimitiveType.Cube, "Backpack Board", itemModels, material).transform;
        board.localPosition = at;
        board.localScale = size;
    }

    void Text(string value, Vector3 at, float letterSize, Color color)
    {
        TextPlate plate = new TextPlate("Backpack Text", hudFont, 40, letterSize, TextAlignment.Center, null, color);
        plate.Root.SetParent(itemModels, false);
        plate.Root.localPosition = at;
        plate.SetText(value);
    }

    // A hint above the thing the laser points at, pulled a bit towards you.
    void UpdateHint()
    {
        string label = farm.AimedLabel;
        if (label == null)
        {
            hint.Hide();
            return;
        }
        if (!ReferenceEquals(label, lastLabel))
        {
            lastLabel = label;
            hintText = "Курок — " + label;
        }
        Vector3 toHead = (head.position - farm.AimPoint).normalized;
        Vector3 position = farm.AimPoint + Vector3.up * 0.35f + toHead * 0.35f;
        float distance = Vector3.Distance(head.position, position);
        hint.Show(hintText, position, head, Mathf.Max(1f, distance / 1.5f));
    }
}
