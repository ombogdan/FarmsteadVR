using System.Collections;
using UnityEngine;

// A black "curtain" right in front of the eyes, for cartoon-like scene changes:
// fade to black, move the player somewhere else, fade back in.
// Works the same on the Mac and in the headset (it's attached to the camera).
public class ScreenFade : MonoBehaviour
{
    Renderer curtain;
    Material material;
    float alpha;

    public static ScreenFade On(Camera camera)
    {
        ScreenFade fade = camera.GetComponent<ScreenFade>();
        if (fade == null) fade = camera.gameObject.AddComponent<ScreenFade>();
        return fade;
    }

    void Awake()
    {
        if (VrShapes.LoadMaterial("Fade") == null) return; // without the material, fades are skipped
        Transform plate = VrShapes.Create(PrimitiveType.Cube, "Fade Curtain", transform, "Fade").transform;
        plate.localPosition = new Vector3(0f, 0f, 0.12f);
        plate.localRotation = Quaternion.identity;
        plate.localScale = new Vector3(3f, 3f, 0.001f); // wide enough to cover everything you can see
        curtain = plate.GetComponent<Renderer>();
        material = curtain.material;   // our own copy, so changing it doesn't touch the saved material
        material.renderQueue = 4000;   // drawn after everything else
        Set(0f);
    }

    public void Set(float value)
    {
        alpha = value;
        if (material == null) return;
        Color color = material.color;
        color.a = value;
        material.color = color;
        curtain.enabled = value > 0.001f;
    }

    public IEnumerator FadeTo(float target, float seconds)
    {
        float start = alpha;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            Set(Mathf.Lerp(start, target, t / seconds));
            yield return null;
        }
        Set(target);
    }
}
