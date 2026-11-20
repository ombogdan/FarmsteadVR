using UnityEngine;

// Day and night. The first 3/4 of the cycle is day (the sun crosses the sky), the last 1/4 is night
// (the same light becomes a dim blue moon). The sky is a flat cartoon color that follows the time:
// blue by day, orange at sunrise and sunset, dark blue at night with stars.
// FarmPrototype starts the clock (running) in stage 3; before that it's always a sunny afternoon.
public class DayNight : MonoBehaviour
{
    public float cycleSeconds = 360f;                   // a whole day + night: 6 minutes
    [System.NonSerialized] public bool running;
    [System.NonSerialized] public float time01 = 0.3f;  // 0 = sunrise, 0.75 = sunset, 1 = next sunrise
    [System.NonSerialized] public int day = 1;
    public System.Action onMorning, onEvening;

    static readonly Color DaySky = new Color(0.67f, 0.82f, 0.94f);
    static readonly Color SunsetSky = new Color(0.98f, 0.62f, 0.42f);
    static readonly Color NightSky = new Color(0.05f, 0.07f, 0.16f);
    static readonly Color DayAmbient = new Color(0.65f, 0.73f, 0.78f);
    static readonly Color NightAmbient = new Color(0.16f, 0.18f, 0.3f);
    const float SunYaw = -35f;

    Material movingSky;
    Material previousSky;
    void OnDestroy()
    {
        if (movingSky == null) return;
        if (RenderSettings.skybox == movingSky) RenderSettings.skybox = previousSky;
        Destroy(movingSky);
    }
    Light sun;
    Camera cam;
    Transform sky, sunDisc, moonDisc, stars;
    bool wasNight;

    public bool IsNight => time01 >= 0.75f;
    public string PartOfDay => time01 < 0.12f ? "ранок" : time01 < 0.55f ? "день" : time01 < 0.75f ? "вечір" : "ніч";

    public void Init(Light sunLight, Camera camera)
    {
        sun = sunLight;
        cam = camera;
        previousSky = RenderSettings.skybox;
        Material panorama = Resources.Load<Material>("CountryArt/Sky");
        if (panorama != null) { movingSky = new Material(panorama); RenderSettings.skybox = movingSky; }
        cam.clearFlags = movingSky == null ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox; // our own sky color instead of the default sky
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.sun = sun;

        sky = new GameObject("Sky").transform;
        sunDisc = VrShapes.Create(PrimitiveType.Sphere, "Sun", sky, "SunDisc").transform;
        sunDisc.localScale = Vector3.one * 14f;
        moonDisc = VrShapes.Create(PrimitiveType.Sphere, "Moon", sky, "MoonDisc").transform;
        moonDisc.localScale = Vector3.one * 9f;
        stars = new GameObject("Stars").transform;
        stars.SetParent(sky, false);
        System.Random random = new System.Random(11);
        for (int i = 0; i < 70; i++)
        {
            Vector3 direction = new Vector3((float)random.NextDouble() * 2f - 1f, 0.15f + (float)random.NextDouble(), (float)random.NextDouble() * 2f - 1f).normalized;
            Transform star = VrShapes.Create(PrimitiveType.Cube, "Star", stars, "Star").transform;
            star.localPosition = direction * 280f;
            star.localScale = Vector3.one * (0.9f + (float)random.NextDouble() * 0.8f);
        }
        wasNight = IsNight;
        Apply();
    }

    // Used when a saved game is loaded: jumps to that time without firing "evening"/"morning".
    public void SetTime(float time, int dayNumber)
    {
        time01 = time;
        day = dayNumber;
        wasNight = IsNight;
        Apply();
    }

    // Used by "sleep until morning".
    public void SkipToMorning()
    {
        time01 = 0.01f;
        day++;
        wasNight = false;
        Apply();
        if (onMorning != null) onMorning();
    }

    void Update()
    {
        if (sun == null) return;
        if (running)
        {
            time01 += Time.deltaTime / cycleSeconds;
            if (time01 >= 1f)
            {
                time01 -= 1f;
                day++;
                if (onMorning != null) onMorning();
            }
        }
        bool night = IsNight;
        if (night && !wasNight && onEvening != null) onEvening();
        wasNight = night;
        Apply();
    }

    void Apply()
    {
        // The path angle: 0..180 degrees during the day, 180..360 at night.
        float angle = time01 < 0.75f ? time01 / 0.75f * 180f : 180f + (time01 - 0.75f) / 0.25f * 180f;
        float sunHeight = Mathf.Sin(angle * Mathf.Deg2Rad); // > 0 day, < 0 night
        float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.05f, 0.3f, sunHeight));
        float horizonGlow = Mathf.Clamp01(1f - Mathf.Abs(sunHeight) * 5f); // strongest at sunrise and sunset

        if (sunHeight >= 0f)
        {
            sun.transform.rotation = Quaternion.Euler(angle, SunYaw, 0f);
            sun.intensity = 1.25f * Mathf.SmoothStep(0f, 1f, sunHeight * 4f);
            sun.color = Color.Lerp(new Color(1f, 0.55f, 0.3f), new Color(1f, 0.9f, 0.74f), Mathf.Clamp01(sunHeight * 2.5f));
        }
        else // the moon: the same light, from the other side, dim and blue
        {
            sun.transform.rotation = Quaternion.Euler(angle - 180f, SunYaw, 0f);
            sun.intensity = 0.35f * Mathf.SmoothStep(0f, 1f, -sunHeight * 4f);
            sun.color = new Color(0.6f, 0.7f, 1f);
        }

        RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, daylight) + new Color(0.15f, 0.08f, 0.05f) * horizonGlow;
        Color skyColor = Color.Lerp(NightSky, DaySky, daylight);
        if (movingSky != null) {
            movingSky.SetFloat("_Exposure", Mathf.Lerp(.025f,.85f,daylight));
            movingSky.SetColor("_Tint", Color.Lerp(new Color(.20f,.28f,.48f), Color.Lerp(Color.gray,new Color(.65f,.43f,.35f),horizonGlow),daylight));
        }
        cam.backgroundColor = Color.Lerp(skyColor, SunsetSky, horizonGlow * 0.6f);

        // Sun, moon and stars follow the camera, so they always look infinitely far away.
        sky.position = cam.transform.position;
        Vector3 toSun = -(Quaternion.Euler(angle, SunYaw, 0f) * Vector3.forward);
        sunDisc.localPosition = toSun * 250f;
        moonDisc.localPosition = -toSun * 250f;
        sunDisc.gameObject.SetActive(sunHeight > -0.1f);
        moonDisc.gameObject.SetActive(sunHeight < 0.1f);
        stars.gameObject.SetActive(daylight < 0.35f);
        FarmAudio.NightQuiet = daylight < 0.3f; // birds go quiet in the dark
    }
}
