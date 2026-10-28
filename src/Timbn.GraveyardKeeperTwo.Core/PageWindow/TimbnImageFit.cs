namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>How a <see cref="TimbnImage"/> sizes its picture in its box.</summary>
public enum TimbnImageFit
{
    /// <summary>The picture keeps its shape and is drawn as large as fits inside the box, in the middle of it. Any room left on the long side stays empty.</summary>
    Inside,

    /// <summary>The picture fills the box, which squeezes it when the box has another shape.</summary>
    Stretch,

    /// <summary>
    /// The picture is as wide as the room its parent gives it and as tall as its shape needs at that width, so it shrinks
    /// on a narrow page and keeps its shape. The image's own height is ignored. In a row, give the image a width or a
    /// fill, since a row has no width to hand it.
    /// </summary>
    Width,
}
