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
        Material material = LoadMaterial(materialName);
        if (material != null) shape.GetComponent<Renderer>().sharedMaterial = material;
        return shape;
    }

    public static Material LoadMaterial(string name)
    {
        return Resources.Load<Material>("FarmMaterials/" + name);
    }
}
