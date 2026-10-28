namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal readonly struct TimbnPlacedPicture
{
    internal TimbnPlacedPicture(byte[] image, object key, TimbnFaceRequest request)
    {
        Image = image;
        Key = key;
        Request = request;
    }

    internal byte[] Image { get; }

    // Faces with an equal key share one texture and one mesh.
    internal object Key { get; }

    internal TimbnFaceRequest Request { get; }
}
