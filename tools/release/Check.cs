// Read only readiness check for one mod. It collects every failure before it reports, so one run shows everything to fix.
class Check(string repo, ModInfo mod)
{
    static readonly System.Version _firstRelease = new(1, 0, 0);
    static readonly HttpClient _http = CreateClient();

    readonly List<string> _failures = [];
    readonly List<string> _warnings = [];
    readonly List<string> _notes = [];

    public static async Task<int> Run(string repo, string name)
    {
        var mod = ModInfo.Load(repo, name);
        return await new Check(repo, mod).Run();
    }

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TimbnRelease/1.0");
        return client;
    }

    async Task<int> Run()
    {
        var tags = ModTags.Read(repo, mod);
        var isNew = tags.Count == 0;
        var version = System.Version.Parse(mod.Version);
        _notes.Add($"{mod.Product} {mod.Version} is {(isNew ? "new, with no release tag" : $"existing, newest tag {tags.Max()}")}");

        CheckVersion(isNew, version, tags);
        CheckChangelog(isNew);
        CheckPages();
        CheckArt();
        CheckLang();
        CheckCore();
        await CheckSites(isNew);

        foreach (var note in _notes)
        {
            Console.WriteLine($"note: {note}");
        }

        foreach (var warning in _warnings)
        {
            Console.WriteLine($"warning: {warning}");
        }

        foreach (var failure in _failures)
        {
            Console.WriteLine($"FAIL: {failure}");
        }

        Console.WriteLine(_failures.Count == 0 ? $"{mod.ProjectName} passed" : $"{mod.ProjectName} has {_failures.Count} failure(s)");
        return _failures.Count == 0 ? 0 : 1;
    }

    void CheckVersion(bool isNew, System.Version version, List<System.Version> tags)
    {
        if (isNew)
        {
            if (version > _firstRelease)
            {
                _failures.Add($"a new mod must be below 1.0.0 or exactly 1.0.0, but the csproj says {mod.Version}");
            }

            return;
        }

        if (version < _firstRelease)
        {
            _failures.Add($"an existing mod must be 1.0.0 or above, but the csproj says {mod.Version}");
        }

        if (version <= tags.Max())
        {
            _failures.Add($"{mod.Version} is not greater than the newest tag {tags.Max()}");
        }
    }

    void CheckChangelog(bool isNew)
    {
        var path = Path.Combine(mod.ProjectDir, "CHANGELOG.md");
        if (!File.Exists(path))
        {
            _failures.Add($"no CHANGELOG.md at {path}");
            return;
        }

        // A new mod's heading is renamed to 1.0.0 when it is prepared, so either heading counts.
        var versions = isNew ? new[] { mod.Version, "1.0.0" } : [mod.Version];
        if (versions.All(version => Changelog.TryRead(path, version) == null))
        {
            _failures.Add($"CHANGELOG.md has no bullets under ## {string.Join(" or ## ", versions)}");
        }
    }

    // The builder in pages only dry run parses page.yml, checks its images and enforces the length limits.
    void CheckPages()
    {
        var output = Path.Combine(Path.GetTempPath(), "timbn-check-" + Guid.NewGuid().ToString("N"));
        var captured = new StringWriter();
        var console = Console.Out;
        try
        {
            var page = PageParser.Load(Path.Combine(mod.DocsDir, "page.yml"));
            var options = new Options(mod.ModName, null, output, new string('0', 40), true) { PagesOnly = true };
            Console.SetOut(captured);
            new ReleaseBuilder(repo, mod, page, options).Run();
        }
        catch (ReleaseException e)
        {
            _failures.Add(e.Message);
        }
        catch (Exception e)
        {
            _failures.Add($"page.yml could not be read, {e.Message}");
        }
        finally
        {
            Console.SetOut(console);
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }
        }

        foreach (var line in captured.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(line => line.StartsWith("warning: ")))
        {
            _warnings.Add(line["warning: ".Length..]);
        }

        if (!Regex.IsMatch(mod.ThunderstoreName, "^[A-Za-z0-9_]+$"))
        {
            _failures.Add($"Thunderstore name {mod.ThunderstoreName} may only hold letters, digits and underscores");
        }
    }

    void CheckArt()
    {
        var icon = Path.Combine(mod.DocsDir, "icon.png");
        if (!File.Exists(icon))
        {
            _failures.Add($"no icon at {icon}");
        }
        else
        {
            var bytes = File.ReadAllBytes(icon);
            var isPng = bytes.Length > 24 && bytes[1] == 'P' && bytes[2] == 'N' && bytes[3] == 'G';
            // A PNG keeps its width and height as two big endian ints right after the signature and chunk header.
            var width = isPng ? System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16)) : 0;
            var height = isPng ? System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20)) : 0;
            if (width != 256 || height != 256)
            {
                _failures.Add($"icon.png must be a 256x256 PNG, but it is {width}x{height}");
            }
        }

        var header = Path.Combine(mod.DocsDir, "images", "header.jpg");
        if (!File.Exists(header))
        {
            _failures.Add($"no page banner at {header}");
        }
    }

    // A missing translation shows English, so only a missing en.txt fails and gaps are warnings.
    void CheckLang()
    {
        var folder = Path.Combine(mod.ProjectDir, "lang");
        if (!Directory.Exists(folder))
        {
            return;
        }

        var english = Path.Combine(folder, "en.txt");
        if (!File.Exists(english))
        {
            _failures.Add($"{folder} has no en.txt");
            return;
        }

        var keys = LangKeys(english);
        foreach (var file in Directory.EnumerateFiles(folder, "*.txt").Where(file => file != english).Order(StringComparer.Ordinal))
        {
            var missing = keys.Except(LangKeys(file)).Count();
            if (missing > 0)
            {
                _warnings.Add($"{Path.GetFileName(file)} is missing {missing} of {keys.Count} keys, which show in English");
            }
        }
    }

    static HashSet<string> LangKeys(string path) =>
        File.ReadLines(path)
            .Where(line => !line.StartsWith('#') && line.Contains('='))
            .Select(line => line[..line.IndexOf('=')].Trim())
            .ToHashSet();

    void CheckCore()
    {
        if (!mod.HasCoreFloor)
        {
            return;
        }

        if (System.Version.TryParse(mod.CoreFloor, out var floor) && floor > System.Version.Parse(mod.Core!.Version))
        {
            _failures.Add($"the Core floor {mod.CoreFloor} is above Core's current version {mod.Core.Version}");
        }
    }

    async Task CheckSites(bool isNew)
    {
        if (!mod.ToNexus)
        {
            _notes.Add("Nexus is off, since NexusModId is blank");
        }

        if (!mod.ToWorkshop)
        {
            _notes.Add(isNew ? "Workshop is off until prepare-new creates the item and writes WorkshopItemId" : "Workshop is off, since WorkshopItemId is blank");
        }

        if (!mod.ToThunderstore)
        {
            _notes.Add("Thunderstore is off, since SkipThunderstore is true");
        }

        if (mod.ToThunderstore && mod.HasCoreFloor)
        {
            var core = mod.Shared["ThunderstoreCorePackage"].Split('-');
            var status = await Status($"https://thunderstore.io/api/experimental/package/{core[0]}/{core[1]}/{mod.CoreFloor}/");
            if (status == HttpStatusCode.NotFound)
            {
                _failures.Add($"Core {mod.CoreFloor} is not on Thunderstore, so this mod's dependency would not resolve");
            }
            else if (status != HttpStatusCode.OK)
            {
                _warnings.Add($"could not confirm Core {mod.CoreFloor} on Thunderstore ({status})");
            }
        }

        if (isNew)
        {
            return;
        }

        if (mod.ToNexus)
        {
            var status = await Status($"https://www.nexusmods.com/{Repo.NexusGame}/mods/{mod.NexusModId}");
            if (status == HttpStatusCode.Forbidden)
            {
                _notes.Add($"Nexus blocks automated page checks, so confirm {mod.NexusUrl} by hand");
            }
            else
            {
                Report(status, "the Nexus page", mod.NexusUrl);
            }
        }

        if (mod.ToWorkshop)
        {
            var status = await Status(mod.WorkshopUrl);
            Report(status, "the Workshop page", mod.WorkshopUrl);
        }

        if (mod.ToThunderstore)
        {
            var status = await Status($"https://thunderstore.io/api/experimental/package/{mod.Shared["ThunderstoreNamespace"]}/{mod.ThunderstoreName}/");
            if (status == HttpStatusCode.NotFound)
            {
                _warnings.Add($"{mod.Shared["ThunderstoreNamespace"]}/{mod.ThunderstoreName} is not on Thunderstore, so the release creates that package and its name is permanent");
            }
            else if (status != HttpStatusCode.OK)
            {
                _warnings.Add($"could not confirm the Thunderstore package ({status})");
            }
        }
    }

    void Report(HttpStatusCode? status, string what, string url)
    {
        if (status == HttpStatusCode.OK)
        {
            return;
        }

        if (status == HttpStatusCode.NotFound)
        {
            _failures.Add($"{what} does not exist at {url}");
        }
        else
        {
            _warnings.Add($"could not confirm {what} at {url} ({status?.ToString() ?? "no answer"})");
        }
    }

    static async Task<HttpStatusCode?> Status(string url)
    {
        try
        {
            using var response = await _http.GetAsync(url);
            return response.StatusCode;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }
}
