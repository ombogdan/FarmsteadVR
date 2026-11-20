using UnityEngine;

// The player's invisible body for collisions: a capsule from knee height up to the head.
// Walking (Mac and VR) asks it "can I move there?", teleport asks "can I stand there?".
public static class PlayerBody
{
    const float Radius = 0.25f;
    const float Bottom = 0.35f; // starts above the ground, so the ground itself never blocks
    const float Top = 1.6f;

    // Is there room to stand with the feet at this point?
    public static bool CanStand(Vector3 feet)
    {
        feet.y = 0f;
        return !Physics.CheckCapsule(feet + Vector3.up * Bottom, feet + Vector3.up * Top, Radius,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    // Keeps the player on the map (the ground is 70 x 70 m).
    public static Vector3 ClampToFarm(Vector3 position)
    {
        return new Vector3(Mathf.Clamp(position.x, -33f, 33f), position.y, Mathf.Clamp(position.z, -28f, 38f));
    }

    // How much of the wanted step is allowed. Slides along walls instead of stopping dead.
    public static Vector3 Move(Vector3 feet, Vector3 step)
    {
        step.y = 0f;
        if (!CanStand(feet)) return step; // already stuck inside something: let the player walk out
        if (CanStand(feet + step)) return step;
        Vector3 alongX = new Vector3(step.x, 0f, 0f);
        if (CanStand(feet + alongX)) return alongX;
        Vector3 alongZ = new Vector3(0f, 0f, step.z);
        if (CanStand(feet + alongZ)) return alongZ;
        return Vector3.zero;
    }
}
