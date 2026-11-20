using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The shop window that hangs in the air in front of you (Marquis's stall). Two halves:
//   left  - the goods (or, when selling, your things), in pages;
//   right - the basket: what you're about to buy (or sell).
// Drag a thing from the left into the basket (hold the trigger, move, let go), or just click it.
// Click a line in the basket to take one back. Then press КУПИТИ / ПРОДАТИ.
// While the window is open the laser only talks to it (nothing behind it gets in the way).
// It is pure math on the window's flat surface: no colliders, so the stall can't block it.
// What's in the lists and what the buttons do is decided by FarmPrototype.Shop.cs (OnAction).
public class ShopWindow : MonoBehaviour
{
    public struct Entry
    {
        public string id;
        public string text;
        public bool dim;          // greyed out (can't afford, already have it...)
        public GameObject look;   // a model to show as a small turning icon (may be null)
    }

    public System.Action<string> OnAction; // "add:planks", "all:egg", "remove:planks", "page:1", "confirm", "close"

    const float Width = 0.84f, Height = 0.58f;
    const int PageSize = 5;
    static readonly Color Normal = Color.white, Dim = new Color(0.6f, 0.6f, 0.6f), Hot = new Color(1f, 0.82f, 0.25f);
    static readonly Rect CartZone = new Rect(0.01f, -0.24f, 0.4f, 0.47f); // the right half (drop things here)

    class Button
    {
        public Rect area;      // in the window's own flat coordinates (meters)
        public string action;  // what a click does
        public string dragId;  // if set, it can also be dragged into the basket
        public TextPlate plate;
        public Color color;
        public Transform icon;
    }

    readonly List<Button> buttons = new List<Button>();
    readonly List<GameObject> parts = new List<GameObject>();
    Font font;
    Button hovered, pressed;
    Vector2 pressPoint, aimPoint;
    Transform dragIcon;
    bool dragging, aimOnWindow;
    float openedAt;
    int page;

    public bool IsOpen => gameObject.activeSelf;
    public int Page { get => page; set => page = value; }

    public static ShopWindow Create()
    {
        ShopWindow window = new GameObject("Shop Window").AddComponent<ShopWindow>();
        window.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        window.gameObject.SetActive(false);
        return window;
    }

    // Opens in front of you, between you and whatever is ahead (so it never sinks into the counter).
    public void Open(Transform head)
    {
        Vector3 ahead = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (ahead.sqrMagnitude < 0.01f) ahead = Vector3.forward;
        ahead.Normalize();
        float distance = 0.65f;
        if (Physics.Raycast(head.position, ahead, out RaycastHit hit, 1f, PlayerBodyLayers(), QueryTriggerInteraction.Ignore))
            distance = Mathf.Clamp(hit.distance - 0.12f, 0.42f, 0.65f);
        transform.position = head.position + ahead * distance + Vector3.down * 0.12f;
        transform.rotation = Quaternion.LookRotation(ahead, Vector3.up);
        page = 0;
        openedAt = Time.time;
        gameObject.SetActive(true);
        FarmAudio.Play2D("page_turn", 0.6f);
    }

    static int PlayerBodyLayers() => Physics.DefaultRaycastLayers & ~(1 << PlayerBody.WalkThroughLayer);

    public void Close()
    {
        DropDrag(false);
        Clear();
        gameObject.SetActive(false);
    }

    // (Re)draws everything. goods = left side, cart = right side.
    public void Show(string title, string goodsTitle, List<Entry> goods, bool selling, string cartTitle, List<Entry> cart,
                     string total, string confirm, bool canConfirm)
    {
        Clear();
        Board("Back", Vector3.zero, new Vector2(Width, Height), "CountryArt/Timber", 0.012f);
        Board("Goods Panel", new Vector3(-0.2f, -0.025f, -0.004f), new Vector2(0.39f, 0.44f), "FarmMaterials/Eyes", 0.006f);
        Board("Cart Panel", new Vector3(0.21f, -0.025f, -0.004f), new Vector2(0.39f, 0.44f), "FarmMaterials/Eyes", 0.006f);
        Label(title, new Vector2(0f, 0.255f), 0.0032f, null, Normal);
        Label(goodsTitle, new Vector2(-0.2f, 0.17f), 0.0026f, null, Hot);
        Label(cartTitle, new Vector2(0.21f, 0.17f), 0.0026f, null, Hot);

        int pages = Mathf.Max(1, Mathf.CeilToInt(goods.Count / (float)PageSize));
        page = Mathf.Clamp(page, 0, pages - 1);
        float y = 0.115f;
        for (int i = page * PageSize; i < Mathf.Min(goods.Count, (page + 1) * PageSize); i++, y -= 0.062f)
        {
            Entry e = goods[i];
            Button row = AddButton(e.text, new Vector2(-0.19f, y), new Vector2(0.36f, 0.055f), "add:" + e.id, e.dim);
            row.dragId = e.id;
            row.icon = Icon(e.look, new Vector2(-0.355f, y));
            if (selling) AddButton("усі", new Vector2(-0.045f, y), new Vector2(0.06f, 0.045f), "all:" + e.id, false);
        }
        if (goods.Count == 0) Label(selling ? "Нічого продати.\nНеси яйця й моркву!" : "Порожньо", new Vector2(-0.2f, 0.05f), 0.0026f, null, Dim);
        if (pages > 1)
        {
            AddButton("<", new Vector2(-0.33f, -0.205f), new Vector2(0.06f, 0.045f), page > 0 ? "page:-1" : null, page == 0);
            Label((page + 1) + " / " + pages, new Vector2(-0.2f, -0.205f), 0.0024f, null, Dim);
            AddButton(">", new Vector2(-0.07f, -0.205f), new Vector2(0.06f, 0.045f), page < pages - 1 ? "page:1" : null, page == pages - 1);
        }

        y = 0.115f;
        foreach (Entry e in cart)
        {
            Button line = AddButton(e.text, new Vector2(0.225f, y), new Vector2(0.34f, 0.05f), "remove:" + e.id, false);
            line.icon = Icon(e.look, new Vector2(0.05f, y));
            y -= 0.056f;
        }
        if (cart.Count == 0) Label("Перетягни сюди товар\nабо просто клацни на нього", new Vector2(0.21f, 0.03f), 0.0024f, null, Dim);

        Label(total, new Vector2(0.11f, -0.27f), 0.0028f, null, Normal);
        AddButton(confirm, new Vector2(0.33f, -0.27f), new Vector2(0.15f, 0.05f), canConfirm ? "confirm" : null, !canConfirm, "FarmMaterials/Leaves");
        AddButton("Закрити", new Vector2(-0.31f, -0.27f), new Vector2(0.15f, 0.05f), "close", false);
        hovered = null;
    }

    // Called every frame by FarmPrototype with the laser (or the camera on the Mac).
    // Returns true when the laser points at the window; "hit" is where (the laser ends there).
    public bool Aim(Vector3 origin, Vector3 direction, out Vector3 hit)
    {
        hit = Vector3.zero;
        aimOnWindow = false;
        Plane surface = new Plane(-transform.forward, transform.position);
        Ray ray = new Ray(origin, direction);
        if (!surface.Raycast(ray, out float distance) || distance > 3f) { SetHover(null); return false; }
        hit = ray.GetPoint(distance);
        Vector3 local = transform.InverseTransformPoint(hit); // already undoes the opening scale
        aimPoint = new Vector2(local.x, local.y);
        aimOnWindow = Mathf.Abs(aimPoint.x) <= Width / 2f && Mathf.Abs(aimPoint.y) <= Height / 2f;
        Button over = null;
        foreach (Button b in buttons)
            if (b.action != null && b.area.Contains(aimPoint)) over = b;
        SetHover(over);
        return aimOnWindow;
    }

    public bool Hovering => hovered != null;

    // The trigger: pressed / held / let go this frame.
    public void Tick(bool press, bool held, bool release)
    {
        if (press && hovered != null)
        {
            if (hovered.dragId != null)
            {
                pressed = hovered;    // could be a click or the start of a drag: wait and see
                pressPoint = aimPoint;
                dragging = false;
            }
            else
            {
                FarmAudio.Play2D("tick", 0.5f);
                StartCoroutine(Squash(hovered.plate.Root));
                OnAction?.Invoke(hovered.action);
                return;
            }
        }
        if (pressed != null && held && !dragging && Vector2.Distance(aimPoint, pressPoint) > 0.025f)
        {
            dragging = true; // it's a drag: a copy of the thing follows the laser
            dragIcon = MakeDragIcon(pressed);
        }
        if (dragging && dragIcon != null)
            dragIcon.localPosition = new Vector3(aimPoint.x, aimPoint.y, -0.04f);
        if (pressed != null && release)
        {
            if (!dragging) // a click: the thing flies into the basket by itself
            {
                StartCoroutine(FlyIcon(MakeDragIcon(pressed), Center(pressed.area), CartZone.center));
                OnAction?.Invoke("add:" + pressed.dragId);
            }
            else DropDrag(CartZone.Contains(aimPoint));
            pressed = null;
        }
    }

    void DropDrag(bool intoCart)
    {
        if (dragIcon != null)
        {
            if (intoCart)
            {
                StartCoroutine(FlyIcon(dragIcon, aimPoint, CartZone.center));
                if (pressed != null) OnAction?.Invoke("add:" + pressed.dragId);
            }
            else if (pressed != null) StartCoroutine(FlyIcon(dragIcon, aimPoint, Center(pressed.area))); // back where it came from
            else Destroy(dragIcon.gameObject);
        }
        dragIcon = null;
        dragging = false;
    }

    void Update()
    {
        // Opening: grows from small with a little bounce. Icons turn slowly.
        float k = Mathf.Clamp01((Time.time - openedAt) / 0.28f);
        float s = 1f + 2.70158f * Mathf.Pow(k - 1f, 3f) + 1.70158f * Mathf.Pow(k - 1f, 2f);
        transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, s);
        foreach (Button b in buttons)
            if (b.icon != null) b.icon.Rotate(0f, 60f * Time.deltaTime, 0f, Space.Self);
    }

    // ---------- building the pieces ----------

    void SetHover(Button b)
    {
        if (b == hovered) return;
        if (hovered != null && hovered.plate != null) { hovered.plate.SetColor(hovered.color); hovered.plate.Root.localScale = Vector3.one; }
        hovered = b;
        if (hovered != null)
        {
            hovered.plate.SetColor(Hot);
            hovered.plate.Root.localScale = Vector3.one * 1.06f;
            FarmAudio.Play2D("tick", 0.25f, 1.4f);
        }
    }

    Button AddButton(string text, Vector2 at, Vector2 size, string action, bool dim, string plateMaterial = "Eyes")
    {
        Color color = dim ? Dim : Normal;
        TextPlate plate = Label(text, at, size.y < 0.05f ? 0.0026f : 0.0028f, plateMaterial, color);
        Button b = new Button { area = new Rect(at - size / 2f, size), action = action, plate = plate, color = color };
        buttons.Add(b);
        return b;
    }

    TextPlate Label(string text, Vector2 at, float letterSize, string plateMaterial, Color color)
    {
        if (plateMaterial != null && plateMaterial.StartsWith("FarmMaterials/")) plateMaterial = plateMaterial.Substring(14);
        TextPlate plate = new TextPlate("Window Text", font, 48, letterSize, TextAlignment.Center, plateMaterial, color);
        plate.Root.SetParent(transform, false);
        plate.Root.localPosition = new Vector3(at.x, at.y, -0.012f);
        plate.SetText(text);
        parts.Add(plate.Root.gameObject);
        return plate;
    }

    void Board(string name, Vector3 at, Vector2 size, string material, float thickness)
    {
        Transform board = VrShapes.Create(PrimitiveType.Cube, name, transform, "Eyes").transform;
        board.localPosition = at + new Vector3(0f, 0f, thickness / 2f);
        board.localScale = new Vector3(size.x, size.y, thickness);
        Material look = Resources.Load<Material>(material);
        if (look != null) board.GetComponent<Renderer>().sharedMaterial = look;
        parts.Add(board.gameObject);
    }

    // A small copy of the thing (about 6 cm), turning slowly next to its line.
    Transform Icon(GameObject look, Vector2 at)
    {
        if (look == null) return null;
        Transform icon = Instantiate(look, transform).transform;
        icon.gameObject.SetActive(true);
        foreach (Transform part in icon.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = VrShapes.IgnoreRaycastLayer;
        foreach (Collider c in icon.GetComponentsInChildren<Collider>(true)) Destroy(c);
        icon.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        Bounds b = new Bounds(icon.position, Vector3.zero);
        foreach (Renderer r in icon.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        float size = Mathf.Max(b.size.x, b.size.y, b.size.z) / Mathf.Max(0.01f, transform.lossyScale.x);
        icon.localScale = Vector3.one * (size > 0.001f ? 0.055f / size : 1f);
        icon.localPosition = new Vector3(at.x, at.y - 0.02f, -0.03f);
        parts.Add(icon.gameObject);
        return icon;
    }

    Transform MakeDragIcon(Button from)
    {
        Transform icon = from.icon != null ? Instantiate(from.icon.gameObject, transform).transform : null;
        if (icon == null)
        {
            TextPlate tag = new TextPlate("Drag", font, 48, 0.0026f, TextAlignment.Center, "Eyes", Hot);
            tag.SetText(from.plate != null ? "•" : "?");
            icon = tag.Root;
            icon.SetParent(transform, false);
        }
        icon.localScale *= 1.4f;
        FarmAudio.Play2D("pop", 0.4f, 1.3f);
        return icon;
    }

    IEnumerator FlyIcon(Transform icon, Vector2 from, Vector2 to)
    {
        if (icon == null) yield break;
        for (float t = 0f; t < 0.3f && icon != null; t += Time.deltaTime)
        {
            float k = t / 0.3f;
            Vector2 p = Vector2.Lerp(from, to, k * (2f - k));
            icon.localPosition = new Vector3(p.x, p.y + Mathf.Sin(k * Mathf.PI) * 0.05f, -0.05f);
            yield return null;
        }
        if (icon != null) Destroy(icon.gameObject);
    }

    IEnumerator Squash(Transform thing)
    {
        for (float t = 0f; t < 0.15f && thing != null; t += Time.deltaTime)
        {
            thing.localScale = Vector3.one * (1f - 0.12f * Mathf.Sin(t / 0.15f * Mathf.PI));
            yield return null;
        }
    }

    static Vector2 Center(Rect r) => r.center;

    void Clear()
    {
        foreach (GameObject part in parts) if (part != null) Destroy(part);
        parts.Clear();
        buttons.Clear();
        hovered = null;
    }
}
