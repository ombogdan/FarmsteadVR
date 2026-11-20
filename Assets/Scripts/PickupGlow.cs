using UnityEngine;

// The orange circle around things you can pick up or use (like in Arizona Sunshine).
// Normally it's small and dim, so it doesn't get in the way.
// When your laser points at the thing, the circle grows and shines brightly.
public class PickupGlow : MonoBehaviour
{
    public float idleScale = 0.6f;
    public float highlightScale = 1.25f;
    [System.NonSerialized] public bool highlighted; // FarmPrototype sets this

    Vector3 baseScale;
    Renderer[] dashes;
    Material dim, bright;
    bool showingBright;
    float size;

    void Awake()
    {
        baseScale = transform.localScale;
        dashes = GetComponentsInChildren<Renderer>(true);
        dim = VrShapes.LoadMaterial("GlowDim");
        bright = VrShapes.LoadMaterial("Glow");
        size = idleScale;
        Paint(false);
    }

    void Update()
    {
        size = Mathf.MoveTowards(size, highlighted ? highlightScale : idleScale, Time.deltaTime * 3f);
        float pulse = highlighted ? 1f + 0.06f * Mathf.Sin(Time.time * 6f) : 1f;
        transform.localScale = baseScale * size * pulse;
        transform.Rotate(0f, (highlighted ? 60f : 15f) * Time.deltaTime, 0f, Space.Self);
        if (highlighted != showingBright) Paint(highlighted);
    }

    void Paint(bool on)
    {
        showingBright = on;
        Material material = on ? bright : dim;
        if (material == null) return;
        foreach (Renderer dash in dashes) dash.sharedMaterial = material;
    }
}
