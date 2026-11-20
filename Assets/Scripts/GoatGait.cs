using UnityEngine;

// Boris's walk. Driven by the distance he actually walks (Grazer moves him), so his legs stop
// when he stops to graze and never "slide" over the grass.
// Legs go in diagonal pairs, like a real goat: front-left with hind-right, then the other two.
public class GoatGait : MonoBehaviour
{
    public Transform body;   // bobs up and down a little with each step
    public Transform[] legs; // front left, front right, hind left, hind right; each swings at its top

    const float StepsPerMeter = 9f; // ~0.7 m per full stride
    const float Swing = 24f;        // degrees forward/back
    Vector3 previous;
    float phase, weight;

    void OnEnable()
    {
        previous = transform.position;
    }

    void LateUpdate()
    {
        Vector3 moved = transform.position - previous;
        previous = transform.position;
        moved.y = 0f;
        float distance = moved.magnitude;
        bool walking = distance > 0.0001f && distance < 0.2f; // a teleport-like jump is not a step
        if (walking) phase += distance * StepsPerMeter;
        weight = Mathf.MoveTowards(weight, walking ? 1f : 0f, Time.deltaTime * 5f);

        for (int i = 0; i < legs.Length; i++)
        {
            bool firstPair = i == 0 || i == 3;
            float angle = Mathf.Sin(phase + (firstPair ? 0f : Mathf.PI)) * Swing * weight;
            legs[i].localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
        if (body != null) body.localPosition = Vector3.up * (0.012f * weight * Mathf.Abs(Mathf.Sin(phase)));
    }
}
