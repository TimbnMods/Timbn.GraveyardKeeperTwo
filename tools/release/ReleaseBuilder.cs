class ReleaseBuilder(string repo, ModInfo mod, Page page, Options options)
{
    const int _steamDescriptionLimit = 8000;

    static readonly IReleaseSite[] _sites = [new NexusSite(), new SteamWorkshopSite(), new ThunderstoreSite()];

    readonly ReleaseContext _context = new(repo, mod, page, options);

    public void Run()
    {
        if (!options.DryRun && !options.PagesOnly && options.Sha == null)
        {
            throw new ReleaseException("a real run needs --sha, the release commit");
        }

        var sha = options.Sha ?? Repo.Run("git", repo, "rev-parse", "HEAD").Trim();
        var dllDir = Path.GetFullPath(options.DllDir ?? Path.Combine(mod.ProjectDir, "bin", "Release"));
        var dll = Path.Combine(dllDir, mod.AssemblyName + ".dll");
        if (!options.PagesOnly)
        {
            CheckDll(dll);
        }

        CheckImages();

        if (Directory.Exists(_context.OutDir))
        {
            Directory.Delete(_context.OutDir, true);
        }

        Directory.CreateDirectory(_context.OutDir);
        var changelog = Path.Combine(mod.ProjectDir, "CHANGELOG.md");
        var changes = options.PagesOnly ? Changelog.TryRead(changelog, mod.Version) ?? [] : Changelog.Read(changelog, mod.Version);
        string ImageUrl(string path) => $"https://raw.githubusercontent.com/{Repo.GitHubRepo}/{sha}/docs/{mod.ModName}/{path}";
        var title = page.Name ?? mod.Product;
        var pages = _sites.ToDictionary(site => site.Site, site => site.CreateRenderer(ImageUrl).Render(title, page, site.Requirements(mod)));
        if (pages[Site.Steam].Length > _steamDescriptionLimit)
        {
            _context.Warnings.Add($"the Steam description is {pages[Site.Steam].Length} characters and Steam takes {_steamDescriptionLimit}");
        }

        var pagesDir = Path.Combine(_context.OutDir, "pages");
        Directory.CreateDirectory(pagesDir);
        foreach (var site in _sites)
        {
            ReleaseContext.Write(Path.Combine(pagesDir, site.PageFile), pages[site.Site]);
        }

        ReleaseContext.Write(Path.Combine(pagesDir, "github.md"), GitHubBody(mod, changes));

        var plugin = options.PagesOnly ? string.Empty : _context.StagePlugin(dll);
        var packages = new Dictionary<Site, string>();
        foreach (var site in _sites)
        {
            var wanted = options.CreateWorkshop ? site.Site == Site.Steam : site.IsEnabled(mod);
            packages[site.Site] = wanted ? site.Package(_context, plugin, pages[site.Site], changes) : string.Empty;
        }

        WriteReleaseJson(sha, changes, packages);
        if (!string.IsNullOrEmpty(plugin))
        {
            Directory.Delete(plugin, true);
        }

        Console.WriteLine($"{mod.Product} {mod.Version} written to {_context.OutDir}");
        foreach (var warning in _context.Warnings)
        {
            Console.WriteLine($"warning: {warning}");
        }
    }

    void CheckDll(string dll)
    {
        if (!File.Exists(dll))
        {
            throw new ReleaseException($"no built DLL at {dll}");
        }

        var built = AssemblyName.GetAssemblyName(dll).Version!;
        var builtVersion = $"{built.Major}.{built.Minor}.{built.Build}";
        if (builtVersion == mod.Version)
        {
            return;
        }

        var message = $"{Path.GetFileName(dll)} is version {builtVersion} but the csproj says {mod.Version}";
        if (!options.DryRun)
        {
            throw new ReleaseException(message);
        }

        _context.Warnings.Add(message);
    }

    void CheckImages()
    {
        var images = new List<string>();
        if (page.Banner != null)
        {
            images.Add(page.Banner);
        }

        foreach (var block in page.Sections.SelectMany(section => section.Blocks))
        {
            if (block is ImageBlock image)
            {
                images.Add(image.Path);
            }
            else if (block is ListBlock list)
            {
                images.AddRange(list.Items.Where(item => item.Image != null).Select(item => item.Image!));
            }
        }

        foreach (var image in images.Where(image => !File.Exists(Path.Combine(mod.DocsDir, image))))
        {
            throw new ReleaseException($"page.yml links {image}, which isn't in {mod.DocsDir}");
        }
    }

    public static string GitHubBody(ModInfo mod, List<string> changes)
    {
        var body = new StringBuilder()
            .Append($"## Game Version\n\n{mod.GameVersion}\n\n")
            .Append($"## Build Number\n\n{mod.GameBuild}\n\n");
        if (mod.HasCoreFloor)
        {
            body.Append($"## Timbn Core Minimum Version\n\n{mod.CoreFloor}\n\n");
        }

        body.Append("## Changes\n\n");
        foreach (var change in changes)
        {
            body.Append($"- {change}\n");
        }

        return body.ToString();
    }

    // Empty fields are written as null, since jq treats an empty string as true.
    void WriteReleaseJson(string sha, List<string> changes, Dictionary<Site, string> packages)
    {
        var fileDescription = $"Tested on Graveyard Keeper 2 {mod.GameVersion} (build {mod.GameBuild}).";
        if (mod.HasCoreFloor)
        {
            fileDescription += $" Needs {mod.Core!.Product} {mod.CoreFloor} or newer.";
        }

        string? Relative(string path) => string.IsNullOrEmpty(path) ? null : Path.GetRelativePath(_context.OutDir, path).Replace('\\', '/');
        string? Blank(string value) => string.IsNullOrEmpty(value) ? null : value;

        var release = new Dictionary<string, object?>
        {
            ["project"] = mod.ProjectName,
            ["product"] = mod.Product,
            ["version"] = mod.Version,
            ["tag"] = mod.Tag,
            ["sha"] = sha,
            ["summary"] = page.Summary,
            ["coreFloor"] = Blank(mod.CoreFloor),
            ["gameVersion"] = mod.GameVersion,
            ["gameBuild"] = mod.GameBuild,
            ["nexusModId"] = Blank(mod.NexusModId),
            ["coreNexusModId"] = mod.HasCoreFloor ? Blank(mod.Core!.NexusModId) : null,
            ["nexusFileDescription"] = fileDescription,
            ["nexusZip"] = Relative(packages[Site.Nexus]),
            ["thunderstorePackage"] = string.IsNullOrEmpty(packages[Site.Thunderstore]) ? null : $"{mod.Shared["ThunderstoreNamespace"]}/{mod.ThunderstoreName}",
            ["thunderstoreZip"] = Relative(packages[Site.Thunderstore]),
            ["workshopItemId"] = Blank(mod.WorkshopItemId),
            ["workshopVdf"] = string.IsNullOrEmpty(packages[Site.Steam]) ? null : Relative(Path.Combine(packages[Site.Steam], "item.vdf")),
            ["changes"] = changes,
        };

        ReleaseContext.Write(Path.Combine(_context.OutDir, "release.json"), JsonSerializer.Serialize(release, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n");
    }
}
