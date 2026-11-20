using UnityEngine;

// Dress the existing buildings and trees; interactive roots keep their original transforms.
public static class CountryFarmArt
{
    static Material Surface(string id) => Resources.Load<Material>("CountryArt/" + id);

    public static void Dress(Transform world)
    {
        var statics = world.Find("Static");
        foreach (var r in world.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r.sharedMaterial) continue;
            string style = null;
            switch(r.sharedMaterial.name) {
                case "Grass":style="Meadow";break;
                case "Dirt":style="Path";break;
                case "Asphalt":style="Road";break;
                case "Barn":style="PaintedBarn";break;
                case "House":style="Lime";break;
                case "Roof":if(r.name.Contains("Roof")) style="Tile";break;
                case "Wood":case "DarkWood":case "FreshWood":style="Timber";break;
            }
            if (style != null && Surface(style) is Material mat) r.sharedMaterial=mat;
        }
        var forest=statics.Find("Forest");
        foreach(Transform tree in forest)
        {
            bool pine=tree.name=="Pine";
            if (!pine && tree.name!="Tree") continue;
            var trunk=tree.Find("Trunk");
            float scale=trunk.localScale.y / (pine ? 1f : 1.25f);
            foreach(var r in tree.GetComponentsInChildren<MeshRenderer>()) r.enabled=false;
            string shape=pine?"Spruce":"Orchard";
            float yaw=Mathf.Repeat(tree.position.x*51f+tree.position.z*19f,360f);
            AddModel(tree,shape+"Wood","Bark",Vector3.zero,scale,yaw);
            AddModel(tree,shape+"Foliage",pine?"FirSpray":"LeafSpray",Vector3.zero,scale,yaw);
        }
        AddModel(statics,"MeadowTufts","GrassBlade",Vector3.zero,1f,0);
        AddModel(statics,"OakCask","Barrel",new Vector3(-4.5f,0,11.95f),1.12f,30);
        AddModel(statics,"OakCask","Barrel",new Vector3(13.96f,0,14.9f),1f,150);
        AddModel(statics,"OakCask","Barrel",new Vector3(-6.6f,0,-8.3f),.88f,70);
        foreach (string path in new[] {"Path to Farm","Farmyard Path"})
            statics.Find(path).GetComponent<MeshRenderer>().enabled=false;
        AddModel(statics,"CartTrack","Path",Vector3.zero,1f,0);
        AddModel(statics,"PastureHills","HillGrass",Vector3.zero,1f,0);
        AddModel(statics,"SunflowerPlants","GrassBlade",Vector3.zero,1f,0);
        AddModel(statics,"RayPetals","Gold",Vector3.zero,1f,0);
        AddModel(statics,"SeedHeads","Seed",Vector3.zero,1f,0);
        foreach(string species in new[]{"Oak","Pine","Underbrush"})
            AddModel(statics,species+"Grove",species+"Silhouette",Vector3.zero,1f,0);
        Light sun=world.Find("Warm Sun").GetComponent<Light>();
        sun.shadows=LightShadows.Soft;
        sun.shadowStrength=.78f;
        var rng = new System.Random(517);
        for (int i=0; i<115; i++) {
            float side = i%2==0 ? 1 : -1;
            float z = -11f + (float)rng.NextDouble()*22f;
            float x = side*(1.75f + (float)rng.NextDouble()*1.0f);
            bool flowers = i%4 == 0;
            AddModel(statics,flowers?"Wildflower":"WildGrass", flowers?"WildflowerSpray":"WildGrassSpray",
                new Vector3(x,0,z), flowers?.63f:2.3f, rng.Next(360));
        }
        for(int i=0;i<15;i++) {
            float x = -7.3f + i*1.62f;
            AddModel(statics,"Hedge","HedgeSpray",new Vector3(x,0,23.1f),.84f,rng.Next(360));
        }
        var house=statics.Find("Old Farmhouse");
        foreach(float x in new[]{-2.2f,2.2f}) {
            foreach(float side in new[]{-1f,1f}) {
                Detail(house,"Window casing",new Vector3(x+side*.61f,2.8f,-3.10f),new Vector3(.1f,1.24f,.12f),"Timber");
                for(int slat=0;slat<8;slat++)
                    Detail(house,"Louver",new Vector3(x+side*.91f,2.34f+slat*.13f,-3.09f),new Vector3(.42f,.11f,.09f),"Boards");
            }
            Detail(house,"Sill",new Vector3(x,2.22f,-3.14f),new Vector3(1.48f,.14f,.28f),"Timber");
            Detail(house,"Lintel",new Vector3(x,3.39f,-3.1f),new Vector3(1.38f,.13f,.14f),"Boards");
        }
        var barn=statics.Find("Big Barn");
        for(int i=0;i<9;i++) {
            float y=7.45f+i*.12f;
            Detail(barn,"Loft vent",new Vector3(0,y,-4.52f),new Vector3(1.7f-i*.12f,.075f,.12f),"Timber");
        }
        var boris = world.Find("Goat Boris");
        var head = boris.Find("Head");
        foreach (var r in boris.GetComponentsInChildren<MeshRenderer>())
            if (r.name=="Body" || r.name=="Leg" || r.name=="Skull" || r.name=="Horn") r.enabled=false;
        AddModel(boris,"BorisCoat","Coat",Vector3.zero,1,0);
        AddModel(boris,"BorisHooves","Hoof",Vector3.zero,1,0);
        AddModel(head,"BorisFace","Coat",Vector3.zero,1,0);
        AddModel(head,"BorisHorns","HornKeratin",Vector3.zero,1,0);
        RenderSettings.skybox=Surface("Sky");
    }

    static void Detail(Transform parent,string name,Vector3 p,Vector3 size,string surface)
    {
        var piece=FarmWorldBuilder.Shape(name,PrimitiveType.Cube,p,size,"Wood",parent);
        piece.GetComponent<MeshRenderer>().sharedMaterial=Surface(surface);
    }

    static void AddModel(Transform parent,string model,string surface,Vector3 position,float scale,float yaw)
    {
        var asset=Resources.Load<GameObject>("CountryArt/"+model);
        if (!asset) return;
        var obj=Object.Instantiate(asset,parent,false);
        obj.name=model;
        obj.transform.localPosition=position;
        obj.transform.localRotation=Quaternion.Euler(0,yaw,0);
        obj.transform.localScale=Vector3.one*scale;
        var material=Surface(surface);
        foreach(var renderer in obj.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterial=material;
    }
}
