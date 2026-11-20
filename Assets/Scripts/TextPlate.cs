using System.Text;
using UnityEngine;

// A piece of 3D text, optionally on a plate behind it.
// Used for every text in VR (messages, speech bubbles, task panel, inventory) and for the intro book.
// White text on a dark plate stays readable on any background.
public class TextPlate
{
    public readonly Transform Root;
    readonly Transform plate;
    readonly TextMesh text;
    readonly MeshRenderer textRenderer;
    string shown;

    // plateMaterial: material name from Resources/FarmMaterials, or null for no plate.
    public TextPlate(string name, Font font, int fontSize, float characterSize, TextAlignment alignment,
                     string plateMaterial = "Eyes", Color? color = null)
    {
        Root = new GameObject(name).transform;
        if (plateMaterial != null)
            plate = VrShapes.Create(PrimitiveType.Cube, "Background", Root, plateMaterial).transform;

        GameObject go = new GameObject("Text");
        go.layer = VrShapes.IgnoreRaycastLayer;
        go.transform.SetParent(Root, false);
        text = go.AddComponent<TextMesh>();
        text.font = font;
        text.fontSize = fontSize;          // big font size + small character size = sharp letters
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = alignment;
        text.color = color ?? Color.white;
        textRenderer = go.GetComponent<MeshRenderer>();
        textRenderer.sharedMaterial = font.material;
    }

    public Bounds TextBounds => textRenderer.localBounds;

    public void SetColor(Color color)
    {
        if (text.color != color) text.color = color;
    }

    // Places the plate in the world, facing the player's head.
    // extraLeft: more plate on the left side, e.g. room for an icon.
    public void Show(string value, Vector3 position, Transform head, float scale = 1f, float extraLeft = 0f)
    {
        if (!Root.gameObject.activeSelf) Root.gameObject.SetActive(true);
        SetTextOnly(value);
        Root.position = position;
        Root.rotation = Quaternion.LookRotation(position - head.position, Vector3.up);
        Root.localScale = Vector3.one * scale;
        FitPlate(extraLeft);
    }

    // For texts that are placed by their parent (like the pages of the intro book).
    public void SetText(string value)
    {
        SetTextOnly(value);
        FitPlate(0f);
    }

    public void Hide()
    {
        if (Root.gameObject.activeSelf) Root.gameObject.SetActive(false);
    }

    void SetTextOnly(string value)
    {
        if (ReferenceEquals(value, shown)) return;
        shown = value;
        text.text = value;
    }

    void FitPlate(float extraLeft)
    {
        if (plate == null) return;
        Bounds size = textRenderer.localBounds;
        float pad = text.characterSize * 12f;
        plate.localPosition = new Vector3(size.center.x - extraLeft / 2f, size.center.y, 0.005f);
        plate.localScale = new Vector3(size.size.x + pad * 1.4f + extraLeft, size.size.y + pad, 0.002f);
    }

    // TextMesh can't wrap long lines by itself, so we break them on spaces.
    public static string Wrap(string value, int maxChars)
    {
        StringBuilder result = new StringBuilder();
        foreach (string paragraph in value.Split('\n'))
        {
            if (result.Length > 0) result.Append('\n');
            int lineLength = 0;
            foreach (string word in paragraph.Split(' '))
            {
                if (lineLength > 0 && lineLength + 1 + word.Length > maxChars)
                {
                    result.Append('\n');
                    lineLength = 0;
                }
                else if (lineLength > 0)
                {
                    result.Append(' ');
                    lineLength++;
                }
                result.Append(word);
                lineLength += word.Length;
            }
        }
        return result.ToString();
    }
}
