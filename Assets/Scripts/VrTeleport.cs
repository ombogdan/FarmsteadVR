using UnityEngine;

// Teleport, like in many VR games:
// push the right stick forward -> an arc comes out of your right hand,
// footprints show where you'll land (blue = OK, red = can't stand there);
// let go of the stick -> you jump there.
// VrPlayer adds this script and calls Init.
public class VrTeleport : MonoBehaviour
{
    const int ArcPoints = 40;
    const float LaunchSpeed = 8f;      // how far the arc reaches (about 6-7 m on flat ground)
    const float TimeStep = 0.035f;
    const float MaxGroundHeight = 0.3f; // land only on the ground, not on roofs or the goat

    VrPlayer player;
    Transform hand;
    Transform[] dots;
    Renderer[] dotRenderers;
    Transform footprints;
    Renderer[] footRenderers;
    Material good, bad;
    bool aiming;
    bool valid;
    bool lastValid = true;
    Vector3 target;

    public bool IsAiming => aiming;

    public void Init(VrPlayer player, Transform hand)
    {
        this.player = player;
        this.hand = hand;
        good = VrShapes.LoadMaterial("Teleport");
        bad = VrShapes.LoadMaterial("Toolbox");

        dots = new Transform[ArcPoints];
        dotRenderers = new Renderer[ArcPoints];
        for (int i = 0; i < ArcPoints; i++)
        {
            dots[i] = VrShapes.Create(PrimitiveType.Sphere, "Arc Dot", null, "Teleport").transform;
            dots[i].localScale = Vector3.one * 0.03f;
            dotRenderers[i] = dots[i].GetComponent<Renderer>();
        }

        // Two flat "soles" side by side: the footprints of where you will stand.
        footprints = new GameObject("Teleport Footprints").transform;
        footRenderers = new Renderer[2];
        for (int side = 0; side < 2; side++)
        {
            Transform foot = VrShapes.Create(PrimitiveType.Sphere, "Foot", footprints, "Teleport").transform;
            foot.localPosition = new Vector3(side == 0 ? -0.1f : 0.1f, 0f, side == 0 ? 0f : 0.04f);
            foot.localScale = new Vector3(0.11f, 0.015f, 0.26f);
            footRenderers[side] = foot.GetComponent<Renderer>();
        }
        Show(false, 0);
    }

    void Update()
    {
        if (hand == null) return;
        Vector2 stick = FarmInput.RightStick();

        if (!aiming && stick.y > 0.6f && Mathf.Abs(stick.x) < 0.6f) aiming = true;
        if (!aiming) return;

        if (stick.magnitude < 0.3f) // stick released: jump (if the spot is good)
        {
            aiming = false;
            Show(false, 0);
            if (valid)
            {
                player.TeleportTo(target);
                FarmAudio.Play2D("whoosh", 0.7f);
            }
            return;
        }
        Aim();
    }

    // Throw an imaginary ball from the hand and follow its arc until it hits something.
    void Aim()
    {
        Vector3 position = hand.position;
        Vector3 velocity = hand.forward * LaunchSpeed;
        bool landed = false;
        int used = 0;
        valid = false;

        for (int i = 0; i < ArcPoints; i++)
        {
            Vector3 next = position + velocity * TimeStep;
            velocity += Physics.gravity * TimeStep;
            if (Physics.Linecast(position, next, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                target = hit.point;
                valid = hit.normal.y > 0.7f && hit.point.y < MaxGroundHeight && PlayerBody.CanStand(hit.point);
                dots[i].position = hit.point;
                used = i + 1;
                landed = true;
                break;
            }
            dots[i].position = next;
            used = i + 1;
            position = next;
        }

        Show(true, used);
        footprints.gameObject.SetActive(landed);
        if (landed)
        {
            Vector3 facing = Vector3.ProjectOnPlane(hand.forward, Vector3.up);
            if (facing.sqrMagnitude < 0.001f) facing = Vector3.forward;
            footprints.position = target + Vector3.up * 0.01f;
            footprints.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        }

        if (valid != lastValid)
        {
            lastValid = valid;
            Material material = valid ? good : bad;
            foreach (Renderer r in dotRenderers) r.sharedMaterial = material;
            foreach (Renderer r in footRenderers) r.sharedMaterial = material;
        }
    }

    void Show(bool visible, int dotCount)
    {
        for (int i = 0; i < ArcPoints; i++) dots[i].gameObject.SetActive(visible && i < dotCount);
        if (!visible) footprints.gameObject.SetActive(false);
    }
}
