using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnEmbedded
{
    internal static byte[] Read(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"{resourceName} is not embedded in {assembly.GetName().Name}.");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
