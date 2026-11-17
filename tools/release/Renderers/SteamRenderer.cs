class SteamRenderer : Renderer
{
    public SteamRenderer(Func<string, string> imageUrl)
        : base(Site.Steam, imageUrl)
    {
    }

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

    protected override void List(bool ordered, List<ListItem> items)
    {
        Output.Append(ordered ? "[olist]\n" : "[list]\n");
        foreach (var item in items)
        {
            Output.Append("[*]").Append(item.Text).Append('\n');
            if (item.Image != null)
            {
                Output.Append($"[img]{item.Image}[/img]\n");
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
