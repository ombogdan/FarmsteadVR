using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The opening "cartoon", in three short scenes joined by fades to black:
// 1. At home: you stand in your room, Grandpa's letter and a family photograph introduce the farm.
//    Trigger (E on the Mac) turns the page.
// 2. On the bus: you stand in the aisle, the world outside the windows slides past.
//    The bus itself doesn't move (the world does) - that is much more comfortable in VR.
//    It slows down, the door opens with a "pshh".
// 3. You are at the bus stop next to the farm (FarmPrototype takes over from here).
// The home and the bus are built far away from the farm, so they never see each other.
public class IntroSequence : MonoBehaviour
{
    static readonly Vector3 HomeSpot = new Vector3(-2000f, 0f, 0f);
    static readonly Vector3 RideSpot = new Vector3(2000f, 0f, 0f);
    const float RideSpeed = 11f;          // m/s of the world outside the windows (about 40 km/h)
    const float SceneryLength = 400f;     // the strip of trees and poles that loops past the bus

    static readonly string[] PageHeadings = { "Лист від діда", "Стара ферма", "Квиток до села" };
    static readonly string[] Pages =
    {
        "Привіт! Це дід Панас. Залишаю тобі ферму «Соняшник». Подбай про неї.",
        "Колись тут було людно. Тепер стежка заросла, а паркан похилився. Думаю, ти зможеш повернути сюди життя.",
        "У рюкзаку вже лежить лист. Автобус відходить зранку. Побачимось на фермі!",
    };
    static readonly string[] PhotoNotes = { "Із дідового альбому", "Ферма «Соняшник»", "Рейс 21  •  До села" };

    static readonly Color Ink = new Color(0.22f, 0.15f, 0.1f);

    FarmPrototype farm;
    Transform head;
    ScreenFade fade;
    Font font;
    Transform home, book, ride, door;
    TextPlate pageTitle, pageText, photoNote, footer, caption;
    readonly List<Transform> scenery = new List<Transform>();
    float rideSpeed;
    Vector3 bookPosition, captionPosition;
    bool bookPlaced, captionPlaced;

    public IEnumerator Run(FarmPrototype farm, Camera camera, string nextHint)
    {
        this.farm = farm;
        head = camera.transform;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fade = ScreenFade.On(camera);
        fade.Set(1f);
        yield return null; // one frame, so the VR player is ready to be moved

        // ---- 1. At home with the letter ----
        BuildHome();
        BuildBook();
        farm.PlacePlayer(HomeSpot + new Vector3(0f, 0f, -0.6f), 0f);
        yield return null;
        yield return fade.FadeTo(0f, 0.8f);

        for (int page = 0; page < Pages.Length; page++)
        {
            FarmAudio.Play2D("page_turn", 0.8f);
            FitText(pageTitle, PageHeadings[page], 0.40f, 0.062f);
            FitText(pageText, TextPlate.Wrap(Pages[page], 24), 0.40f, 0.25f);
            FitText(photoNote, PhotoNotes[page], 0.34f, 0.025f);
            FitText(footer, $"{page + 1} / {Pages.Length}    •    {nextHint}", 0.55f, 0.029f);
            float shownAt = Time.time;
            while (true)
            {
                float shown = Time.time - shownAt;
                if (shown > 0.5f && (FarmInput.InteractPressed() || shown > 17f)) break;
                yield return null;
            }
        }
        yield return fade.FadeTo(1f, 0.7f);
        Destroy(home.gameObject);
        Destroy(book.gameObject);
        book = null;

        // ---- 2. On the bus ----
        BuildRide();
        farm.PlacePlayer(RideSpot + new Vector3(0.04f, 0.39f, -2.04f), 7f);
        FarmAudio.PlayLoopOn("bus_engine", ride, 0.6f, 6f);
        caption = new TextPlate("Caption", font, 52, 0.005f, TextAlignment.Center);
        caption.Root.SetParent(ride, true);
        rideSpeed = RideSpeed;
        ShowCaption("Автобус везе тебе до села...");
        yield return fade.FadeTo(0f, 0.8f);

        float rideStart = Time.time;
        while (Time.time - rideStart < 4.5f) // trigger skips the ride
        {
            if (Time.time - rideStart > 1f && FarmInput.InteractPressed()) break;
            yield return null;
        }

        ShowCaption("Наступна зупинка — ферма «Соняшник»!");
        for (float t = 0f; t < 2f; t += Time.deltaTime) // the bus slows down smoothly
        {
            rideSpeed = RideSpeed * (1f - Mathf.SmoothStep(0f, 1f, t / 2f));
            yield return null;
        }
        rideSpeed = 0f;

        FarmAudio.Play2D("door_hiss", 0.9f);
        Vector3 closed = door.localPosition;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime) // the door slides open
        {
            door.localPosition = closed + Vector3.back * (1.25f * Mathf.SmoothStep(0f, 1f, t / 0.8f));
            yield return null;
        }
        door.localPosition = closed + Vector3.back * 1.25f;
        ShowCaption("Приїхали! Виходимо…");
        yield return new WaitForSeconds(0.9f);

