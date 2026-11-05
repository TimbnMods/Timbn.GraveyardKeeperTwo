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
