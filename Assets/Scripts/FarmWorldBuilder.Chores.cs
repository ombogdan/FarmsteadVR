using UnityEngine;

// Stage 5 of the farm (the rules are in FarmPrototype.Chores.cs): things to do while the hens lay eggs.
//   - a grain sack and a feeder by the coop: fed hens lay twice as often (once a day);
//   - grandpa Panas's 5 lucky horseshoes hidden around the farm, and one more that later hangs over the coop door;
//   - a mound of earth by the house with a chest buried in it;
//   - grandpa's old garden in front of the house: 4 raised beds full of weeds, a watering can by the water barrel;
//   - the house: boards on its windows, and its repaired look (hidden until the repair).
// The round things (horseshoe, carrot, watering can, sack) are our own models from
// Tools/ModelGen/generate_props.py (Resources/FarmProps). Wood, soil and plaster use the same CC0 photo
// textures as the rest of the farm (CountryArt), so everything looks like one world.
//
// Map of this part (+Z = away from the road):
//
//        ┌───────── house ─────────┐  barrel + watering can
//        │       (door, porch)      │
//        └──────────────────────────┘
//          [bed] [bed] [bed] [bed]        mound with the chest
//                ┌─ coop ─┐
//                └─ door ─┘  sack
//                           feeder
public static partial class FarmWorldBuilder
{
    // Properties (not fields): they are built from positions in the other file of this class.
    public static Vector3 HousePosition => new Vector3(11f, 0f, 18.5f);
    static Vector3 DoorHinge => new Vector3(-0.6f, 0f, -2.95f); // the house's door hangs here (house space)
    public static Vector3 FeederPosition => CoopPosition + new Vector3(2.1f, 0f, -3.4f);
    public static Vector3 MoundPosition => new Vector3(15f, 0f, 10.8f);
    public static Vector3 CanPosition => new Vector3(13.25f, 0f, 14.4f);
    public const int BedCount = 4;
    public const int CarrotsPerBed = 2;
    const float BedHeight = 0.4f, SoilTop = 0.38f;
    public static Vector3 BedPosition(int i) => new Vector3(9.05f + i * 1.3f, 0f, 11.6f);

    // Where the horseshoes hide: at places of the farm that matter later (the bus stop - the bus to the fair,
    // the well - water, the barn - the animals, grandpa's house, the coop). All hang on something.
    public const int HorseshoeCount = 5;
    static readonly Vector3[] HorseshoeSpots =
    {
        new Vector3(2.8f, 1.6f, -11.654f),  // on the bus stop sign's pole (the side that looks at the farm)
        new Vector3(5.75f, 1.25f, 8.74f),   // on a post of the old well
        new Vector3(-1.1f, 2.9f, 12.33f),   // on the big barn's left door
        new Vector3(11f, 2.78f, 15.44f),    // over the door of grandpa's house
        new Vector3(12.7f, 1.7f, 7.87f),    // inside the coop, over the nests
    };
    static readonly float[] HorseshoeYaw = { 180f, 0f, 0f, 0f, 0f }; // which way each one faces

    static void Chores()
    {
        Lawn();
        Well();
        BarnDoors();
        Animals();
        KeyLook();
        ChickenFlight();
        RaceTrack();
        ShootingGallery();
        FairFlags();
        CheeseHouse();
        EggRushSign();
        Kitchen();
        StorageShed();
        Feeder();
        Horseshoes();
        MoundAndChest();
        Garden();
        HouseRepair();
    }

    // A real pasture inside the fence: ~40,000 short grass blades in tufts (three shades of green),
    // made by generate_props.py around the path, the buildings and the beds. It never moves, so it's
    // batched with the rest of "Static"; it doesn't cast shadows (that would cost a lot on the Quest).
    static void Lawn()
    {
        foreach (string part in new[] { "LawnA", "LawnB", "LawnC" })
        {
            Transform grass = Prop(statics, part, "FarmProps/" + part, Vector3.zero, Vector3.zero, 1f);
            MeshRenderer look = grass.GetComponent<MeshRenderer>();
            if (look != null) look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    // ---------- feeding the hens ----------

    // The storage shed by the barn: grandpa's grain sacks inside (one sack shows per 20 kg left),
    // a sign "СКЛАД" (point at it to buy animal food). Grain goes from here to the feeder by the coop.
    public static Vector3 StoragePosition => new Vector3(-6f, 0f, 9.6f);

    static void StorageShed()
    {
        Transform shed = Group("Storage Shed", statics, StoragePosition);
        const float w = 3f, d = 2.4f, h = 2.4f;
        Shape("Walls", PrimitiveType.Cube, new Vector3(0f, h / 2f, d / 2f - 0.06f), new Vector3(w, h, 0.12f), "Wood", shed, true);
        foreach (float side in new[] { -1f, 1f })
        {
            Shape("Walls", PrimitiveType.Cube, new Vector3(side * (w / 2f - 0.06f), h / 2f, 0f), new Vector3(0.12f, h, d), "Wood", shed, true);
            Shape("Walls", PrimitiveType.Cube, new Vector3(side * 1.2f, h / 2f, -d / 2f + 0.06f), new Vector3(0.6f, h, 0.12f), "Wood", shed, true);
        }
        Shape("Walls", PrimitiveType.Cube, new Vector3(0f, h - 0.2f, -d / 2f + 0.06f), new Vector3(1.8f, 0.4f, 0.12f), "Wood", shed, true);
        GameObject roof = Shape("Roof", PrimitiveType.Cube, new Vector3(0f, h + 0.12f, 0f), new Vector3(w + 0.4f, 0.12f, d + 0.5f), "Roof", shed);
        roof.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
        Shape("Floor", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(w - 0.2f, 0.06f, d - 0.2f), "Wood", shed, true);
        Shape("Sign Board", PrimitiveType.Cube, new Vector3(0f, h - 0.2f, -d / 2f - 0.03f), new Vector3(1.4f, 0.32f, 0.05f), "FreshWood", shed);
        SignText(shed, "СКЛАД", new Vector3(0f, h - 0.2f, -d / 2f - 0.07f), 0.026f);

        // The sacks you take grain from (FarmPrototype.Chores.cs shows one per 20 kg on stock).
        Transform sacks = Group("Grain Sack", world, StoragePosition + new Vector3(0f, 0f, 0.3f));
        BoxCollider hit = sacks.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.45f, 0f);
        hit.size = new Vector3(2.2f, 0.9f, 1f);
        FarmInteractable take = sacks.gameObject.AddComponent<FarmInteractable>();
        take.kind = FarmInteractable.Kind.GrainSack;
        take.label = "набрати зерна";
        Transform model = Model(sacks, Quaternion.identity);
        Vector3[] spots = { new Vector3(-0.7f, 0f, 0.2f), new Vector3(0f, 0f, 0.25f), new Vector3(0.7f, 0f, 0.2f), new Vector3(-0.35f, 0f, -0.3f), new Vector3(0.35f, 0f, -0.3f) };
        for (int i = 0; i < spots.Length; i++)
            Prop(model, "GrainSack", "FarmProps/Burlap", spots[i], new Vector3(0f, 25f + i * 47f, 0f), 0.85f);
        Transform ring = GlowRing(sacks, 0.8f, 0.02f);
        ring.localPosition = new Vector3(0f, 0.08f, -0.75f);

        // Hay bales by the shed's side (one bale shows per 10 kg); cows and goats eat hay.
        Transform hay = Group("Hay Stack", world, StoragePosition + new Vector3(2.25f, 0f, 0.1f));
        BoxCollider hayHit = hay.gameObject.AddComponent<BoxCollider>();
        hayHit.center = new Vector3(0f, 0.5f, 0f);
        hayHit.size = new Vector3(1f, 1f, 1.4f);
        FarmInteractable takeHay = hay.gameObject.AddComponent<FarmInteractable>();
        takeHay.kind = FarmInteractable.Kind.HayStack;
        takeHay.label = "набрати сіна";
        Transform bales = Model(hay, Quaternion.identity);
        for (int i = 0; i < 4; i++)
            Shape("Bale", PrimitiveType.Cube, new Vector3(0f, 0.23f + (i / 2) * 0.46f, -0.33f + (i % 2) * 0.66f), new Vector3(0.9f, 0.45f, 0.62f), "Straw", bales);

        Transform sign = Group("Storage Sign", world, StoragePosition);
        BoxCollider signHit = sign.gameObject.AddComponent<BoxCollider>();
        signHit.center = new Vector3(0f, h - 0.2f, -d / 2f - 0.08f);
        signHit.size = new Vector3(1.5f, 0.45f, 0.12f);
        FarmInteractable buy = sign.gameObject.AddComponent<FarmInteractable>();
        buy.kind = FarmInteractable.Kind.Storage;
        buy.label = "склад: купити корм";
    }

    // A wooden trough in front of the coop. The grain in it shows up when you feed the hens.
    static void Feeder()
    {
        Transform feeder = Group("Feeder", world, FeederPosition);
        BoxCollider hit = feeder.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.17f, 0f);
        hit.size = new Vector3(1.25f, 0.4f, 0.5f);
        FarmInteractable action = feeder.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Feeder;
        action.label = "насипати зерна в годівницю";
        Shape("Bottom", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(1.1f, 0.04f, 0.3f), "Wood", feeder);
        foreach (float z in new[] { -0.15f, 0.15f })
            Shape("Side", PrimitiveType.Cube, new Vector3(0f, 0.19f, z), new Vector3(1.1f, 0.14f, 0.03f), "Wood", feeder);
        foreach (float x in new[] { -0.56f, 0.56f })
        {
            Shape("End", PrimitiveType.Cube, new Vector3(x, 0.17f, 0f), new Vector3(0.04f, 0.2f, 0.36f), "Wood", feeder);
            Shape("Leg", PrimitiveType.Cube, new Vector3(x, 0.05f, 0f), new Vector3(0.06f, 0.1f, 0.3f), "Wood", feeder);
        }
        Piece("Feeder Grain", PrimitiveType.Cube, new Vector3(0f, 0.17f, 0f), new Vector3(1.06f, 0.05f, 0.26f), "FarmProps/Grain", feeder)
            .SetActive(false);
        GlowRing(feeder, 0.8f, 0.02f);

        // The big feeder (Marquis sells it): a second trough right next to the first, 20 kg in all.
        Transform extra = Group("Feeder Extension", feeder, new Vector3(1.2f, 0f, 0f));
        Shape("Bottom", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(1.1f, 0.04f, 0.3f), "FreshWood", extra);
        foreach (float z in new[] { -0.15f, 0.15f })
            Shape("Side", PrimitiveType.Cube, new Vector3(0f, 0.19f, z), new Vector3(1.1f, 0.14f, 0.03f), "FreshWood", extra);
        Shape("End", PrimitiveType.Cube, new Vector3(0.56f, 0.17f, 0f), new Vector3(0.04f, 0.2f, 0.36f), "FreshWood", extra);
        Shape("Leg", PrimitiveType.Cube, new Vector3(0.56f, 0.05f, 0f), new Vector3(0.06f, 0.1f, 0.3f), "FreshWood", extra);
        Piece("Feeder Grain", PrimitiveType.Cube, new Vector3(0f, 0.17f, 0f), new Vector3(1.06f, 0.05f, 0.26f), "FarmProps/Grain", extra);
        extra.gameObject.SetActive(false);
    }

