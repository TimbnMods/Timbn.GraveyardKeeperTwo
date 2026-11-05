class NexusSite : IReleaseSite
{
    public Site Site => Site.Nexus;
    public string PageFile => "nexus.bbcode";

    public bool IsEnabled(ModInfo mod) => mod.ToNexus;

    public Renderer CreateRenderer(Func<string, string> imageUrl) => new NexusRenderer(imageUrl);

    public List<Requirement> Requirements(ModInfo mod)
    {
        var requirements = new List<Requirement>();
        if (mod.HasCoreFloor)
        {
            requirements.Add(new(mod.Core!.Product, mod.Core.NexusUrl, $"{mod.CoreFloor} or newer"));
        }

        return requirements;
    }

    public string Package(ReleaseContext context, string pluginDir, string pageText, List<string> changes)
    {
        if (string.IsNullOrEmpty(pluginDir))
        {
            return string.Empty;
        }

        var zip = Path.Combine(context.OutDir, "nexus", $"{context.Mod.ProjectName}-{context.Mod.Version}.zip");
        context.Zip(zip, [.. ReleaseContext.Files(pluginDir, context.Mod.ProjectName)]);
        return zip;
    }

    public Task Upload(string folder) => new NexusClient(folder).Run();
}
