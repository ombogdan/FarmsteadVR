using UnityEngine;

// Driven by the actual distance walked, so the feet settle when Grazer stops to peck.
public class ChickenGait : MonoBehaviour
{
    public Transform leftFoot, rightFoot, body;
    Vector3 previous;
    float phase;
    float weight;

    void OnEnable()
    {
        previous = transform.position;
        phase = 0f;
        weight = 0f;
    }

    void LateUpdate()
    {
        Vector3 displacement = transform.position - previous;
        previous = transform.position;
        displacement.y = 0f;
        float distance = displacement.magnitude;
        bool walking = distance > 0.0002f && distance < 0.15f;
        if (walking) phase += distance * 33f;
        weight = Mathf.MoveTowards(weight, walking ? 1f : 0f, Time.deltaTime * 7f);
        AnimateFoot(leftFoot, phase);
        AnimateFoot(rightFoot, phase + Mathf.PI);
        body.localPosition = Vector3.up * (0.009f * weight * Mathf.Abs(Mathf.Sin(phase)));
    }

    void AnimateFoot(Transform foot, float step)
    {
        Vector3 p = foot.localPosition;
        p.y = Mathf.Max(0f, Mathf.Cos(step)) * 0.032f * weight;
        p.z = Mathf.Sin(step) * 0.043f * weight;
        foot.localPosition = p;
        foot.localRotation = Quaternion.Euler(-10f * p.y / 0.032f, 0f, 0f);
    }

    public static Transform Dress(Transform root, bool rooster, string plumage)
    {
        var model = rooster ? "Rooster" : "Hen";
        var body = ArrivalArt.Part(root, model + "Body", plumage);
        var head = ArrivalArt.Group(root, "Head", rooster
            ? new Vector3(0f, 0.49f, -0.15f)
            : new Vector3(0f, 0.365f, -0.12f));
        ArrivalArt.Part(head, model + "Head", plumage);
        var gait = root.gameObject.AddComponent<ChickenGait>();
        gait.body = body;
        gait.leftFoot = MakeFoot(root, -1f, rooster, plumage);
        gait.rightFoot = MakeFoot(root, 1f, rooster, plumage);
        if (rooster) AddBowTie(head);
        return head;
    }

    static Transform MakeFoot(Transform root, float side, bool rooster, string plumage)
    {
        var foot = ArrivalArt.Part(root, "ChickenFoot", plumage);
        foot.localPosition = Vector3.right * side * (rooster ? 0.071f : 0.058f);
        foot.localScale = new Vector3(1f, rooster ? 1.31f : 1f, 1f);
        return foot;
    }

    static void AddBowTie(Transform head)
    {
        Material fabric = ArrivalArt.Material("CoachEnamel");
        foreach (float side in new[] { -1f, 1f })
        {
            var sideOfTie = ArrivalArt.Piece(head, "Bow Tie", PrimitiveType.Sphere,
                new Vector3(side * 0.024f, -0.12f, -0.132f), new Vector3(0.063f, 0.032f, 0.023f), fabric);
            sideOfTie.localRotation = Quaternion.Euler(0f, 0f, side * -15f);
        }
    }
}
