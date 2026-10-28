namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Names the feature a Harmony patch class belongs to. When a game update breaks one feature's patches, only that
/// feature turns off and the rest of the mod keeps running. The name is the key of the config entry that switches
/// the feature. While the feature is broken, Settings.Toggle and Settings.While read that entry as off. The main menu
/// popup names the feature and <see cref="TimbnFrameworkPlugin.IsFeatureBroken"/> reports it. A patch class with no
/// feature belongs to the mod itself. A failure there still turns off the whole mod.
/// </summary>
/// <example>
/// <code>
/// [TimbnFeature(nameof(PluginConfig.StuckCarriers))]
/// [HarmonyPatch(typeof(ZombieWgoData))]
/// internal static class ZombieWgoDataStuckCarriersPatch
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TimbnFeatureAttribute : Attribute
{
    /// <summary>Marks a patch class as part of a feature.</summary>
    /// <param name="name">The key of the config entry that switches the feature on and off.</param>
    public TimbnFeatureAttribute(string name)
    {
        Name = name;
    }

    /// <summary>The key of the config entry that switches the feature.</summary>
    public string Name { get; }
}
