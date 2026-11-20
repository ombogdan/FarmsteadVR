using UnityEngine;

public static class CoachModel
{
    public static Transform Build(Transform coach, bool inRide)
    {
        ArrivalArt.Part(coach, "CoachPanels", "CoachEnamel");
        ArrivalArt.Part(coach, "CoachCap", "CoachIvory");
        ArrivalArt.Part(coach, "CoachSeals", "CoachRubber");
        ArrivalArt.Part(coach, "CoachWindows", "CoachGlass");
        ArrivalArt.Part(coach, "CoachBrightwork", "CoachMetal");
        ArrivalArt.Part(coach, "CoachHeadlights", "CoachLens");
        ArrivalArt.Part(coach, "CoachTailLights", "CoachRedLens");
        ArrivalArt.Part(coach, "CoachSignals", "CoachAmberLens");
        ArrivalArt.Part(coach, "CoachCabin", "CoachRubber");
        ArrivalArt.Part(coach, "CoachDashboard", "CoachDash");
        ArrivalArt.Part(coach, "CoachRails", "CoachMetal");
        ArrivalArt.Part(coach, "CoachMarker", "CoachRubber");

        foreach (float side in new[] { -1f, 1f })
            ArrivalArt.Piece(coach, "Upper window rail", PrimitiveType.Cube,
                new Vector3(side * 1.215f, 2.511f, -.27f), new Vector3(.137f, .117f, 9.48f),
                ArrivalArt.Material("CoachIvory"));
        ArrivalArt.Piece(coach, "Rubber aisle mat", PrimitiveType.Cube, new Vector3(0f, .647f, -.38f),
            new Vector3(.74f, .012f, 8.87f), ArrivalArt.Material("CoachDash"));

        var door = ArrivalArt.Group(coach, "Sliding door", new Vector3(1.19f, 1.38f, 1.43f));
        ArrivalArt.Part(door, "CoachDoorPanels", "CoachEnamel");
        ArrivalArt.Part(door, "CoachDoorWindow", "CoachGlass");
        ArrivalArt.Part(door, "CoachDoorTrim", "CoachRubber");

        foreach (float side in new[] { -1f, 1f })
        {
            foreach (float z in new[] { -3.42f, 2.95f })
            {
                var wheel = ArrivalArt.Group(coach, "Wheel hub pivot", new Vector3(side * 1.14f, 0.50f, z));
                if (side < 0f) wheel.localRotation = Quaternion.Euler(0f, 180f, 0f);
                ArrivalArt.Part(wheel, "CoachTire", "CoachRubber");
                ArrivalArt.Part(wheel, "CoachHub", "CoachMetal");
            }

            foreach (float z in new[] { -4f, -2.85f, -1.70f, -0.55f })
            {
                var seat = ArrivalArt.Group(coach, "Passenger seat", new Vector3(side * 0.78f, 0.99f, z));
                ArrivalArt.Part(seat, "CoachSeat", "CoachVinyl");
                ArrivalArt.Part(seat, "CoachSeatFrame", "CoachMetal");
            }
        }
        var driverSeat = ArrivalArt.Group(coach, "Driver seat", new Vector3(-0.70f, 0.99f, 3.09f));
        ArrivalArt.Part(driverSeat, "CoachSeat", "CoachVinyl");
        ArrivalArt.Part(driverSeat, "CoachSeatFrame", "CoachMetal");
        var wheelRim = ArrivalArt.Part(coach, "CoachSteering", "CoachRubber");
        wheelRim.localPosition = new Vector3(-0.71f, 1.59f, 3.60f);
        wheelRim.localRotation = Quaternion.Euler(-33f, 0f, 90f);
        DressDriver(coach);

        if (inRide) CabinLight(coach);
        else AddRouteSign(coach);
        return door;
    }

    static void AddRouteSign(Transform coach)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var route = new TextPlate("Route board", font, 50, 0.007f, TextAlignment.Center, null, new Color(0.95f, 0.77f, 0.40f));
        route.Root.SetParent(coach, false);
        route.Root.localPosition = new Vector3(0f, 2.583f, 4.55f);
        route.Root.localRotation = Quaternion.Euler(0f, 180f, 0f);
        route.SetText("21   СИДОРІВКА");
        route.Root.localScale = Vector3.one * Mathf.Min(1.12f / Mathf.Max(0.01f, route.TextBounds.size.x), 0.15f / Mathf.Max(0.01f, route.TextBounds.size.y));

    }

    static void DressDriver(Transform coach)
    {
        Material coat = ArrivalArt.Material("DriverCoat");
        ArrivalArt.Piece(coach, "Driver coat", PrimitiveType.Capsule, new Vector3(-0.70f, 1.39f, 3.18f),
            new Vector3(0.39f, 0.25f, 0.22f), coat);
        var head = ArrivalArt.Part(coach, "CoachDriverHead", "DriverSkin");
        head.localPosition = new Vector3(-0.70f, 1.77f, 3.23f);
        ArrivalArt.Piece(coach, "Driver cap", PrimitiveType.Sphere, new Vector3(-0.70f, 1.859f, 3.25f),
            new Vector3(0.215f, 0.10f, 0.242f), coat);
        ArrivalArt.Piece(coach, "Cap brim", PrimitiveType.Sphere, new Vector3(-0.70f, 1.843f, 3.36f),
            new Vector3(0.20f, 0.028f, 0.137f), coat);
        foreach (float side in new[] { -1f, 1f })
        {
            Vector3 shoulder = new Vector3(-0.70f + side * 0.143f, 1.52f, 3.22f);
            Vector3 hand = new Vector3(-0.70f + side * 0.146f, 1.59f, 3.59f);
            var arm = ArrivalArt.Piece(coach, "Coat sleeve", PrimitiveType.Capsule, Vector3.Lerp(shoulder, hand, 0.45f),
                new Vector3(0.091f, 0.20f, 0.088f), coat);
            arm.rotation = Quaternion.FromToRotation(Vector3.up, coach.TransformDirection(hand - shoulder));
            ArrivalArt.Piece(coach, "Driver hand", PrimitiveType.Sphere, hand,
                new Vector3(0.069f, 0.09f, 0.077f), ArrivalArt.Material("DriverSkin"));
        }
    }

    static void CabinLight(Transform coach)
    {
        foreach (float z in new[] { -1.55f, 1.4f })
            ArrivalArt.Piece(coach, "Ceiling diffuser", PrimitiveType.Cube, new Vector3(0f, 2.502f, z),
                new Vector3(0.19f, 0.026f, 0.69f), ArrivalArt.Material("CoachLens"));
    }
}
