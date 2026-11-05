// One place a mod is published. A site renders its own page text and, for a full run, builds the package it uploads.
interface IReleaseSite
{
    Site Site { get; }

    // File name of the rendered page text under pages/.
    string PageFile { get; }

    bool IsEnabled(ModInfo mod);

    Renderer CreateRenderer(Func<string, string> imageUrl);

    List<Requirement> Requirements(ModInfo mod);

    // Returns the zip or folder written, or an empty string when nothing is built. An empty pluginDir is a pages only run.
    string Package(ReleaseContext context, string pluginDir, string pageText, List<string> changes);
}
