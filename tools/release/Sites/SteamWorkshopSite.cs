class SteamWorkshopSite : IReleaseSite
{
    public Site Site => Site.Steam;
    public string PageFile => "steam.bbcode";

    public bool IsEnabled(ModInfo mod) => mod.ToWorkshop;

    public Renderer CreateRenderer(Func<string, string> imageUrl) => new SteamRenderer(imageUrl);

    public List<Requirement> Requirements(ModInfo mod)
    {
        var requirements = new List<Requirement>();
        if (mod.HasCoreFloor)
        {
            requirements.Add(new(mod.Core!.Product, mod.Core.WorkshopUrl, $"{mod.CoreFloor} or newer"));
        }

        return requirements;
    }

    // With no plugin folder the item.vdf only sets the title and description, which updates the page text alone.
    public string Package(ReleaseContext context, string pluginDir, string pageText, List<string> changes)
    {
        var mod = context.Mod;
        var dir = Path.Combine(context.OutDir, "workshop");
        // Id 0 makes steamcmd create the item, and visibility 2 keeps it private until the first release makes it public.
        var itemId = context.Options.CreateWorkshop ? "0" : mod.WorkshopItemId;
        var visibility = context.Options.CreateWorkshop ? "2" : "0";
        // Valve's KeyValues reader caps a value at 4096 bytes and may not read \" escapes. Check both on the first upload.
        var descriptionBytes = Encoding.UTF8.GetByteCount(pageText);
        if (descriptionBytes > 4096)
        {
            context.Warnings.Add($"the Steam description is {descriptionBytes} bytes, and steamcmd may cut item.vdf values at 4096");
        }

        if (string.IsNullOrEmpty(pluginDir))
        {
            var text = new StringBuilder("\"workshopitem\"\n{\n")
                .Append($"\t\"appid\"\t\t\"{Repo.SteamAppId}\"\n")
                .Append($"\t\"publishedfileid\"\t\t{VdfString(itemId)}\n")
                .Append($"\t\"title\"\t\t{VdfString(mod.Product)}\n")
                .Append($"\t\"description\"\t\t{VdfString(pageText.TrimEnd())}\n")
                .Append("}\n");

            ReleaseContext.Write(Path.Combine(dir, "item.vdf"), text.ToString());
            return dir;
        }

        var content = Path.Combine(dir, "content");
        ReleaseContext.CopyDirectory(pluginDir, Path.Combine(content, "BepInEx", "plugins", mod.ProjectName));
        // The game's Mods window shows this name and version, and its id must stay the same across releases.
        var modInfo = JsonSerializer.Serialize(
            new Dictionary<string, object>
            {
                ["schemaVersion"] = 1,
                ["id"] = mod.AssemblyName.ToLowerInvariant(),
                ["name"] = mod.Product,
                ["version"] = mod.Version,
            },
            new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        ReleaseContext.Write(Path.Combine(content, "modinfo.json"), modInfo + "\n");
        // The preview sits inside the Workshop folder so the upload job has everything in one artifact.
        var icon = Path.Combine(dir, "preview.png");
        File.Copy(context.RequireIcon("Steam"), icon);
        var changeNote = new StringBuilder("[list]\n");
        foreach (var change in changes)
        {
            changeNote.Append($"[*]{change}\n");
        }

        changeNote.Append("[/list]");
        var vdf = new StringBuilder("\"workshopitem\"\n{\n")
            .Append($"\t\"appid\"\t\t\"{Repo.SteamAppId}\"\n")
            .Append($"\t\"publishedfileid\"\t\t{VdfString(itemId)}\n")
            .Append($"\t\"contentfolder\"\t\t{VdfString(Path.Combine(dir, "content"))}\n")
            .Append($"\t\"previewfile\"\t\t{VdfString(icon)}\n")
            .Append($"\t\"visibility\"\t\t\"{visibility}\"\n")
            .Append($"\t\"title\"\t\t{VdfString(mod.Product)}\n")
            .Append($"\t\"description\"\t\t{VdfString(pageText.TrimEnd())}\n")
            .Append($"\t\"changenote\"\t\t{VdfString(changeNote.ToString())}\n")
            .Append("}\n");

        ReleaseContext.Write(Path.Combine(dir, "item.vdf"), vdf.ToString());
        return dir;
    }

    static string VdfString(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
