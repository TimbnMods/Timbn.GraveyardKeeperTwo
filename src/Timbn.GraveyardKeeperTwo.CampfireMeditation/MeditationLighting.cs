using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

/// <summary>Lights the world like midnight with the sun, moon and ambient light turned off while meditating.</summary>
internal static class MeditationLighting
{
    private static readonly AccessTools.FieldRef<EnvironmentEngine, LightEnvironmentPreset?> _presetOverride =
        AccessTools.FieldRefAccess<EnvironmentEngine, LightEnvironmentPreset?>("presetOverride");

    private static LightEnvironmentPreset? _midnight;
    private static LightEnvironmentPreset? _previousOverride;
    private static float _previousIntensity;

    public static void Begin()
    {
        End();
        var engine = EnvironmentEngine.Instance;
        if (engine == null)
            return;

        var midnight = FindMidnight(Traverse.Create(engine).Field<TimeOfDayPresets>("timesOfDay").Value);
        if (midnight == null)
        {
            Plugin.Logger.LogWarning("No midnight lighting preset found, so the lighting stays as it is.");
            return;
        }

        _midnight = midnight;
        _previousOverride = _presetOverride(engine);
        _previousIntensity = engine.presetOverrideIntensity;
        engine.ApplyOverridePreset(midnight, 1f);
        Plugin.Logger.LogInfo($"Lighting like midnight ({midnight.name}) with the sun, moon and ambient light off.");
    }

    public static void End()
    {
        if (_midnight == null)
            return;

        var midnight = _midnight;
        var previousOverride = _previousOverride;
        _midnight = null;
        _previousOverride = null;
        var engine = EnvironmentEngine.Instance;
        if (engine == null)
            return;

        if (_presetOverride(engine) == midnight)
            engine.ApplyOverridePreset(previousOverride, _previousIntensity);
        else
            Plugin.Logger.LogInfo("The game changed the lighting while meditating, so it stays as the game set it.");
    }

    public static void Darken()
    {
        if (_midnight == null)
            return;

        var engine = EnvironmentEngine.Instance;
        if (engine == null || _presetOverride(engine) != _midnight)
            return;

        var lights = LightsSystem.Instance;
        if (lights != null && lights.SunLight != null)
            lights.SunLight.intensity = 0f;

        var shaderParameters = LazySingleton<GlobalShaderParameters>.Instance;
        if (shaderParameters != null)
        {
            shaderParameters.sunLight = 0f;
            shaderParameters.ApplyShaderParameters();
        }

        RenderSettings.ambientLight = Color.black;
    }

    private static LightEnvironmentPreset? FindMidnight(TimeOfDayPresets? timesOfDay)
    {
        if (timesOfDay == null || timesOfDay.presets == null)
            return null;

        LightEnvironmentPreset? best = null;
        var bestDistance = float.MaxValue;
        foreach (var timeAndPreset in timesOfDay.presets)
        {
            if (timeAndPreset.preset == null)
                continue;

            var distance = Mathf.Min(timeAndPreset.time, 1f - timeAndPreset.time);
            if (distance >= bestDistance)
                continue;

            best = timeAndPreset.preset;
            bestDistance = distance;
        }

        return best;
    }
}
