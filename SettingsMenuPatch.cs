using HarmonyLib;

namespace SpiritVale.ModConfig;

[HarmonyPatch(typeof(UITabMenu), "Start")]
internal static class SettingsTabInjectionPatch
{
    private static void Prefix(UITabMenu __instance)
    {
        try
        {
            SettingsMenuController.TryInject(__instance);
        }
        catch (Exception ex)
        {
            ModConfigPlugin.Error($"Could not add the MODS tab: {ex}");
        }
    }

    private static void Postfix(UITabMenu __instance)
    {
        try
        {
            SettingsMenuController.TryCompleteInjection(__instance);
        }
        catch (Exception ex)
        {
            ModConfigPlugin.Error($"Could not build the MODS screen: {ex}");
        }
    }
}

[HarmonyPatch(typeof(UIManager), "LateUpdate")]
internal static class SettingsKeyCapturePatch
{
    private static bool Prefix()
    {
        try
        {
            // Skip the game's escape handling for the one frame in which a keybind is captured.
            return !SettingsMenuController.TickKeyCapture();
        }
        catch (Exception ex)
        {
            ModConfigPlugin.Warning($"Key capture failed: {ex.Message}");
            SettingsMenuController.CancelKeyCapture();
            return true;
        }
    }
}
