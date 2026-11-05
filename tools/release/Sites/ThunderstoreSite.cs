class ThunderstoreSite : IReleaseSite
{
    public Site Site => Site.Thunderstore;
    public string PageFile => "thunderstore.md";

    public bool IsEnabled(ModInfo mod) => mod.ToThunderstore;

    public Renderer CreateRenderer(Func<string, string> imageUrl) => new MarkdownRenderer(imageUrl);

    public List<Requirement> Requirements(ModInfo mod)
    {
        var requirements = new List<Requirement>();
        if (mod.HasCoreFloor)
        {
            var core = mod.Shared["ThunderstoreCorePackage"].Split('-');
            requirements.Add(new(mod.Core!.Product, PackageUrl(core[0], core[1]), $"{mod.CoreFloor} or newer"));
        }

        return requirements;
    }

    public string Package(ReleaseContext context, string pluginDir, string pageText, List<string> changes)
    {
        if (string.IsNullOrEmpty(pluginDir))
        {
            return string.Empty;
        }

        var mod = context.Mod;
        var name = mod.ThunderstoreName;
        if (!Regex.IsMatch(name, "^[A-Za-z0-9_]+$"))
        {
            throw new ReleaseException($"Thunderstore name {name} may only hold letters, digits and underscores");
        }

        var icon = context.RequireIcon("Thunderstore");
        var dependencies = Dependencies(mod);
        var dir = Path.Combine(context.OutDir, "thunderstore");
        Directory.CreateDirectory(dir);
        var manifest = JsonSerializer.Serialize(
            new Dictionary<string, object>
            {
                ["name"] = name,
                ["version_number"] = mod.Version,
                ["website_url"] = mod.Shared["ThunderstoreWebsiteUrl"],
                ["description"] = context.Page.Summary,
                ["dependencies"] = dependencies.Select(dependency => $"{dependency.Key}-{dependency.Value}").ToArray(),
            },
            new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        var entries = new List<(string, byte[])>
        {
            ("manifest.json", Encoding.UTF8.GetBytes(manifest + "\n")),
            ("README.md", Encoding.UTF8.GetBytes(pageText)),
            ("CHANGELOG.md", File.ReadAllBytes(Path.Combine(mod.ProjectDir, "CHANGELOG.md"))),
            ("icon.png", File.ReadAllBytes(icon)),
        };

        entries.AddRange(ReleaseContext.Files(pluginDir, mod.ProjectName));
        var zip = Path.Combine(dir, $"{mod.Shared["ThunderstoreNamespace"]}-{name}-{mod.Version}.zip");
        context.Zip(zip, entries);
        ReleaseContext.Write(Path.Combine(dir, "thunderstore.toml"), Toml(context, name, dependencies));
        return zip;
    }

    static string PackageUrl(string owner, string name) => $"https://thunderstore.io/c/{Repo.ThunderstoreCommunity}/p/{owner}/{name}/";

    static Dictionary<string, string> Dependencies(ModInfo mod)
    {
        var dependencies = new Dictionary<string, string>();
        if (mod.HasCoreFloor)
        {
            dependencies[mod.Shared["ThunderstoreCorePackage"]] = mod.CoreFloor;
        }

        return dependencies;
    }

    // tcli refuses to publish without a project config, even for a prebuilt zip, so this only names the package.
    static string Toml(ReleaseContext context, string name, Dictionary<string, string> dependencies)
    {
        var mod = context.Mod;
        var toml = new StringBuilder()
            .Append("[config]\nschemaVersion = \"0.0.1\"\n\n")
            .Append("[package]\n")
            .Append($"namespace = {TomlString(mod.Shared["ThunderstoreNamespace"])}\n")
            .Append($"name = {TomlString(name)}\n")
            .Append($"versionNumber = {TomlString(mod.Version)}\n")
            .Append($"description = {TomlString(context.Page.Summary)}\n")
            .Append($"websiteUrl = {TomlString(mod.Shared["ThunderstoreWebsiteUrl"])}\n")
            .Append("containsNsfwContent = false\n\n")
            .Append("[package.dependencies]\n");

        foreach (var (package, version) in dependencies)
        {
            toml.Append($"{package} = {TomlString(version)}\n");
        }

        toml.Append("\n[build]\nicon = \"./icon.png\"\nreadme = \"./README.md\"\noutdir = \"./build\"\n\n")
            .Append("[publish]\nrepository = \"https://thunderstore.io\"\n")
            .Append($"communities = [{TomlString(Repo.ThunderstoreCommunity)}]\n\n")
            .Append("[publish.categories]\n")
            .Append($"{Repo.ThunderstoreCommunity} = [{string.Join(", ", (mod.IsCore ? new[] { "mods", "libraries" } : ["mods"]).Select(TomlString))}]\n");

        return toml.ToString();
    }

    static string TomlString(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
