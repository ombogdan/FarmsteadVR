using UnityEngine;

// Makes simple shapes for VR helpers (hands, laser, teleport arc, text boards) while the game runs.
//
// Two rules learned the hard way on the Quest:
// - Only Cube and Sphere here. A Quad/Plane needs MeshCollider, which Unity strips from the build
//   because the scene doesn't use it, and then CreatePrimitive fails.
// - Helpers must never block the aiming ray, so their colliders are removed
//   and they live on the "Ignore Raycast" layer.
public static class VrShapes
{
    public const int IgnoreRaycastLayer = 2;

    public static GameObject Create(PrimitiveType type, string name, Transform parent, string materialName)
    {
        GameObject shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        shape.layer = IgnoreRaycastLayer;
        if (parent != null) shape.transform.SetParent(parent, false);
        Collider collider = shape.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Object.Destroy(collider);
        }
        Renderer renderer = shape.GetComponent<Renderer>();
        Material material = LoadMaterial(materialName);
        if (material != null) renderer.sharedMaterial = material;
        // Helpers never cast shadows: the sun disc in the sky used to throw a 14 m round shadow
        // exactly onto the player (it always hangs between you and the sun).
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return shape;
    }

    public static Material LoadMaterial(string name)
    {
        return Resources.Load<Material>("FarmMaterials/" + name);
    }
}
