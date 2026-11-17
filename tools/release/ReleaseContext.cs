// What every release site shares while it packages a mod. A site reads the mod and page from here and writes under OutDir.
record ZipEntry(string Path, byte[] Bytes);

class ReleaseContext
{
    readonly string _repo;
    readonly ModInfo _mod;
    readonly Page _page;
    readonly Options _options;

    public ReleaseContext(string repo, ModInfo mod, Page page, Options options)
    {
        _repo = repo;
        _mod = mod;
        _page = page;
        _options = options;
        OutDir = Path.GetFullPath(Path.Combine(options.OutDir ?? Path.Combine(repo, "artifacts", "release"), mod.ProjectName));
    }

    public ModInfo Mod => _mod;
    public Page Page => _page;
    public Options Options => _options;
    public List<string> Warnings { get; } = [];
    public string OutDir { get; }

    public string StagePlugin(string dll)
    {
        var stage = Path.Combine(OutDir, "plugin");
        Directory.CreateDirectory(stage);
        File.Copy(dll, Path.Combine(stage, Path.GetFileName(dll)));
        var xml = Path.ChangeExtension(dll, ".xml");
        if (_mod.GenerateDocumentationFile && File.Exists(xml))
        {
            File.Copy(xml, Path.Combine(stage, Path.GetFileName(xml)));
        }

        foreach (var file in _mod.GatherContent())
        {
            var target = Path.Combine(stage, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file.Source, target);
        }

        var license = Path.Combine(_repo, "LICENSE");
        if (File.Exists(license))
        {
            File.Copy(license, Path.Combine(stage, "LICENSE.txt"));
        }

        return stage;
    }

    public string RequireIcon(string site)
    {
        var icon = Path.Combine(_mod.DocsDir, "icon.png");
        return File.Exists(icon) ? icon : throw new ReleaseException($"{site} needs a 256x256 icon at {icon}");
    }

    // Entries are written by hand so every path uses forward slashes, which some mod managers need.
    public void Zip(string path, List<ZipEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach (var entry in entries)
            {
                using var stream = archive.CreateEntry(entry.Path, CompressionLevel.Optimal).Open();
                stream.Write(entry.Bytes);
            }
        }

        if (_options.DryRun)
        {
            ZipFile.ExtractToDirectory(path, path + ".contents");
        }
    }

    public static IEnumerable<ZipEntry> Files(string dir, string root) =>
        Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(file => new ZipEntry($"{root}/{Path.GetRelativePath(dir, file).Replace('\\', '/')}", File.ReadAllBytes(file)));

    public static void CopyDirectory(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }

    public static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }
}
