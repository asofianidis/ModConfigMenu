# SpiritVale Mod Configuration Menu

A BepInEx 6 IL2CPP plugin that adds a native **MODS** tab to SpiritVale's ESC settings menu. Other plugins register typed settings through the public API; players edit them in game and changes persist automatically.

`DemoMenu.cs` remains in the source tree as an integration example, but production builds do not register or display the demo page.

## Build

```powershell
dotnet build -c Release
```

The output is `bin/Release/net6.0/SpiritVale.ModConfigMenu.dll`. The project defaults to `F:\SteamLibrary\steamapps\common\SpiritVale`; override that path when needed:

```powershell
dotnet build -c Release -p:GameDir="D:\Games\SpiritVale"
```

## Install and use

1. Install the IL2CPP build of BepInEx 6 and launch SpiritVale once.
2. Copy `SpiritVale.ModConfigMenu.dll` to `<game>\BepInEx\plugins\ModConfigMenu\`.
3. Launch the game, enter the world, then open **ESC > Settings > MODS** and select an installed mod.

For local updates, use `Install-ModConfigMenu.ps1`. Before replacing the installed DLL, it archives the previous version under `<game>\BepInEx\mod-backups\ModConfigMenu\` with its version and timestamp. Backups use the `.dll.bak` suffix and live outside the plugins directory so BepInEx cannot load them as duplicate plugins.

API-owned values are stored in BepInEx's `alex.spiritvale.modconfigmenu.cfg`. Players never need to edit that file. Mods may instead bind their own existing `ConfigEntry<T>` values, as shown below.

## Public API for mod authors

See [INTEGRATION_GUIDE.md](INTEGRATION_GUIDE.md) for the complete agent and developer integration workflow, API reference, hotkey migration guidance, and release checklist.

Reference `SpiritVale.ModConfigMenu.dll`, add a hard BepInEx dependency so registration happens after MCM loads, then register a page during `Load()`:

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Configuration;
using SpiritVale.ModConfig;
using UnityEngine;

[BepInPlugin("example.my-mod", "My Mod", "1.2.0")]
[BepInDependency(ModConfigPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class MyPlugin : BasePlugin
{
    public override void Load()
    {
        ModPage page = ModConfigMenu.Register(
            "example.my-mod",
            "My Mod",
            "Settings for My Mod.",
            "1.2.0");

        SettingHandle<bool> enabled = page.AddToggle(
            "Enabled", "Enable My Mod", true,
            "Apply this mod while playing.",
            value => Log.LogInfo($"Enabled: {value}"));

        page.AddIntegerSlider("MaxRows", "Maximum rows", 8, 1, 20);
        page.AddFloatSlider("Scale", "UI scale", 1f, 0.75f, 1.5f, 0.05f, "0.00x");
        page.AddChoice("Style", "Style", "Compact", new[] { "Compact", "Detailed" });
        page.AddText("Label", "Custom label", "Party damage");
        page.AddKeybind("ToggleKey", "Toggle key", KeyCode.F8);
        SettingHandle<KeyboardShortcut> overlayHotkey = page.AddHotkey(
            "OverlayHotkey", "Overlay hotkey",
            new KeyboardShortcut(KeyCode.O, new[] { KeyCode.LeftControl, KeyCode.LeftShift }));
        page.AddButton("Clear", "Clear saved data", ClearSavedData);
    }

    private void ClearSavedData() { }
}
```

If a mod already uses BepInEx configuration, bind the existing entry so MCM and the mod share one value:

```csharp
var showOverlay = Config.Bind("Display", "ShowOverlay", true, "Show the overlay.");
page.BindToggle(showOverlay, "Show overlay");
```

Equivalent `BindIntegerSlider`, `BindFloatSlider`, `BindChoice`, `BindText`, `BindKeybind`, and `BindHotkey` overloads are available. Each returned handle exposes `Value`, `DefaultValue`, `Reset()`, and a typed `Changed` event. `ModConfigMenu.Version` reports the public API version.

`AddHotkey` and `BindHotkey` use BepInEx's native `KeyboardShortcut`. Players click the value, hold the desired keys, press the main key last, and release the combination to save it. Mod code can test the returned value with `hotkey.Value.IsDown()`, `IsPressed()`, or `IsUp()`. Escape cancels capture without changing the saved shortcut.

Use a stable, globally unique page id and stable setting keys. `Register` is idempotent for a page id; duplicate setting keys on the same page are rejected with a clear exception.

## Architecture

- A Harmony prefix registers the tab and its empty screen before SpiritVale wires its native settings tabs.
- Custom controls are built only after `UITabMenu.Start` completes, outside the native initialization path.
- The new screen remains under `UISettings`, so the game's normal popup, close, and back behavior is retained.
- UI fonts and sliced panel art are sampled from the active settings popup.
- Layout measurements and typography derive from the live native settings canvas and its real scale factor instead of assuming a fixed resolution.
- The mod list reuses the native navigation panel and selected-pill styles. Dropdowns, action buttons, toggles, sliders, text colors, and interactive states reuse their matching in-game templates.
- Semantic tints follow the native escape-menu palette so Unity hover and selected-state colors cannot turn fields or slider tracks black.
- Each mod receives one discoverable page in a shared navigation list.
- One failed UI injection is logged without replacing SpiritVale's stored settings.
- Every plugin-authored log entry begins with `[ModConfigMenu]` for easy filtering.
