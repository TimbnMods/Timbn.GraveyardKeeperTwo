namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One id on the mod event bus, as <see cref="TimbnPluginModEvents.Known"/> lists it. It exists once any mod has
/// published or listened to it. The list is a copy taken when it was asked for.
/// </summary>
public sealed class TimbnModEvent
{
    internal TimbnModEvent(string id, IReadOnlyList<string> publishingMods)
    {
        Id = id;
        PublishingMods = publishingMods;
    }

    /// <summary>The id mods publish and listen with, such as "moved".</summary>
    public string Id { get; }

    /// <summary>Lists the guids of the mods that publish this id right now. A mod outside Timbn shows under the name Core knows it by. It is empty when a mod only listens and nobody has published yet.</summary>
    public IReadOnlyList<string> PublishingMods { get; }
}
