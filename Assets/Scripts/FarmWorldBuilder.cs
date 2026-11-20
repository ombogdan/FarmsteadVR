using UnityEngine;

// Builds the whole farm out of simple shapes (cubes, spheres, cylinders, capsules).
// The editor runs this (FarmSceneInitializer) and saves the result into the scene.
// Everything that never moves goes under "Static": the editor marks it for batching,
// which makes many small objects cheap to draw on the Quest.
//
// Map, seen from above (+Z = forward, away from the road):
//
//        forest  |  fence ┌──────────────────────────┐
//        (west)  |        │   big red barn    house  │
//                |        │                          │
//                |        │  Boris grazes     coop   │
//                |        └──── gate ─ broken ───────┘   <- z = -2
//                |               path
//                |   hammer       │   bus stop        nails behind a bush
//   ═════════════ road ════════ bus ══════════════════                    <- z = -16
// (Stage 5 things - the garden, the horseshoes, the house repair - are in FarmWorldBuilder.Chores.cs.)
public static partial class FarmWorldBuilder
{
    public const float FenceFront = -2f;
    public const float FenceBack = 24f;
    public const float FenceLeft = -8f;
    public const float FenceRight = 16f;
    public static readonly Vector3 PlayerStart = new Vector3(0f, 1.7f, -11f);
    public static readonly Vector3 BrokenFenceCenter = new Vector3(6f, 0f, FenceFront);
    public static readonly Vector3 CoopPosition = new Vector3(11.5f, 0f, 6f);
    public const float CoopRidge = 4.15f; // height of the repaired coop's roof top (Zhora lands there)
    public const int NestCount = 4;
    public static readonly Vector3 StallPosition = new Vector3(-5.5f, 0f, -9.5f); // Marquis's shop, by the road
    public static readonly Vector3 HayBedPosition = new Vector3(-4.3f, 0f, 11.3f);

    // The chickens' night: in through the coop door, then to their sleeping spots.
    public static Vector3 CoopDoorOutside => CoopPosition + new Vector3(0f, 0f, -2.7f);
    public static Vector3 CoopDoorInside => CoopPosition + new Vector3(0f, 0f, -1.3f);
    public static Vector3 SleepSpot(int i) => CoopPosition + new Vector3(-1.3f + (i % 7) * 0.43f, 0f, 0.4f + (i / 7) * 0.4f);

    static readonly string[] HenNames = { "Ряба", "Біляночка", "Пеструшка", "Чорнушка", "Золотко", "Хохлатка" };
    static readonly string[] HenPlumage = { "Speckled", "White", "Copper", "Black", "Golden", "Buff" };

    // Real-life sizes: VR shows everything 1:1 in meters, and the headset knows your real height.
    const float FenceHeight = 1.3f;   // low enough for a kid to look over
    const float GoatScale = 0.5f;     // Boris is about 1 m tall with horns, like a real goat

    static Transform world;
    static Transform statics;

    public static void Build(Transform farmWorld)
    {
        world = farmWorld;
        statics = Group("Static", world, Vector3.zero);

        Sun();
        GroundAndPaths();
        RoadAndBusStop();
        Bus();
        Forest();
        Fence();
        GateAndSign();
        Barn(new Vector3(0f, 0f, 17f));
        Farmhouse(HousePosition);
        ChickenCoop(CoopPosition);
        FenceVersions();
        Goat(new Vector3(2f, 0f, 4f));
        Zhora();
        for (int i = 0; i < HenNames.Length; i++)
            Hen("Hen " + (i + 1), HenNames[i], HenPlumage[i] + "Plumage");
        EggTemplate();
        Stall();
        HayBed();
        Chores();
        Tools();
        Player();
        CountryFarmArt.Dress(world);
    }

    // ---------- world ----------

