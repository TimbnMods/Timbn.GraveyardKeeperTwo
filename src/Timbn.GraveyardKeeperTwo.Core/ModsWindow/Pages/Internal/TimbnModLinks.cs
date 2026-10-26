namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Checks the links a mod registers, so a button can only open the store page it is named for.</summary>
internal static class TimbnModLinks
{
    internal const string Nexus = "Nexus";
    internal const string SteamWorkshop = "Steam Workshop";
    internal const string Thunderstore = "Thunderstore";
    internal const string Website = "Website";

    private static readonly string[] _nexusHosts = ["nexusmods.com"];
    private static readonly string[] _steamHosts = ["steamcommunity.com"];
    private static readonly string[] _thunderstoreHosts = ["thunderstore.io"];

    /// <summary>The buttons for a mod, in the order they show, each with its label and checked address.</summary>
    internal static IReadOnlyList<(string Label, string Url)> For(TimbnModInfo info, Action<string>? rejected = null)
    {
        var links = new List<(string Label, string Url)>();
        Add(links, Nexus, info.NexusLink, _nexusHosts, rejected);
        Add(links, SteamWorkshop, info.SteamWorkshopLink, _steamHosts, rejected);
        Add(links, Thunderstore, info.ThunderstoreLink, _thunderstoreHosts, rejected);
        Add(links, Website, info.WebsiteLink, null, rejected);
        return links;
    }

    /// <summary>The address as a normal https link when it is one and its host is allowed (the host or a subdomain of it), otherwise null.</summary>
    internal static string? Check(string? link, string[]? hosts)
    {
        if (string.IsNullOrWhiteSpace(link) || !Uri.TryCreate(link.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return null;

        if (hosts != null && !hosts.Any(host => uri.Host == host || uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase)))
            return null;

        return uri.AbsoluteUri;
    }

    private static void Add(List<(string Label, string Url)> links, string label, string? link, string[]? hosts, Action<string>? rejected)
    {
        if (string.IsNullOrWhiteSpace(link))
            return;

        if (Check(link, hosts) is { } url)
            links.Add((label, url));
        else
            rejected?.Invoke($"The {label} link '{link}' is not an https link on {(hosts == null ? "a website" : hosts[0])} and was ignored.");
    }
}
