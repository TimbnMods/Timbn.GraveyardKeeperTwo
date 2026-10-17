// Builds one mod's release packages and page text from its csproj, docs/<Mod>/page.yml and CHANGELOG.md.
// usage: dotnet run tools/release/Release.cs -- <mod> [--dll <folder>] [--out <folder>] [--sha <commit>] [--dry-run]
//        dotnet run tools/release/Release.cs -- list
//        dotnet run tools/release/Release.cs -- notes <mod>
// list prints, as JSON, every project whose CHANGELOG.md has a section for its current version.
// notes prints the GitHub release body for a project's current version.
//        dotnet run tools/release/Release.cs -- nexus <release folder>
// nexus uploads a built release folder to its Nexus page, with the key in NEXUS_API_KEY.
// <mod> is the project name with or without the Timbn.GraveyardKeeperTwo. prefix, in any case (chestlabels, Core).
// --dll is the folder holding the built DLL, bin/Release of the project by default.
// --out is where everything is written, artifacts/release by default.
// --sha pins image links to a commit. A real run needs it, and a dry run uses HEAD.
// --dry-run also unpacks every zip next to it and lets a DLL version mismatch through with a warning.
#:package YamlDotNet@16.3.0
#:property PublishAot=false

using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

try
{
    var repo = Repo.Find();
    if (args is ["list"])
    {
        Commands.List(repo);
    }
    else if (args is ["notes", var name])
    {
        Commands.Notes(repo, name);
    }
    else if (args is ["nexus", var folder])
    {
        await new NexusUploader(folder).Run();
    }
    else
    {
        var options = Options.Parse(args);
        var mod = ModInfo.Load(repo, options.Mod);
        var page = PageParser.Load(Path.Combine(mod.DocsDir, "page.yml"));
        var release = new ReleaseBuilder(repo, mod, page, options);
        release.Run();
    }
}
catch (ReleaseException e)
{
    Console.Error.WriteLine($"error: {e.Message}");
    return 1;
}

return 0;

class ReleaseException(string message) : Exception(message);

static class Commands
{
    public static void List(string repo)
    {
        var releases = new List<Dictionary<string, object>>();
        foreach (var dir in Directory.GetDirectories(Path.Combine(repo, "src"), Repo.ProjectPrefix + "*").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(dir);
            if (!File.Exists(Path.Combine(dir, name + ".csproj")))
            {
                continue;
            }

            var mod = ModInfo.Load(repo, name);
            if (Changelog.TryRead(Path.Combine(mod.ProjectDir, "CHANGELOG.md"), mod.Version) == null)
            {
                continue;
            }

            var assets = new List<string> { mod.AssemblyName + ".dll" };
            if (mod.Flag("GenerateDocumentationFile"))
            {
                assets.Add(mod.AssemblyName + ".xml");
            }

            releases.Add(new()
            {
                ["project"] = mod.ProjectName,
                ["mod"] = mod.ModName,
                ["product"] = mod.Product,
                ["version"] = mod.Version,
                ["tag"] = mod.Tag,
                ["title"] = $"{mod.Product} {mod.Version}",
                ["core"] = mod.IsCore,
                ["assets"] = assets,
            });
        }

        Console.WriteLine(JsonSerializer.Serialize(releases));
    }

    public static void Notes(string repo, string name)
    {
        var mod = ModInfo.Load(repo, name);
        var changes = Changelog.Read(Path.Combine(mod.ProjectDir, "CHANGELOG.md"), mod.Version);
        Console.Write(ReleaseBuilder.GitHubBody(mod, changes));
    }
}

record Options(string Mod, string? DllDir, string? OutDir, string? Sha, bool DryRun)
{
    public static Options Parse(string[] args)
    {
        string? mod = null, dll = null, output = null, sha = null;
        var dryRun = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dll":
                    dll = Next(args, ref i);
                    break;
                case "--out":
                    output = Next(args, ref i);
                    break;
                case "--sha":
                    sha = Next(args, ref i);
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    if (args[i].StartsWith("--") || mod != null)
                    {
                        throw new ReleaseException($"unknown argument {args[i]}");
                    }

                    mod = args[i];
                    break;
            }
        }

        if (mod == null)
        {
            throw new ReleaseException("usage: dotnet run tools/release/Release.cs -- <mod> [--dll <folder>] [--out <folder>] [--sha <commit>] [--dry-run]");
        }

        return new Options(mod, dll, output, sha, dryRun);
    }

    static string Next(string[] args, ref int i)
    {
        if (++i >= args.Length)
        {
            throw new ReleaseException($"{args[i - 1]} needs a value");
        }

        return args[i];
    }
}

static class Repo
{
    public const string ProjectPrefix = "Timbn.GraveyardKeeperTwo.";
    public const string GitHubRepo = "TimbnMods/Timbn.GraveyardKeeperTwo";
    public const string SteamAppId = "4358690";
    public const string NexusGame = "graveyardkeeper2";
    public const string NexusBepInExId = "48";
    public const string ThunderstoreCommunity = "graveyard-keeper-2";

