namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Combines the bounds of a model's meshes into one box in the model's own space.</summary>
internal static class TimbnModelBounds
{
    internal static readonly Bounds Fallback = new(Vector3.zero, Vector3.one);

    // Every corner goes through the matrix so a rotated child still fits, and a model with no meshes gets a unit box.
    internal static Bounds Combine(IEnumerable<(Matrix4x4 ToRoot, Bounds Mesh)> meshes)
    {
        Bounds? total = null;
        foreach (var (toRoot, mesh) in meshes)
        {
            for (var corner = 0; corner < 8; corner++)
            {
                var sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                var point = toRoot.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents, sign));
                if (total is { } bounds)
                {
                    bounds.Encapsulate(point);
                    total = bounds;
                }
                else
                {
                    total = new Bounds(point, Vector3.zero);
                }
            }
        }

        return total ?? Fallback;
    }
}
