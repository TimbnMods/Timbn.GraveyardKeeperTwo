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
        // A spoiler is a mapping already, so it can carry its own only and skip.
        "spoiler" => new SpoilerBlock(
            filter == SiteFilter.All ? ParseFilter((YamlMappingNode)value) : filter,
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
