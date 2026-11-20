using UnityEditor;
using UnityEngine;

public class CountryArtImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/CountryArt/")) return;
        var importer = (TextureImporter)assetImporter;
        if (assetPath.EndsWith("Normal.jpg")) importer.textureType = TextureImporterType.NormalMap;
        if (assetPath.EndsWith("Spray.png") || assetPath.EndsWith("Silhouette.png")) {
            importer.alphaIsTransparency = true;
            importer.mipMapsPreserveCoverage = true;
        }
        importer.anisoLevel = 4;
    }

    public static void BakeMaterials()
    {
        const string folder = "Assets/Resources/CountryArt/";
        foreach (var name in new[] {"Meadow","Path","Boards","Tile","Lime","Timber","Bark","Road","Barrel","FirSpray","LeafSpray","Sky","WildflowerSpray","WildGrassSpray","HedgeSpray","OakSilhouette","PineSilhouette","UnderbrushSilhouette","GrassBlade","HillGrass","Gold","Seed","Coat","HornKeratin","Hoof","PaintedBarn","FenceRail"})
        {
            string path = folder + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) {
                mat = new Material(Shader.Find(name == "Sky" ? "Skybox/Panoramic" : "Standard"));
                AssetDatabase.CreateAsset(mat,path);
            }
            if (name == "Sky") {
                mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture>(folder + "Sky.hdr"));
                mat.SetFloat("_Exposure",0.85f);
            } else if(name.EndsWith("Spray") || name.EndsWith("Silhouette")) {
                mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture>(folder + name + ".png");
                mat.SetFloat("_Mode",1); mat.SetFloat("_Cutoff",0.38f);
                mat.SetOverrideTag("RenderType","TransparentCutout"); mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue=2450;
            } else if(name=="Gold") mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture>(folder+"PetalPigment.png");
            else if(name=="Seed") mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture>(folder+"Florets.png");
            else if(name=="HillGrass") mat.color=new Color(.34f,.41f,.25f);
            else if(name=="Coat") mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture>(folder+"BorisFur.png");
            else if(name=="HornKeratin") mat.color=new Color(.48f,.38f,.25f);
            else if(name=="Hoof") mat.color=new Color(.15f,.12f,.10f);
            else if(name == "GrassBlade") mat.color = new Color(.35f,.43f,.19f);
            else {
                string source = name=="PaintedBarn" ? "Boards" : name=="FenceRail" ? "Timber" : name;
                mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture>(folder+source+"Color.jpg");
                mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture>(folder+source+"Normal.jpg"));
                mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale",.72f);
                mat.color = name=="PaintedBarn" ? new Color(.73f,.42f,.34f) : name=="Meadow" ? new Color(.65f,.78f,.58f) : name=="Path" ? new Color(.79f,.71f,.58f) : Color.white;
                Vector2 tile = Vector2.one;
                switch(name) {
                    case "Meadow": tile=new Vector2(95,95); break;
                    case "Path": tile=new Vector2(1,4);break;
                    case "Road": tile=new Vector2(16,2);break;
                    case "PaintedBarn":tile=new Vector2(4,2);break;
                    case "Lime":tile=new Vector2(3,2);break;
                    case "Tile":tile=new Vector2(4,4);break;
                }
                mat.mainTextureScale=tile;
            }
            if(name=="Gold" || name=="Seed") mat.color=Color.white;
            if (name!="Sky") {mat.SetFloat("_Glossiness",.14f);mat.enableInstancing=true;}
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
        FarmSceneInitializer.RebuildScene();
    }
}
