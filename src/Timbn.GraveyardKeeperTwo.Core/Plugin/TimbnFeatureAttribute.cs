namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Names the feature a Harmony patch class belongs to, so a game update that breaks one feature's patches turns
/// off that feature alone and the rest of the plugin keeps running. The name is the key of the config entry that
/// switches the feature, and while the feature is broken the plugin's Settings.Toggle and Settings.While read
/// that entry as off, the main menu popup names the feature, and <see cref="TimbnFrameworkPlugin.IsFeatureBroken"/>
/// says so. A patch class with no feature belongs to the plugin itself, and a failure there still turns off the
/// whole plugin.
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
