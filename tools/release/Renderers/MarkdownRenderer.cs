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
                Output.Append('\n').Append(' ', marker.Length).Append($"![{Alt(items[i].Image!)}]({items[i].Image})\n\n");
            }
        }

        Output.Append('\n');
    }

    protected override void Image(string url) => Output.Append($"![{Alt(url)}]({url})\n\n");

    static string Alt(string url) => Path.GetFileNameWithoutExtension(url).Replace('-', ' ').Replace('_', ' ');

    protected override void Spoiler(string title, string text) => Output.Append($"<details><summary>{title}</summary>\n\n{text}\n\n</details>\n\n");

    protected override string Bold(string text) => $"**{text}**";

    protected override string Key(string text) => text.Contains('`') ? $"`` {text} ``" : $"`{text}`";

    protected override string Link(string text, string url) => $"[{text}]({url})";

    protected override string Plain(string text) => Regex.Replace(text, @"([\\*<>])", @"\$1");
}
