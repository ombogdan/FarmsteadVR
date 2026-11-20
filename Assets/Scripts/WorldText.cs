using UnityEngine;

// Unity's simple 3D text is drawn on top of everything and from both sides.
// For a sign in the world that looks wrong from behind, so we hide the text
// when you stand behind the sign or far away.
public class WorldText : MonoBehaviour
{
    public float maxDistance = 40f;
    Renderer textRenderer;

    void Awake()
    {
        textRenderer = GetComponent<Renderer>();
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || textRenderer == null) return;
        Vector3 toCamera = cam.transform.position - transform.position;
        // The text reads correctly from its -Z side.
        textRenderer.enabled = toCamera.magnitude < maxDistance && Vector3.Dot(toCamera, -transform.forward) > 0f;
    }
}
