using BepInEx.Unity.IL2CPP.Configuration;
using UnityEngine;

namespace SpiritVale.ModConfig;

internal static class DemoMenu
{
    internal static void Register()
    {
        ModPage page = ModConfigMenu.Register(
            "alex.spiritvale.modconfigmenu.demo",
            "MCM Demo",
            "A safe playground for testing every built-in setting control.",
            ModConfigPlugin.PluginVersion);

        page.AddHeader("GENERAL", "Changes save immediately and remain after restarting the game.")
            .AddToggle("Sparkles", "Enable sparkles", true,
                "Dummy toggle used to verify persisted on/off values.");

        page.AddIntegerSlider("ParticleCount", "Particle count", 24, 0, 100, 1,
            "Dummy whole-number slider.");
        page.AddFloatSlider("InterfaceScale", "Interface scale", 1f, 0.75f, 1.5f, 0.05f, "0.00x",
            "Dummy decimal slider with a formatted value.");
        page.AddChoice("Theme", "Effect theme", "Ember",
            new[] { "Ember", "Moonlight", "Verdant" }, "Dummy previous/next choice control.");
        page.AddText("Greeting", "Greeting", "Welcome, adventurer!",
            "Dummy text value. Press Enter or click elsewhere to commit it.");
        page.AddKeybind("CelebrationKey", "Celebration key", KeyCode.F7,
            "Click the value, then press a key. Escape cancels capture.");
        page.AddHotkey("PhotoModeHotkey", "Photo mode hotkey",
            new KeyboardShortcut(KeyCode.P, new[] { KeyCode.LeftControl, KeyCode.LeftShift }),
            "Click the value, press and release a key combination. Press the main key last.");
        page.AddButton("ResetDemo", "Reset demo settings", page.ResetAll,
            "Restore every demo value to its default.");
    }
}
