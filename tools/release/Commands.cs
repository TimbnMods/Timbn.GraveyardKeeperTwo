static class Commands
{
    // Every project at 1.0.0 or above. A 0.x project is unreleased. Notes go to stderr, since stdout is the JSON the workflows read.
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
            if (!mod.IsReleasable)
            {
                Console.Error.WriteLine($"skipped {mod.ProjectName} {mod.Version}, which is below 1.0.0");
                continue;
            }

            if (Changelog.TryRead(Path.Combine(mod.ProjectDir, "CHANGELOG.md"), mod.Version) == null)
            {
                Console.Error.WriteLine($"warning: {mod.ProjectName} {mod.Version} has no CHANGELOG.md section, so it is not listed");
                continue;
            }

            var assets = new List<string> { mod.AssemblyName + ".dll" };
            if (mod.GenerateDocumentationFile)
            {
                assets.Add(mod.AssemblyName + ".xml");
            }

            releases.Add(new Dictionary<string, object>
            {
                ["project"] = mod.ProjectName,
                ["mod"] = mod.ModName,
                ["product"] = mod.Product,
                ["version"] = mod.Version,
                ["tag"] = mod.Tag,
                ["title"] = $"{mod.Product} {mod.Version}",
                ["core"] = mod.IsCore,
                ["nuget"] = mod.IsPackable,
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