    public static string Find()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Timbn.GraveyardKeeperTwo.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new ReleaseException("run this from inside the Timbn.GraveyardKeeperTwo repo");
    }

    public static string Run(string file, string workingDir, params string[] arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo(file)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new ReleaseException($"{file} {string.Join(' ', arguments)} failed\n{output.Result}{error.Result}");
        }

        return output.Result;
    }
}

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
    public required Dictionary<string, string> Properties { get; init; }
    public required Dictionary<string, string> Shared { get; init; }
    public required string? CoreFloor { get; init; }
    public required string GameVersion { get; init; }
    public required string GameBuild { get; init; }
    public required ModInfo? Core { get; init; }
    public required ModInfo? WorkshopSync { get; init; }

    public bool IsCore => ModName == "Core";
    public bool IsPatcher => Flag("BepInExPatcher");
    public bool Thunderstore => Flag("Thunderstore");
    public bool Workshop => Flag("SteamWorkshop") || Get("WorkshopItemId") != null;
    public string Tag => $"{ModName.ToLowerInvariant()}/v{Version}";
    public string ThunderstoreName => Get("ThunderstoreName") ?? Product.Replace(" ", "");
    public string[] ThunderstoreCategories => (Get("ThunderstoreCategories") ?? "mods").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public string? NexusModId => Get("NexusModId");
    public string? WorkshopItemId => Get("WorkshopItemId");
    public string NexusUrl => $"https://www.nexusmods.com/{Repo.NexusGame}/mods/{NexusModId}";
    public string WorkshopUrl => $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopItemId}";

    public string? Get(string name) => Properties.TryGetValue(name, out var value) && value != "" ? value : null;

    public bool Flag(string name) => string.Equals(Get(name), "true", StringComparison.OrdinalIgnoreCase);

    public static ModInfo Load(string repo, string name)
    {
        var src = Path.Combine(repo, "src");
        var wanted = name.StartsWith(Repo.ProjectPrefix, StringComparison.OrdinalIgnoreCase) ? name[Repo.ProjectPrefix.Length..] : name;
        var match = Directory.GetDirectories(src, Repo.ProjectPrefix + "*")
            .Select(Path.GetFileName)
            .FirstOrDefault(name => string.Equals(name![Repo.ProjectPrefix.Length..], wanted, StringComparison.OrdinalIgnoreCase))
            ?? throw new ReleaseException($"no project in src matches {name}");
        var shared = ReadProperties(Path.Combine(src, "Directory.Build.props"));
        var core = LoadProject(repo, shared, Repo.ProjectPrefix + "Core", null, null);
        var workshopSync = LoadProject(repo, shared, Repo.ProjectPrefix + "WorkshopSync", null, null);
        if (match == core.ProjectName)
        {
            return LoadProject(repo, shared, match, null, workshopSync);
        }

        return LoadProject(repo, shared, match, core, workshopSync);
    }

    static ModInfo LoadProject(string repo, Dictionary<string, string> shared, string projectName, ModInfo? core, ModInfo? workshopSync)
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
            AssemblyName = properties.GetValueOrDefault("AssemblyName") ?? projectName,
            Product = properties.GetValueOrDefault("Product") ?? throw new ReleaseException($"{projectName} has no Product"),
            Version = properties.GetValueOrDefault("Version") ?? shared["Version"],
            Properties = properties,
            Shared = shared,
            CoreFloor = core == null ? null : FindCoreFloor(projectDir),
            GameVersion = Constant(corePlugin, "TestedGameVersion"),
            GameBuild = Constant(corePlugin, "TestedGameBuild"),
            Core = core,
            WorkshopSync = workshopSync,
        };
    }

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

    static string? FindCoreFloor(string projectDir)
    {
        foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
        {
            var match = Regex.Match(File.ReadAllText(file), @"BepInDependency\(\s*TimbnCorePlugin\.Guid\s*,\s*""([^""]+)""");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
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

enum Site
{
    Nexus,
    Steam,
    Thunderstore,
    GitHub,
}

record SiteFilter(HashSet<Site>? Only, HashSet<Site> Skip)
{
    public static readonly SiteFilter All = new(null, []);

    public bool Shows(Site site) => (Only == null || Only.Contains(site)) && !Skip.Contains(site);
}

abstract record Block(SiteFilter Filter);

record TextBlock(SiteFilter Filter, string Text) : Block(Filter);

record ListBlock(SiteFilter Filter, bool Ordered, List<ListItem> Items) : Block(Filter);

record ListItem(string Text, string? Image);

record ImageBlock(SiteFilter Filter, string Path) : Block(Filter);

record SpoilerBlock(SiteFilter Filter, string Title, string Text) : Block(Filter);

record SubtitleBlock(SiteFilter Filter, string Text) : Block(Filter);

record FixedStatus(string Level, string? Note);

record Section(string Title, SiteFilter Filter, FixedStatus? Fixed, List<Block> Blocks);

record Page(string? Name, string Summary, string? Banner, List<Section> Sections);

static class PageParser
{
    const int _summaryLimit = 250;
    static readonly string[] _fixedLevels = ["fixed", "partly", "mostly", "half"];

    public static Page Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new ReleaseException($"no page text at {path}");
        }

        var stream = new YamlStream();
        using (var reader = new StreamReader(path))
        {
            stream.Load(reader);
        }

        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        var summary = Scalar(root, "summary") ?? throw new ReleaseException("page.yml has no summary");
        if (summary.Length > _summaryLimit)
        {
            throw new ReleaseException($"the summary is {summary.Length} characters and Thunderstore takes {_summaryLimit}");
        }

        var sections = new List<Section>();
        if (root.Children.TryGetValue(new YamlScalarNode("sections"), out var sectionNodes))
        {
            foreach (var node in ((YamlSequenceNode)sectionNodes).Children.Cast<YamlMappingNode>())
            {
                sections.Add(ParseSection(node));
            }
        }

        return new Page(Scalar(root, "name"), summary, Scalar(root, "banner"), sections);
    }

    static Section ParseSection(YamlMappingNode node)
    {
        var title = Scalar(node, "title") ?? throw new ReleaseException("a section has no title");
        var blocks = new List<Block>();
        FixedStatus? status = null;
        foreach (var (keyNode, value) in node.Children)
        {
            var key = ((YamlScalarNode)keyNode).Value!;
            switch (key)
            {
                case "title":
                case "only":
                case "skip":
                    break;
                case "fixed":
                    status = ParseFixed(value, title);
                    break;
                case "blocks":
                    foreach (var blockNode in ((YamlSequenceNode)value).Children.Cast<YamlMappingNode>())
                    {
                        blocks.Add(ParseBlockMapping(blockNode, title));
                    }

                    break;
                default:
                    blocks.Add(ParseBlock(key, value, SiteFilter.All, title));
                    break;
            }
        }

        return new Section(title, ParseFilter(node), status, blocks);
    }

    static Block ParseBlockMapping(YamlMappingNode node, string section)
    {
        var filter = ParseFilter(node);
        var kinds = node.Children.Where(pair => ((YamlScalarNode)pair.Key).Value is not ("only" or "skip")).ToList();
        if (kinds.Count != 1)
        {
            throw new ReleaseException($"a block in {section} needs exactly one of text, list, olist, image, spoiler or subtitle");
        }

        return ParseBlock(((YamlScalarNode)kinds[0].Key).Value!, kinds[0].Value, filter, section);
    }

    static Block ParseBlock(string kind, YamlNode value, SiteFilter filter, string section) => kind switch
    {
        "text" => new TextBlock(filter, ((YamlScalarNode)value).Value!),
        "subtitle" => new SubtitleBlock(filter, ((YamlScalarNode)value).Value!),
        "image" => new ImageBlock(filter, ((YamlScalarNode)value).Value!),
        "list" or "olist" => new ListBlock(filter, kind == "olist", [.. ((YamlSequenceNode)value).Children.Select(ParseItem)]),
        "spoiler" => new SpoilerBlock(
            filter,
            Scalar((YamlMappingNode)value, "title") ?? throw new ReleaseException($"a spoiler in {section} has no title"),
            Scalar((YamlMappingNode)value, "text") ?? throw new ReleaseException($"a spoiler in {section} has no text")),
        _ => throw new ReleaseException($"{section} has an unknown block {kind}"),
    };

    static ListItem ParseItem(YamlNode node)
    {
        if (node is YamlScalarNode scalar)
        {
            return new ListItem(scalar.Value!, null);
        }

        var mapping = (YamlMappingNode)node;
        return new ListItem(Scalar(mapping, "text") ?? throw new ReleaseException("a list item has no text"), Scalar(mapping, "image"));
    }

    static FixedStatus ParseFixed(YamlNode node, string section)
    {
        var status = node is YamlScalarNode scalar
            ? new FixedStatus(scalar.Value!, null)
            : new FixedStatus(Scalar((YamlMappingNode)node, "status") ?? "fixed", Scalar((YamlMappingNode)node, "note"));
        if (!_fixedLevels.Contains(status.Level))
        {
            throw new ReleaseException($"{section} has fixed status {status.Level}, which isn't one of {string.Join(", ", _fixedLevels)}");
        }

        return status;
    }

    static SiteFilter ParseFilter(YamlMappingNode node) => new(Sites(node, "only"), Sites(node, "skip") ?? []);

    static HashSet<Site>? Sites(YamlMappingNode node, string key)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(key), out var value))
        {
            return null;
        }

        var names = value is YamlSequenceNode sequence ? sequence.Children.Select(child => ((YamlScalarNode)child).Value!) : [((YamlScalarNode)value).Value!];
        return [.. names.Select(name => Enum.TryParse<Site>(name, true, out var site) ? site : throw new ReleaseException($"unknown site {name}"))];
    }

    static string? Scalar(YamlMappingNode node, string key) =>
        node.Children.TryGetValue(new YamlScalarNode(key), out var value) ? ((YamlScalarNode)value).Value : null;
}

