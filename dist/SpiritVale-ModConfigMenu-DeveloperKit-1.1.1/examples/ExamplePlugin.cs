using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Configuration;
using SpiritVale.ModConfig;
using UnityEngine;

namespace Example.SpiritVale.McmIntegration;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ModConfigPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class ExamplePlugin : BasePlugin
{
    public const string PluginGuid = "example.author.my-mod";
    public const string PluginName = "My Mod";
    public const string PluginVersion = "1.0.0";

    private ConfigEntry<bool> _enabled;
    private ConfigEntry<float> _scale;
    private ConfigEntry<string> _style;
    private ConfigEntry<KeyboardShortcut> _toggleHotkey;

    public override void Load()
    {
        _enabled = Config.Bind("General", "Enabled", true, "Enable My Mod.");
        _scale = Config.Bind("Display", "Scale", 1f, "Overlay scale.");
        _style = Config.Bind("Display", "Style", "Compact", "Display style.");
        _toggleHotkey = Config.Bind(
            "Hotkeys",
            "Toggle",
            new KeyboardShortcut(KeyCode.F8),
            "Show or hide the overlay.");

        RegisterMcmPage();
    }

    public override bool Unload()
    {
        ModConfigMenu.Unregister(PluginGuid);
        return base.Unload();
    }

    private void RegisterMcmPage()
    {
        if (ModConfigMenu.Version < 2)
            throw new InvalidOperationException("My Mod requires MCM API version 2 or newer.");

        ModPage page = ModConfigMenu.Register(
            PluginGuid,
            PluginName,
            "Configure My Mod without editing files.",
            PluginVersion);

        page.AddHeader("GENERAL", "Changes save immediately.");
        page.BindToggle(_enabled, "Enable My Mod");

        page.AddHeader("DISPLAY");
        page.BindFloatSlider(
            _scale,
            "Overlay scale",
            0.5f,
            1.5f,
            0.05f,
            "0.00x");
        page.BindChoice(
            _style,
            "Display style",
            new[] { "Compact", "Detailed", "Minimal" });

        page.AddHeader("HOTKEYS", "Press the main key last, then release the shortcut.");
        page.BindHotkey(_toggleHotkey, "Toggle overlay");
    }
}
