record ContentFile(string Source, string RelativePath);

class ModInfo
{
    public required string ProjectName { get; init; }
    public required string ModName { get; init; }
    public required string ProjectDir { get; init; }
    public required string DocsDir { get; init; }
    public required string AssemblyName { get; init; }
    public required string Product { get; init; }
    public required string Version { get; init; }
    public required string NexusModId { get; init; }
    public required string WorkshopItemId { get; init; }
    public required bool SkipThunderstore { get; init; }
    public required bool GenerateDocumentationFile { get; init; }
    public required bool IsPackable { get; init; }
    public required Dictionary<string, string> Shared { get; init; }
    public required string CoreFloor { get; init; }
    public required string GameVersion { get; init; }
    public required string GameBuild { get; init; }
    public required ModInfo? Core { get; init; }

    public bool IsCore => ModName == "Core";
    public bool IsReleasable => System.Version.Parse(Version) >= new System.Version(1, 0, 0);
    public bool HasCoreFloor => !string.IsNullOrEmpty(CoreFloor);
    public bool ToNexus => !string.IsNullOrEmpty(NexusModId);
    public bool ToWorkshop => !string.IsNullOrEmpty(WorkshopItemId);
    public bool ToThunderstore => !SkipThunderstore;
    public string Tag => $"{ModName.ToLowerInvariant()}/v{Version}";
    public string ThunderstoreName => Product.Replace(" ", "");
    public string NexusUrl => $"https://www.nexusmods.com/{Repo.NexusGame}/mods/{NexusModId}";
    public string WorkshopUrl => $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopItemId}";

    public static ModInfo Load(string repo, string name)
    {
        var src = Path.Combine(repo, "src");
        var wanted = name.StartsWith(Repo.ProjectPrefix, StringComparison.OrdinalIgnoreCase) ? name[Repo.ProjectPrefix.Length..] : name;
        var match = Directory.GetDirectories(src, Repo.ProjectPrefix + "*")
            .Select(Path.GetFileName)
            .FirstOrDefault(name => string.Equals(name![Repo.ProjectPrefix.Length..], wanted, StringComparison.OrdinalIgnoreCase))
            ?? throw new ReleaseException($"no project in src matches {name}");

        var shared = ReadProperties(Path.Combine(src, "Directory.Build.props"));
        var core = LoadProject(repo, shared, Repo.ProjectPrefix + "Core", null);
        if (match == core.ProjectName)
        {
            return LoadProject(repo, shared, match, null);
        }

        return LoadProject(repo, shared, match, core);
    }

    static ModInfo LoadProject(string repo, Dictionary<string, string> shared, string projectName, ModInfo? core)
    {
        var projectDir = Path.Combine(repo, "src", projectName);
        var properties = ReadProperties(Path.Combine(projectDir, projectName + ".csproj"));
        var modName = projectName[Repo.ProjectPrefix.Length..];
        var corePlugin = File.ReadAllText(Path.Combine(repo, "src", Repo.ProjectPrefix + "Core", "TimbnCorePlugin.cs"));
        return new ModInfo
        {
            ProjectName = projectName,
            ModName = modName,
            ProjectDir = projectDir,
            DocsDir = Path.Combine(repo, "docs", modName),
            AssemblyName = Read(properties, "AssemblyName", projectName),
            Product = Read(properties, "Product") is { Length: > 0 } product ? product : throw new ReleaseException($"{projectName} has no Product"),
            Version = Read(properties, "Version", shared["Version"]),
            NexusModId = Read(properties, "NexusModId"),
            WorkshopItemId = Read(properties, "WorkshopItemId"),
            SkipThunderstore = IsTrue(properties, "SkipThunderstore"),
            GenerateDocumentationFile = IsTrue(properties, "GenerateDocumentationFile"),
            IsPackable = IsTrue(properties, "IsPackable"),
            Shared = shared,
            CoreFloor = core == null ? string.Empty : FindCoreFloor(projectDir),
            GameVersion = Constant(corePlugin, "TestedGameVersion"),
            GameBuild = Constant(corePlugin, "TestedGameBuild"),
            Core = core,
        };
    }

    static string Read(Dictionary<string, string> properties, string name, string fallback = "") =>
        properties.TryGetValue(name, out var value) && !string.IsNullOrEmpty(value) ? value : fallback;

    static bool IsTrue(Dictionary<string, string> properties, string name) =>
        string.Equals(Read(properties, name), "true", StringComparison.OrdinalIgnoreCase);

    static Dictionary<string, string> ReadProperties(string path)
    {
        var properties = new Dictionary<string, string>();
        foreach (var group in XDocument.Load(path).Root!.Elements("PropertyGroup").Where(group => group.Attribute("Condition") == null))
        {
            foreach (var property in group.Elements().Where(property => property.Attribute("Condition") == null))
            {
                properties[property.Name.LocalName] = property.Value.Trim();
            }
        }

        return properties;
    }

    static string FindCoreFloor(string projectDir)
    {
        foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
        {
            var match = Regex.Match(File.ReadAllText(file), @"BepInDependency\(\s*TimbnCorePlugin\.Guid\s*,\s*""([^""]+)""");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return string.Empty;
    }

    static string Constant(string source, string name)
    {
        var match = Regex.Match(source, $@"const string {name} = ""([^""]+)""");
        return match.Success ? match.Groups[1].Value : throw new ReleaseException($"TimbnCorePlugin has no {name}");
    }

    // Same rule as DeployToGame, so whatever deploys during dev is what ships.
    public List<ContentFile> GatherContent()
    {
        var json = Repo.Run("dotnet", ProjectDir, "msbuild", ProjectName + ".csproj", "-getItem:None", "-nologo");
        using var document = JsonDocument.Parse(json);
        var files = new List<ContentFile>();
        if (!document.RootElement.GetProperty("Items").TryGetProperty("None", out var items))
        {
            return files;
        }

        foreach (var item in items.EnumerateArray())
        {
            var copy = item.TryGetProperty("CopyToOutputDirectory", out var value) ? value.GetString() : null;
            if (string.IsNullOrEmpty(copy) || copy == "Never")
            {
                continue;
            }

            var fullPath = item.GetProperty("FullPath").GetString()!;
            var link = item.TryGetProperty("Link", out var linkValue) ? linkValue.GetString() : null;
            var relative = !string.IsNullOrEmpty(link) ? link : Path.GetRelativePath(ProjectDir, fullPath);
            files.Add(new ContentFile(fullPath, relative.Replace('\\', '/')));
        }

        return files;
    }
}
