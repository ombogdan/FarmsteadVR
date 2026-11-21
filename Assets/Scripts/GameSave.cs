using UnityEngine;

// Remembers the game between sessions (on the Quest too), so next time you continue where you left off.
// Saved with PlayerPrefs as a small JSON text. "New game" deletes it.
[System.Serializable]
public class GameSave
{
    const string Key = "FarmsteadVR.Save.v1";

    const string BackupKey = Key + ".backup";

    public long savedAt;
    public int introPage;
    public bool hasFeet;
    public Vector3 feet;
    public float yaw;
    public int stage;
    public int coins;
    public int hensBought;
    public string[] itemIds = new string[0];
    public int[] itemCounts = new int[0];
    public string[] usedHints = new string[0];
    public int day = 1;
    public float time01 = 0.3f;
    public int[] dailyEggs;
    public float[] laySeconds;
    public float firstEggSeconds = 12f;
    public int foxNightChecked = -1;
    public string foxNews;
    public int[] cowPetted, goatPetted;
    public int routeSentDay;
    public bool bigHungry;
    public bool[] goldenNests = new bool[0];
    public bool[] nests = new bool[0];
    // stage 5 (FarmPrototype.Chores.cs)
    public float feederKg = 4f;              // grain in the feeder (8 kg fit; each hen eats 1 kg a day)
    public float storageKg = 100f;           // grain in the storage shed (grandpa left 100 kg)
    public int hungryDays;                   // mornings in a row the hens didn't get enough
    public bool[] henGone = new bool[0];     // hens that died of hunger
    public int flightPaid, racePaid, galleryPaid, rushPaid; // rounds already paid on the saved day
    public int flightBest;                   // the best score in "Курячий політ"
    public int galleryBest;                  // the best score at "Тир Маркіза"
    public int rushBest;                     // the best score in "Яєчний переполох"
    public bool cheeseBuilt;                 // the cheese house (FarmPrototype.Cheese.cs)
    public bool kitchenBuilt;                // the kitchen in the house (FarmPrototype.Kitchen.cs)
    public string cooking = "";             // the dish in the pot
    public float cookProgress;
    public bool coopDoorOpen = true;         // the coop door (shut it at night: a fox)
    public bool feederUpgraded, barnUpgraded; // upgrades bought from Marquis
    public int cheeseBatch;                  // what is cooking: 0 nothing, 1 cow cheese, 2 goat cheese
    public float cheeseProgress;
    public int canWater = 4;                 // water in the watering can (beds it can still water)
    public float barnHayKg = 6f;             // hay in the barn trough (cows and goats eat it)
    public float storageHayKg = 30f;         // hay in the storage shed (grandpa left 30 kg)
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
        return Read(PlayerPrefs.GetString(Key), out save) || Read(PlayerPrefs.GetString(BackupKey), out save);
    }

    static bool Read(string json, out GameSave save)
    {
        save = null;
        if (string.IsNullOrEmpty(json) || !json.Contains("\"stage\"")) return false;
        try
        {
            var candidate = JsonUtility.FromJson<GameSave>(json);
            if (candidate == null || candidate.stage < 0 || candidate.stage > FarmPrototype.LastSaveStage ||
                candidate.day < 1 || candidate.savedAt < 0 || candidate.savedAt > 253402300799L ||
                candidate.itemIds == null || candidate.itemCounts == null ||
                candidate.itemIds.Length != candidate.itemCounts.Length || candidate.usedHints == null ||
                candidate.nests == null) return false;
            save = candidate;
            return true;
        }
        catch (System.ArgumentException) { return false; }
    }

    public void Store(bool newGame = false)
    {
        savedAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string json = JsonUtility.ToJson(this);
        string previous = PlayerPrefs.GetString(Key);
        if (newGame) PlayerPrefs.SetString(BackupKey, json);
        else if (Read(previous, out _)) PlayerPrefs.SetString(BackupKey, previous);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.DeleteKey(BackupKey);
        PlayerPrefs.Save();
    }
}