record Requirement(string Name, string Url, string? Note);

abstract class Renderer(Site site, Func<string, string> imageUrl)
{
    static readonly Regex _inline = new(@"\*\*(?<bold>.+?)\*\*|`(?<key>[^`]+)`|\[(?<text>[^\]]+)\]\((?<url>[^)\s]+)\)");

    protected readonly StringBuilder Output = new();

    public Site Site => site;

    protected string ImageUrl(string path) => imageUrl(path);

    public string Render(string title, Page page, List<Requirement> requirements)
    {
        Start(title, page.Banner == null ? null : ImageUrl(page.Banner), Inline(page.Summary));
        var sections = page.Sections.Where(section => section.Filter.Shows(site)).ToList();
        var requirementsShown = false;
        foreach (var section in sections)
        {
            // An empty Requirements section marks where the generated list goes.
            if (section.Title == "Requirements" && section.Blocks.Count == 0)
            {
                Requirements(requirements);
                requirementsShown = true;
                continue;
            }

            Heading(section.Title, section.Fixed);
            if (section.Fixed != null)
            {
                Paragraph(Bold(FixedLine(section.Fixed)));
            }

            foreach (var block in section.Blocks.Where(block => block.Filter.Shows(site)))
            {
                switch (block)
                {
                    case TextBlock text:
                        foreach (var paragraph in Regex.Split(text.Text.Trim(), @"\n\s*\n"))
                        {
                            Paragraph(Inline(paragraph.Trim()));
                        }

                        break;
                    case ListBlock list:
                        List(list.Ordered, [.. list.Items.Select(item => (Inline(item.Text), item.Image == null ? null : ImageUrl(item.Image)))]);
                        break;
                    case ImageBlock image:
                        Image(ImageUrl(image.Path));
                        break;
                    case SpoilerBlock spoiler:
                        Spoiler(Inline(spoiler.Title), Inline(spoiler.Text));
                        break;
                    case SubtitleBlock subtitle:
                        Subtitle(Inline(subtitle.Text));
                        break;
                }
            }
        }

        if (!requirementsShown)
        {
            Requirements(requirements);
        }

        return Regex.Replace(Output.ToString(), @"\n{3,}", "\n\n").TrimEnd() + "\n";
    }

    void Requirements(List<Requirement> requirements)
    {
        if (requirements.Count == 0)
        {
            return;
        }

        Heading("Requirements", null);
        List(false, [.. requirements.Select(RequirementText)]);
    }

    (string, string?) RequirementText(Requirement requirement) =>
        (Link(requirement.Name, requirement.Url) + (requirement.Note == null ? "" : requirement.Note.StartsWith(',') ? requirement.Note : " " + requirement.Note), null);

    static string FixedLine(FixedStatus status) => status.Level switch
    {
        "fixed" => "FIXED BY GAME DEVELOPERS",
        _ => $"{status.Level.ToUpperInvariant()} FIXED BY GAME DEVELOPERS",
    } + (status.Note == null ? "" : $". {status.Note}");