    // ---------- grandpa Panas's lucky horseshoes ----------

    static void Horseshoes()
    {
        for (int i = 0; i < HorseshoeCount; i++)
        {
            bool onWall = HorseshoeSpots[i].y > 0.5f;
            Transform shoe = Group("Horseshoe " + i, world, HorseshoeSpots[i]);
            shoe.localRotation = Quaternion.Euler(0f, HorseshoeYaw[i], 0f);
            BoxCollider hit = shoe.gameObject.AddComponent<BoxCollider>(); // generous, so the laser finds it
            hit.center = onWall ? new Vector3(0f, 0f, -0.1f) : new Vector3(0f, 0.12f, 0f);
            hit.size = onWall ? new Vector3(0.32f, 0.32f, 0.2f) : new Vector3(0.45f, 0.3f, 0.45f);
            FarmInteractable action = shoe.gameObject.AddComponent<FarmInteractable>();
            action.kind = FarmInteractable.Kind.Horseshoe;
            action.label = "підняти підкову";
            action.itemId = i.ToString();
            // On a wall it hangs like a "U" (the open end up: that's the lucky way).
            Transform model = Model(shoe, onWall ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 40f + i * 55f, 0f));
            Prop(model, "Horseshoe", "FarmProps/Iron", Vector3.zero, Vector3.zero, 1f);
            Transform ring = GlowRing(shoe, 0.17f, 0.015f);
            if (onWall)
            {
                ring.localPosition = new Vector3(0f, 0f, -0.02f);
                ring.localRotation = Quaternion.Euler(-90f, 0f, 0f); // a circle on the wall around it
            }
            shoe.gameObject.SetActive(false); // they appear when Boris asks you to find them
        }

