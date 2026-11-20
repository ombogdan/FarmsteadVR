using System.Collections.Generic;
using UnityEngine;

public static class ArrivalArt
{
    const string Folder = "ArrivalArt/";
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    public static Material Material(string name)
    {
        if (!materials.TryGetValue(name, out Material result) || result == null)
        {
            result = Resources.Load<Material>(Folder + name);
            if (result != null) materials[name] = result;
        }
        return result;
    }

    public static Transform Part(Transform parent, string model, string material)
    {
        if (!meshes.TryGetValue(model, out Mesh mesh) || mesh == null)
        {
            var source = Resources.Load<GameObject>(Folder + model);
            var filter = source != null ? source.GetComponentInChildren<MeshFilter>() : null;
            if (filter != null)
            {
                mesh = filter.sharedMesh;
                meshes[model] = mesh;
            }
        }

        var part = new GameObject(model);
        part.transform.SetParent(parent, false);
        if (mesh != null)
        {
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = Material(material);
        }
        return part.transform;
    }

    public static Transform Piece(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 size, Material material)
    {
        var piece = VrShapes.Create(shape, name, parent, "Paper").transform;
        piece.localPosition = position;
        piece.localScale = size;
        if (material != null) piece.GetComponent<MeshRenderer>().sharedMaterial = material;
        return piece;
    }

    public static Transform Group(Transform parent, string name, Vector3 position)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        group.localPosition = position;
        return group;
    }

    public static Material CountrySurface(string name) => Resources.Load<Material>("CountryArt/" + name);

    public static Transform CountryModel(Transform parent, string model, string surface, float scale, float yaw)
    {
        var source = Resources.Load<GameObject>("CountryArt/" + model);
        if (source == null) return null;
        var copy = Object.Instantiate(source, parent, false).transform;
        copy.name = model;
        copy.localScale = Vector3.one * scale;
        copy.localRotation = Quaternion.Euler(0f, yaw, 0f);
        foreach (var renderer in copy.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterial = CountrySurface(surface);
        return copy;
    }
}
