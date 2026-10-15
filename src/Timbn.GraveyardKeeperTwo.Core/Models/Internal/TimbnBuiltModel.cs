namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Turns the faces of a <see cref="TimbnModelFaces"/> into a prefab with one mesh per picture. The rules for where
/// faces stand and what they may not do live in <see cref="TimbnFaceLayout"/>. This part loads the pictures, builds the
/// quads and owns everything it made. The camera looks down at about 53 degrees, so across is 50 pixels a unit, up is
/// 30 and deeper is 40 (50 times the cosine and sine of the tilt). Faces are kept in pixels from the model's front,
/// bottom, left corner and only move to world units, centered on the model, when the mesh is made.
/// </summary>
internal static class TimbnBuiltModel
{
    internal const float PixelsAcross = 50f;
    internal const float PixelsUp = 30f;
    internal const float PixelsDeep = 40f;

    // The station expects its model centered on its footprint, so across and deep are measured from the middle.
    internal static Vector3 ToWorld((int X, int Y, int Z) pixel, float centerX, float centerZ) =>
        new((pixel.X - centerX) / PixelsAcross, pixel.Y / PixelsUp, (pixel.Z - centerZ) / PixelsDeep);

    internal static (TimbnModel Model, IDisposable Owner) Create(string name, IReadOnlyList<TimbnPlacedPicture> pictures)
    {
        if (pictures.Count == 0)
            throw new InvalidOperationException($"Model {name} has no faces. Add at least one Front, Top or Block.");

        List<UnityEngine.Object> owned = [];
        try
        {
            return Assemble(name, pictures, owned);
        }
        catch
        {
            Destroy(owned);
            throw;
        }
    }

    private static (TimbnModel Model, IDisposable Owner) Assemble(string name, IReadOnlyList<TimbnPlacedPicture> pictures, List<UnityEngine.Object> owned)
    {
        var shader = Shader.Find("Custom/3D Object") ?? Shader.Find("Sprites/Default");
        var root = new GameObject(name);
        root.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(root);
        owned.Add(root);

        List<(Texture2D Texture, List<TimbnFace> Faces)> images = [];
        List<TimbnFace> allFaces = [];
        foreach (var group in pictures.GroupBy(picture => picture.Key))
        {
            var first = group.First();
            var texture = LoadTexture($"{name}.{first.Request.Name}", first);
            owned.Add(texture);
            WarnAboutHalfTransparency(name, first.Request.Name, texture);

            List<TimbnFace> faces = [];
            foreach (var picture in group)
                faces.AddRange(TimbnFaceLayout.Resolve(picture.Request, texture.width, texture.height));

            images.Add((texture, faces));
            allFaces.AddRange(faces);
        }

        TimbnFaceLayout.CheckOverlaps(allFaces);
        var extents = TimbnFaceLayout.Extents(allFaces);

        foreach (var (texture, faces) in images)
        {
            var mesh = BuildMesh(faces, texture, extents);
            mesh.name = texture.name;
            owned.Add(mesh);

            // Only the texture matters here. TimbnModel.Create swaps the material for a copy of the game's prop material.
            var material = new Material(shader) { mainTexture = texture, name = texture.name };
            owned.Add(material);

            var child = new GameObject(texture.name);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        return (new TimbnModel(root) { PixelCenter = new Vector2(extents.CenterX, extents.CenterZ) }, new TimbnUndo(() =>
        {
            Destroy(owned);
            TimbnModels.ReleaseMaterialsOfUnloadedTextures();
        }));
    }

    private static Texture2D LoadTexture(string textureName, TimbnPlacedPicture picture)
    {
        // Point filtering and clamping keep the pixel art crisp, like the game's own sprites.
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = textureName };
        if (texture.LoadImage(picture.Png))
            return texture;

        UnityEngine.Object.DestroyImmediate(texture);
        throw new InvalidOperationException($"{picture.Request.Name} is not a PNG the game can read.");
    }

    private static void WarnAboutHalfTransparency(string modelName, string faceName, Texture2D texture)
    {
        var count = TimbnFaceLayout.HalfTransparentPixels(texture.GetPixels32().Select(pixel => pixel.a));
        if (count > 0)
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnBuiltModel)}|{faceName} in model {modelName} has {count} half see-through pixels, which may not draw right.");
    }

    private static Mesh BuildMesh(List<TimbnFace> faces, Texture2D texture, TimbnFaceExtents extents)
    {
        List<Vector3> vertices = [];
        List<Vector3> normals = [];
        List<Vector2> coordinates = [];
        List<int> triangles = [];

        foreach (var face in faces)
        {
            var first = vertices.Count;
            var (x, y, z) = TimbnFaceLayout.Normal(face.Kind);
            var normal = new Vector3(x, y, z);
            var (low, high) = TimbnFaceLayout.PictureRows(face, texture.height);
            Vector2[] picture = [new(0f, low), new(1f, low), new(1f, high), new(0f, high)];

            var corners = TimbnFaceLayout.Corners(face);
            for (var corner = 0; corner < corners.Length; corner++)
            {
                vertices.Add(ToWorld(corners[corner], extents.CenterX, extents.CenterZ));
                normals.Add(normal);
                coordinates.Add(picture[corner]);
            }

            triangles.AddRange(TimbnFaceLayout.Triangles.Select(corner => first + corner));
        }

        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, coordinates);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // Immediate, because the cache of game materials is swept for destroyed textures right after this and a normal destroy waits for the end of the frame.
    private static void Destroy(List<UnityEngine.Object> owned)
    {
        foreach (var item in owned)
        {
            if (item != null)
                UnityEngine.Object.DestroyImmediate(item);
        }

        owned.Clear();
    }
}
