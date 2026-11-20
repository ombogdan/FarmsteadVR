using UnityEditor;
using UnityEngine;

// Our own generated props for the farm chores (stage 5): models and the burlap texture made by
// Tools/ModelGen/generate_props.py, plus their materials. Wood, soil and plaster reuse the CC0
// photo textures from CountryArt, so the new things look like the rest of the farm.
// FarmSceneInitializer calls EnsureMaterials before the farm is built.
public class FarmPropsImport : AssetPostprocessor
{
    const string Root = "Assets/Resources/FarmProps/";
    const string Country = "Assets/Resources/CountryArt/";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (ModelImporter)assetImporter;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importNormals = ModelImporterNormals.Import; // the generator writes smooth normals
        importer.isReadable = false;
    }

    public static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/FarmProps")) return; // the generator hasn't run yet
        foreach (string png in new[] { "Burlap.png", "GoatFur.png" })
            if (AssetDatabase.LoadAssetAtPath<Texture>(Root + png) == null && System.IO.File.Exists(Root + png))
            {
                AssetDatabase.Refresh(); // brand-new files: import them first, so the materials can use them
                break;
            }
        Make("LawnA", new Color(.34f, .5f, .19f), .08f);  // three greens for the lawn inside the fence
        Make("LawnB", new Color(.43f, .58f, .23f), .08f);
        Make("LawnC", new Color(.28f, .43f, .16f), .08f);
        Make("GoatCoat", Color.white, .12f, Root + "GoatFur.png", Vector2.one);
        Make("GoatLegs", new Color(.36f, .26f, .19f), .1f);
        Make("Iron", new Color(.25f, .235f, .22f), .38f, metallic: .55f);
        Make("CarrotSkin", new Color(.95f, .48f, .13f), .3f);
        Make("CarrotTop", new Color(.33f, .56f, .2f), .18f);
        Make("CanPaint", new Color(.24f, .47f, .36f), .55f, metallic: .2f);
        Make("Burlap", Color.white, .05f, Root + "Burlap.png", Vector2.one);
        Make("Grain", new Color(.88f, .74f, .4f), .12f);
        Make("Soil", new Color(.58f, .44f, .33f), .08f, Country + "PathColor.jpg", new Vector2(1f, 2.5f), Country + "PathNormal.jpg", .9f);
        Make("WetSoil", new Color(.36f, .27f, .2f), .42f, Country + "PathColor.jpg", new Vector2(1f, 2.5f), Country + "PathNormal.jpg", .9f);
        Make("FreshLime", new Color(1f, .96f, .87f), .12f, Country + "LimeColor.jpg", new Vector2(3f, 2f), Country + "LimeNormal.jpg", .25f);
        Make("DoorPaint", new Color(.42f, .68f, .66f), .3f, Country + "TimberColor.jpg", Vector2.one, Country + "TimberNormal.jpg", .5f);
        Material lamp = Make("LampGlow", new Color(1f, .82f, .5f), .5f);
        if (!lamp.IsKeywordEnabled("_EMISSION"))
        {
            lamp.EnableKeyword("_EMISSION");
            lamp.SetColor("_EmissionColor", new Color(1f, .82f, .5f) * 1.4f);
            lamp.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(lamp);
        }
        AssetDatabase.SaveAssets();
    }

    // Creates the material once; later runs only fill in a texture that wasn't imported yet the first time.
    static Material Make(string name, Color color, float smoothness, string texture = null, Vector2 tiling = default,
        string normalMap = null, float bumpScale = 1f, float metallic = 0f)
    {
        string path = Root + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool created = material == null;
        if (created)
        {
            material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, path);
        }
        bool changed = created;
        if (texture != null && material.mainTexture == null)
        {
            Texture main = AssetDatabase.LoadAssetAtPath<Texture>(texture);
            if (main != null)
            {
                material.mainTexture = main;
                material.mainTextureScale = tiling == default ? Vector2.one : tiling;
                changed = true;
            }
        }
        if (normalMap != null && material.GetTexture("_BumpMap") == null)
        {
            Texture bump = AssetDatabase.LoadAssetAtPath<Texture>(normalMap);
            if (bump != null)
            {
                material.SetTexture("_BumpMap", bump);
                material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_BumpScale", bumpScale);
                changed = true;
            }
        }
        if (changed) EditorUtility.SetDirty(material);
        return material;
    }
}
