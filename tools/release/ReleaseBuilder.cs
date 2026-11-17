class ReleaseBuilder
{
    const int _steamDescriptionLimit = 8000;

    static readonly IReleaseSite[] _sites = [new NexusSite(), new SteamWorkshopSite(), new ThunderstoreSite()];

    readonly string _repo;
    readonly ModInfo _mod;
    readonly Page _page;
    readonly Options _options;
    readonly ReleaseContext _context;

    public ReleaseBuilder(string repo, ModInfo mod, Page page, Options options)
    {
        _repo = repo;
        _mod = mod;
        _page = page;
        _options = options;
        _context = new ReleaseContext(repo, mod, page, options);
    }

    public void Run()
    {
        if (!_options.DryRun && !_options.PagesOnly && _options.Sha == null)
        {
            throw new ReleaseException("a real run needs --sha, the release commit");
        }

        var sha = _options.Sha ?? Repo.Run("git", _repo, "rev-parse", "HEAD").Trim();
        var dllDir = Path.GetFullPath(_options.DllDir ?? Path.Combine(_mod.ProjectDir, "bin", "Release"));
        var dll = Path.Combine(dllDir, _mod.AssemblyName + ".dll");
        if (!_options.PagesOnly)
        {
            CheckDll(dll);
        }

        CheckImages();

        if (Directory.Exists(_context.OutDir))
        {
            Directory.Delete(_context.OutDir, true);
        }

        Directory.CreateDirectory(_context.OutDir);
        var changelog = Path.Combine(_mod.ProjectDir, "CHANGELOG.md");
        var changes = _options.PagesOnly ? Changelog.TryRead(changelog, _mod.Version) ?? [] : Changelog.Read(changelog, _mod.Version);
        string ImageUrl(string path) => $"https://raw.githubusercontent.com/{Repo.GitHubRepo}/{sha}/docs/{_mod.ModName}/{path}";
        var title = _page.Name ?? _mod.Product;
        var pages = _sites.ToDictionary(
            site => site.Site,
            site => site.CreateRenderer(ImageUrl).Render(title, _page, site.Requirements(_mod)));

        if (pages[Site.Steam].Length > _steamDescriptionLimit)
        {
            _context.Warnings.Add($"the Steam description is {pages[Site.Steam].Length} characters and Steam takes {_steamDescriptionLimit}");
        }

        // Valve's KeyValues reader caps a value at 4096 bytes and may not read \" escapes. Check both on the first upload.
        var descriptionBytes = Encoding.UTF8.GetByteCount(pages[Site.Steam]);
        if (descriptionBytes > 4096)
        {
            _context.Warnings.Add($"the Steam description is {descriptionBytes} bytes, and steamcmd may cut item.vdf values at 4096");
        }

        var pagesDir = Path.Combine(_context.OutDir, "pages");
        Directory.CreateDirectory(pagesDir);
        foreach (var site in _sites)
        {
            ReleaseContext.Write(Path.Combine(pagesDir, site.PageFile), pages[site.Site]);
        }

        ReleaseContext.Write(Path.Combine(pagesDir, "github.md"), GitHubBody(_mod, changes));

        var plugin = _options.PagesOnly ? string.Empty : _context.StagePlugin(dll);
        var packages = new Dictionary<Site, string>();
        foreach (var site in _sites)
        {
            var wanted = _options.CreateWorkshop ? site.Site == Site.Steam : site.IsEnabled(_mod);
            packages[site.Site] = wanted ? site.Package(_context, plugin, pages[site.Site], changes) : string.Empty;
        }

        WriteReleaseJson(sha, changes, packages);
        if (!string.IsNullOrEmpty(plugin))
        {
            Directory.Delete(plugin, true);
        }

        Console.WriteLine($"{_mod.Product} {_mod.Version} written to {_context.OutDir}");
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
        if (builtVersion == _mod.Version)
        {
            return;
        }

        var message = $"{Path.GetFileName(dll)} is version {builtVersion} but the csproj says {_mod.Version}";
        if (!_options.DryRun)
        {
            throw new ReleaseException(message);
        }

        _context.Warnings.Add(message);
    }

    void CheckImages()
    {
        var images = new List<string>();
        if (_page.Banner != null)
        {
            images.Add(_page.Banner);
        }

        foreach (var block in _page.Sections.SelectMany(section => section.Blocks))
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

        foreach (var image in images.Where(image => !File.Exists(Path.Combine(_mod.DocsDir, image))))
        {
            throw new ReleaseException($"page.yml links {image}, which isn't in {_mod.DocsDir}");
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
        var fileDescription = $"Tested on Graveyard Keeper 2 {_mod.GameVersion} (build {_mod.GameBuild}).";
        if (_mod.HasCoreFloor)
        {
            fileDescription += $" Needs {_mod.Core!.Product} {_mod.CoreFloor} or newer.";
        }

        string? Relative(string path) => string.IsNullOrEmpty(path) ? null : Path.GetRelativePath(_context.OutDir, path).Replace('\\', '/');
        string? Blank(string value) => string.IsNullOrEmpty(value) ? null : value;

        var release = new Dictionary<string, object?>
        {
            ["project"] = _mod.ProjectName,
            ["product"] = _mod.Product,
            ["version"] = _mod.Version,
            ["tag"] = _mod.Tag,
            ["sha"] = sha,
            ["summary"] = _page.Summary,
            ["coreFloor"] = Blank(_mod.CoreFloor),
            ["gameVersion"] = _mod.GameVersion,
            ["gameBuild"] = _mod.GameBuild,
            ["nexusModId"] = Blank(_mod.NexusModId),
            ["coreNexusModId"] = _mod.HasCoreFloor ? Blank(_mod.Core!.NexusModId) : null,
            ["nexusFileDescription"] = fileDescription,
            ["nexusZip"] = Relative(packages[Site.Nexus]),
            ["thunderstorePackage"] = string.IsNullOrEmpty(packages[Site.Thunderstore]) ? null : $"{_mod.Shared["ThunderstoreNamespace"]}/{_mod.ThunderstoreName}",
            ["thunderstoreZip"] = Relative(packages[Site.Thunderstore]),
            ["workshopItemId"] = Blank(_mod.WorkshopItemId),
            ["workshopVdf"] = string.IsNullOrEmpty(packages[Site.Steam]) ? null : Relative(Path.Combine(packages[Site.Steam], "item.vdf")),
            ["changes"] = changes,
        };

        ReleaseContext.Write(Path.Combine(_context.OutDir, "release.json"), JsonSerializer.Serialize(release, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n");
    }
}