        // Boris hangs them up "for luck" over the coop door (shown after you bring all five).
        Transform lucky = Group("Lucky Horseshoe", world, CoopPosition + new Vector3(0f, 2.33f, -2.015f));
        Prop(lucky, "Horseshoe", "FarmProps/Iron", Vector3.zero, new Vector3(90f, 0f, 0f), 1.4f);
        lucky.gameObject.SetActive(false);
    }

    // ---------- the buried chest ----------

    static void MoundAndChest()
    {
        Transform mound = Group("Garden Mound", world, MoundPosition);
        BoxCollider hit = mound.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.15f, 0f);
        hit.size = new Vector3(1.15f, 0.4f, 1.05f);
        FarmInteractable action = mound.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Mound;
        action.label = "оглянути горбочок землі";
        Piece("Earth", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(1f, 0.34f, 0.86f), "FarmProps/Soil", mound);
        Piece("Earth", PrimitiveType.Sphere, new Vector3(0.18f, 0.06f, -0.12f), new Vector3(0.55f, 0.24f, 0.5f), "FarmProps/Soil", mound);
        GameObject stick = Shape("Marker Stick", PrimitiveType.Cube, new Vector3(-0.25f, 0.3f, 0.2f), new Vector3(0.035f, 0.55f, 0.035f), "Wood", mound);
        stick.transform.localRotation = Quaternion.Euler(8f, 0f, -12f);
        GameObject bar = Shape("Marker Bar", PrimitiveType.Cube, new Vector3(-0.27f, 0.45f, 0.18f), new Vector3(0.2f, 0.03f, 0.03f), "Wood", mound);
        bar.transform.localRotation = Quaternion.Euler(8f, 0f, -12f);
        GlowRing(mound, 0.72f, 0.02f);

        // The chest: hidden until you dig. Its lid turns around its back edge ("Lid").
        Transform chest = Group("Buried Chest", world, MoundPosition);
        chest.localRotation = Quaternion.Euler(0f, -25f, 0f); // its front looks towards the garden
        BoxCollider chestHit = chest.gameObject.AddComponent<BoxCollider>();
        chestHit.center = new Vector3(0f, 0.25f, 0f);
        chestHit.size = new Vector3(0.85f, 0.5f, 0.6f);
        FarmInteractable open = chest.gameObject.AddComponent<FarmInteractable>();
        open.kind = FarmInteractable.Kind.Chest;
        open.label = "відкрити скриню";
        Shape("Body", PrimitiveType.Cube, new Vector3(0f, 0.16f, 0f), new Vector3(0.7f, 0.32f, 0.42f), "Wood", chest);
        foreach (float x in new[] { -0.22f, 0.22f })
            Piece("Band", PrimitiveType.Cube, new Vector3(x, 0.16f, 0f), new Vector3(0.045f, 0.33f, 0.43f), "FarmProps/Iron", chest);
        Transform treasure = Group("Treasure", chest, new Vector3(0f, 0.32f, 0f));
        for (int i = 0; i < 5; i++)
            Shape("Coin", PrimitiveType.Cylinder, new Vector3(-0.2f + i * 0.045f, 0.006f + (i % 2) * 0.008f, -0.06f + (i % 3) * 0.04f),
                new Vector3(0.05f, 0.004f, 0.05f), "Yellow", treasure);
        GameObject letter = Shape("Letter", PrimitiveType.Cylinder, new Vector3(0.12f, 0.025f, 0f), new Vector3(0.05f, 0.11f, 0.05f), "Paper", treasure);
        letter.transform.localRotation = Quaternion.Euler(0f, 70f, 90f);
        Transform packet = Group("Seed Packet", treasure, new Vector3(-0.05f, 0.01f, 0.08f));
        packet.localRotation = Quaternion.Euler(90f, 10f, 0f);
        SeedPacket(packet);
        Transform lid = Group("Lid", chest, new Vector3(0f, 0.32f, 0.21f));
        Shape("Lid Board", PrimitiveType.Cube, new Vector3(0f, 0.06f, -0.21f), new Vector3(0.72f, 0.12f, 0.44f), "Wood", lid);
        foreach (float x in new[] { -0.22f, 0.22f })
            Piece("Lid Band", PrimitiveType.Cube, new Vector3(x, 0.06f, -0.21f), new Vector3(0.05f, 0.13f, 0.45f), "FarmProps/Iron", lid);
        Piece("Lock", PrimitiveType.Cube, new Vector3(0f, -0.01f, -0.44f), new Vector3(0.08f, 0.1f, 0.03f), "ArrivalArt/Brass", lid);
        GlowRing(chest, 0.6f, 0.02f);
        chest.gameObject.SetActive(false);
    }

    // ---------- grandpa's garden ----------

    // A raised bed: a wooden frame with soil. "Weeds" grow on it at first; "Plants" are the carrots
    // (each a green top and an orange root that hides in the soil until it's ripe).
    static void Garden()
    {
        System.Random random = new System.Random(8);
        for (int i = 0; i < BedCount; i++)
        {
            Transform bed = Group("Garden Bed " + i, world, BedPosition(i));
            BoxCollider hit = bed.gameObject.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, 0.22f, 0f);
            hit.size = new Vector3(1f, 0.44f, 2.3f);
            FarmInteractable action = bed.gameObject.AddComponent<FarmInteractable>();
            action.kind = FarmInteractable.Kind.Bed;
            action.label = "оглянути грядку";
            action.itemId = i.ToString();

            // 40 cm tall, like real raised beds: easy to reach, and the meadow grass doesn't poke through.
            foreach (float x in new[] { -0.47f, 0.47f })
                Shape("Frame", PrimitiveType.Cube, new Vector3(x, BedHeight / 2f, 0f), new Vector3(0.06f, BedHeight, 2.3f), "Wood", bed);
            foreach (float z in new[] { -1.12f, 1.12f })
                Shape("Frame", PrimitiveType.Cube, new Vector3(0f, BedHeight / 2f, z), new Vector3(0.94f, BedHeight, 0.06f), "Wood", bed);
            Piece("Soil", PrimitiveType.Cube, new Vector3(0f, SoilTop / 2f, 0f), new Vector3(0.88f, SoilTop, 2.18f), "FarmProps/Soil", bed);

            Transform weeds = Group("Weeds", bed, new Vector3(0f, SoilTop, 0f));
            foreach (float z in new[] { -0.85f, -0.4f, 0f, 0.45f, 0.85f })
            {
                Transform grass = ArrivalArt.CountryModel(weeds, "WildGrass", "WildGrassSpray", Rand(random, 2.8f, 3.6f), random.Next(360));
                if (grass != null) grass.localPosition = new Vector3(Rand(random, -0.25f, 0.25f), 0f, z);
            }
            Transform flower = ArrivalArt.CountryModel(weeds, "Wildflower", "WildflowerSpray", 0.5f, random.Next(360));
            if (flower != null) flower.localPosition = new Vector3(0.1f, 0f, 0.35f);

            Transform plants = Group("Plants", bed, new Vector3(0f, SoilTop, 0f));
            for (int p = 0; p < CarrotsPerBed; p++)
            {
                Transform plant = Group("Carrot", plants, new Vector3(0f, 0f, p == 0 ? -0.5f : 0.5f));
                plant.localRotation = Quaternion.Euler(0f, random.Next(360), 0f);
                Prop(plant, "CarrotLeaves", "FarmProps/CarrotTop", Vector3.zero, Vector3.zero, 1.15f).name = "Leaves";
                // The root sticks out of the soil just a little (shown when the carrot is ripe).
                Prop(plant, "CarrotRoot", "FarmProps/CarrotSkin", new Vector3(0f, 0.012f, 0f), Vector3.zero, 1.15f).name = "Root";
            }
            plants.gameObject.SetActive(false);
        }

        // The watering can, by the water barrel next to the house.
        Transform can = Item("Watering Can", "can", "взяти лійку", CanPosition);
        Transform canModel = Model(can, Quaternion.Euler(0f, 200f, 0f));
        Prop(canModel, "WateringCan", "FarmProps/CanPaint", Vector3.zero, Vector3.zero, 1.2f);

        // What a carrot looks like in the inventory.
        Transform look = Group("Carrot Look", world, Vector3.zero);
        Transform lookModel = Model(look, Quaternion.identity);
        Prop(lookModel, "CarrotRoot", "FarmProps/CarrotSkin", new Vector3(0f, 0.17f, 0f), Vector3.zero, 1f);
        Prop(lookModel, "CarrotLeaves", "FarmProps/CarrotTop", new Vector3(0f, 0.17f, 0f), Vector3.zero, 0.8f);
        look.gameObject.SetActive(false);
    }

    // ---------- the farmhouse: now and after the repair ----------

    static void HouseRepair()
    {
        // The old, shabby bits (they go away during the repair): boards nailed over the windows,
        // the old door hanging open, a broken chair on the floor.
        Transform boards = Group("House Boards", world, HousePosition);
        foreach (float x in new[] { -2.2f, 2.2f })
        {
            GameObject board = Shape("Board", PrimitiveType.Cube, new Vector3(x, 2.8f, -3.09f), new Vector3(1.3f, 0.16f, 0.05f), "DarkWood", boards);
            board.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            board = Shape("Board", PrimitiveType.Cube, new Vector3(x, 2.8f, -3.1f), new Vector3(1.3f, 0.16f, 0.05f), "DarkWood", boards);
            board.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        }
        Transform oldHinge = Group("Old Door", boards, DoorHinge);
        oldHinge.localRotation = Quaternion.Euler(0f, -100f, 0f); // open into the house, out of your way
        Shape("Door", PrimitiveType.Cube, new Vector3(0.6f, 1.13f, 0f), new Vector3(1.18f, 2.24f, 0.05f), "DarkWood", oldHinge);
        GameObject brokenSeat = Shape("Broken Chair", PrimitiveType.Cube, new Vector3(-0.9f, 0.08f, 0.9f), new Vector3(0.45f, 0.05f, 0.45f), "Wood", boards);
        brokenSeat.transform.localRotation = Quaternion.Euler(8f, 35f, 70f);
        GameObject brokenLeg = Shape("Broken Chair", PrimitiveType.Cube, new Vector3(-0.55f, 0.03f, 1.2f), new Vector3(0.04f, 0.04f, 0.45f), "Wood", boards);
        brokenLeg.transform.localRotation = Quaternion.Euler(0f, 60f, 0f);

        // What you point at outside: the front of the house, with the doorway left free so you can walk in.
        Transform house = Group("Farmhouse", world, HousePosition);
        foreach (float side in new[] { -1f, 1f })
        {
            BoxCollider wall = house.gameObject.AddComponent<BoxCollider>();
            wall.center = new Vector3(side * 2.125f, 2.25f, -3.1f);
            wall.size = new Vector3(2.95f, 4.5f, 0.16f);
        }
        BoxCollider overDoor = house.gameObject.AddComponent<BoxCollider>();
        overDoor.center = new Vector3(0f, 3.4f, -3.1f);
        overDoor.size = new Vector3(1.3f, 2.2f, 0.16f);
        FarmInteractable action = house.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.House;
        action.label = "оглянути старий будинок";
        Transform ring = GlowRing(house, 0.9f, 0.03f);
        ring.localPosition = new Vector3(0f, 0.03f, -3.9f); // in front of the door

        // The old bed inside: sleep here until morning.
        Transform bed = Group("House Bed", world, HousePosition + new Vector3(-2.75f, 0f, 1.4f));
        BoxCollider bedHit = bed.gameObject.AddComponent<BoxCollider>();
        bedHit.center = new Vector3(0f, 0.36f, 0f);
        bedHit.size = new Vector3(1f, 0.72f, 2f);
        FarmInteractable sleep = bed.gameObject.AddComponent<FarmInteractable>();
        sleep.kind = FarmInteractable.Kind.Sleep;
        sleep.label = "поспати до ранку";

        // The repaired look. Every part is a direct child, so it can pop in part by part.
        Transform fresh = Group("House Repairs", world, HousePosition);
        Transform newHinge = Group("New Door", fresh, DoorHinge);
        newHinge.localRotation = Quaternion.Euler(0f, -100f, 0f);
        Piece("Door", PrimitiveType.Cube, new Vector3(0.6f, 1.14f, 0f), new Vector3(1.2f, 2.26f, 0.06f), "FarmProps/DoorPaint", newHinge);
        Piece("Door Knob", PrimitiveType.Sphere, new Vector3(1.05f, 1.1f, -0.05f), Vector3.one * 0.07f, "ArrivalArt/Brass", newHinge);
        Piece("Porch Step", PrimitiveType.Cube, new Vector3(0f, 0.08f, -3.6f), new Vector3(2.4f, 0.16f, 1f), "CountryArt/Boards", fresh);
        // Inside: a new quilt, a rug, flowers and a warm lamp on the table.
        Piece("New Quilt", PrimitiveType.Cube, new Vector3(-2.75f, 0.645f, 1f), new Vector3(1f, 0.06f, 1.3f), "ArrivalArt/QuiltFabric", fresh);
        Piece("Rug", PrimitiveType.Cube, new Vector3(0f, 0.066f, 0.1f), new Vector3(2.2f, 0.012f, 1.5f), "ArrivalArt/LampLinen", fresh);
        Transform vase = Group("Table Flowers", fresh, new Vector3(1.75f, 0.79f, 1.55f));
        Piece("Pot", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f), Vector3.one * 0.12f, "ArrivalArt/Brass", vase);
        Transform posy = ArrivalArt.CountryModel(vase, "Wildflower", "WildflowerSpray", 0.3f, 40f);
        if (posy != null) posy.localPosition = new Vector3(0f, 0.12f, 0f);
        Transform tableLamp = Group("Table Lamp", fresh, new Vector3(2.35f, 0.79f, 1.55f));
        Piece("Base", PrimitiveType.Cube, new Vector3(0f, 0.015f, 0f), new Vector3(0.12f, 0.03f, 0.12f), "ArrivalArt/Brass", tableLamp);
        Piece("Glow", PrimitiveType.Cube, new Vector3(0f, 0.11f, 0f), new Vector3(0.09f, 0.15f, 0.09f), "FarmProps/LampGlow", tableLamp);
        Piece("Cap", PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.13f, 0.03f, 0.13f), "ArrivalArt/Brass", tableLamp);
        Light warm = new GameObject("Lamp Light").AddComponent<Light>();
        warm.transform.SetParent(tableLamp, false);
        warm.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        warm.type = LightType.Point;
        warm.range = 5f;
        warm.intensity = 1.1f;
        warm.color = new Color(1f, 0.82f, 0.55f);
        warm.shadows = LightShadows.None;

        Transform lamp = Group("Lantern", fresh, new Vector3(1.05f, 2.3f, -3.2f));
        Piece("Bracket", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.1f), new Vector3(0.03f, 0.03f, 0.2f), "FarmProps/Iron", lamp);
        Piece("Light", PrimitiveType.Cube, Vector3.zero, new Vector3(0.12f, 0.18f, 0.12f), "FarmProps/LampGlow", lamp);
        Piece("Cap", PrimitiveType.Cube, new Vector3(0f, 0.11f, 0f), new Vector3(0.17f, 0.04f, 0.17f), "ArrivalArt/Brass", lamp);
        Piece("Base", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(0.14f, 0.03f, 0.14f), "ArrivalArt/Brass", lamp);

        System.Random random = new System.Random(15);
        foreach (float x in new[] { -2.2f, 2.2f })
        {
            Transform box = Group("Flower Box", fresh, new Vector3(x, 1.98f, -3.36f));
            Shape("Box", PrimitiveType.Cube, Vector3.zero, new Vector3(1.2f, 0.2f, 0.24f), "Wood", box);
            Piece("Soil", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(1.12f, 0.02f, 0.18f), "FarmProps/Soil", box);
            foreach (float fx in new[] { -0.4f, 0f, 0.4f })
            {
                Transform flowers = ArrivalArt.CountryModel(box, "Wildflower", "WildflowerSpray", 0.42f, random.Next(360));
                if (flowers != null) flowers.localPosition = new Vector3(fx, 0.1f, 0f);
            }
        }

        Transform bench = Group("Bench", fresh, new Vector3(-2.2f, 0f, -3.72f));
        Shape("Seat", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.06f, 0.38f), "Wood", bench);
        Shape("Back", PrimitiveType.Cube, new Vector3(0f, 0.76f, 0.16f), new Vector3(1.4f, 0.3f, 0.05f), "Wood", bench);
        foreach (float x in new[] { -0.6f, 0.6f })
            Shape("Leg", PrimitiveType.Cube, new Vector3(x, 0.22f, 0f), new Vector3(0.06f, 0.44f, 0.32f), "Wood", bench);
        fresh.gameObject.SetActive(false);
    }

    // A paper packet of carrot seeds with a little carrot drawn on it (Marquis sells them, the chest has one).
    static void SeedPacket(Transform parent)
    {
        Shape("Packet", PrimitiveType.Cube, new Vector3(0f, 0.075f, 0f), new Vector3(0.11f, 0.15f, 0.015f), "Paper", parent);
        GameObject root = Piece("Picture", PrimitiveType.Cube, new Vector3(-0.005f, 0.065f, -0.009f), new Vector3(0.022f, 0.07f, 0.003f), "FarmProps/CarrotSkin", parent);
        root.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        Piece("Picture", PrimitiveType.Cube, new Vector3(0.008f, 0.108f, -0.009f), new Vector3(0.03f, 0.022f, 0.003f), "FarmProps/CarrotTop", parent);
    }

    // ---------- the old well (a horseshoe hangs on it; later: water) ----------

    public static Vector3 WellPosition => new Vector3(5.2f, 0f, 8.8f);

    static void Well()
    {
        Transform well = Group("Old Well", statics, WellPosition);
        Shape("Well Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(1.1f, 0.4f, 1.1f), "House", well, true);
        Shape("Water", PrimitiveType.Cylinder, new Vector3(0f, 0.7f, 0f), new Vector3(0.92f, 0.01f, 0.92f), "Glass", well);
        foreach (float x in new[] { -0.55f, 0.55f })
            Shape("Well Post", PrimitiveType.Cube, new Vector3(x, 0.95f, 0f), new Vector3(0.1f, 1.9f, 0.1f), "Wood", well, true);
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject roof = Shape("Well Roof", PrimitiveType.Cube, new Vector3(0f, 2.05f, side * 0.28f), new Vector3(1.4f, 0.05f, 0.7f), "Roof", well);
            roof.transform.localRotation = Quaternion.Euler(side * 35f, 0f, 0f);
        }
        GameObject axle = Shape("Axle", PrimitiveType.Cylinder, new Vector3(0f, 1.45f, 0f), new Vector3(0.12f, 0.55f, 0.12f), "Wood", well);
        axle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Shape("Rope", PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f), new Vector3(0.015f, 0.5f, 0.015f), "Paper", well);
        Piece("Bucket", PrimitiveType.Cylinder, new Vector3(0f, 0.92f, 0f), new Vector3(0.25f, 0.12f, 0.25f), "ArrivalArt/CoachMetal", well);
        Piece("Crank", PrimitiveType.Cube, new Vector3(0.66f, 1.45f, 0.08f), new Vector3(0.03f, 0.03f, 0.18f), "FarmProps/Iron", well);

        // Point at the well to fill the watering can.
        Transform water = Group("Well Water", world, WellPosition);
        BoxCollider hit = water.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.55f, 0f);
        hit.size = new Vector3(1.25f, 1.1f, 1.25f);
        FarmInteractable action = water.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Well;
        action.label = "набрати води в лійку";
    }

    // Where the big animals graze (the left part of the yard, the lawn) and what they walk around.
    public static Vector2 PastureMin => new Vector2(-7.2f, -1f);
    public static Vector2 PastureMax => new Vector2(8.2f, 11.2f);
    public static readonly Rect[] PastureObstacles = { new Rect(4.3f, 7.9f, 1.8f, 1.8f), new Rect(-7.7f, 8.2f, 4.6f, 2.8f), new Rect(-7.8f, 3.1f, 3.1f, 2.8f) }; // the well, the storage shed and its hay, the cheese house
    public static Vector3 BarnDoorOutside => new Vector3(0f, 0f, 10.9f);
    public static Vector3 BarnDoorInside => new Vector3(0f, 0f, 14f);
    public static Vector3 CowStall(int i) => i < 2 ? new Vector3(-4.6f, 0f, 15.5f + i * 3f) : new Vector3(-4.6f, 0f, 13.4f);
    public static Vector3 GoatPen(int i) => new Vector3(4.7f, 0f, 14f + i * 1.6f);
    // The last cow and the last two goats only fit after the barn is made bigger (Marquis sells that).
    public static readonly string[] CowNames = { "Зірка", "Ласка", "Ромашка" };
    public static readonly string[] GoatNames = { "Мілка", "Белла", "Сніжка", "Кнопка", "Лялька" };
    public const int CowsBeforeUpgrade = 2, GoatsBeforeUpgrade = 3;

    // ---------- the barn's big doors (they swing open in FarmPrototype.Barn.cs) ----------

    static void BarnDoors()
    {
        Vector3 barn = new Vector3(0f, 0f, 17f);
        Transform doors = Group("Barn Doors", world, barn);
        BoxCollider hit = doors.gameObject.AddComponent<BoxCollider>(); // also keeps you out while they're shut
        hit.center = new Vector3(0f, 2.1f, -4.6f);
        hit.size = new Vector3(4.5f, 4.3f, 0.3f);
        FarmInteractable action = doors.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Barn;
        action.label = "оглянути стару стодолу";
        foreach (float side in new[] { -1f, 1f })
        {
            Transform hinge = Group(side < 0 ? "Left Door" : "Right Door", doors, new Vector3(side * 2.25f, 0f, -4.55f));
            float mid = -side * 1.1f;
            Shape("Door", PrimitiveType.Cube, new Vector3(mid, 2.1f, 0f), new Vector3(2.2f, 4.2f, 0.1f), "DarkWood", hinge);
            foreach (float tilt in new[] { 27f, -27f })
            {
                GameObject cross = Shape("Trim", PrimitiveType.Cube, new Vector3(mid, 2.1f, -0.06f), new Vector3(0.12f, 4.6f, 0.05f), "Paint", hinge);
                cross.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }
            Shape("Trim", PrimitiveType.Cube, new Vector3(mid * 2f + side * 0.06f, 2.15f, -0.06f), new Vector3(0.06f, 4.3f, 0.06f), "Paint", hinge);
        }
        Transform padlock = Group("Lock", doors, new Vector3(0f, 1.6f, -4.67f));
        Piece("Chain", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.9f, 0.04f, 0.03f), "FarmProps/Iron", padlock);
        Piece("Padlock", PrimitiveType.Cube, new Vector3(0f, -0.08f, -0.02f), new Vector3(0.13f, 0.15f, 0.06f), "FarmProps/Iron", padlock);
        Piece("Shackle", PrimitiveType.Cube, new Vector3(0f, 0.01f, -0.02f), new Vector3(0.09f, 0.06f, 0.02f), "ArrivalArt/Brass", padlock);
        Transform ring = GlowRing(doors, 1.2f, 0.03f);
        ring.localPosition = new Vector3(0f, 0.03f, -5.8f);
    }

    // ---------- cows and goats (hidden until you buy them; they live in the barn) ----------

    static void Animals()
    {
        for (int i = 0; i < CowNames.Length; i++) Cow(i);
        for (int i = 0; i < GoatNames.Length; i++) SheGoat(i);
    }

    static Transform Animal(string objectName, string animalName, Vector3 at, float radius, float height)
    {
        Transform animal = Group(objectName, world, at);
        animal.gameObject.layer = PlayerBody.WalkThroughLayer;
        SphereCollider hit = animal.gameObject.AddComponent<SphereCollider>();
        hit.center = new Vector3(0f, height, -0.1f);
        hit.radius = radius;
        FarmInteractable action = animal.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Animal;
        action.label = "погладити";
        action.itemId = animalName;
        GlowRing(animal, radius + 0.2f, 0.03f, 0.8f); // shines when the animal can be milked
        return animal;
    }

    static void Grazing(Transform animal, Transform head, float speed)
    {
        Grazer grazer = animal.gameObject.AddComponent<Grazer>();
        grazer.head = head;
        grazer.areaMin = PastureMin;
        grazer.areaMax = PastureMax;
        grazer.avoid = PastureObstacles;
        grazer.speed = speed;
        animal.gameObject.SetActive(false);
    }

    // A black-and-white cow, about 1.4 m at the shoulder.
    static void Cow(int i)
    {
        Transform cow = Animal("Cow " + i, CowNames[i], BarnDoorInside, 0.75f, 1.0f);
        Transform body = Group("Body", cow, Vector3.zero);
        Prop(body, "CowBody", "FarmProps/CowCoat", Vector3.zero, Vector3.zero, 1f);
        Transform head = Group("Head", body, new Vector3(0f, 1.26f, -1.04f));
        Prop(head, "CowHead", "FarmProps/CowCoat", Vector3.zero, Vector3.zero, 1f);
        Prop(head, "CowHorns", "CountryArt/HornKeratin", Vector3.zero, Vector3.zero, 1f);
        Shape("Nose", PrimitiveType.Sphere, new Vector3(0f, -0.2f, -0.49f), new Vector3(0.2f, 0.12f, 0.08f), "Nose", head);
        foreach (float side in new[] { -1f, 1f })
            Shape("Eye", PrimitiveType.Sphere, new Vector3(side * 0.125f, 0.02f, -0.14f), Vector3.one * 0.045f, "Eyes", head);
        GoatGait gait = cow.gameObject.AddComponent<GoatGait>();
        gait.body = body;
        gait.stepsPerMeter = 5f;
        gait.legs = new Transform[4];
        Vector3[] hips = { new Vector3(-0.17f, 1f, -0.55f), new Vector3(0.17f, 1f, -0.55f), new Vector3(-0.17f, 1f, 0.58f), new Vector3(0.17f, 1f, 0.58f) };
        for (int k = 0; k < 4; k++)
        {
            Transform leg = Group("Leg", cow, hips[k]);
            Prop(leg, "CowLeg", "FarmProps/CowCoat", Vector3.zero, Vector3.zero, 1f);
            Prop(leg, "CowHoof", "CountryArt/Hoof", new Vector3(0f, -1f, 0f), Vector3.zero, 1f);
            gait.legs[k] = leg;
        }
        Grazing(cow, head, 0.3f);
    }

    // A white she-goat: Boris's model, a bit smaller, with small horns.
    static void SheGoat(int i)
    {
        Transform goat = Animal("She-goat " + i, GoatNames[i], BarnDoorInside, 0.42f, 0.55f);
        Transform body = Group("Body", goat, Vector3.zero);
        body.localScale = Vector3.one * 0.9f;
        Prop(body, "GoatBody", "FarmProps/GoatCoatWhite", Vector3.zero, Vector3.zero, 1f);
        Transform head = Group("Head", body, new Vector3(0f, 0.84f, -0.42f));
        Prop(head, "GoatHead", "FarmProps/GoatCoatWhite", Vector3.zero, Vector3.zero, 1f);
        Prop(head, "GoatHorns", "CountryArt/HornKeratin", Vector3.zero, Vector3.zero, 0.55f);
        foreach (float side in new[] { -1f, 1f })
            Shape("Eye", PrimitiveType.Sphere, new Vector3(side * 0.056f, 0.012f, -0.075f), new Vector3(0.026f, 0.024f, 0.026f), "Eyes", head);
        GoatGait gait = goat.gameObject.AddComponent<GoatGait>();
        gait.body = body;
        gait.legs = new Transform[4];
        Vector3[] hips = { new Vector3(-0.07f, 0.5f, -0.22f), new Vector3(0.07f, 0.5f, -0.22f), new Vector3(-0.07f, 0.5f, 0.29f), new Vector3(0.07f, 0.5f, 0.29f) };
        for (int k = 0; k < 4; k++)
        {
            Transform leg = Group("Leg", goat, hips[k]);
            leg.localScale = Vector3.one * 0.9f;
            Prop(leg, k < 2 ? "GoatFrontLeg" : "GoatHindLeg", "FarmProps/GoatLegs", Vector3.zero, Vector3.zero, 1f);
            Prop(leg, "GoatHoof", "CountryArt/Hoof", new Vector3(0f, -0.545f, 0f), Vector3.zero, 1f);
            gait.legs[k] = leg;
        }
        Grazing(goat, head, 0.32f);
    }

    // What the barn key looks like (in the inventory and when it turns in the lock).
    static void KeyLook()
    {
        Transform key = Group("Barn Key Look", world, Vector3.zero);
        Transform model = Model(key, Quaternion.identity);
        GameObject bow = Piece("Key Ring", PrimitiveType.Cylinder, new Vector3(-0.07f, 0f, 0f), new Vector3(0.06f, 0.006f, 0.06f), "ArrivalArt/Brass", model);
        bow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Piece("Key Shaft", PrimitiveType.Cube, new Vector3(0.02f, 0f, 0f), new Vector3(0.13f, 0.014f, 0.014f), "ArrivalArt/Brass", model);
        Piece("Key Bit", PrimitiveType.Cube, new Vector3(0.07f, -0.016f, 0f), new Vector3(0.025f, 0.03f, 0.012f), "ArrivalArt/Brass", model);
        key.gameObject.SetActive(false);

        MilkBottle("Milk Bottle Look", "ToolboxRed");  // cow's milk: a red cap
        MilkBottle("Goat Milk Look", "Leaves");        // goat's milk: a green cap
    }

    // A glass bottle of milk (in the backpack, in the shop window, when you milk).
    static void MilkBottle(string name, string capColor)
    {
        Transform bottle = Group(name, world, Vector3.zero);
        Transform model = Model(bottle, Quaternion.identity);
        Shape("Milk", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.07f, 0.08f, 0.07f), "Paper", model);
        Shape("Neck", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f), new Vector3(0.04f, 0.03f, 0.04f), "Paper", model);
        Shape("Cap", PrimitiveType.Cylinder, new Vector3(0f, 0.215f, 0f), new Vector3(0.045f, 0.008f, 0.045f), capColor, model);
        bottle.gameObject.SetActive(false);
    }

    // ---------- fun: "Курячий політ" - a big slingshot, hens in helmets, hay targets ----------
    // In the field between the farm fence and the road, right of the gate. The game is in
    // FarmPrototype.Slingshot.cs; the bands and the pouch move, so the slingshot is not "Static".

    public static Vector3 SlingshotPosition => new Vector3(7.5f, 0f, -6.5f);
    public static readonly Vector3[] TargetSpots = { new Vector3(12.5f, 0f, -5.4f), new Vector3(15f, 0f, -7.6f), new Vector3(17.6f, 0f, -5.2f) };

    static void ChickenFlight()
    {
        Transform sling = Group("Slingshot", world, SlingshotPosition);
        sling.localRotation = Quaternion.Euler(0f, 90f, 0f); // shoots along +x, towards the targets
        BoxCollider hit = sling.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 1f, 0f);
        hit.size = new Vector3(1.4f, 2f, 0.6f);
        FarmInteractable action = sling.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Slingshot;
        action.label = "«Курячий політ»: грати";
        Shape("Trunk", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.22f, 0.55f, 0.22f), "Wood", sling, true);
        foreach (float side in new[] { -1f, 1f })
        {
            GameObject arm = Shape("Arm", PrimitiveType.Cylinder, new Vector3(side * 0.32f, 1.45f, 0f), new Vector3(0.16f, 0.45f, 0.16f), "Wood", sling);
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 28f);
            Shape("Band", PrimitiveType.Cube, new Vector3(side * 0.3f, 1.75f, -0.2f), new Vector3(0.03f, 0.03f, 0.4f), "Eyes", sling).name = side < 0 ? "Band L" : "Band R";
            Group(side < 0 ? "Tip L" : "Tip R", sling, new Vector3(side * 0.53f, 1.82f, 0f));
        }
        Transform pouch = Group("Pouch", sling, new Vector3(0f, 1.72f, -0.35f));
        Shape("Leather", PrimitiveType.Cube, Vector3.zero, new Vector3(0.3f, 0.08f, 0.22f), "DarkWood", pouch);
        GlowRing(sling, 1f, 0.03f, 1f).gameObject.SetActive(true);

        Transform sign = Group("Flight Sign", statics, SlingshotPosition + new Vector3(-0.6f, 0f, 1.4f));
        Shape("Post", PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), "Wood", sign, true);
        Shape("Board", PrimitiveType.Cube, new Vector3(0f, 1.75f, -0.06f), new Vector3(1.5f, 0.45f, 0.05f), "FreshWood", sign);
        SignText(sign, "КУРЯЧИЙ ПОЛІТ", new Vector3(0f, 1.75f, -0.1f), 0.022f);

        string[] points = { "5", "3", "2", "1" };
        foreach (Vector3 spot in TargetSpots)
        {
            Transform target = Group("Hay Target", statics, spot);
            Shape("Hay", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(2.8f, 0.08f, 2.8f), "Straw", target, true);
            Shape("Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.17f, 0f), new Vector3(2f, 0.01f, 2f), "ToolboxRed", target);
            Shape("Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f), new Vector3(1.3f, 0.01f, 1.3f), "Paint", target);
            Shape("Bullseye", PrimitiveType.Cylinder, new Vector3(0f, 0.19f, 0f), new Vector3(0.7f, 0.01f, 0.7f), "ToolboxRed", target);
            Shape("Flag Pole", PrimitiveType.Cube, new Vector3(1.5f, 0.6f, 0f), new Vector3(0.05f, 1.2f, 0.05f), "Wood", target);
            Shape("Flag", PrimitiveType.Cube, new Vector3(1.5f, 1.1f, -0.15f), new Vector3(0.03f, 0.22f, 0.3f), "Yellow", target);
        }
    }

    // ---------- fun: "Перегони" on the road (the bus is gone): Boris vs Zhora vs Marquis ----------
    // You don't ride (that would make you dizzy in VR): you stand on a little wooden stand by the
    // road and spur Boris on. The game is in FarmPrototype.Race.cs.

    public const float RaceStartX = 8f, RaceFinishX = 31f;
    public static readonly float[] RaceLanes = { -14.4f, -16f, -17.6f }; // Boris, Zhora, Marquis
    public static Vector3 RaceStandSpot => new Vector3(19.5f, 0f, -11.4f);

    static void RaceTrack()
    {
        Transform track = Group("Race Track", statics, Vector3.zero);
        RaceArch(track, RaceStartX, "СТАРТ", false);
        RaceArch(track, RaceFinishX, "ФІНІШ", true);
        foreach (float z in new[] { -15.2f, -16.8f })
            Shape("Lane Line", PrimitiveType.Cube, new Vector3((RaceStartX + RaceFinishX) / 2f, 0.05f, z), new Vector3(RaceFinishX - RaceStartX, 0.01f, 0.08f), "Paint", track);
        // the stand: a small wooden platform with a railing, facing the road
        Transform stand = Group("Race Stand", track, RaceStandSpot);
        Shape("Platform", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(2.2f, 0.2f, 1.4f), "Wood", stand);
        Shape("Rail", PrimitiveType.Cube, new Vector3(0f, 1f, -0.68f), new Vector3(2.2f, 0.08f, 0.08f), "Wood", stand);
        foreach (float x in new[] { -1.05f, 1.05f })
            Shape("Rail Post", PrimitiveType.Cube, new Vector3(x, 0.55f, -0.68f), new Vector3(0.08f, 1f, 0.08f), "Wood", stand);

        // Point at this to start a race.
        Transform sign = Group("Race Sign", world, new Vector3(RaceStartX - 1.2f, 0f, -11.6f));
        BoxCollider hit = sign.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 1.3f, 0f);
        hit.size = new Vector3(1.6f, 1.2f, 0.3f);
        FarmInteractable action = sign.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Race;
        action.label = "«Перегони»: почати";
        Shape("Post", PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), "Wood", sign);
        Shape("Board", PrimitiveType.Cube, new Vector3(0f, 1.5f, -0.06f), new Vector3(1.5f, 0.5f, 0.05f), "FreshWood", sign);
        SignText(sign, "ПЕРЕГОНИ", new Vector3(0f, 1.55f, -0.1f), 0.024f);
        SignText(sign, "Борис • Жора • Маркіз", new Vector3(0f, 1.38f, -0.1f), 0.012f);
        GlowRing(sign, 0.8f, 0.03f, 1f).gameObject.SetActive(true);
    }

    // An arch across the road: two posts and a banner (the finish one is chequered).
    static void RaceArch(Transform parent, float x, string word, bool chequered)
    {
        Transform arch = Group(word + " Arch", parent, new Vector3(x, 0f, -16f));
        foreach (float z in new[] { -3.9f, 3.9f })
            Shape("Arch Post", PrimitiveType.Cube, new Vector3(0f, 1.7f, z), new Vector3(0.18f, 3.4f, 0.18f), "Wood", arch, true);
        Shape("Banner", PrimitiveType.Cube, new Vector3(0f, 3.2f, 0f), new Vector3(0.06f, 0.6f, 7.6f), "FreshWood", arch);
        if (chequered)
            for (int i = 0; i < 16; i++)
                Shape("Check", PrimitiveType.Cube, new Vector3(-0.035f, 3.05f + (i % 2) * 0.3f, -3.6f + (i / 2) * 1.03f + (i % 2) * 0.5f), new Vector3(0.01f, 0.28f, 0.5f), (i / 2) % 2 == 0 ? "Eyes" : "Paint", arch);
        // The word faces the race stand (which is between the two arches).
        float side = chequered ? -1f : 1f;
        SignText(arch, word, new Vector3(side * 0.06f, 3.2f, 0f), 0.05f);
        arch.GetChild(arch.childCount - 1).localRotation = Quaternion.Euler(0f, -90f * side, 0f);
    }

    // ---------- fun: "Тир Маркіза" - knock down cans and wooden ducks with pebbles ----------
    // A booth with a striped awning in the field by the road, right of the bus stop. You stand at the
    // counter (facing the road); cans stand on a shelf, ducks slide on a rail above them.
    // The game (and the cans and ducks themselves) are in FarmPrototype.Gallery.cs.

    public static Vector3 GalleryPosition => new Vector3(11.5f, 0f, -9.8f);
    public const float GalleryShelfY = 1.06f, GalleryShelfZ = -11.6f, GalleryRailY = 1.55f;

    static void ShootingGallery()
    {
        Transform booth = Group("Shooting Gallery", statics, GalleryPosition);
        Shape("Counter", PrimitiveType.Cube, new Vector3(0f, 0.5f, 1.15f), new Vector3(3.2f, 1f, 0.5f), "Wood", booth, true);
        Shape("Counter Top", PrimitiveType.Cube, new Vector3(0f, 1.02f, 1.1f), new Vector3(3.4f, 0.06f, 0.7f), "FreshWood", booth);
        Shape("Back Board", PrimitiveType.Cube, new Vector3(0f, 1.3f, -2.05f), new Vector3(3.8f, 2.2f, 0.08f), "Barn", booth, true);
        Shape("Shelf", PrimitiveType.Cube, new Vector3(0f, GalleryShelfY - 0.03f, GalleryShelfZ - GalleryPosition.z), new Vector3(3.4f, 0.06f, 0.35f), "Wood", booth);
        Shape("Rail", PrimitiveType.Cube, new Vector3(0f, GalleryRailY - 0.12f, -1.95f), new Vector3(3.4f, 0.04f, 0.06f), "Metal", booth);
        foreach (float x in new[] { -1.85f, 1.85f })
            foreach (float z in new[] { -2.05f, 1.15f })
                Shape("Post", PrimitiveType.Cube, new Vector3(x, 1.3f, z), new Vector3(0.1f, 2.6f, 0.1f), "DarkWood", booth, true);
        for (int i = 0; i < 8; i++)
        {
            GameObject stripe = Shape("Awning", PrimitiveType.Cube, new Vector3(-1.75f + i * 0.5f, 2.65f, -0.45f), new Vector3(0.5f, 0.05f, 3.5f), i % 2 == 0 ? "ToolboxRed" : "Paint", booth);
            stripe.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
        }
        Shape("Sign Board", PrimitiveType.Cube, new Vector3(0f, 2.35f, 1.25f), new Vector3(2.2f, 0.4f, 0.05f), "FreshWood", booth);
        SignText(booth, "ТИР МАРКІЗА", new Vector3(0f, 2.35f, 1.3f), 0.024f);
        booth.Find("Sign Text").localRotation = Quaternion.Euler(0f, 180f, 0f); // faces the farm side, where you stand

        // Point at the counter to play.
        Transform start = Group("Gallery Counter", world, GalleryPosition + new Vector3(0f, 0f, 1.15f));
        BoxCollider hit = start.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.75f, 0.1f);
        hit.size = new Vector3(3.3f, 0.5f, 0.6f);
        FarmInteractable action = start.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Gallery;
        action.label = "«Тир Маркіза»: грати";
        Transform ring = GlowRing(start, 0.8f, 0.03f, 1f);
        ring.localPosition = new Vector3(0f, 0.03f, 0.9f);
        ring.gameObject.SetActive(true);
    }

    // Bunting along the farm side of the road, from Marquis's stall to the race finish (fair days only).
    static void FairFlags()
    {
        Transform flags = Group("Fair Flags", world, Vector3.zero);
        string[] colors = { "Yellow", "ToolboxRed", "Teleport", "Leaves", "Paint" };
        const float z = -12.2f; // just at the road's edge
        int flag = 0;
        // two runs: around the shooting gallery's booth there's no room for posts
        foreach (Vector2 run in new[] { new Vector2(-3f, 9f), new Vector2(14.5f, 30.5f) })
        {
            int spans = Mathf.RoundToInt((run.y - run.x) / 4f);
            float step = (run.y - run.x) / spans;
            for (int span = 0; span <= spans; span++)
            {
                float x = run.x + span * step;
                Shape("Flag Post", PrimitiveType.Cube, new Vector3(x, 1.25f, z), new Vector3(0.07f, 2.5f, 0.07f), "Wood", flags);
                if (span == spans) break;
                for (int i = 1; i < 8; i++)
                {
                    float k = i / 8f;
                    float sag = Mathf.Sin(k * Mathf.PI) * 0.3f;
                    GameObject f = Shape("Flag", PrimitiveType.Cube, new Vector3(x + k * step, 2.35f - sag, z), new Vector3(0.18f, 0.18f, 0.01f), colors[flag++ % colors.Length], flags);
                    f.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
            }
        }
        flags.gameObject.SetActive(false);
    }

    // ---------- the cheese house (Marquis sells it after the barn opens) ----------
    // A small hut in the left corner of the yard: 2 bottles of milk -> 1 cheese (FarmPrototype.Cheese.cs).
    public static Vector3 CheesePosition => new Vector3(-6.3f, 0f, 4.5f);

    static void CheeseHouse()
    {
        Transform hut = Group("Cheese House", world, CheesePosition);
        hut.localRotation = Quaternion.Euler(0f, -90f, 0f); // its front (-z) looks into the yard (+x)
        BoxCollider hit = hut.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 1.1f, -0.2f);
        hit.size = new Vector3(2.4f, 2.2f, 2.2f);
        FarmInteractable action = hut.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.CheesePress;
        action.label = "сироварня";
        // every part is a direct child, so it can be built part by part
        Shape("Walls", PrimitiveType.Cube, new Vector3(0f, 0.9f, 0.2f), new Vector3(2.2f, 1.8f, 1.8f), "House", hut);
        GameObject roofL = Shape("Roof", PrimitiveType.Cube, new Vector3(-0.6f, 2.05f, 0.2f), new Vector3(1.5f, 0.1f, 2.2f), "Roof", hut);
        roofL.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
        GameObject roofR = Shape("Roof", PrimitiveType.Cube, new Vector3(0.6f, 2.05f, 0.2f), new Vector3(1.5f, 0.1f, 2.2f), "Roof", hut);
        roofR.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
        Piece("Chimney", PrimitiveType.Cube, new Vector3(0.55f, 2.35f, 0.6f), new Vector3(0.22f, 0.7f, 0.22f), "FarmProps/Iron", hut);
        Piece("Door", PrimitiveType.Cube, new Vector3(-0.45f, 0.85f, -0.71f), new Vector3(0.7f, 1.5f, 0.05f), "FarmProps/DoorPaint", hut);
        Shape("Counter", PrimitiveType.Cube, new Vector3(0.45f, 0.45f, -0.95f), new Vector3(0.9f, 0.9f, 0.5f), "Wood", hut);
        Piece("Cheese Pot", PrimitiveType.Cylinder, new Vector3(0.45f, 1.05f, -0.95f), new Vector3(0.5f, 0.15f, 0.5f), "ArrivalArt/CoachMetal", hut);
        Shape("Sign Board", PrimitiveType.Cube, new Vector3(0f, 1.95f, -0.73f), new Vector3(1.3f, 0.3f, 0.05f), "FreshWood", hut);
        SignText(hut, "СИРОВАРНЯ", new Vector3(0f, 1.95f, -0.77f), 0.018f);
        Transform ring = GlowRing(hut, 0.7f, 0.03f);
        ring.localPosition = new Vector3(0.45f, 0.03f, -1.6f);
        hut.gameObject.SetActive(false);

        CheeseLook("Cheese Look", "Yellow");      // a wheel of cow's cheese
        CheeseLook("Goat Cheese Look", "Paint");  // a white goat's cheese
    }

    static void CheeseLook(string name, string color)
    {
        Transform cheese = Group(name, world, Vector3.zero);
        Transform model = Model(cheese, Quaternion.identity);
        Shape("Cheese", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.2f, 0.04f, 0.2f), color, model);
        Shape("Rind", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.205f, 0.03f, 0.205f), "Straw", model);
        cheese.gameObject.SetActive(false);
    }

    // "Яєчний переполох": a sign by the coop door (the game is in FarmPrototype.EggRush.cs).
    static void EggRushSign()
    {
        Transform sign = Group("Egg Rush Sign", world, CoopPosition + new Vector3(-1.6f, 0f, -2.3f));
        BoxCollider hit = sign.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 1.25f, 0f);
        hit.size = new Vector3(1.2f, 0.9f, 0.25f);
        FarmInteractable action = sign.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.EggRush;
        action.label = "«Яєчний переполох»: грати";
        Shape("Post", PrimitiveType.Cube, new Vector3(0f, 0.7f, 0f), new Vector3(0.08f, 1.4f, 0.08f), "Wood", sign);
        Shape("Board", PrimitiveType.Cube, new Vector3(0f, 1.35f, -0.05f), new Vector3(1.1f, 0.4f, 0.04f), "FreshWood", sign);
        SignText(sign, "ЯЄЧНИЙ ПЕРЕПОЛОХ", new Vector3(0f, 1.35f, -0.08f), 0.012f);
        sign.gameObject.SetActive(false); // appears with the repaired coop
    }

    // ---------- the kitchen in grandpa's house (Marquis sells the kitchenware after the repair) ----------
    // Point at the old stove inside: recipes that turn eggs, carrots, milk and cheese into dishes
    // worth much more (FarmPrototype.Kitchen.cs).
    static void Kitchen()
    {
        Transform stove = Group("Kitchen Stove", world, HousePosition + new Vector3(2.85f, 0f, 2.35f));
        BoxCollider hit = stove.gameObject.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.55f, 0f);
        hit.size = new Vector3(1f, 1.15f, 0.9f);
        FarmInteractable action = stove.gameObject.AddComponent<FarmInteractable>();
        action.kind = FarmInteractable.Kind.Kitchen;
        action.label = "стара пічка";
        Transform extras = Group("Kitchen Extras", stove, Vector3.zero);
        Piece("Pot", PrimitiveType.Cylinder, new Vector3(0.15f, 1.16f, 0f), new Vector3(0.34f, 0.1f, 0.34f), "ArrivalArt/CoachMetal", extras);
        Piece("Pan", PrimitiveType.Cylinder, new Vector3(-0.25f, 1.08f, -0.12f), new Vector3(0.28f, 0.015f, 0.28f), "FarmProps/Iron", extras);
        Shape("Recipe Board", PrimitiveType.Cube, new Vector3(-1.05f, 1.65f, 0.47f), new Vector3(0.8f, 0.55f, 0.03f), "FreshWood", extras);
        SignText(extras, "РЕЦЕПТИ", new Vector3(-1.05f, 1.82f, 0.44f), 0.014f);
        foreach (float y in new[] { 1.7f, 1.6f, 1.5f })
            Shape("Recipe Line", PrimitiveType.Cube, new Vector3(-1.05f, y, 0.452f), new Vector3(0.6f, 0.015f, 0.005f), "DarkWood", extras);
        extras.gameObject.SetActive(false);
        Transform ring = GlowRing(stove, 0.6f, 0.08f);
        ring.localPosition = new Vector3(0f, 0.08f, -0.75f);

        DishLook("Omelet Look", "Yellow", new Vector3(0.14f, 0.02f, 0.12f));
        DishLook("Pie Look", "Ginger", new Vector3(0.15f, 0.05f, 0.15f));
        DishLook("Syrnyky Look", "Straw", new Vector3(0.12f, 0.03f, 0.12f));
        DishLook("Casserole Look", "ToolboxRed", new Vector3(0.14f, 0.05f, 0.1f));
    }

    static void DishLook(string name, string color, Vector3 food)
    {
        Transform dish = Group(name, world, Vector3.zero);
        Transform model = Model(dish, Quaternion.identity);
        Shape("Plate", PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(0.24f, 0.01f, 0.24f), "Paper", model);
        Shape("Food", PrimitiveType.Cylinder, new Vector3(0f, 0.02f + food.y / 2f, 0f), new Vector3(food.x, food.y / 2f, food.z), color, model);
        dish.gameObject.SetActive(false);
    }

    // ---------- helpers for textured pieces and models ----------

    // A simple shape with a material from anywhere in Resources ("FarmProps/Soil", "CountryArt/Boards"...).
    static GameObject Piece(string name, PrimitiveType type, Vector3 position, Vector3 size, string surface, Transform parent)
    {
        GameObject piece = Shape(name, type, position, size, "Wood", parent);
        Material material = Resources.Load<Material>(surface);
        if (material != null) piece.GetComponent<Renderer>().sharedMaterial = material;
        return piece;
    }

    // One of our generated models (Resources/FarmProps/<model>.obj).
    static Transform Prop(Transform parent, string model, string surface, Vector3 position, Vector3 rotation, float scale)
    {
        GameObject source = Resources.Load<GameObject>("FarmProps/" + model);
        MeshFilter filter = source != null ? source.GetComponentInChildren<MeshFilter>() : null;
        GameObject prop;
        if (filter != null)
        {
            prop = new GameObject(model);
            prop.transform.SetParent(parent, false);
            prop.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            Material material = Resources.Load<Material>(surface);
            if (material == null) material = Resources.Load<Material>("FarmMaterials/Wood");
            prop.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
        else
        {
            // Not imported yet: a small box stands in. "Farm Prototype → Rebuild Scene" fixes it.
            Debug.LogWarning("[Farm] Model not found: Resources/FarmProps/" + model + ". Run Tools/ModelGen/generate_props.py, then Farm Prototype → Rebuild Scene.");
            prop = Piece(model, PrimitiveType.Cube, Vector3.zero, Vector3.one, surface, parent);
            scale *= 0.1f;
        }
        prop.transform.localPosition = position;
        prop.transform.localRotation = Quaternion.Euler(rotation);
        prop.transform.localScale = Vector3.one * scale;
        return prop.transform;
    }
}
