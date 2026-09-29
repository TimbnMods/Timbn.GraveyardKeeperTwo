using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

/// <summary>Lights the world like midnight with the sun, moon and ambient light turned off while meditating.</summary>
internal static class MeditationLighting
{
    private static bool _active;
    private static LightEnvironmentPreset? _previousOverride;
    private static float _previousIntensity;

    public static void Begin()
    {
        End();
        var engine = EnvironmentEngine.Instance;
        if (engine == null)
            return;

        var fields = Traverse.Create(engine);
        var midnight = FindMidnight(fields.Field<TimeOfDayPresets>("timesOfDay").Value);
        if (midnight == null)
        {
            Plugin.Logger.LogWarning("No midnight lighting preset found, so the lighting stays as it is.");
            return;
        }

        _active = true;
        _previousOverride = fields.Field<LightEnvironmentPreset>("presetOverride").Value;
        _previousIntensity = engine.presetOverrideIntensity;
        engine.ApplyOverridePreset(midnight, 1f);
        Plugin.Logger.LogInfo($"Lighting like midnight ({midnight.name}) with the sun, moon and ambient light off.");
    }

    public static void End()
    {
        if (!_active)
            return;

        _active = false;
        var engine = EnvironmentEngine.Instance;
        if (engine != null)
            engine.ApplyOverridePreset(_previousOverride, _previousIntensity);

        _previousOverride = null;
    }

    public static void Darken()
    {
        if (!_active)
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
