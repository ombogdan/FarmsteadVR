using UnityEngine;

// Simple controller "hands", so you can see where your hands are,
// and a laser from the right hand: point it at something and press the trigger (or grip, or A).
// The laser turns orange when it points at something you can use.
// VrPlayer adds this script and calls Init.
public class VrHands : MonoBehaviour
{
    const float LaserWidth = 0.005f;

    FarmPrototype farm;
    Transform laser;
    Transform leftHand, rightHand;
    TextPlate leftGuide, rightGuide;
    Renderer laserRenderer;
    Material laserNormal, laserActive;

    public void Init(Transform left, Transform right, FarmPrototype farm)
    {
        this.farm = farm;
        leftHand = left;
        rightHand = right;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        HandModel(left, -1, font);
        HandModel(right, 1, font);
        leftGuide = Guide(left, font);
        rightGuide = Guide(right, font);

        laserNormal = VrShapes.LoadMaterial("Horn");
        laserActive = VrShapes.LoadMaterial("FreshWood");
        laser = VrShapes.Create(PrimitiveType.Cube, "Laser", right, "Horn").transform;
        laserRenderer = laser.GetComponent<Renderer>();
    }

    // Coordinates are in metres in the tracked pointer frame. Mirror the thumb controls
    // across the hands and keep them behind the ray origin.
    static void HandModel(Transform hand, int side, Font font)
    {
        Transform handle = Part(hand, "Grip", 0f, -0.048f, -0.060f, 0.037f, 0.117f, 0.044f, "Paper");
        handle.localRotation = Quaternion.Euler(22f, 0f, -side * 7f);
        Part(hand, "Shell", 0f, -0.001f, -0.043f, 0.073f, 0.045f, 0.086f, "Paper");
        Part(hand, "Thumb Deck", 0f, 0.012f, -0.043f, 0.067f, 0.025f, 0.078f, "Eyes");
        Part(hand, "Trigger", 0f, -0.019f, -0.010f, 0.030f, 0.036f, 0.020f, "Metal");
        Part(hand, "Grip Button", -side * 0.017f, -0.043f, -0.049f, 0.009f, 0.034f, 0.024f, "Metal");
        Part(hand, "Stick Stem", -side * 0.012f, 0.026f, -0.025f, 0.010f, 0.012f, 0.010f, "Metal");
        Part(hand, "Stick Cap", -side * 0.012f, 0.032f, -0.025f, 0.022f, 0.008f, 0.022f, "Eyes");
        FaceButton(hand, font, side < 0 ? "X" : "A", side * 0.006f, -0.061f);
        FaceButton(hand, font, side < 0 ? "Y" : "B", side * 0.019f, -0.044f);
    }

    static Transform Part(Transform parent, string name, float x, float y, float z, float w, float h, float d, string material)
    {
        Transform part = VrShapes.Create(PrimitiveType.Sphere, name, parent, material).transform;
        part.localPosition = new Vector3(x, y, z);
        part.localScale = new Vector3(w, h, d);
        return part;
    }

    static void FaceButton(Transform hand, Font font, string letter, float x, float z)
    {
        Part(hand, letter, x, 0.024f, z, 0.014f, 0.008f, 0.014f, "Metal");
        TextPlate mark = new TextPlate("Button " + letter, font, 48, 0.0021f, TextAlignment.Center, null, new Color(0.05f, 0.08f, 0.1f));
        mark.Root.SetParent(hand, false);
        mark.Root.localPosition = new Vector3(x, 0.0282f, z);
        mark.Root.localRotation = Quaternion.Euler(90f, 0f, 0f);
        mark.SetText(letter);
    }

    static TextPlate Guide(Transform hand, Font font)
    {
        var guide = new TextPlate("Controller Guide", font, 48, 0.0025f, TextAlignment.Left);
        guide.Root.SetParent(hand, false);
        guide.Hide();
        return guide;
    }

    void UpdateGuide(Transform hand, TextPlate guide, int side)
    {
        Vector3 toHand = hand.position - transform.position;
        // Look down at the face of a controller to read its legend. Hide it when aiming ahead.
        if (toHand.sqrMagnitude > 0.9f * 0.9f || toHand.sqrMagnitude < 0.01f ||
            Vector3.Dot(transform.forward, toHand.normalized) < 0.87f ||
            Vector3.Dot(hand.up, -toHand.normalized) < 0.15f)
        {
            guide.Hide();
            return;
        }
        string text;
        if (side < 0) text = "X   Меню\nY   Рюкзак\nСтік   Ходити";
        else if (farm.AtMainMenu) text = "A   Обрати";
        else if (farm.InMinigame) text = "A   Дія\nB   Вийти з раунду";
        else text = "A   Дія\nB   Завдання\nСтік ↔   Поворот\nСтік ↑   Телепорт";
        Vector3 anchor = hand.position + transform.right * (side * 0.097f) + Vector3.up * 0.032f;
        guide.Show(text, anchor, transform);
    }

    void LateUpdate()
    {
        if (laser == null || farm == null) return;
        UpdateGuide(leftHand, leftGuide, -1);
        UpdateGuide(rightHand, rightGuide, 1);
        float length = farm.AimHasHit ? farm.AimHitDistance : farm.aimDistance;
        laser.localScale = new Vector3(LaserWidth, LaserWidth, length);
        laser.localPosition = new Vector3(0f, 0f, length / 2f);
        laserRenderer.sharedMaterial = farm.AimedAtSomething ? laserActive : laserNormal;
    }
}
