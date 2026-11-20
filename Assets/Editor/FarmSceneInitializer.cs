using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs in the editor after scripts compile. It makes sure the colors (materials) exist and that
// the scene contains the current version of the farm (built by FarmWorldBuilder).
// When FarmPrototype.WorldVersion goes up, the farm in the scene is rebuilt automatically.
[InitializeOnLoad]
public static class FarmSceneInitializer
{
    const string ScenePath = "Assets/Scenes/FarmPrototype.unity";
    const string MaterialFolder = "Assets/Resources/FarmMaterials";

    static readonly string[] names =
    {
        "Grass", "Dirt", "Barn", "House", "Roof", "Wood", "DarkWood",
        "FreshWood", "Glass", "Goat", "Nose", "Horn", "Eyes", "Toolbox", "Leaves",
        "ToolboxRed", "Metal", "Yellow", "Teleport",
        "Glow", "Night", "Paper", "Asphalt", "Paint", "Bus", "Pine",
        "GlowDim", "Fade",
        "Star", "SunDisc", "MoonDisc", "Straw", "Egg", "Ginger"
    };

    static readonly string[] colors =
    {
        "#78A65B", "#AC805C", "#9D4F45", "#E3C792", "#70433F", "#A77D53", "#654A38",
        "#D49A61", "#8BC1C7", "#E4D7B8", "#B78D81", "#E4D7A6", "#292829", "#B75C4B", "#4F8A50",
        "#C8392B", "#A3ABB3", "#E9B730", "#6FD3E6",
        "#FF8A1E", "#141826", "#F3E9D2", "#4B4D52", "#F2F2F2", "#F2C230", "#2F6B3A",
        "#B8641C", "#000000",
        "#FFFFFF", "#FFE27A", "#E8EEFF", "#E8C870", "#F5EBDD", "#E58A3A"
    };

    // These shine by themselves (the orange "pick me up" circles, the sun, the moon, the stars),
    // and how strongly.
    static readonly string[] glowing = { "Glow", "GlowDim", "Star", "SunDisc", "MoonDisc" };
    static readonly float[] glowStrength = { 1.4f, 0.35f, 1.2f, 1.5f, 0.9f };

    static FarmSceneInitializer()
    {
        EditorApplication.delayCall += EnsureScene;
    }

    [MenuItem("Farm Prototype/Rebuild Scene")]
    public static void RebuildScene()
    {
        CreateMaterials();
        RebuildFarm();
    }

    // The game remembers progress between Play sessions; this starts it from the very beginning.
    [MenuItem("Farm Prototype/Reset Save (start over)")]
    public static void ResetSave()
    {
        GameSave.Delete();
        Debug.Log("[Farm] Save deleted: the next Play starts with the intro.");
    }

    static void EnsureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(ScenePath)) return;
        CreateMaterials();

        string marker = "m_Name: " + FarmPrototype.WorldVersionMarker + FarmPrototype.WorldVersion;
        if (File.ReadAllText(ScenePath).Contains(marker)) return;
        Debug.Log("[Farm] Scene is older than world version " + FarmPrototype.WorldVersion + ", rebuilding the farm.");
        RebuildFarm();
    }

    static void RebuildFarm()
    {
        AssetDatabase.Refresh(); // brand-new materials must be visible to Resources.Load
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
        Scene previous = SceneManager.GetActiveScene();
        if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            FarmPrototype farm = root.GetComponent<FarmPrototype>();
            if (farm == null) continue;
            Transform oldWorld = farm.transform.Find("Farm World");
            if (oldWorld != null) Object.DestroyImmediate(oldWorld.gameObject);
            farm.BuildWorld();
            MarkStatic(farm.transform.Find("Farm World/Static"));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            break;
        }
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true);
    }

    // Things that never move get merged into big chunks when the game is built,
    // so the Quest draws hundreds of simple shapes much faster.
    static void MarkStatic(Transform statics)
    {
        if (statics == null) return;
        foreach (Transform t in statics.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    // The black curtain for fades: the Standard shader in "Fade" mode, so it can be half see-through.
    static void MakeSeeThrough(Material material)
    {
        material.SetFloat("_Mode", 2f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetFloat("_Glossiness", 0f);
        material.renderQueue = 3000;
    }

    static void CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Resources", "FarmMaterials");
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return;

        for (int i = 0; i < names.Length; i++)
        {
            string path = MaterialFolder + "/" + names[i] + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;
            Color color;
            ColorUtility.TryParseHtmlString(colors[i], out color);
            Material material = new Material(shader);
            material.name = names[i];
            material.color = color;
            int glow = System.Array.IndexOf(glowing, names[i]);
            if (glow >= 0)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * glowStrength[glow]);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            if (names[i] == "Fade") MakeSeeThrough(material);
            AssetDatabase.CreateAsset(material, path);
        }
        AssetDatabase.SaveAssets();
        FarmPropsImport.EnsureMaterials(); // the textured materials of the stage 5 props
    }
}
