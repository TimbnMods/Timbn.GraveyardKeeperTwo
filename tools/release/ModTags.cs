// A mod is new until it has a release tag. Tags come from the remote, since a local clone may not have fetched them.
static class ModTags
{
    public static List<System.Version> Read(string repo, ModInfo mod)
    {
        var prefix = $"refs/tags/{mod.ModName.ToLowerInvariant()}/v";
        var versions = new List<System.Version>();
        foreach (var line in Repo.Run("git", repo, "ls-remote", "--tags", "origin").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var reference = line.Split('\t').Last();
            if (reference.StartsWith(prefix) && !reference.EndsWith("^{}") && System.Version.TryParse(reference[prefix.Length..], out var version))
            {
                versions.Add(version);
            }
        }

        return versions;
    }
}
