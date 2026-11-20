using UnityEngine;

// Remembers the game between sessions (on the Quest too), so next time you continue where you left off.
// Saved with PlayerPrefs as a small JSON text. "New game" deletes it.
[System.Serializable]
public class GameSave
{
    const string Key = "FarmsteadVR.Save.v1";

    public int stage;
    public int coins;
    public int hensBought;
    public string[] itemIds = new string[0];
    public int[] itemCounts = new int[0];
    public string[] usedHints = new string[0];
    public int day = 1;
    public float time01 = 0.3f;
    public bool[] nests = new bool[0];
    // stage 5 (FarmPrototype.Chores.cs)
    public int fedDay;                       // the day the hens were last fed (0 = never)
    public bool coopUpgraded;                // the coop has its extra nests (more hens fit)
    public int cowsBought, goatsBought;      // the big animals (FarmPrototype.Barn.cs)
    public int[] cowMilked = new int[0], goatMilked = new int[0]; // the day each one was last milked
    public string[] taskIds = new string[0]; // side jobs (FarmPrototype.Tasks.cs)
    public int[] taskNumbers = new int[0];   // for each: how many needed, done so far, coins
    public int horseshoes;                   // which horseshoes are found: one bit each
    public int[] beds = new int[0];          // each garden bed: weeds / clear / planted / growing / ripe
    public float[] bedGrowth = new float[0]; // 0..1 while growing

    public static bool TryLoad(out GameSave save)
    {
        save = null;
        if (!PlayerPrefs.HasKey(Key)) return false;
        try
        {
            save = JsonUtility.FromJson<GameSave>(PlayerPrefs.GetString(Key));
        }
        catch
        {
            save = null; // a broken save: start a new game instead of crashing
        }
        return save != null;
    }

    public void Store()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
