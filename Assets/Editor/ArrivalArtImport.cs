using UnityEditor;
using UnityEngine;

public class ArrivalArtImport : AssetPostprocessor
{
    const string Root = "Assets/Resources/ArrivalArt/";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (ModelImporter)assetImporter;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importNormals = ModelImporterNormals.Calculate;
        importer.normalSmoothingAngle = 55f;
        importer.isReadable = false;
    }

    public static void Bake()
    {
        foreach (string bird in new[] { "Speckled", "White", "Copper", "Black", "Golden", "Buff", "Zhora" })
            Make(bird + "Plumage", Color.white, 0.19f, bird + "Plumage.png");
        Make("CoachEnamel", Color.white, 0.48f, "EnamelColor.png");
        Make("CoachIvory", new Color(.82f, .77f, .63f), .36f);
        Make("CoachRubber", new Color(.098f, .106f, .104f), .24f);
        Make("CoachDash", new Color(.18f, .215f, .20f), .30f);
        Make("CoachMetal", new Color(.54f, .58f, .56f), .67f, metallic: .48f);
        Make("CoachLens", new Color(.90f, .84f, .65f), .58f);
        Make("CoachRedLens", new Color(.52f, .091f, .059f), .62f);
        Make("CoachAmberLens", new Color(.88f, .35f, .095f), .62f);
        Make("CoachVinyl", Color.white, .21f, "SeatVinyl.png");
        Make("DriverCoat", new Color(.195f, .263f, .235f), .12f);
        Make("DriverSkin", new Color(.69f, .48f, .35f), .30f);
        Make("LetterPaper", Color.white, 0, "LetterPaper.png", "Unlit/Texture");
        Make("FamilyPhoto", Color.white, 0, "FamilyPhoto.jpg", "Unlit/Texture");
        Make("LetterBacking", new Color(.222f, .123f, .071f), .15f);
        Make("WindowPlaster", Color.white, .085f, "PlasterInterior.png");
        Make("Linen", new Color(.82f,.775f,.67f), .1f);
        Make("LampLinen", new Color(.91f,.68f,.38f), .1f);
        Make("QuiltFabric", Color.white, .105f, "QuiltTextile.png");
        Make("Brass", new Color(.64f, .42f, .19f), .55f, metallic: .28f);
        var glass = Make("CoachGlass", new Color(.39f,.57f,.57f,.22f), .8f);
        glass.SetFloat("_Mode", 2);
        glass.SetOverrideTag("RenderType", "Transparent");
        glass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glass.SetInt("_ZWrite", 0);
        glass.EnableKeyword("_ALPHABLEND_ON");
        glass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        AssetDatabase.SaveAssets();
        FarmSceneInitializer.RebuildScene();
    }

    static Material Make(string name, Color color, float smoothness, string texture = null, string shader = "Standard", float metallic = 0)
    {
        string path = Root + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_Color")) material.color = color;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (texture != null) material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture>(Root + texture);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }
}
