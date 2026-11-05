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
