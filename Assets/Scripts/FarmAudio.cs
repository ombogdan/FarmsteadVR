using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// All game sounds in one place.
//
// Sound files live in Assets/Resources/Sounds. They are made by Tools/SoundGen/generate_sounds.py
// from pure math (no recordings, nobody else's sounds), so there are no copyright problems.
// To replace a sound later, drop a new file with the same name into that folder.
//
//   FarmAudio.Play("coin", position)             3D sound from that spot (in VR you hear where it is)
//   FarmAudio.Play2D("tick")                     a sound "inside your head", for interface clicks
//   FarmAudio.Speak(goat, "goat_voice", text)    cartoon babble while a character talks
//   FarmAudio.StartAmbience(trees)               wind + birds singing in the trees
public class FarmAudio : MonoBehaviour
{
    const int MaxSources = 24;

    // Birds don't sing at night (DayNight switches this).
    public static bool NightQuiet;

    static FarmAudio instance;

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, List<AudioClip>> sets = new Dictionary<string, List<AudioClip>>();
    readonly List<AudioSource> pool = new List<AudioSource>();
    readonly Dictionary<Transform, Coroutine> talking = new Dictionary<Transform, Coroutine>();
    int nextSteal;

    static FarmAudio Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("Farm Audio").AddComponent<FarmAudio>();
            return instance;
        }
    }

    public static void Play(string name, Vector3 position, float volume = 1f, float pitch = 1f, float nearDistance = 2f)
    {
        FarmAudio a = Instance;
        a.PlayClip(a.Clip(name), position, volume, pitch, nearDistance, true);
    }

    public static void Play2D(string name, float volume = 1f, float pitch = 1f)
    {
        FarmAudio a = Instance;
        a.PlayClip(a.Clip(name), Vector3.zero, volume, pitch, 1f, false);
    }

    // A looping sound that moves together with an object (e.g. the bus engine).
    public static AudioSource PlayLoopOn(string name, Transform parent, float volume = 1f, float nearDistance = 3f)
    {
        AudioClip clip = Instance.Clip(name);
        if (clip == null || parent == null) return null;
        AudioSource source = parent.gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = nearDistance;
        source.maxDistance = 80f;
        source.dopplerLevel = 0f;
        source.Play();
        return source;
    }

    // Babble like in cartoon games: short random "syllables" of the character's voice.
    // Longer text = more syllables. firstSound (e.g. "goat_bleat") plays before the babble.
    public static void Speak(Transform speaker, string voice, string text, string firstSound = null)
    {
        if (speaker == null) return;
        FarmAudio a = Instance;
        if (a.talking.TryGetValue(speaker, out Coroutine running) && running != null) a.StopCoroutine(running);
        int syllables = Mathf.Clamp(text.Length / 6, 4, 14);
        a.talking[speaker] = a.StartCoroutine(a.Babble(speaker, voice, syllables, firstSound));
    }

    // Endless soft wind + a bird singing in a random tree every few seconds.
    public static void StartAmbience(IList<Transform> birdTrees)
    {
        FarmAudio a = Instance;
        AudioClip windClip = a.Clip("ambient_wind");
        if (windClip != null)
        {
            AudioSource wind = a.gameObject.AddComponent<AudioSource>();
            wind.clip = windClip;
            wind.loop = true;
            wind.volume = 0.3f;
            wind.spatialBlend = 0f;
            wind.Play();
        }
        a.StartCoroutine(a.Birds(new List<Transform>(birdTrees)));
    }

    IEnumerator Babble(Transform speaker, string voice, int syllables, string firstSound)
    {
        Vector3 mouth = Vector3.up * 1.4f;
        if (firstSound != null)
        {
            PlayClip(Clip(firstSound), speaker.position + mouth, 1f, 1f, 3f, true);
            yield return new WaitForSeconds(0.8f);
        }
        List<AudioClip> set = Set(voice);
        for (int i = 0; i < syllables && set.Count > 0 && speaker != null; i++)
        {
            AudioClip clip = set[Random.Range(0, set.Count)];
            PlayClip(clip, speaker.position + mouth, 0.85f, Random.Range(0.94f, 1.08f), 3f, true);
            yield return new WaitForSeconds(Random.Range(0.085f, 0.11f));
        }
        if (speaker != null) talking.Remove(speaker);
    }

    IEnumerator Birds(List<Transform> trees)
    {
        List<AudioClip> birds = Set("bird");
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2.5f, 7f));
            if (birds.Count == 0 || trees.Count == 0 || NightQuiet) continue;
            Transform tree = trees[Random.Range(0, trees.Count)];
            if (tree == null) continue;
            AudioClip song = birds[Random.Range(0, birds.Count)];
            PlayClip(song, tree.position + Vector3.up * 3.2f, Random.Range(0.5f, 0.8f), Random.Range(0.9f, 1.15f), 6f, true);
        }
    }

    void PlayClip(AudioClip clip, Vector3 position, float volume, float pitch, float nearDistance, bool in3D)
    {
        if (clip == null) return;
        AudioSource source = FreeSource();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = in3D ? 1f : 0f; // 1 = comes from a place in the world, 0 = everywhere
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = nearDistance;    // full loudness this close, then gets quieter
        source.maxDistance = 60f;
        source.dopplerLevel = 0f;             // no pitch bending when you teleport
        source.Play();
    }

    // Reuses finished sound players instead of creating new ones all the time.
    AudioSource FreeSource()
    {
        foreach (AudioSource s in pool)
            if (!s.isPlaying) return s;
        if (pool.Count >= MaxSources)
        {
            nextSteal = (nextSteal + 1) % pool.Count;
            return pool[nextSteal];
        }
        GameObject go = new GameObject("Sound");
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        pool.Add(source);
        return source;
    }

    AudioClip Clip(string name)
    {
        if (clips.TryGetValue(name, out AudioClip clip)) return clip;
        clip = Resources.Load<AudioClip>("Sounds/" + name);
        if (clip == null) Debug.LogWarning("[Farm] Sound not found: Resources/Sounds/" + name);
        clips[name] = clip;
        return clip;
    }

    // All files named like "bird_1", "bird_2", ... as one list.
    List<AudioClip> Set(string prefix)
    {
        if (sets.TryGetValue(prefix, out List<AudioClip> list)) return list;
        list = new List<AudioClip>();
        for (int i = 1; i < 100; i++)
        {
            AudioClip clip = Resources.Load<AudioClip>("Sounds/" + prefix + "_" + i);
            if (clip == null) break;
            list.Add(clip);
        }
        sets[prefix] = list;
        return list;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetBetweenPlaySessions()
    {
        instance = null;
        NightQuiet = false;
    }
}
