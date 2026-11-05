record Options(string Mod, string? DllDir, string? OutDir, string? Sha, bool DryRun)
{
    public bool PagesOnly { get; init; }

    // Packages only the Steam Workshop folder, with item id 0 and private visibility, so steamcmd creates the item.
    public bool CreateWorkshop { get; init; }
}
