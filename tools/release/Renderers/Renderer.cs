record Requirement(string Name, string Url, string? Note);

abstract class Renderer
{
    static readonly Regex _inline = new(@"\*\*(?<bold>.+?)\*\*|`(?<key>[^`]+)`|\[(?<text>[^\]]+)\]\((?<url>[^)\s]+)\)");

    protected readonly StringBuilder Output = new();

    readonly Site _site;
    readonly Func<string, string> _imageUrl;

    protected Renderer(Site site, Func<string, string> imageUrl)
    {
        _site = site;
        _imageUrl = imageUrl;
    }

    public Site Site => _site;

    protected string ImageUrl(string path) => _imageUrl(path);

    public string Render(string title, Page page, List<Requirement> requirements)
    {
        Start(title, page.Banner == null ? null : ImageUrl(page.Banner), Inline(page.Summary));
        var sections = page.Sections.Where(section => section.Filter.Shows(_site)).ToList();
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

            foreach (var block in section.Blocks.Where(block => block.Filter.Shows(_site)))
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
                        List(list.Ordered, [.. list.Items.Select(item => new ListItem(Inline(item.Text), item.Image == null ? null : ImageUrl(item.Image)))]);
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

    ListItem RequirementText(Requirement requirement) =>
        new ListItem(Link(requirement.Name, requirement.Url) + (requirement.Note == null ? "" : requirement.Note.StartsWith(',') ? requirement.Note : " " + requirement.Note), null);

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
    protected abstract void List(bool ordered, List<ListItem> items);
    protected abstract void Image(string url);
    protected abstract void Spoiler(string title, string text);
    protected abstract string Bold(string text);
    protected abstract string Key(string text);
    protected abstract string Link(string text, string url);
}