        // ---- 3. Out at the bus stop ----
        yield return fade.FadeTo(1f, 0.7f);
        Destroy(ride.gameObject);
        ride = null;
        scenery.Clear();
        caption = null;
        farm.PlacePlayer(new Vector3(FarmWorldBuilder.PlayerStart.x, 0f, FarmWorldBuilder.PlayerStart.z), -12f);
        yield return null;
        yield return fade.FadeTo(0f, 0.8f);
    }

    void Update()
    {
        // The world outside the bus slides past and wraps around.
        if (ride != null && rideSpeed > 0f)
        {
            float step = rideSpeed * Time.deltaTime;
            foreach (Transform thing in scenery)
            {
                Vector3 p = thing.localPosition;
                p.z -= step;
                if (p.z < -SceneryLength / 2f) p.z += SceneryLength;
                thing.localPosition = p;
            }
        }
        if (book != null) Follow(book, ref bookPosition, ref bookPlaced, 1.22f, -0.10f);
        if (caption != null) Follow(caption.Root, ref captionPosition, ref captionPlaced, 1.6f, -0.3f);
    }

    // Floats in front of you and slowly follows your head (like other texts in the game).
    void Follow(Transform thing, ref Vector3 position, ref bool placed, float distance, float drop)
    {
        Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
        Vector3 target = head.position + flat.normalized * distance + Vector3.up * drop;
        position = placed ? Vector3.Lerp(position, target, Time.deltaTime * 3f) : target;
        placed = true;
        thing.position = position;
        thing.rotation = Quaternion.LookRotation(position - head.position, Vector3.up);
    }

    void ShowCaption(string text)
    {
        caption.SetText(text);
        captionPlaced = false;
    }

    // ---------- scene 1: your room ----------

    void BuildHome()
    {
        home = new GameObject("Home").transform;
        home.position = HomeSpot;
        const float half = 2f, height = 2.8f;
        Box(home, "Floor", new Vector3(0f, 0.02f, 0f), new Vector3(4f, 0.04f, 4f), "Wood");
        Box(home, "Ceiling", new Vector3(0f, height, 0f), new Vector3(4f, 0.1f, 4f), "Paper");
        Box(home, "Wall", new Vector3(0f, height / 2f, half), new Vector3(4f, height, 0.1f), "House");
        Box(home, "Wall", new Vector3(0f, height / 2f, -half), new Vector3(4f, height, 0.1f), "House");
        Box(home, "Wall", new Vector3(half, height / 2f, 0f), new Vector3(0.1f, height, 4f), "House");
        Box(home, "Wall", new Vector3(-half, height / 2f, 0f), new Vector3(0.1f, height, 4f), "House");

        // window with a white cross frame
        Box(home, "Window", new Vector3(0.9f, 1.6f, half - 0.06f), new Vector3(1.2f, 0.9f, 0.03f), "Glass");
        Box(home, "Frame", new Vector3(0.9f, 1.6f, half - 0.08f), new Vector3(1.2f, 0.05f, 0.02f), "Paint");
        Box(home, "Frame", new Vector3(0.9f, 1.6f, half - 0.08f), new Vector3(0.05f, 0.9f, 0.02f), "Paint");
        Box(home, "Sill", new Vector3(0.9f, 1.13f, half - 0.12f), new Vector3(1.35f, 0.05f, 0.15f), "Paint");
        // bed with a red blanket
        Box(home, "Bed", new Vector3(-1.45f, 0.22f, 0.6f), new Vector3(1f, 0.3f, 2f), "DarkWood");
        Box(home, "Mattress", new Vector3(-1.45f, 0.43f, 0.6f), new Vector3(0.95f, 0.14f, 1.95f), "Paper");
        ArrivalArt.Part(home, "Quilt", "QuiltFabric").localPosition = new Vector3(-1.45f, .523f, .37f);
        ArrivalArt.Part(home, "LinenPillow", "Linen").localPosition = new Vector3(-1.45f, .56f, 1.35f);
        // desk with a lamp
        Box(home, "Desk", new Vector3(1.55f, 0.75f, 0.9f), new Vector3(0.8f, 0.05f, 1.3f), "Wood");
        foreach (float x in new[] { 1.2f, 1.9f })
            foreach (float z in new[] { 0.3f, 1.5f })
                Box(home, "Leg", new Vector3(x, 0.37f, z), new Vector3(0.05f, 0.74f, 0.05f), "DarkWood");
        ArrivalArt.Piece(home, "Lamp base", PrimitiveType.Cylinder, new Vector3(1.6f,.786f,1.30f),
            new Vector3(.19f,.013f,.19f), ArrivalArt.Material("Brass"));
        ArrivalArt.Piece(home, "Lamp stem", PrimitiveType.Cylinder, new Vector3(1.6f,.936f,1.30f),
            new Vector3(.029f,.142f,.029f), ArrivalArt.Material("Brass"));
        ArrivalArt.Part(home, "LinenLamp", "LampLinen").localPosition = new Vector3(1.6f,1.049f,1.30f);
        // The desk and woodwork catch the warm light from the window.
        foreach (var renderer in home.GetComponentsInChildren<MeshRenderer>())
        {
            string style = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "";
            if (style == "House") renderer.sharedMaterial = ArrivalArt.Material("WindowPlaster");
            if (style == "Wood" || style == "DarkWood") renderer.sharedMaterial = ArrivalArt.CountrySurface("Timber");
        }
        var photoOnDesk = Panel(home, "Framed family snapshot", new Vector3(1.57f, .96f, 1.44f),
            new Vector3(.35f,.245f,.025f), "LetterBacking");
        Panel(photoOnDesk, "Snapshot", new Vector3(0f,0f,-.57f), new Vector3(.91f,.89f,.05f), "FamilyPhoto")
            .localRotation = Quaternion.Euler(0f,0f,180f);
        photoOnDesk.localRotation = Quaternion.Euler(-12f, -28f, 0f);
        Box(home, "Baseboard", new Vector3(0f,.07f,1.92f), new Vector3(3.86f,.14f,.11f), "DarkWood");
        Box(home, "Window Header", new Vector3(.9f,2.075f,1.91f), new Vector3(1.37f,.085f,.09f), "DarkWood");
        ArrivalArt.Part(home, "LinenCurtain", "Linen").localPosition = new Vector3(.9f,1.56f,1.82f);
        var rod = ArrivalArt.Piece(home, "Curtain rod", PrimitiveType.Cylinder, new Vector3(.9f,2.165f,1.79f),
            new Vector3(.027f, .94f, .027f), ArrivalArt.Material("Brass"));
        rod.localRotation = Quaternion.Euler(0f,0f,90f);
        var roomLight = ArrivalArt.Group(home, "Window light", new Vector3(.9f,1.9f,1.48f)).gameObject.AddComponent<Light>();
        roomLight.type = LightType.Point;
        roomLight.color = new Color(1f,.82f,.61f);
        roomLight.intensity = .88f;
        roomLight.range = 5f;

        // rug, door and the packed backpack
        Box(home, "Rug", new Vector3(0f, 0.045f, -0.2f), new Vector3(1.8f, 0.01f, 1.2f), "Teleport");
        Box(home, "Door", new Vector3(1f, 1f, -half + 0.06f), new Vector3(0.9f, 2f, 0.04f), "DarkWood");
        Ball(home, "Door Knob", new Vector3(0.65f, 1f, -half + 0.1f), Vector3.one * 0.06f, "Yellow");
        Box(home, "Backpack", new Vector3(0.5f, 0.2f, -1.5f), new Vector3(0.35f, 0.4f, 0.2f), "ToolboxRed");
        Box(home, "Backpack Pocket", new Vector3(0.5f, 0.15f, -1.61f), new Vector3(0.25f, 0.18f, 0.05f), "Bus");
    }

    void BuildBook()
    {
        book = new GameObject("Знайдений лист").transform;
        Panel(book, "Letter backing", Vector3.forward * 0.012f, new Vector3(0.98f, 0.59f, 0.024f), "LetterBacking");
        Panel(book, "Stationery", Vector3.zero, new Vector3(0.944f, 0.551f, 0.011f), "LetterPaper");
        Panel(book, "Middle fold", new Vector3(0.016f, 0f, -0.007f), new Vector3(0.003f, 0.506f, 0.003f), "Brass");

        Transform photo = ArrivalArt.Group(book, "Photo print", new Vector3(0.248f, 0.01f, -0.019f));
        photo.localRotation = Quaternion.Euler(0f, 0f, -3f);
        Panel(photo, "Photo border", Vector3.zero, new Vector3(.39f, .285f, .006f), "LetterPaper");
        Panel(photo, "Grandpa's photograph", new Vector3(0f, .013f, -.005f), new Vector3(.364f, .242f, .004f), "FamilyPhoto")
            .localRotation = Quaternion.Euler(0f, 0f, 180f);
        Panel(photo, "Photo mount", new Vector3(.139f, .122f, -.008f), new Vector3(.077f, .026f, .002f), "LetterPaper")
            .localRotation = Quaternion.Euler(0f, 0f, 25f);

        pageTitle = LetterText(book, "Heading", new Vector3(-.227f, .184f, -.018f), Ink);
        pageText = LetterText(book, "Letter", new Vector3(-.227f, -.016f, -.018f), Ink);
        photoNote = LetterText(photo, "Photo caption", new Vector3(0f, -.122f, -.009f), Ink);
        footer = LetterText(book, "Page and controls", new Vector3(0f, -.244f, -.018f), Ink);
        var sender = LetterText(book, "Postmark", new Vector3(.25f, .203f, -.018f), new Color(.45f,.27f,.13f));
        FitText(sender, "ФЕРМА  СОНЯШНИК", .34f, .028f);
    }

    TextPlate LetterText(Transform parent, string name, Vector3 position, Color color)
    {
        var text = new TextPlate(name, font, 56, .006f, TextAlignment.Center, null, color);
        text.Root.SetParent(parent, false);
        text.Root.localPosition = position;
        return text;
    }

    static void FitText(TextPlate plate, string text, float width, float height)
    {
        plate.SetText(text);
        Vector3 size = plate.TextBounds.size;
        plate.Root.localScale = Vector3.one * Mathf.Min(width / Mathf.Max(0.001f, size.x), height / Mathf.Max(0.001f, size.y));
    }

    static Transform Panel(Transform parent, string name, Vector3 position, Vector3 size, string material)
    {
        return ArrivalArt.Piece(parent, name, PrimitiveType.Cube, position, size, ArrivalArt.Material(material));
    }

    // ---------- scene 2: the bus ----------

    void BuildRide()
    {
        ride = new GameObject("Bus Ride").transform;
        ride.position = RideSpot;
        var ground = Box(ride, "Ground", new Vector3(0f, -0.15f, 0f), new Vector3(160f, 0.3f, SceneryLength + 40f), "Grass");
        ground.GetComponent<MeshRenderer>().sharedMaterial = ArrivalArt.CountrySurface("Meadow");
        var road = Box(ride, "Road", new Vector3(0f, 0.02f, 0f), new Vector3(7f, 0.04f, SceneryLength + 40f), "Asphalt");
        road.GetComponent<MeshRenderer>().sharedMaterial = ArrivalArt.CountrySurface("Road");

        // What slides past the windows.
        System.Random random = new System.Random(5);
        float half = SceneryLength / 2f;
        for (float z = -half; z < half; z += 4f)
            scenery.Add(Box(ride, "Road Line", new Vector3(-1.75f, 0.045f, z), new Vector3(0.15f, 0.01f, 2f), "Paint"));
        for (float z = -half; z < half; z += 25f)
        {
            Transform pole = new GameObject("Pole").transform;
            pole.SetParent(ride, false);
            pole.localPosition = new Vector3(5f, 0f, z);
            Box(pole, "Post", new Vector3(0f, 3f, 0f), new Vector3(0.15f, 6f, 0.15f), "DarkWood");
            Box(pole, "Bar", new Vector3(0f, 5.6f, 0f), new Vector3(1.4f, 0.1f, 0.1f), "DarkWood");
            scenery.Add(pole);
        }
        for (int i = 0; i < 70; i++)
        {
            float side = random.NextDouble() < 0.5 ? -1f : 1f;
            Transform tree = new GameObject("Tree").transform;
            tree.SetParent(ride, false);
            tree.localPosition = new Vector3(side * Rand(random, 7f, 40f), 0f, Rand(random, -half, half));
            bool pine = random.NextDouble() < .44;
            string shape = pine ? "Spruce" : "Orchard";
            float scale = Rand(random, .8f, 1.45f);
            float yaw = Rand(random, 0f, 360f);
            ArrivalArt.CountryModel(tree, shape + "Wood", "Bark", scale, yaw);
            ArrivalArt.CountryModel(tree, shape + "Foliage", pine ? "FirSpray" : "LeafSpray", scale, yaw);
            scenery.Add(tree);
        }
        for (int i = 0; i < 36; i++)
        {
            var verge = ArrivalArt.Group(ride, "Wildflower verge", new Vector3(
                -Rand(random, 4.15f, 10.5f), 0f, Rand(random, -half, half)));
            ArrivalArt.CountryModel(verge, "Wildflower", "WildflowerSpray", Rand(random, .8f, 1.4f), Rand(random, 0f, 360f));
            scenery.Add(verge);
        }

        // Same model on the road and at the stop. Only the scenery moves during the ride.
        Transform bus = ArrivalArt.Group(ride, "Bus Inside", Vector3.zero);
        door = CoachModel.Build(bus, true);
    }

    // ---------- tiny helpers ----------

    static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, string material, float tilt = 0f)
    {
        Transform box = VrShapes.Create(PrimitiveType.Cube, name, parent, material).transform;
        box.localPosition = position;
        box.localScale = size;
        box.localRotation = Quaternion.Euler(0f, 0f, tilt);
        return box;
    }

    static Transform Ball(Transform parent, string name, Vector3 position, Vector3 size, string material)
    {
        Transform ball = VrShapes.Create(PrimitiveType.Sphere, name, parent, material).transform;
        ball.localPosition = position;
        ball.localScale = size;
        return ball;
    }

    static float Rand(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