    static void Sun()
    {
        RenderSettings.ambientLight = new Color(0.65f, 0.73f, 0.78f);
        GameObject sun = new GameObject("Warm Sun");
        sun.transform.SetParent(world, false);
        sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.9f, 0.74f);
        light.intensity = 1.25f;
    }

    static void GroundAndPaths()
    {
        // Much bigger than the walkable area, so you never see the edge of the world on the horizon.
        Shape("Ground", PrimitiveType.Cube, new Vector3(0f, -0.15f, 5f), new Vector3(400f, 0.3f, 400f), "Grass", statics, true);
        Shape("Path to Farm", PrimitiveType.Cube, new Vector3(0f, 0.02f, -7.25f), new Vector3(3f, 0.04f, 10.5f), "Dirt", statics, true);
        Shape("Farmyard Path", PrimitiveType.Cube, new Vector3(0f, 0.02f, 5.25f), new Vector3(3f, 0.04f, 14.5f), "Dirt", statics, true);
    }

    static void RoadAndBusStop()
    {
        Shape("Road", PrimitiveType.Cube, new Vector3(0f, 0.02f, -16f), new Vector3(70f, 0.04f, 7f), "Asphalt", statics, true);
        for (float x = -34f; x < 34f; x += 4f)
            Shape("Road Line", PrimitiveType.Cube, new Vector3(x + 1f, 0.045f, -16f), new Vector3(2f, 0.01f, 0.15f), "Paint", statics);

        Transform stop = Group("Bus Stop", statics, new Vector3(5f, 0f, -11.2f));
        Shape("Post", PrimitiveType.Cube, new Vector3(-1.5f, 1.2f, 0.6f), new Vector3(0.1f, 2.4f, 0.1f), "Metal", stop, true);
        Shape("Post", PrimitiveType.Cube, new Vector3(1.5f, 1.2f, 0.6f), new Vector3(0.1f, 2.4f, 0.1f), "Metal", stop, true);
        Shape("Roof", PrimitiveType.Cube, new Vector3(0f, 2.45f, 0.1f), new Vector3(3.4f, 0.1f, 1.6f), "Roof", stop);
        Shape("Back Wall", PrimitiveType.Cube, new Vector3(0f, 1.3f, 0.65f), new Vector3(3f, 1.8f, 0.05f), "Glass", stop, true);
        Shape("Bench", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.35f), new Vector3(2.4f, 0.08f, 0.45f), "Wood", stop, true);
        Shape("Sign Pole", PrimitiveType.Cube, new Vector3(-2.2f, 1.3f, -0.5f), new Vector3(0.08f, 2.6f, 0.08f), "Metal", stop, true);
        Shape("Bus Sign", PrimitiveType.Cube, new Vector3(-2.2f, 2.45f, -0.5f), new Vector3(0.5f, 0.5f, 0.05f), "Bus", stop);
    }

    // The bus you arrived on. It drives away after the intro (FarmPrototype moves it).
    static void Bus()
    {
        Transform bus = Group("Bus", world, new Vector3(-4.4f, 0f, -15.1f));
        bus.localRotation = Quaternion.Euler(0f, -90f, 0f);
        CoachModel.Build(bus, false);
    }

    static void Forest()
    {
        System.Random random = new System.Random(20261120); // same forest every time
        Transform forest = Group("Forest", statics, Vector3.zero);
        for (int i = 0; i < 46; i++) TreeAt(forest, Rand(random, -33f, -11f), Rand(random, -10.5f, 38f), random); // the forest (west)
        for (int i = 0; i < 12; i++) TreeAt(forest, Rand(random, 19f, 33f), Rand(random, -9f, 38f), random);     // a few trees east
        for (int i = 0; i < 12; i++) TreeAt(forest, Rand(random, -33f, 33f), Rand(random, -29f, -21f), random);  // across the road
        for (int i = 0; i < 26; i++)
        {
            float s = Rand(random, 0.7f, 1.3f);
            Vector3 p = new Vector3(Rand(random, -13f, -9f), 0.45f * s, Rand(random, -10f, 36f));
            if (Mathf.Abs(p.x + 13f) < 2.2f && Mathf.Abs(p.z + 3f) < 2.2f) continue; // don't hide the hammer
            Shape("Bush", PrimitiveType.Sphere, p, new Vector3(1.6f, 1f, 1.4f) * s, "Leaves", forest);
        }
    }

    static void TreeAt(Transform parent, float x, float z, System.Random random)
    {
        if (Mathf.Abs(x + 13f) < 2.5f && Mathf.Abs(z + 3f) < 2.5f) return; // keep the hammer's stump clear
        float s = Rand(random, 0.8f, 1.3f);
        if (random.NextDouble() < 0.45) Pine(parent, new Vector3(x, 0f, z), s);
        else RoundTree(parent, new Vector3(x, 0f, z), s);
    }

    static void RoundTree(Transform parent, Vector3 position, float s)
    {
        Transform tree = Group("Tree", parent, position);
        Shape("Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.25f * s, 0f), new Vector3(0.4f, 1.25f, 0.4f) * s, "DarkWood", tree, true);
        Shape("Crown", PrimitiveType.Sphere, new Vector3(0f, 3.25f * s, 0f), new Vector3(2.4f, 2.6f, 2.4f) * s, "Leaves", tree);
    }

    static void Pine(Transform parent, Vector3 position, float s)
    {
        Transform tree = Group("Pine", parent, position);
        Shape("Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1f * s, 0f), new Vector3(0.3f, 1f, 0.3f) * s, "DarkWood", tree, true);
        Shape("Needles", PrimitiveType.Sphere, new Vector3(0f, 2.2f * s, 0f), new Vector3(2.2f, 1.4f, 2.2f) * s, "Pine", tree);
        Shape("Needles", PrimitiveType.Sphere, new Vector3(0f, 3.2f * s, 0f), new Vector3(1.7f, 1.3f, 1.7f) * s, "Pine", tree);
        Shape("Needles", PrimitiveType.Sphere, new Vector3(0f, 4.1f * s, 0f), new Vector3(1.1f, 1.2f, 1.1f) * s, "Pine", tree);
    }

    // ---------- the farmyard ----------

    // A fence around the whole yard. The front has two gaps: the gate (x -2..2) and the broken part (x 4..8).
    static void Fence()
    {
        Transform fence = Group("Fence", statics, Vector3.zero);
        Vector3 frontLeft = new Vector3(FenceLeft, 0f, FenceFront);
        Vector3 frontRight = new Vector3(FenceRight, 0f, FenceFront);
        Vector3 backRight = new Vector3(FenceRight, 0f, FenceBack);
        Vector3 backLeft = new Vector3(FenceLeft, 0f, FenceBack);
        FenceLine(fence, frontLeft, frontRight,
            new Vector2(-2f - FenceLeft, 2f - FenceLeft),  // gate
            new Vector2(4f - FenceLeft, 8f - FenceLeft));  // broken part (see FenceVersions)
        FenceLine(fence, frontRight, backRight);
        FenceLine(fence, backRight, backLeft);
        FenceLine(fence, backLeft, frontLeft);
    }

    // Posts every ~2 m with two rails between them. gaps: (from, to) distances along the line with no fence.
    static void FenceLine(Transform parent, Vector3 from, Vector3 to, params Vector2[] gaps)
    {
        Vector3 direction = (to - from).normalized;
        float length = Vector3.Distance(from, to);
        int segments = Mathf.Max(1, Mathf.RoundToInt(length / 2f));
        float step = length / segments;
        Quaternion along = Quaternion.LookRotation(direction);
        for (int i = 0; i < segments; i++) // the last post is the first post of the next side
        {
            float d = i * step;
            if (!InsideGap(d, gaps))
                Shape("Fence Post", PrimitiveType.Cube, from + direction * d + Vector3.up * (FenceHeight / 2f), new Vector3(0.2f, FenceHeight, 0.2f), "Wood", parent, true);
            if (OverlapsGap(d, d + step, gaps)) continue;
            Vector3 middle = from + direction * (d + step / 2f);
            foreach (float height in new[] { 0.45f, 0.95f })
            {
                GameObject rail = Shape("Fence Rail", PrimitiveType.Cube, middle + Vector3.up * height, new Vector3(0.18f, 0.18f, step - 0.05f), "Wood", parent, true);
                rail.transform.localRotation = along;
            }
        }
    }

    static bool InsideGap(float d, Vector2[] gaps)
    {
        foreach (Vector2 gap in gaps)
            if (d > gap.x && d < gap.y) return true;
        return false;
    }

    static bool OverlapsGap(float from, float to, Vector2[] gaps)
    {
        foreach (Vector2 gap in gaps)
            if (from < gap.y && to > gap.x) return true;
        return false;
    }

    // An arch over the gate with the farm's name and two sunflowers.
    static void GateAndSign()
    {
        Transform gate = Group("Gate", statics, new Vector3(0f, 0f, FenceFront - 0.25f));
        Shape("Arch Post", PrimitiveType.Cube, new Vector3(-2.3f, 1.8f, 0f), new Vector3(0.3f, 3.6f, 0.3f), "Wood", gate, true);
        Shape("Arch Post", PrimitiveType.Cube, new Vector3(2.3f, 1.8f, 0f), new Vector3(0.3f, 3.6f, 0.3f), "Wood", gate, true);
        Shape("Arch Beam", PrimitiveType.Cube, new Vector3(0f, 3.45f, 0f), new Vector3(5.2f, 0.25f, 0.25f), "Wood", gate);
        Shape("Chain", PrimitiveType.Cube, new Vector3(-1.6f, 3.28f, -0.1f), new Vector3(0.03f, 0.2f, 0.03f), "Metal", gate);
        Shape("Chain", PrimitiveType.Cube, new Vector3(1.6f, 3.28f, -0.1f), new Vector3(0.03f, 0.2f, 0.03f), "Metal", gate);
        Shape("Sign Board", PrimitiveType.Cube, new Vector3(0f, 2.9f, -0.1f), new Vector3(4f, 0.85f, 0.08f), "FreshWood", gate);
        SignText(gate, "ФЕРМА «СОНЯШНИК»", new Vector3(0f, 2.9f, -0.15f));
        Sunflower(gate, new Vector3(-1.62f, 2.9f, -0.15f));
        Sunflower(gate, new Vector3(1.62f, 2.9f, -0.15f));
    }

    static void SignText(Transform parent, string value, Vector3 position, float characterSize = 0.03f)
    {
        GameObject go = new GameObject("Sign Text");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        TextMesh text = go.AddComponent<TextMesh>();
        text.font = font;
        text.text = value;
        text.fontSize = 64;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.23f, 0.14f, 0.08f);
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        go.AddComponent<WorldText>();
    }

    static void Sunflower(Transform parent, Vector3 position)
    {
        Transform flower = Group("Sunflower", parent, position);
        for (int i = 0; i < 10; i++)
        {
            Quaternion turn = Quaternion.Euler(0f, 0f, i * 36f);
            GameObject petal = Shape("Petal", PrimitiveType.Sphere, turn * new Vector3(0f, 0.2f, 0f), new Vector3(0.12f, 0.22f, 0.04f), "Yellow", flower);
            petal.transform.localRotation = turn;
        }
        Shape("Seeds", PrimitiveType.Sphere, new Vector3(0f, 0f, -0.02f), new Vector3(0.24f, 0.24f, 0.06f), "DarkWood", flower);
    }

    // The big red barn with white trims, like on classic farms.
    static void Barn(Vector3 position)
    {
        Transform barn = Group("Big Barn", statics, position);
        const float width = 12f, height = 7f, depth = 9f;
        Shape("Walls", PrimitiveType.Cube, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth), "Barn", barn, true);
        GableRoof(barn, width, height, depth, 35f, "Roof", "Barn");

        float front = -depth / 2f - 0.05f;
        Shape("Door", PrimitiveType.Cube, new Vector3(-1.15f, 2.1f, front), new Vector3(2.2f, 4.2f, 0.1f), "DarkWood", barn);
        Shape("Door", PrimitiveType.Cube, new Vector3(1.15f, 2.1f, front), new Vector3(2.2f, 4.2f, 0.1f), "DarkWood", barn);
        float trim = front - 0.04f;
        Trim(barn, new Vector3(0f, 4.3f, trim), new Vector3(4.8f, 0.18f, 0.06f), 0f);
        Trim(barn, new Vector3(-2.35f, 2.15f, trim), new Vector3(0.18f, 4.3f, 0.06f), 0f);
        Trim(barn, new Vector3(2.35f, 2.15f, trim), new Vector3(0.18f, 4.3f, 0.06f), 0f);
        Trim(barn, new Vector3(0f, 2.15f, trim), new Vector3(0.12f, 4.3f, 0.06f), 0f);
        foreach (float x in new[] { -1.15f, 1.15f })
        {
            Trim(barn, new Vector3(x, 2.1f, trim - 0.01f), new Vector3(0.12f, 4.6f, 0.05f), 27f);
            Trim(barn, new Vector3(x, 2.1f, trim - 0.01f), new Vector3(0.12f, 4.6f, 0.05f), -27f);
        }
        Shape("Hayloft Door", PrimitiveType.Cube, new Vector3(0f, 5.7f, front), new Vector3(2f, 1.6f, 0.1f), "DarkWood", barn);
        Trim(barn, new Vector3(0f, 6.55f, trim), new Vector3(2.3f, 0.15f, 0.06f), 0f);
        Trim(barn, new Vector3(0f, 4.85f, trim), new Vector3(2.3f, 0.15f, 0.06f), 0f);
        Trim(barn, new Vector3(-1.08f, 5.7f, trim), new Vector3(0.15f, 1.85f, 0.06f), 0f);
        Trim(barn, new Vector3(1.08f, 5.7f, trim), new Vector3(0.15f, 1.85f, 0.06f), 0f);
        Trim(barn, new Vector3(-width / 2f + 0.1f, height / 2f, trim), new Vector3(0.2f, height, 0.08f), 0f);
        Trim(barn, new Vector3(width / 2f - 0.1f, height / 2f, trim), new Vector3(0.2f, height, 0.08f), 0f);
    }

    static void Trim(Transform parent, Vector3 position, Vector3 size, float tilt)
    {
        GameObject piece = Shape("Trim", PrimitiveType.Cube, position, size, "Paint", parent);
        piece.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    // Two sloped roof halves plus triangle "gables" at the front and back.
    static void GableRoof(Transform parent, float width, float wallHeight, float depth, float angle, string roofMaterial, string gableMaterial)
    {
        float half = width / 2f;
        float rise = half * Mathf.Tan(angle * Mathf.Deg2Rad);
        float slope = half / Mathf.Cos(angle * Mathf.Deg2Rad) + 0.4f; // a little overhang
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject roof = Shape("Roof", PrimitiveType.Cube, new Vector3(side * half / 2f, wallHeight + rise / 2f + 0.1f, 0f),
                new Vector3(slope, 0.25f, depth + 0.6f), roofMaterial, parent);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, -side * angle);
        }
        // A cube turned 45 degrees is a diamond; squashing its parent makes the diamond's top half
        // exactly the triangle under the roof. The bottom half hides inside the walls.
        for (int end = -1; end <= 1; end += 2)
        {
            Transform gable = Group("Gable", parent, new Vector3(0f, wallHeight, end * (depth / 2f - 0.15f)));
            gable.localScale = new Vector3(half, rise, 1f);
            GameObject fill = Shape("Gable Fill", PrimitiveType.Cube, Vector3.zero, new Vector3(Mathf.Sqrt(2f), Mathf.Sqrt(2f), 0.2f), gableMaterial, gable);
            fill.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
    }

    // Grandpa's old house: still standing. The boards on its windows and its repaired look
    // are separate (HouseRepair in FarmWorldBuilder.Chores.cs), so the repair can swap them.
    static void Farmhouse(Vector3 position)
    {
        Transform house = Group("Old Farmhouse", statics, position);
        Shape("Walls", PrimitiveType.Cube, new Vector3(0f, 2.25f, 0f), new Vector3(7f, 4.5f, 6f), "House", house, true);
        GableRoof(house, 7f, 4.5f, 6f, 30f, "Roof", "House");
        Shape("Door", PrimitiveType.Cube, new Vector3(0f, 1.25f, -3.03f), new Vector3(1.4f, 2.5f, 0.08f), "DarkWood", house);
        foreach (float x in new[] { -2.2f, 2.2f })
            Shape("Window", PrimitiveType.Cube, new Vector3(x, 2.8f, -3.04f), new Vector3(1.1f, 1f, 0.06f), "Glass", house);
    }

    // The chicken coop in two versions: the old broken one and the repaired one (hidden until
    // you pay 40 coins). "Chicken Coop" is what you point at, with an orange circle that appears
    // when Boris tells you about it.
    static void ChickenCoop(Vector3 position)
    {
        Transform look = Group("Old Coop", world, position);
        Shape("Walls", PrimitiveType.Cube, new Vector3(0f, 1.1f, 0f), new Vector3(3f, 2.2f, 3f), "Wood", look, true);
        GameObject roof = Shape("Sagging Roof", PrimitiveType.Cube, new Vector3(0.2f, 2.45f, 0f), new Vector3(3.6f, 0.15f, 3.6f), "Roof", look);
        roof.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
        Shape("Hole", PrimitiveType.Cube, new Vector3(-0.5f, 1.3f, -1.52f), new Vector3(0.8f, 0.6f, 0.05f), "Eyes", look);
        GameObject plank = Shape("Loose Plank", PrimitiveType.Cube, new Vector3(0.9f, 0.9f, -1.6f), new Vector3(0.15f, 1.4f, 0.04f), "DarkWood", look);
        plank.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        GameObject ramp = Shape("Ramp", PrimitiveType.Cube, new Vector3(-0.5f, 0.35f, -2.1f), new Vector3(0.5f, 0.05f, 1.3f), "DarkWood", look);
        ramp.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);

        // The repaired coop: big enough to walk in. A doorway in front, straw on the floor,
        // 4 nests on a shelf by the back wall (eggs appear there), a perch, a red roof.
        // Every part is a direct child, so the repair animation can pop them in one by one.
        Transform fresh = Group("Repaired Coop", world, position);
        const float width = 4f, height = 2.6f, depth = 4f, doorway = 1.1f, wall = 0.12f;
        float sidePiece = (width - doorway) / 2f;
        Shape("Back Wall", PrimitiveType.Cube, new Vector3(0f, height / 2f, depth / 2f - wall / 2f), new Vector3(width, height, wall), "FreshWood", fresh, true);
        Shape("Left Wall", PrimitiveType.Cube, new Vector3(-width / 2f + wall / 2f, height / 2f, 0f), new Vector3(wall, height, depth), "FreshWood", fresh, true);
        Shape("Right Wall", PrimitiveType.Cube, new Vector3(width / 2f - wall / 2f, height / 2f, 0f), new Vector3(wall, height, depth), "FreshWood", fresh, true);
        Shape("Front Wall", PrimitiveType.Cube, new Vector3(-(doorway + sidePiece) / 2f, height / 2f, -depth / 2f + wall / 2f), new Vector3(sidePiece, height, wall), "FreshWood", fresh, true);
        Shape("Front Wall", PrimitiveType.Cube, new Vector3((doorway + sidePiece) / 2f, height / 2f, -depth / 2f + wall / 2f), new Vector3(sidePiece, height, wall), "FreshWood", fresh, true);
        Shape("Above Door", PrimitiveType.Cube, new Vector3(0f, height - 0.25f, -depth / 2f + wall / 2f), new Vector3(doorway, 0.5f, wall), "FreshWood", fresh, true);
        GableRoof(fresh, width, height, depth, 35f, "ToolboxRed", "FreshWood");
        Shape("Straw Floor", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(width - 0.2f, 0.06f, depth - 0.2f), "Straw", fresh);
        Shape("Nest Shelf", PrimitiveType.Cube, new Vector3(0f, 0.6f, depth / 2f - 0.45f), new Vector3(width - 0.4f, 0.06f, 0.65f), "Wood", fresh);
        for (int i = 0; i < NestCount; i++)
        {
            float x = -1.2f + i * 0.8f;
            Shape("Nest", PrimitiveType.Cube, new Vector3(x, 0.72f, depth / 2f - 0.45f), new Vector3(0.6f, 0.18f, 0.5f), "Wood", fresh);
            Shape("Nest Straw", PrimitiveType.Sphere, new Vector3(x, 0.8f, depth / 2f - 0.45f), new Vector3(0.45f, 0.1f, 0.38f), "Straw", fresh);
            Group("Nest Spot " + i, fresh, new Vector3(x, 0.84f, depth / 2f - 0.45f)); // where an egg appears
        }
        Shape("Perch", PrimitiveType.Cube, new Vector3(-width / 2f + 0.4f, 1.1f, 0f), new Vector3(0.08f, 0.08f, depth - 0.6f), "Wood", fresh);
        Shape("Window", PrimitiveType.Sphere, new Vector3(width / 2f + 0.01f, 1.6f, 0f), new Vector3(0.05f, 0.5f, 0.5f), "Glass", fresh);
        Shape("Trim", PrimitiveType.Cube, new Vector3(-doorway / 2f - 0.05f, 1.05f, -depth / 2f - 0.01f), new Vector3(0.1f, 2.1f, 0.04f), "Paint", fresh);
        Shape("Trim", PrimitiveType.Cube, new Vector3(doorway / 2f + 0.05f, 1.05f, -depth / 2f - 0.01f), new Vector3(0.1f, 2.1f, 0.04f), "Paint", fresh);
        GameObject newRamp = Shape("Ramp", PrimitiveType.Cube, new Vector3(0f, 0.08f, -depth / 2f - 0.45f), new Vector3(1f, 0.05f, 0.9f), "Wood", fresh);
        newRamp.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
        Shape("Vane Pole", PrimitiveType.Cube, new Vector3(0f, CoopRidge + 0.25f, 1.4f), new Vector3(0.04f, 0.5f, 0.04f), "Metal", fresh);
        Shape("Vane Arrow", PrimitiveType.Cube, new Vector3(0f, CoopRidge + 0.5f, 1.4f), new Vector3(0.04f, 0.04f, 0.5f), "Metal", fresh);
        fresh.gameObject.SetActive(false);

        Transform coop = Group("Chicken Coop", world, position);
        BoxCollider hit = coop.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 1.2f, 0f);
        hit.size = new Vector3(3.2f, 2.6f, 3.2f);
        FarmInteractable action = coop.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Coop;
        action.label = "оглянути старий курник";
        GlowRing(coop, 2.3f, 0.03f, 1f); // around the coop, so it keeps its full size
    }

    // The broken piece of the front fence and its repaired version (hidden until the repair).
    static void FenceVersions()
    {
        Transform broken = Group("Broken Fence", world, BrokenFenceCenter);
        BoxCollider brokenHit = broken.gameObject.AddComponent<BoxCollider>();
        brokenHit.center = new Vector3(0f, FenceHeight / 2f, 0f);
        brokenHit.size = new Vector3(3.8f, FenceHeight, 0.5f);
        FarmInteractable action = broken.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Fence;
        action.label = "оглянути паркан";
        GameObject rail = Shape("Fallen Rail", PrimitiveType.Cube, new Vector3(0.2f, 0.35f, 0f), new Vector3(3.6f, 0.16f, 0.16f), "DarkWood", broken);
        rail.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
        Shape("Broken Post", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(0.22f, 0.7f, 0.22f), "DarkWood", broken);
        GameObject plank = Shape("Plank on Ground", PrimitiveType.Cube, new Vector3(-1f, 0.1f, -0.5f), new Vector3(1.8f, 0.12f, 0.16f), "DarkWood", broken);
        plank.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
        GameObject lean = Shape("Leaning Rail", PrimitiveType.Cube, new Vector3(1.1f, 0.6f, 0.15f), new Vector3(1.9f, 0.16f, 0.16f), "Wood", broken);
        lean.transform.localRotation = Quaternion.Euler(0f, 10f, -22f);

        Transform repaired = Group("Repaired Fence", world, BrokenFenceCenter);
        BoxCollider repairedHit = repaired.gameObject.AddComponent<BoxCollider>();
        repairedHit.center = new Vector3(0f, FenceHeight / 2f, 0f);
        repairedHit.size = new Vector3(4f, FenceHeight, 0.3f);
        Shape("New Post", PrimitiveType.Cube, new Vector3(0f, FenceHeight / 2f, 0f), new Vector3(0.22f, FenceHeight, 0.22f), "FreshWood", repaired);
        Shape("New Rail", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(3.9f, 0.18f, 0.18f), "FreshWood", repaired);
        Shape("New Rail", PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(3.9f, 0.18f, 0.18f), "FreshWood", repaired);
        repaired.gameObject.SetActive(false);
    }

    // Boris: grazes in the yard from the start. The head parts sit under "Head" so he can lower it to eat.
    static void Goat(Vector3 position)
    {
        Transform goat = Group("Goat Boris", world, position);
        goat.localRotation = Quaternion.Euler(0f, 160f, 0f);
        goat.localScale = Vector3.one * GoatScale; // the parts below are drawn at 2x and scaled down
        SphereCollider hit = goat.gameObject.AddComponent<SphereCollider>();
        hit.center = new Vector3(0f, 1f, 0f);
        hit.radius = 0.95f;
        FarmInteractable action = goat.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Goat;
        action.label = "погладити козла";

        Shape("Body", PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0f), new Vector3(1.25f, 1f, 1.7f), "Goat", goat);
        Shape("Tail", PrimitiveType.Sphere, new Vector3(0f, 1.25f, 0.85f), new Vector3(0.18f, 0.25f, 0.18f), "Goat", goat);
        Transform head = Group("Head", goat, new Vector3(0f, 1.3f, -0.45f));
        Shape("Skull", PrimitiveType.Sphere, new Vector3(0f, 0.2f, -0.25f), new Vector3(0.75f, 0.76f, 0.7f), "Goat", head);
        Shape("Nose", PrimitiveType.Sphere, new Vector3(0f, -0.01f, -0.59f), new Vector3(0.4f, 0.25f, 0.3f), "Nose", head);
        Shape("Beard", PrimitiveType.Sphere, new Vector3(0f, -0.22f, -0.5f), new Vector3(0.16f, 0.3f, 0.14f), "Horn", head);
        for (int side = -1; side <= 1; side += 2)
        {
            Shape("Leg", PrimitiveType.Capsule, new Vector3(side * 0.42f, 0.45f, -0.45f), new Vector3(0.22f, 0.55f, 0.22f), "Goat", goat);
            Shape("Leg", PrimitiveType.Capsule, new Vector3(side * 0.42f, 0.45f, 0.5f), new Vector3(0.22f, 0.55f, 0.22f), "Goat", goat);
            GameObject horn = Shape("Horn", PrimitiveType.Capsule, new Vector3(side * 0.22f, 0.68f, -0.33f), new Vector3(0.12f, 0.33f, 0.12f), "Horn", head);
            horn.transform.localRotation = Quaternion.Euler(-15f, 0f, side * -10f);
            Shape("Eye", PrimitiveType.Sphere, new Vector3(side * 0.26f, 0.31f, -0.55f), Vector3.one * 0.12f, "Eyes", head);
            Shape("Ear", PrimitiveType.Sphere, new Vector3(side * 0.42f, 0.3f, -0.2f), new Vector3(0.3f, 0.1f, 0.15f), "Goat", head);
        }
        Grazer grazer = goat.gameObject.AddComponent<Grazer>();
        grazer.head = head; // calm: walks slowly, eats for 5-12 s, looks at you when you come close
    }

    // Zhora the rooster, "director of the coop". Hidden until the coop is repaired.
    static void Zhora()
    {
        Transform rooster = Group("Zhora", world, CoopPosition + new Vector3(-0.6f, 0f, -2.4f));
        SphereCollider hit = rooster.gameObject.AddComponent<SphereCollider>();
        hit.center = new Vector3(0f, 0.4f, 0f);
        hit.radius = 0.3f;
        FarmInteractable action = rooster.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Rooster;
        action.label = "поговорити з Жорою";

        Transform head = ChickenGait.Dress(rooster, true, "ZhoraPlumage");

        Grazer grazer = rooster.gameObject.AddComponent<Grazer>();
        ChickenHabits(grazer, head);
        rooster.gameObject.SetActive(false);
    }

    // A hen you can buy from Zhora. Hidden until bought. Her name is kept in itemId.
    static void Hen(string objectName, string henName, string plumage)
    {
        Transform hen = Group(objectName, world, CoopPosition + new Vector3(-0.6f, 0f, -2.6f));
        SphereCollider hit = hen.gameObject.AddComponent<SphereCollider>();
        hit.center = new Vector3(0f, 0.28f, 0f);
        hit.radius = 0.22f;
        FarmInteractable action = hen.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Hen;
        action.label = "погладити курочку";
        action.itemId = henName;

        Transform head = ChickenGait.Dress(hen, false, plumage);

        Grazer grazer = hen.gameObject.AddComponent<Grazer>();
        ChickenHabits(grazer, head);
        hen.gameObject.SetActive(false);
    }

    // The egg that hens lay. Hidden; FarmPrototype copies it into a nest each time a hen lays one.
    static void EggTemplate()
    {
        Transform egg = Group("Egg Template", world, Vector3.zero);
        BoxCollider hit = egg.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.05f, 0f);
        hit.size = new Vector3(0.22f, 0.2f, 0.22f);
        FarmInteractable action = egg.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Egg;
        action.label = "взяти яйце";
        action.itemId = "egg";
        Transform model = Model(egg, Quaternion.identity);
        Shape("Egg", PrimitiveType.Sphere, new Vector3(0f, 0.035f, 0f), new Vector3(0.05f, 0.065f, 0.05f), "Egg", model);
        GlowRing(egg, 0.12f, 0.005f).gameObject.SetActive(true);
        egg.gameObject.SetActive(false);
    }

    // Marquis's shop by the road: a counter under a striped awning, the cat, an egg basket and goods.
    // Built facing -Z and then turned so its front faces the path.
    static void Stall()
    {
        Quaternion facePath = Quaternion.Euler(0f, -90f, 0f);
        Transform look = Group("Shop Stall", statics, StallPosition);
        look.localRotation = facePath;
        Shape("Counter", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1f, 0.7f), "Wood", look, true);
        Shape("Counter Top", PrimitiveType.Cube, new Vector3(0f, 1.02f, -0.05f), new Vector3(2.6f, 0.06f, 0.85f), "FreshWood", look);
        Shape("Shelf", PrimitiveType.Cube, new Vector3(0f, 1.3f, 0.8f), new Vector3(2.4f, 0.06f, 0.4f), "Wood", look);
        foreach (float x in new[] { -1.25f, 1.25f })
        {
            Shape("Post", PrimitiveType.Cube, new Vector3(x, 1.25f, 0.95f), new Vector3(0.1f, 2.5f, 0.1f), "DarkWood", look, true);
            Shape("Post", PrimitiveType.Cube, new Vector3(x, 1.25f, -0.35f), new Vector3(0.1f, 2.5f, 0.1f), "DarkWood", look, true);
        }
        for (int i = 0; i < 6; i++) // red and white stripes
        {
            GameObject stripe = Shape("Awning", PrimitiveType.Cube, new Vector3(-1.125f + i * 0.45f, 2.55f, 0.3f), new Vector3(0.45f, 0.05f, 1.7f), i % 2 == 0 ? "ToolboxRed" : "Paint", look);
            stripe.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        }
        Shape("Sign Board", PrimitiveType.Cube, new Vector3(0f, 2.95f, -0.45f), new Vector3(2.2f, 0.42f, 0.06f), "FreshWood", look);
        SignText(look, "ЛАВКА МАРКІЗА", new Vector3(0f, 2.95f, -0.49f), 0.022f);

        Transform shop = Group("Shop", world, StallPosition);
        shop.localRotation = facePath;
        Marquis(shop);

        Transform basket = ShopThing(shop, "Egg Basket", new Vector3(0f, 1.05f, -0.15f), FarmInteractable.Kind.SellBasket, "", "продати яйця (3 монети) і моркву (5)");
        Shape("Basket", PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(0.42f, 0.18f, 0.3f), "Straw", basket);
        Shape("Handle", PrimitiveType.Cube, new Vector3(0f, 0.26f, 0f), new Vector3(0.42f, 0.03f, 0.03f), "Straw", basket);
        GlowRing(basket, 0.35f, 0.01f, 0.8f);

        Transform henBasket = ShopThing(shop, "Hen Basket", new Vector3(0.8f, 1.05f, -0.1f), FarmInteractable.Kind.ShopItem, "hen", "купити курочку — 5 монет");
        Shape("Basket", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.36f, 0.2f, 0.28f), "Straw", henBasket);
        var basketHen = ArrivalArt.Group(henBasket, "Hen in basket", new Vector3(0f, 0.24f, 0.005f));
        basketHen.localScale = Vector3.one * 0.8f;
        ArrivalArt.Part(basketHen, "HenHead", "WhitePlumage");

        Transform planks = ShopThing(shop, "Planks", new Vector3(-0.6f, 1.33f, 0.8f), FarmInteractable.Kind.ShopItem, "planks", "купити дошки — 15 монет");
        Transform planksModel = Model(planks, Quaternion.identity);
        for (int i = 0; i < 3; i++)
            Shape("Plank", PrimitiveType.Cube, new Vector3(0f, 0.03f + i * 0.055f, 0f), new Vector3(0.6f, 0.05f, 0.15f), "FreshWood", planksModel);

        Transform paint = ShopThing(shop, "Paint", new Vector3(0.05f, 1.33f, 0.8f), FarmInteractable.Kind.ShopItem, "paint", "купити фарбу — 10 монет");
        Transform paintModel = Model(paint, Quaternion.identity);
        Shape("Bucket", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(0.2f, 0.24f, 0.2f), "Teleport", paintModel);
        Shape("Lid", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.22f, 0.03f, 0.22f), "Metal", paintModel);

        Transform seeds = ShopThing(shop, "Seeds", new Vector3(0.62f, 1.33f, 0.8f), FarmInteractable.Kind.ShopItem, "seeds", "купити насіння моркви (4 шт.) — 4 монети");
        SeedPacket(Model(seeds, Quaternion.Euler(-12f, 0f, 0f)));

        Transform shovel = ShopThing(shop, "Shovel", new Vector3(1.05f, 0f, -0.55f), FarmInteractable.Kind.ShopItem, "shovel", "купити лопату — 8 монет");
        Transform shovelModel = Model(shovel, Quaternion.Euler(-8f, 0f, 0f));
        Shape("Handle", PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), new Vector3(0.04f, 1f, 0.04f), "FreshWood", shovelModel);
        Shape("Blade", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f), new Vector3(0.22f, 0.28f, 0.02f), "Metal", shovelModel);
        TallCollider(shovel);

        Transform rake = ShopThing(shop, "Rake", new Vector3(-1.05f, 0f, -0.55f), FarmInteractable.Kind.ShopItem, "rake", "купити граблі — 8 монет");
        Transform rakeModel = Model(rake, Quaternion.Euler(-8f, 0f, 0f));
        Shape("Handle", PrimitiveType.Cube, new Vector3(0f, 0.65f, 0f), new Vector3(0.04f, 1.2f, 0.04f), "FreshWood", rakeModel);
        Shape("Head", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), new Vector3(0.42f, 0.04f, 0.04f), "Metal", rakeModel);
        for (int i = 0; i < 6; i++)
            Shape("Tooth", PrimitiveType.Cube, new Vector3(-0.18f + i * 0.072f, 0.02f, -0.02f), new Vector3(0.015f, 0.07f, 0.015f), "Metal", rakeModel);
        TallCollider(rake);
    }

    // For long tools standing on the ground: the laser should find them anywhere along the handle.
    static void TallCollider(Transform thing)
    {
        BoxCollider hit = thing.GetComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.65f, 0f);
        hit.size = new Vector3(0.45f, 1.3f, 0.35f);
    }

    static Transform ShopThing(Transform shop, string name, Vector3 localPosition, FarmInteractable.Kind kind, string id, string label)
    {
        Transform thing = Group(name, shop, localPosition);
        BoxCollider hit = thing.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.2f, 0f);
        hit.size = new Vector3(0.6f, 0.45f, 0.45f);
        FarmInteractable action = thing.gameObject.AddComponent<FarmInteractable>();
        action.kind = kind;
        action.itemId = id;
        action.label = label;
        action.alwaysHint = true; // prices: always shown
        return thing;
    }

    // Marquis: a ginger cat in a little black hat, sitting on the counter. Appears in stage 4.
    static void Marquis(Transform shop)
    {
        Transform cat = Group("Marquis", shop, new Vector3(-0.8f, 1.05f, 0.05f));
        SphereCollider hit = cat.gameObject.AddComponent<SphereCollider>();
        hit.center = new Vector3(0f, 0.22f, 0f);
        hit.radius = 0.24f;
        FarmInteractable action = cat.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Cat;
        action.label = "поговорити з Маркізом";

        Shape("Body", PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0.05f), new Vector3(0.28f, 0.3f, 0.36f), "Ginger", cat);
        Shape("Paw", PrimitiveType.Sphere, new Vector3(-0.06f, 0.02f, -0.12f), new Vector3(0.07f, 0.05f, 0.09f), "Ginger", cat);
        Shape("Paw", PrimitiveType.Sphere, new Vector3(0.06f, 0.02f, -0.12f), new Vector3(0.07f, 0.05f, 0.09f), "Ginger", cat);
        GameObject tail = Shape("Tail", PrimitiveType.Capsule, new Vector3(0.14f, 0.12f, 0.2f), new Vector3(0.06f, 0.18f, 0.06f), "Ginger", cat);
        tail.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
        Transform head = Group("Head", cat, new Vector3(0f, 0.36f, -0.08f));
        Shape("Skull", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.22f, 0.2f, 0.2f), "Ginger", head);
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject ear = Shape("Ear", PrimitiveType.Cube, new Vector3(side * 0.07f, 0.1f, 0f), new Vector3(0.07f, 0.07f, 0.03f), "Ginger", head);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Shape("Eye", PrimitiveType.Sphere, new Vector3(side * 0.05f, 0.02f, -0.09f), Vector3.one * 0.04f, "Leaves", head);
            for (int w = 0; w < 2; w++)
            {
                GameObject whisker = Shape("Whisker", PrimitiveType.Cube, new Vector3(side * 0.09f, -0.03f - w * 0.015f, -0.09f), new Vector3(0.12f, 0.005f, 0.005f), "Paint", head);
                whisker.transform.localRotation = Quaternion.Euler(0f, 0f, side * (w == 0 ? 8f : -8f));
            }
        }
        Shape("Nose", PrimitiveType.Sphere, new Vector3(0f, -0.015f, -0.1f), Vector3.one * 0.025f, "Nose", head);
        Shape("Hat Brim", PrimitiveType.Cylinder, new Vector3(0f, 0.11f, 0f), new Vector3(0.17f, 0.008f, 0.17f), "Eyes", head);
        Shape("Hat", PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.11f, 0.04f, 0.11f), "Eyes", head);
        cat.gameObject.SetActive(false);
    }

    // A pile of hay by the barn: at night you can sleep here until morning.
    static void HayBed()
    {
        Transform bed = Group("Hay Bed", world, HayBedPosition);
        BoxCollider hit = bed.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.3f, 0f);
        hit.size = new Vector3(1.6f, 0.6f, 1.1f);
        FarmInteractable action = bed.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Sleep;
        action.label = "поспати до ранку";
        Shape("Hay", PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0f), new Vector3(1.6f, 0.5f, 1.1f), "Straw", bed);
        Shape("Hay", PrimitiveType.Sphere, new Vector3(-0.4f, 0.32f, 0.1f), new Vector3(0.8f, 0.4f, 0.7f), "Straw", bed);
        Shape("Blanket", PrimitiveType.Cube, new Vector3(0.2f, 0.42f, 0f), new Vector3(0.8f, 0.04f, 0.7f), "Teleport", bed);
        Shape("Pillow", PrimitiveType.Sphere, new Vector3(-0.45f, 0.5f, 0f), new Vector3(0.35f, 0.14f, 0.25f), "Paint", bed);
    }

    // Chickens stay in front of the coop, take quick little walks and peck at the ground.
    static void ChickenHabits(Grazer grazer, Transform head)
    {
        grazer.head = head;
        grazer.areaMin = new Vector2(CoopPosition.x - 3f, 0.5f);
        grazer.areaMax = new Vector2(CoopPosition.x + 3f, CoopPosition.z - 2.2f);
        grazer.speed = 0.5f;
        grazer.waitMin = 1f;
        grazer.waitMax = 4f;
        grazer.eatTilt = 50f;
        grazer.pecks = true;
        grazer.curiousDistance = 1.8f;
    }

    // Hammer on a stump at the forest edge, a box of nails behind a bush near the road,
    // and a saw leaning on the barn wall. Each has an orange circle (shown when you need them).
    static void Tools()
    {
        Shape("Stump", PrimitiveType.Cylinder, new Vector3(-13f, 0.3f, -3f), new Vector3(0.9f, 0.3f, 0.9f), "DarkWood", statics, true);
        Transform hammer = Item("Hammer", "hammer", "взяти молоток", new Vector3(-13f, 0.6f, -3f));
        Transform hammerModel = Model(hammer, Quaternion.Euler(0f, 30f, 0f));
        GameObject handle = Shape("Handle", PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(0.035f, 0.18f, 0.035f), "FreshWood", hammerModel);
        handle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Shape("Head", PrimitiveType.Cube, new Vector3(0.18f, 0.045f, 0f), new Vector3(0.06f, 0.06f, 0.17f), "Metal", hammerModel);

        Shape("Bush", PrimitiveType.Sphere, new Vector3(14.4f, 0.5f, -8.3f), new Vector3(1.6f, 1f, 1.4f), "Leaves", statics);
        Transform nails = Item("Nails", "nails", "взяти цвяхи", new Vector3(13.4f, 0f, -9.2f));
        Transform nailsModel = Model(nails, Quaternion.Euler(0f, -15f, 0f));
        Shape("Box", PrimitiveType.Cube, new Vector3(0f, 0.07f, 0f), new Vector3(0.3f, 0.14f, 0.2f), "ToolboxRed", nailsModel);
        for (int i = 0; i < 6; i++)
            Shape("Nail Head", PrimitiveType.Sphere, new Vector3(-0.08f + (i % 3) * 0.08f, 0.15f, i < 3 ? -0.04f : 0.04f), Vector3.one * 0.035f, "Metal", nailsModel);

        Transform saw = Item("Saw", "saw", "взяти пилку", new Vector3(-6.3f, 0f, 15f));
        Transform sawModel = Model(saw, Quaternion.Euler(0f, 0f, -15f)); // leans on the barn wall
        Shape("Blade", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(0.012f, 0.6f, 0.15f), "Metal", sawModel);
        Shape("Handle", PrimitiveType.Cube, new Vector3(0f, 0.68f, 0f), new Vector3(0.03f, 0.14f, 0.17f), "ToolboxRed", sawModel);
    }

    // A small orange circle right at the tool (it grows and shines when you point at it).
    static Transform Item(string name, string id, string label, Vector3 position)
    {
        const float ringRadius = 0.3f, ringY = 0.02f;
        Transform item = Group(name, world, position);
        BoxCollider hit = item.gameObject.AddComponent<BoxCollider>(); // generous, so the laser finds it easily
        hit.center = new Vector3(0f, 0.35f, 0f);
        hit.size = new Vector3(0.7f, 0.8f, 0.7f);
        FarmInteractable action = item.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Item;
        action.label = label;
        action.itemId = id;
        GlowRing(item, ringRadius, ringY);
        return item;
    }

    // The item's look lives under "Model": the inventory copies it to show a small version.
    static Transform Model(Transform item, Quaternion rotation)
    {
        Transform model = Group("Model", item, Vector3.zero);
        model.localRotation = rotation;
        return model;
    }

    // A dashed orange circle. FarmPrototype switches it on when it's needed.
    // idleScale: how small it is until you point at it (1 = full size).
    static Transform GlowRing(Transform parent, float radius, float y, float idleScale = 0.6f)
    {
        Transform ring = Group("Glow Ring", parent, new Vector3(0f, y, 0f));
        const int dashes = 18;
        for (int i = 0; i < dashes; i++)
        {
            float angle = i * 360f / dashes;
            Quaternion turn = Quaternion.Euler(0f, angle, 0f);
            GameObject dash = Shape("Glow", PrimitiveType.Cube, turn * new Vector3(0f, 0f, radius),
                new Vector3(radius * 0.22f, 0.015f, Mathf.Max(0.02f, radius * 0.08f)), "GlowDim", ring);
            dash.transform.localRotation = turn;
        }
        PickupGlow glow = ring.gameObject.AddComponent<PickupGlow>();
        glow.idleScale = idleScale;
        glow.highlightScale = idleScale < 1f ? 1.25f : 1.1f;
        ring.gameObject.SetActive(false);
        return ring;
    }

    static void Player()
    {
        GameObject player = new GameObject("Desktop Player");
        player.transform.SetParent(world, false);
        player.transform.localPosition = PlayerStart; // at the bus stop, looking at the farm
        player.AddComponent<DesktopPlayer>();
        Camera camera = player.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 68f;
        camera.nearClipPlane = 0.05f; // in VR your hands come close to your eyes
        camera.backgroundColor = new Color(0.67f, 0.82f, 0.94f);
        player.AddComponent<AudioListener>();
    }

    // ---------- helpers ----------

    static Transform Group(string name, Transform parent, Vector3 localPosition)
    {
        Transform group = new GameObject(name).transform;
        group.SetParent(parent, false);
        group.localPosition = localPosition;
        return group;
    }

    static float Rand(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    // solid = keep the shape's collider (walls, ground). Decorations don't need one.
    public static GameObject Shape(string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, string materialName, Transform parent, bool solid = false)
    {
        GameObject shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = localPosition;
        shape.transform.localScale = localScale;
        Material material = Resources.Load<Material>("FarmMaterials/" + materialName);
        if (material != null) shape.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = shape.GetComponent<Collider>();
        if (collider != null && !solid)
        {
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
        return shape;
    }
}