    protected string Inline(string text)
    {
        var builder = new StringBuilder();
        var last = 0;
        foreach (Match match in _inline.Matches(text))
        {
            builder.Append(Plain(text[last..match.Index]));
            if (match.Groups["bold"].Success)
            {
                builder.Append(Bold(Plain(match.Groups["bold"].Value)));
            }
            else if (match.Groups["key"].Success)
            {
                builder.Append(Key(match.Groups["key"].Value));
            }
            else
            {
                builder.Append(Link(Plain(match.Groups["text"].Value), match.Groups["url"].Value));
            }

            last = match.Index + match.Length;
        }

        return builder.Append(Plain(text[last..])).ToString();
    }

    protected virtual string Plain(string text) => text;

    protected abstract void Start(string title, string? banner, string summary);
    protected abstract void Heading(string title, FixedStatus? status);
    protected abstract void Subtitle(string text);
    protected abstract void Paragraph(string text);
    protected abstract void List(bool ordered, List<(string Text, string? Image)> items);
    protected abstract void Image(string url);
    protected abstract void Spoiler(string title, string text);
    protected abstract string Bold(string text);
    protected abstract string Key(string text);
    protected abstract string Link(string text, string url);
}

class NexusRenderer(Func<string, string> imageUrl) : Renderer(Site.Nexus, imageUrl)
{
    const string _headingColor = "#ffd65a";
    const string _keyColor = "#f0e2c4";

    protected override void Start(string title, string? banner, string summary)
    {
        if (banner != null)
        {
            Output.Append($"[img]{banner}[/img]\n");
        }

        Output.Append($"[left]{summary}[/left]\n");
    }

    protected override void Heading(string title, FixedStatus? status)
    {
        var text = status?.Level == "fixed" ? $"[s]{title}[/s]" : title;
        Output.Append($"[line]\n[center][b][size=5][color={_headingColor}]{text}[/color][/size][/b][/center]\n");
    }

    protected override void Subtitle(string text) => Output.Append($"[b][color={_keyColor}]{text}[/color][/b]\n");

    protected override void Paragraph(string text) => Output.Append(text).Append('\n');

    protected override void List(bool ordered, List<(string Text, string? Image)> items)
    {
        Output.Append(ordered ? "[list=1]\n" : "[list]\n");
        foreach (var (text, image) in items)
        {
            Output.Append("[*]").Append(text);
            if (image != null)
            {
                Output.Append($"\n[img]{image}[/img]");
            }

            Output.Append("[/*]\n");
        }

        Output.Append("[/list]\n");
    }

    protected override void Image(string url) => Output.Append($"[center][img]{url}[/img][/center]\n");

    protected override void Spoiler(string title, string text) => Output.Append($"[b]{title}[/b]\n[spoiler]{text}[/spoiler]\n");

    protected override string Bold(string text) => $"[b]{text}[/b]";

    protected override string Key(string text) => $"[color={_keyColor}]{text}[/color]";

    protected override string Link(string text, string url) => $"[url={url}]{text}[/url]";
}

class SteamRenderer(Func<string, string> imageUrl) : Renderer(Site.Steam, imageUrl)
{
    protected override void Start(string title, string? banner, string summary)
    {
        if (banner != null)
        {
            Output.Append($"[img]{banner}[/img]\n\n");
        }

        Output.Append(summary).Append('\n');
    }

    protected override void Heading(string title, FixedStatus? status)
    {
        var text = status?.Level == "fixed" ? $"[strike]{title}[/strike]" : title;
        Output.Append($"\n[h2]{text}[/h2]\n");
    }

    protected override void Subtitle(string text) => Output.Append($"[h3]{text}[/h3]\n");

    protected override void Paragraph(string text) => Output.Append(text).Append('\n');

    protected override void List(bool ordered, List<(string Text, string? Image)> items)
    {
        Output.Append(ordered ? "[olist]\n" : "[list]\n");
        foreach (var (text, image) in items)
        {
            Output.Append("[*]").Append(text).Append('\n');
            if (image != null)
            {
                Output.Append($"[img]{image}[/img]\n");
            }
        }

        Output.Append(ordered ? "[/olist]\n" : "[/list]\n");
    }

    protected override void Image(string url) => Output.Append($"[img]{url}[/img]\n");

    protected override void Spoiler(string title, string text) => Output.Append($"{title}\n[spoiler]{text}[/spoiler]\n");

    protected override string Bold(string text) => $"[b]{text}[/b]";

    protected override string Key(string text) => $"[b]{text}[/b]";

    protected override string Link(string text, string url) => $"[url={url}]{text}[/url]";
}

class MarkdownRenderer(Func<string, string> imageUrl) : Renderer(Site.Thunderstore, imageUrl)
{
    protected override void Start(string title, string? banner, string summary)
    {
        Output.Append($"# {title}\n\n");
        if (banner != null)
        {
            Output.Append($"![{title}]({banner})\n\n");
        }

        Output.Append(summary).Append('\n');
    }

    protected override void Heading(string title, FixedStatus? status)
    {
        var text = status?.Level == "fixed" ? $"~~{title}~~" : title;
        Output.Append($"\n## {text}\n\n");
    }

    protected override void Subtitle(string text) => Output.Append($"### {text}\n\n");

    protected override void Paragraph(string text) => Output.Append(text).Append("\n\n");

    protected override void List(bool ordered, List<(string Text, string? Image)> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var marker = ordered ? $"{i + 1}. " : "- ";
            Output.Append(marker).Append(items[i].Text).Append('\n');
            if (items[i].Image != null)
            {
                Output.Append('\n').Append(' ', marker.Length).Append($"![]({items[i].Image})\n\n");
            }
        }

        Output.Append('\n');
    }

    protected override void Image(string url) => Output.Append($"![]({url})\n\n");

    // Thunderstore's Markdown may strip <details>, so the spoiler is a plain paragraph there.
    protected override void Spoiler(string title, string text) => Output.Append($"**{title}**\n\n{text}\n\n");

    protected override string Bold(string text) => $"**{text}**";

    protected override string Key(string text) => text.Contains('`') ? $"`` {text} ``" : $"`{text}`";

    protected override string Link(string text, string url) => $"[{text}]({url})";

    protected override string Plain(string text) => Regex.Replace(text, @"([\\*<>])", @"\$1");
}

