using System.Collections.Generic;
using UnityEngine;

// A menu of buttons hanging in the air in front of you (the shop: one menu to BUY, one to SELL).
// Point the laser at a line and pull the trigger (on the Mac: aim and press E).
// Long lists are split into pages ("< Назад" / "Далі >"), so there can be as many goods as we like.
// What's in the lists and what a button does is decided by FarmPrototype.Shop.cs.
public class ShopMenu
{
    public struct Line
    {
        public string text;
        public string action; // what pressing does ("buy:planks", "sell:egg"...); empty = just a text
        public bool dim;      // greyed out: can't do it now (not enough coins, already have it)
    }

    const int PageSize = 5;
    const float LineGap = 0.022f;
    static readonly Color Normal = Color.white, Dim = new Color(0.62f, 0.62f, 0.62f), Highlighted = new Color(1f, 0.82f, 0.25f);

    readonly Transform root;
    readonly Font font;
    readonly List<GameObject> parts = new List<GameObject>();
    readonly Dictionary<FarmInteractable, TextPlate> buttons = new Dictionary<FarmInteractable, TextPlate>();
    readonly Dictionary<FarmInteractable, Color> colors = new Dictionary<FarmInteractable, Color>();
    int page;
    float openedAt;

    public ShopMenu()
    {
        root = new GameObject("Shop Menu").transform;
        root.gameObject.SetActive(false);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public bool IsOpen => root.gameObject.activeSelf;
    public Vector3 Position => root.position;

    // Opens in front of you, a little below your eyes, facing you.
    public void Open(Transform head)
    {
        Vector3 ahead = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (ahead.sqrMagnitude < 0.01f) ahead = Vector3.forward;
        root.position = head.position + ahead.normalized * 0.9f + Vector3.down * 0.2f;
        root.rotation = Quaternion.LookRotation(ahead.normalized, Vector3.up);
        root.localScale = Vector3.one * 0.6f;
        page = 0;
        openedAt = Time.time;
        root.gameObject.SetActive(true);
    }

    public void Close()
    {
        Clear();
        root.gameObject.SetActive(false);
    }

    public void TurnPage(int delta)
    {
        page += delta;
    }

    // (Re)builds the menu: a title, the lines of the current page, and the buttons at the bottom.
    public void Show(string title, List<Line> lines)
    {
        Clear();
        int pages = Mathf.Max(1, Mathf.CeilToInt(lines.Count / (float)PageSize));
        page = Mathf.Clamp(page, 0, pages - 1);
        float y = 0f;
        y = AddPlate(title, null, false, y, 0f, "DarkWood");
        y -= LineGap;
        for (int i = page * PageSize; i < Mathf.Min(lines.Count, (page + 1) * PageSize); i++)
            y = AddPlate(lines[i].text, lines[i].action, lines[i].dim, y, 0f, "Eyes");
        y -= LineGap;
        if (pages > 1)
        {
            AddPlate("< Назад", page > 0 ? "page:-1" : null, page == 0, y, -0.2f, "Eyes");
            AddPlate((page + 1) + "/" + pages, null, true, y, 0f, null);
            AddPlate("Далі >", page < pages - 1 ? "page:1" : null, page == pages - 1, y, 0.2f, "Eyes");
            y -= 0.06f;
        }
        AddPlate("Закрити", "close", false, y, 0f, "Eyes");
    }

    // The button under the laser turns yellow. The menu also pops open (grows in a moment).
    public void Hover(FarmInteractable aimed)
    {
        float grow = Mathf.Clamp01((Time.time - openedAt) / 0.18f);
        root.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, 1f - (1f - grow) * (1f - grow));
        foreach (var pair in buttons)
            pair.Value.SetColor(pair.Key == aimed ? Highlighted : colors[pair.Key]);
    }

    float AddPlate(string text, string action, bool dim, float top, float x, string plateMaterial)
    {
        GameObject line = new GameObject(action != null ? "Menu Button" : "Menu Text");
        line.transform.SetParent(root, false);
        line.layer = PlayerBody.WalkThroughLayer; // the laser finds it, your body walks through it
        parts.Add(line);
        TextPlate plate = new TextPlate("Menu Plate", font, 48, 0.0034f, TextAlignment.Center, plateMaterial, dim ? Dim : Normal);
        plate.Root.SetParent(line.transform, false);
        plate.SetText(text);
        Bounds size = plate.TextBounds;
        line.transform.localPosition = new Vector3(x, top - size.extents.y - 0.012f, 0f);
        if (action != null)
        {
            BoxCollider hit = line.AddComponent<BoxCollider>();
            hit.center = size.center;
            hit.size = new Vector3(size.size.x + 0.05f, size.size.y + 0.025f, 0.02f);
            FarmInteractable button = line.AddComponent<FarmInteractable>();
            button.kind = FarmInteractable.Kind.MenuRow;
            button.itemId = action;
            button.label = "вибрати";
            buttons[button] = plate;
            colors[button] = dim ? Dim : Normal;
        }
        return top - size.size.y - 0.024f - LineGap;
    }

    void Clear()
    {
        foreach (GameObject part in parts) Object.Destroy(part);
        parts.Clear();
        buttons.Clear();
        colors.Clear();
    }
}
