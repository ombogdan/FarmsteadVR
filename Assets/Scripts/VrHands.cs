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
    Renderer laserRenderer;
    Material laserNormal, laserActive;

    public void Init(Transform left, Transform right, FarmPrototype farm)
    {
        this.farm = farm;
        HandModel(left);
        HandModel(right);

        laserNormal = VrShapes.LoadMaterial("Horn");
        laserActive = VrShapes.LoadMaterial("FreshWood");
        laser = VrShapes.Create(PrimitiveType.Cube, "Laser", right, "Horn").transform;
        laserRenderer = laser.GetComponent<Renderer>();
    }

    // A dark rounded controller body with a light button on top.
    static void HandModel(Transform hand)
    {
        Transform body = VrShapes.Create(PrimitiveType.Sphere, "Controller", hand, "Eyes").transform;
        body.localPosition = new Vector3(0f, -0.02f, -0.05f);
        body.localScale = new Vector3(0.055f, 0.07f, 0.12f);
        Transform button = VrShapes.Create(PrimitiveType.Sphere, "Button", hand, "Horn").transform;
        button.localPosition = new Vector3(0f, 0.015f, -0.04f);
        button.localScale = Vector3.one * 0.022f;
    }

    void LateUpdate()
    {
        if (laser == null || farm == null) return;
        float length = farm.AimHasHit ? farm.AimHitDistance : farm.aimDistance;
        laser.localScale = new Vector3(LaserWidth, LaserWidth, length);
        laser.localPosition = new Vector3(0f, 0f, length / 2f);
        laserRenderer.sharedMaterial = farm.AimedAtSomething ? laserActive : laserNormal;
    }
}