class ReleaseBuilder(string repo, ModInfo mod, Page page, Options options)
{
    const int _steamDescriptionLimit = 8000;

    readonly string _outDir = Path.GetFullPath(Path.Combine(options.OutDir ?? Path.Combine(repo, "artifacts", "release"), mod.ProjectName));
    readonly List<string> _warnings = [];

    public void Run()
    {
        if (!options.DryRun && options.Sha == null)
        {
            throw new ReleaseException("a real run needs --sha, the release commit");
        }

        var sha = options.Sha ?? Repo.Run("git", repo, "rev-parse", "HEAD").Trim();
        var dllDir = Path.GetFullPath(options.DllDir ?? Path.Combine(mod.ProjectDir, "bin", "Release"));
        var dll = Path.Combine(dllDir, mod.AssemblyName + ".dll");
        CheckDll(dll);
        CheckImages();

        if (Directory.Exists(_outDir))
        {
            Directory.Delete(_outDir, true);
        }

        Directory.CreateDirectory(_outDir);
        var changes = Changelog.Read(Path.Combine(mod.ProjectDir, "CHANGELOG.md"), mod.Version);
        string ImageUrl(string path) => $"https://raw.githubusercontent.com/{Repo.GitHubRepo}/{sha}/docs/{mod.ModName}/{path}";
        var title = page.Name ?? mod.Product;
        var pages = new Dictionary<Site, string>
        {
            [Site.Nexus] = new NexusRenderer(ImageUrl).Render(title, page, NexusRequirements()),
            [Site.Steam] = new SteamRenderer(ImageUrl).Render(title, page, SteamRequirements()),
            [Site.Thunderstore] = new MarkdownRenderer(ImageUrl).Render(title, page, ThunderstoreRequirements()),
            [Site.GitHub] = GitHubBody(mod, changes),
        };
        if (pages[Site.Steam].Length > _steamDescriptionLimit)
        {
            _warnings.Add($"the Steam description is {pages[Site.Steam].Length} characters and Steam takes {_steamDescriptionLimit}");
        }

        var pagesDir = Path.Combine(_outDir, "pages");
        Directory.CreateDirectory(pagesDir);
        Write(Path.Combine(pagesDir, "nexus.bbcode"), pages[Site.Nexus]);
        Write(Path.Combine(pagesDir, "steam.bbcode"), pages[Site.Steam]);
        Write(Path.Combine(pagesDir, "thunderstore.md"), pages[Site.Thunderstore]);
        Write(Path.Combine(pagesDir, "github.md"), pages[Site.GitHub]);

        var plugin = StagePlugin(dll, pages[Site.Thunderstore]);
        var nexusZip = WriteNexusZip(plugin);
        var thunderstoreZip = mod.Thunderstore ? WriteThunderstore(plugin, pages[Site.Thunderstore]) : null;
        var workshopDir = mod.Workshop ? WriteWorkshop(plugin, pages[Site.Steam], changes) : null;
        WriteReleaseJson(sha, changes, nexusZip, thunderstoreZip, workshopDir);
        Directory.Delete(plugin, true);

        Console.WriteLine($"{mod.Product} {mod.Version} written to {_outDir}");
        foreach (var warning in _warnings)
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

        _warnings.Add(message);
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

    List<Requirement> NexusRequirements()
    {
        var requirements = new List<Requirement> { new("BepInEx for Graveyard Keeper 2", $"https://www.nexusmods.com/{Repo.NexusGame}/mods/{Repo.NexusBepInExId}", null) };
        if (mod.CoreFloor != null)
        {
            requirements.Add(new(mod.Core!.Product, mod.Core.NexusUrl, $"{mod.CoreFloor} or newer"));
        }

        return requirements;
    }

    List<Requirement> SteamRequirements()
    {
        var requirements = new List<Requirement>
        {
            new("BepInEx for Graveyard Keeper 2", $"https://www.nexusmods.com/{Repo.NexusGame}/mods/{Repo.NexusBepInExId}", null),
            new(mod.WorkshopSync!.Product, mod.WorkshopSync.NexusUrl, ", which installs the Workshop mods you subscribe to"),
        };
        if (mod.CoreFloor != null)
        {
            requirements.Add(new(mod.Core!.Product, mod.Core.WorkshopUrl, $"{mod.CoreFloor} or newer"));
        }

        return requirements;
    }

    List<Requirement> ThunderstoreRequirements()
    {
        var bepInEx = mod.Shared["ThunderstoreBepInExPack"].Split('-');
        var requirements = new List<Requirement> { new("BepInExPack", ThunderstoreUrl(bepInEx[0], bepInEx[1]), null) };
        if (mod.CoreFloor != null)
        {
            var core = mod.Shared["ThunderstoreCorePackage"].Split('-');
            requirements.Add(new(mod.Core!.Product, ThunderstoreUrl(core[0], core[1]), $"{mod.CoreFloor} or newer"));
        }

        return requirements;
    }

    static string ThunderstoreUrl(string owner, string name) => $"https://thunderstore.io/c/{Repo.ThunderstoreCommunity}/p/{owner}/{name}/";

    public static string GitHubBody(ModInfo mod, List<string> changes)
    {
        var body = new StringBuilder()
            .Append($"## Game Version\n\n{mod.GameVersion}\n\n")
            .Append($"## Build Number\n\n{mod.GameBuild}\n\n");
        if (mod.CoreFloor != null)
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

    string StagePlugin(string dll, string readme)
    {
        var stage = Path.Combine(_outDir, "plugin");
        Directory.CreateDirectory(stage);
        File.Copy(dll, Path.Combine(stage, Path.GetFileName(dll)));
        var xml = Path.ChangeExtension(dll, ".xml");
        if (mod.Flag("GenerateDocumentationFile") && File.Exists(xml))
        {
            File.Copy(xml, Path.Combine(stage, Path.GetFileName(xml)));
        }

        foreach (var file in mod.GatherContent())
        {
            var target = Path.Combine(stage, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file.Source, target);
        }

        Write(Path.Combine(stage, "README.md"), readme);
        var license = Path.Combine(repo, "LICENSE");
        if (File.Exists(license))
        {
            File.Copy(license, Path.Combine(stage, "LICENSE.txt"));
        }

        return stage;
    }

    string WriteNexusZip(string plugin)
    {
        var root = mod.IsPatcher ? $"BepInEx/patchers/{mod.ProjectName}" : mod.ProjectName;
        var zip = Path.Combine(_outDir, "nexus", $"{mod.ProjectName}-{mod.Version}.zip");
        Zip(zip, [.. Files(plugin, root)]);
        return zip;
    }

    string WriteThunderstore(string plugin, string readme)
    {
        if (mod.IsPatcher)
        {
            throw new ReleaseException("a preloader patcher can't go to Thunderstore yet");
        }

        var name = mod.ThunderstoreName;
        if (!Regex.IsMatch(name, "^[A-Za-z0-9_]+$"))
        {
            throw new ReleaseException($"Thunderstore name {name} may only hold letters, digits and underscores");
        }

        var icon = RequireIcon("Thunderstore");
        var dependencies = ThunderstoreDependencies();
        var dir = Path.Combine(_outDir, "thunderstore");
        Directory.CreateDirectory(dir);
        var manifest = JsonSerializer.Serialize(
            new Dictionary<string, object>
            {
                ["name"] = name,
                ["version_number"] = mod.Version,
                ["website_url"] = mod.Shared["ThunderstoreWebsiteUrl"],
                ["description"] = page.Summary,
                ["dependencies"] = dependencies.Select(dependency => $"{dependency.Key}-{dependency.Value}").ToArray(),
            },
            new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        var entries = new List<(string, byte[])>
        {
            ("manifest.json", Encoding.UTF8.GetBytes(manifest + "\n")),
            ("README.md", Encoding.UTF8.GetBytes(readme)),
            ("CHANGELOG.md", File.ReadAllBytes(Path.Combine(mod.ProjectDir, "CHANGELOG.md"))),
            ("icon.png", File.ReadAllBytes(icon)),
        };
        entries.AddRange(Files(plugin, mod.ProjectName));
        var zip = Path.Combine(dir, $"{mod.Shared["ThunderstoreNamespace"]}-{name}-{mod.Version}.zip");
        Zip(zip, entries);
        Write(Path.Combine(dir, "thunderstore.toml"), ThunderstoreToml(name, dependencies));
        return zip;
    }

    Dictionary<string, string> ThunderstoreDependencies()
    {
        var bepInEx = mod.Shared["ThunderstoreBepInExPack"];
        var split = bepInEx.LastIndexOf('-');
        var dependencies = new Dictionary<string, string> { [bepInEx[..split]] = bepInEx[(split + 1)..] };
        if (mod.CoreFloor != null)
        {
            var first = mod.Shared["ThunderstoreCoreFirstVersion"];
            dependencies[mod.Shared["ThunderstoreCorePackage"]] = Version.Parse(mod.CoreFloor) < Version.Parse(first) ? first : mod.CoreFloor;
        }

        return dependencies;
    }

    // tcli refuses to publish without a project config, even for a prebuilt zip, so this only names the package.
    string ThunderstoreToml(string name, Dictionary<string, string> dependencies)
    {
        var toml = new StringBuilder()
            .Append("[config]\nschemaVersion = \"0.0.1\"\n\n")
            .Append("[package]\n")
            .Append($"namespace = {TomlString(mod.Shared["ThunderstoreNamespace"])}\n")
            .Append($"name = {TomlString(name)}\n")
            .Append($"versionNumber = {TomlString(mod.Version)}\n")
            .Append($"description = {TomlString(page.Summary)}\n")
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
            .Append($"{Repo.ThunderstoreCommunity} = [{string.Join(", ", mod.ThunderstoreCategories.Select(TomlString))}]\n");
        return toml.ToString();
    }

    static string TomlString(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    string WriteWorkshop(string plugin, string description, List<string> changes)
    {
        var itemId = mod.WorkshopItemId;
        if (itemId == null)
        {
            if (!options.DryRun)
            {
                throw new ReleaseException("the Workshop item has to be made once by hand before its id goes in WorkshopItemId");
            }

            _warnings.Add("no WorkshopItemId, so item.vdf uses 0");
            itemId = "0";
        }

        var dir = Path.Combine(_outDir, "workshop");
        var folder = mod.IsPatcher ? "patchers" : "plugins";
        var content = Path.Combine(dir, "content", "BepInEx", folder, mod.ProjectName);
        CopyDirectory(plugin, content);
        // The preview sits inside the Workshop folder so the upload job has everything in one artifact.
        var icon = Path.Combine(dir, "preview.png");
        File.Copy(RequireIcon("Steam"), icon);
        var changeNote = new StringBuilder("[list]\n");
        foreach (var change in changes)
        {
            changeNote.Append($"[*]{change}\n");
        }

        changeNote.Append("[/list]");
        // Valve's KeyValues reader caps a value at 4096 bytes and may not read \" escapes. Check both on the first upload.
        var descriptionBytes = Encoding.UTF8.GetByteCount(description);
        if (descriptionBytes > 4096)
        {
            _warnings.Add($"the Steam description is {descriptionBytes} bytes, and steamcmd may cut item.vdf values at 4096");
        }

        var vdf = new StringBuilder("\"workshopitem\"\n{\n")
            .Append($"\t\"appid\"\t\t\"{Repo.SteamAppId}\"\n")
            .Append($"\t\"publishedfileid\"\t\t{VdfString(itemId)}\n")
            .Append($"\t\"contentfolder\"\t\t{VdfString(Path.Combine(dir, "content"))}\n")
            .Append($"\t\"previewfile\"\t\t{VdfString(icon)}\n")
            .Append($"\t\"visibility\"\t\t\"0\"\n")
            .Append($"\t\"title\"\t\t{VdfString(mod.Product)}\n")
            .Append($"\t\"description\"\t\t{VdfString(description.TrimEnd())}\n")
            .Append($"\t\"changenote\"\t\t{VdfString(changeNote.ToString())}\n")
            .Append("}\n");
        Write(Path.Combine(dir, "item.vdf"), vdf.ToString());
        return dir;
    }

    static string VdfString(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    string RequireIcon(string site)
    {
        var icon = Path.Combine(mod.DocsDir, "icon.png");
        return File.Exists(icon) ? icon : throw new ReleaseException($"{site} needs a 256x256 icon at {icon}");
    }

    void WriteReleaseJson(string sha, List<string> changes, string nexusZip, string? thunderstoreZip, string? workshopDir)
    {
        var fileDescription = $"Tested on Graveyard Keeper 2 {mod.GameVersion} (build {mod.GameBuild}).";
        if (mod.CoreFloor != null)
        {
            fileDescription += $" Needs {mod.Core!.Product} {mod.CoreFloor} or newer.";
        }

        var release = new Dictionary<string, object?>
        {
            ["project"] = mod.ProjectName,
            ["product"] = mod.Product,
            ["version"] = mod.Version,
            ["tag"] = mod.Tag,
            ["sha"] = sha,
            ["summary"] = page.Summary,
            ["coreFloor"] = mod.CoreFloor,
            ["gameVersion"] = mod.GameVersion,
            ["gameBuild"] = mod.GameBuild,
            ["nexusModId"] = mod.NexusModId,
            ["coreNexusModId"] = mod.CoreFloor == null ? null : mod.Core!.NexusModId,
            ["nexusFileDescription"] = fileDescription,
            ["nexusZip"] = Path.GetRelativePath(_outDir, nexusZip).Replace('\\', '/'),
            ["thunderstorePackage"] = thunderstoreZip == null ? null : $"{mod.Shared["ThunderstoreNamespace"]}/{mod.ThunderstoreName}",
            ["thunderstoreZip"] = thunderstoreZip == null ? null : Path.GetRelativePath(_outDir, thunderstoreZip).Replace('\\', '/'),
            ["workshopItemId"] = mod.WorkshopItemId,
            ["workshopVdf"] = workshopDir == null ? null : Path.GetRelativePath(_outDir, Path.Combine(workshopDir, "item.vdf")).Replace('\\', '/'),
            ["changes"] = changes,
        };
        Write(Path.Combine(_outDir, "release.json"), JsonSerializer.Serialize(release, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n");
    }

    static IEnumerable<(string Path, byte[] Bytes)> Files(string dir, string root) =>
        Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(file => ($"{root}/{Path.GetRelativePath(dir, file).Replace('\\', '/')}", File.ReadAllBytes(file)));

    // Entries are written by hand so every path uses forward slashes, which some mod managers need.
    void Zip(string path, List<(string Path, byte[] Bytes)> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach (var (name, bytes) in entries)
            {
                using var stream = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
                stream.Write(bytes);
            }
        }

        if (options.DryRun)
        {
            ZipFile.ExtractToDirectory(path, path + ".contents");
        }
    }

    static void CopyDirectory(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }

    static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
}

static class Changelog
{
    public static List<string> Read(string path, string version)
    {
        if (!File.Exists(path))
        {
            throw new ReleaseException($"no changelog at {path}");
        }

        return TryRead(path, version) ?? throw new ReleaseException($"{Path.GetFileName(path)} has no bullets under ## {version}");
    }

    public static List<string>? TryRead(string path, string version)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var changes = new List<string>();
        var inVersion = false;
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("## "))
            {
                if (inVersion)
                {
                    break;
                }

                inVersion = line[3..].Trim() == version;
            }
            else if (inVersion && line.StartsWith("- "))
            {
                changes.Add(line[2..].Trim());
            }
            else if (inVersion && changes.Count > 0 && line.StartsWith("  ") && line.Trim() != "")
            {
                changes[^1] += " " + line.Trim();
            }
        }

        return changes.Count > 0 ? changes : null;
    }
}

// Most of these v3 calls are marked Experimental by Nexus, so expect to adjust them as the API changes.
class NexusUploader(string folder)
{
    const string _api = "https://api.nexusmods.com/v3";

    readonly HttpClient _http = new();
    readonly JsonObject _release = JsonNode.Parse(File.ReadAllText(Path.Combine(folder, "release.json")))!.AsObject();

    string Text(string key) => _release[key]?.GetValue<string>() ?? throw new ReleaseException($"release.json has no {key}");

    public async Task Run()
    {
        var key = Environment.GetEnvironmentVariable("NEXUS_API_KEY");
        if (string.IsNullOrEmpty(key))
        {
            throw new ReleaseException("NEXUS_API_KEY isn't set");
        }
        _http.DefaultRequestHeaders.Add("apikey", key);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("TimbnRelease/1.0");

        var version = Text("version");
        var modId = await GlobalModId(Text("nexusModId"));
        var mainFile = await MainFile(modId);
        if (mainFile != null && mainFile.Versions.Any(entry => entry["version"]?.GetValue<string>() == version))
        {
            Console.WriteLine($"Nexus already has {version}, so only the page text is updated");
        }
        else
        {
            var versionId = await UploadFile(modId, mainFile?.Id, version);
            await SetRequirements(versionId);
            var changelog = string.Join("\n", _release["changes"]!.AsArray().Select(change => "- " + change!.GetValue<string>()));
            await Send(HttpMethod.Post, $"/mods/{modId}/changelogs", new JsonObject { ["version"] = version, ["changelog"] = changelog });
        }

        var description = File.ReadAllText(Path.Combine(folder, "pages", "nexus.bbcode"));
        await Send(HttpMethod.Patch, $"/mods/{modId}", new JsonObject { ["summary"] = Text("summary"), ["description"] = description });
        Console.WriteLine("Updated the Nexus page text");

        var status = await Status(modId);
        if (status != "published")
        {
            Console.WriteLine($"::warning::The Nexus page is {status}, so the files and text are up but players can't see them. Publish it on the website.");
        }
    }

    async Task<string> GlobalModId(string gameScopedId)
    {
        var mod = await Send(HttpMethod.Get, $"/games/{Repo.NexusGame}/mods/{gameScopedId}");
        return Id(mod["data"]!["id"]!);
    }

    async Task<string?> Status(string modId)
    {
        var batch = await Send(HttpMethod.Post, "/mods/batch", new JsonObject { ["mod_ids"] = new JsonArray(modId) });
        return batch["data"]?["mods"]?.AsArray().FirstOrDefault()?["status"]?.GetValue<string>();
    }

    record ModFile(string Id, List<JsonNode> Versions);

    // The files list carries no category, so the main file is the one whose versions say main.
    async Task<ModFile?> MainFile(string modId)
    {
        var files = await Send(HttpMethod.Get, $"/mods/{modId}/files");
        foreach (var file in files["data"]!["mod_files"]!.AsArray())
        {
            if (file!["is_active"]?.GetValue<bool>() == false)
            {
                continue;
            }

            var id = Id(file["id"]!);
            var versions = (await Send(HttpMethod.Get, $"/mod-files/{id}/versions"))["data"]!["versions"]!.AsArray().Select(entry => entry!).ToList();
            if (versions.Any(entry => entry["category"]?.GetValue<string>() == "main"))
            {
                return new ModFile(id, versions);
            }
        }

        return null;
    }

    async Task<string> UploadFile(string modId, string? mainFileId, string version)
    {
        var zip = Path.Combine(folder, Text("nexusZip"));
        var bytes = await File.ReadAllBytesAsync(zip);
        var name = Path.GetFileName(zip);
        var md5 = MD5.HashData(bytes);
        var upload = (await Send(HttpMethod.Post, "/uploads", new JsonObject
        {
            ["size_bytes"] = bytes.Length,
            ["filename"] = name,
            ["md5"] = Convert.ToHexStringLower(md5),
        }))["data"]!;
        var uploadId = Id(upload["id"]!);

        using (var put = new HttpRequestMessage(HttpMethod.Put, upload["presigned_url"]!.GetValue<string>()))
        {
            put.Content = new ByteArrayContent(bytes);
            put.Content.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse($"attachment; filename=\"{name}\"");
            put.Content.Headers.ContentMD5 = md5;
            using var stored = await new HttpClient().SendAsync(put);
            if (!stored.IsSuccessStatusCode)
            {
                throw new ReleaseException($"storing {name} failed with {(int)stored.StatusCode} {await stored.Content.ReadAsStringAsync()}");
            }
        }

        await Send(HttpMethod.Post, $"/uploads/{uploadId}/finalise");
        for (var attempt = 0; ; attempt++)
        {
            var state = (await Send(HttpMethod.Get, $"/uploads/{uploadId}"))["data"]!["state"]!.GetValue<string>();
            if (state == "available")
            {
                break;
            }

            if (attempt == 60)
            {
                throw new ReleaseException($"the upload stayed {state} for five minutes");
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        var fields = new JsonObject
        {
            ["upload_id"] = uploadId,
            ["name"] = Text("product"),
            ["version"] = version,
            ["file_category"] = "main",
            ["description"] = Text("nexusFileDescription"),
            ["update_mod_version"] = true,
            ["show_requirements_pop_up"] = true,
        };
        if (mainFileId != null)
        {
            fields["archive_existing_file"] = true;
            var added = await Send(HttpMethod.Post, $"/mod-files/{mainFileId}/versions", fields);
            Console.WriteLine($"Uploaded {name} as a new version of the main file");
            return Id(added["data"]!["version"]!["id"]!);
        }

        fields["mod_id"] = modId;
        var created = await Send(HttpMethod.Post, "/mod-files", fields);
        var fileId = Id(created["data"]!["id"]!);
        Console.WriteLine($"Uploaded {name} as the first main file");
        var versions = (await Send(HttpMethod.Get, $"/mod-files/{fileId}/versions"))["data"]!["versions"]!.AsArray();
        return Id(versions.First(entry => entry!["version"]?.GetValue<string>() == version)!["id"]!);
    }

    // Requirements are BepInEx and, for a mod, Core at its floor. A failure here only warns, since the file is already up.
    async Task SetRequirements(string versionId)
    {
        try
        {
            var definitions = new JsonArray();
            var bepInEx = await MainFile(await GlobalModId(Repo.NexusBepInExId));
            if (bepInEx != null)
            {
                definitions.Add(Range(bepInEx.Versions.MinBy(entry => entry["uploaded_at"]?.GetValue<string>())!));
            }

            var floor = _release["coreFloor"]?.GetValue<string>();
            if (floor != null)
            {
                var core = await MainFile(await GlobalModId(Text("coreNexusModId")));
                var atFloor = core?.Versions.FirstOrDefault(entry => entry["version"]?.GetValue<string>() == floor)
                    ?? throw new ReleaseException($"Core's main file on Nexus has no version {floor}. Release Core first.");
                definitions.Add(Range(atFloor));
            }

            await Send(HttpMethod.Put, $"/mod-file-versions/{versionId}/dependencies/ranges", new JsonObject { ["dependency_definitions"] = definitions });
            Console.WriteLine("Set the Nexus requirements");
        }
        catch (ReleaseException e)
        {
            Console.WriteLine($"::warning::Nexus requirements weren't set, so set them on the website. {e.Message}");
        }
    }

    static JsonObject Range(JsonNode version) => new()
    {
        ["ranges"] = new JsonArray(new JsonObject { ["min_version_id"] = Id(version["id"]!), ["max_version_id"] = null }),
    };

    static string Id(JsonNode node) => node.GetValueKind() == JsonValueKind.Number ? node.GetValue<long>().ToString() : node.GetValue<string>();

    async Task<JsonNode> Send(HttpMethod method, string path, JsonObject? body = null)
    {
        using var request = new HttpRequestMessage(method, _api + path);
        if (body != null)
        {
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new ReleaseException($"Nexus {method} {path} failed with {(int)response.StatusCode} {text}");
        }

        return string.IsNullOrWhiteSpace(text) ? new JsonObject() : JsonNode.Parse(text)!;
    }
}
