namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal readonly struct TimbnPlacedPicture
{
    internal TimbnPlacedPicture(byte[] png, object key, TimbnFaceRequest request)
    {
        Png = png;
        Key = key;
        Request = request;
    }

    internal byte[] Png { get; }

    // Faces with an equal key share one texture and one mesh.
    internal object Key { get; }

    internal TimbnFaceRequest Request { get; }
}
