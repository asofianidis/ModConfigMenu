using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SpiritVale.ModConfig;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class ModConfigPlugin : BasePlugin
{
    public const string PluginGuid = "alex.spiritvale.modconfigmenu";
    public const string PluginName = "Mod Configuration Menu";
    public const string PluginVersion = "1.1.1";
    public const int ApiVersion = 2;
    internal const string LogPrefix = "[ModConfigMenu]";

    internal static ManualLogSource Logger { get; private set; }
    private Harmony _harmony;

    public override void Load()
    {
        Logger = Log;
        ModConfigMenu.Initialize(Config);

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());
        Info($"{PluginVersion} loaded; public API version {ApiVersion}.");
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        SettingsMenuController.Shutdown();
        ModConfigMenu.Shutdown();
        return base.Unload();
    }

    internal static void Info(string message) => Logger.LogInfo($"{LogPrefix} {message}");
    internal static void Warning(string message) => Logger.LogWarning($"{LogPrefix} {message}");
    internal static void Error(string message) => Logger.LogError($"{LogPrefix} {message}");
}
