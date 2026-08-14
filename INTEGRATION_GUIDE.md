# Mod Configuration Menu Integration Guide

This guide is for developers and coding agents integrating a SpiritVale BepInEx 6 IL2CPP mod with Mod Configuration Menu (MCM).

MCM adds a centralized **MODS** section under **ESC > Settings**. A mod registers one page and adds typed controls to it. Values are persisted automatically, and mods that already use BepInEx configuration can bind their existing entries directly.

## Requirements

- SpiritVale with the IL2CPP build of BepInEx 6
- Mod Configuration Menu 1.1.0 or newer
- A .NET 6 BepInEx IL2CPP mod project
- A compile-time reference to `SpiritVale.ModConfigMenu.dll`

MCM 1.1.0 exposes public API version 2. API version 2 adds multi-key hotkeys through BepInEx `KeyboardShortcut`.

## 1. Add the assembly reference

Reference the installed MCM DLL from the mod's project file. Keep `Private` set to `false` so the MCM assembly is not copied into the mod's output or distributed as a duplicate plugin.

```xml
<PropertyGroup>
  <GameDir Condition="'$(GameDir)' == ''">F:\SteamLibrary\steamapps\common\SpiritVale</GameDir>
</PropertyGroup>

<ItemGroup>
  <Reference Include="SpiritVale.ModConfigMenu">
    <HintPath>$(GameDir)\BepInEx\plugins\ModConfigMenu\SpiritVale.ModConfigMenu.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

Add the namespaces needed by the settings API:

```csharp
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using SpiritVale.ModConfig;
using UnityEngine;
```

## 2. Declare the dependency

Add a hard BepInEx dependency so MCM loads before the integrating mod:

```csharp
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ModConfigPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BasePlugin
{
    // ...
}
```

Do not bundle `SpiritVale.ModConfigMenu.dll` with the integrating mod. List MCM 1.1.0 or newer as a separate installation requirement.

## 3. Register one page during `Load()`

Use the mod's stable plugin GUID as the page ID. Register once, after creating the mod's configuration entries.

```csharp
ModPage page = ModConfigMenu.Register(
    PluginGuid,
    PluginName,
    "Configure My Mod without editing files.",
    PluginVersion);
```

`Register` is idempotent for a page ID and returns the existing page if that ID is already present. Setting keys on a page are case-insensitive and must be unique, so do not populate the same page more than once.

For API-dependent features, the public API level can also be checked:

```csharp
if (ModConfigMenu.Version < 2)
    throw new InvalidOperationException("My Mod requires MCM API version 2 or newer.");
```

## 4. Prefer binding existing BepInEx configuration

If the mod already has `ConfigEntry<T>` values, use the `Bind...` methods. MCM and the mod will then read and write the same entry in the mod's own configuration file.

```csharp
ConfigEntry<bool> enabled = Config.Bind(
    "General", "Enabled", true, "Enable My Mod.");

ConfigEntry<float> scale = Config.Bind(
    "Display", "Scale", 1f, "Scale the overlay.");

page.BindToggle(enabled, "Enable My Mod");
page.BindFloatSlider(
    scale,
    "Overlay scale",
    0.5f,
    1.5f,
    0.05f,
    "0.00x",
    "Resize the overlay.",
    value => ApplyScale(value));
```

Benefits of binding existing entries:

- Existing player values are retained.
- MCM and the mod cannot drift out of sync.
- Changes from either source raise the same configuration events.
- Removing MCM does not erase the mod's configuration.

Use `Add...` methods when the mod does not already own a BepInEx entry. MCM stores those values in `alex.spiritvale.modconfigmenu.cfg`, under a section derived from the page ID.

## 5. Available controls

| Control | MCM-owned method | Existing-entry method | Value type |
| --- | --- | --- | --- |
| Section heading | `AddHeader` | Not applicable | None |
| Toggle | `AddToggle` | `BindToggle` | `bool` |
| Integer slider | `AddIntegerSlider` | `BindIntegerSlider` | `int` |
| Decimal slider | `AddFloatSlider` | `BindFloatSlider` | `float` |
| Previous/next choice | `AddChoice` | `BindChoice` | `string` |
| Text field | `AddText` | `BindText` | `string` |
| Single key | `AddKeybind` | `BindKeybind` | `KeyCode` |
| Multi-key hotkey | `AddHotkey` | `BindHotkey` | `KeyboardShortcut` |
| Immediate action | `AddButton` | Not applicable | None |

### Headers

Use short headers to group related controls. Headers do not persist values.

```csharp
page.AddHeader("GENERAL", "Changes save immediately.");
```

### Toggles

```csharp
SettingHandle<bool> enabled = page.AddToggle(
    "Enabled",
    "Enable effects",
    true,
    "Apply the mod's effects while playing.",
    value => RefreshEffects(value));
```

### Integer sliders

The maximum must be greater than the minimum. Integer steps are clamped to at least 1.

```csharp
IntegerSliderSetting rows = page.AddIntegerSlider(
    "MaxRows", "Maximum rows", 8, 1, 20, 1,
    "Maximum number of rows shown.");
```

### Float sliders

`valueFormat` uses standard .NET numeric formatting. Formats such as `0`, `0.0`, `0.00x`, and `0s` work well for settings pages.

```csharp
FloatSliderSetting opacity = page.AddFloatSlider(
    "Opacity", "Opacity", 0.9f, 0.1f, 1f, 0.05f, "0%",
    "Opacity of the overlay.");
```

### Choices

Choices require at least one non-empty option. If a saved value is no longer present, MCM selects the first available option.

```csharp
ChoiceSetting style = page.AddChoice(
    "Style", "Display style", "Compact",
    new[] { "Compact", "Detailed", "Minimal" });
```

### Text fields

Text is committed when the player presses Enter or clicks elsewhere.

```csharp
SettingHandle<string> label = page.AddText(
    "Label", "Custom label", "Party damage",
    "Label displayed above the overlay.");
```

### Single-key bindings

Keep `KeyCode` bindings only when the setting must remain a single key.

```csharp
SettingHandle<KeyCode> key = page.AddKeybind(
    "OpenKey", "Open menu", KeyCode.F8,
    "Press the key used to open the menu.");
```

### Multi-key hotkeys

Prefer `KeyboardShortcut` for user-triggered actions, even when the default is a single key. It supports both `F8` and combinations such as `Left Ctrl + Left Shift + K`.

```csharp
ConfigEntry<KeyboardShortcut> toggleHotkey = Config.Bind(
    "Hotkeys",
    "Toggle",
    new KeyboardShortcut(KeyCode.K, new[]
    {
        KeyCode.LeftControl,
        KeyCode.LeftShift
    }),
    "Show or hide the overlay.");

SettingHandle<KeyboardShortcut> hotkey = page.BindHotkey(
    toggleHotkey,
    "Toggle overlay",
    "Show or hide the overlay.");
```

Check the shortcut from the mod's normal, world-safe update path:

```csharp
if (toggleHotkey.Value.IsDown())
    ToggleOverlay();
```

Available checks are:

- `IsDown()`: the main key was pressed this frame while the exact modifiers were held.
- `IsPressed()`: the complete shortcut is currently held.
- `IsUp()`: the main key was released this frame while the modifiers were held.

During capture, the player clicks the value, holds the desired keys, presses the main key last, and releases the combination. Escape cancels without changing the saved value.

To migrate an existing `ConfigEntry<KeyCode>`, change the entry type to `ConfigEntry<KeyboardShortcut>` while keeping the same section and key. A saved value such as `F8` is valid as a one-key `KeyboardShortcut`:

```csharp
// Before:
// ConfigEntry<KeyCode> toggle = Config.Bind("Hotkeys", "Toggle", KeyCode.F8, "...");

// After, using the same section and key:
ConfigEntry<KeyboardShortcut> toggle = Config.Bind(
    "Hotkeys", "Toggle", new KeyboardShortcut(KeyCode.F8), "...");
```

Back up the player's configuration before shipping a type migration and verify the saved value after a real game launch.

### Action buttons

Buttons run an action immediately and do not store a value.

```csharp
page.AddButton(
    "ResetLayout",
    "Reset overlay layout",
    ResetLayout,
    "Restore the default position and size.");
```

## 6. React to changes

Every returned value handle exposes:

- `Value`: current typed value
- `DefaultValue`: registered default
- `Reset()`: restore the default
- `Changed`: typed change event
- `ValueChanged`: untyped event inherited from `ModSetting`

Use either the method's `onChanged` argument or the returned handle's `Changed` event:

```csharp
SettingHandle<bool> enabled = page.BindToggle(
    enabledEntry,
    "Enable overlay");

enabled.Changed += value =>
{
    if (_overlay is not null)
        _overlay.SetActive(value);
};
```

Keep callbacks fast and defensive. A settings callback should update existing managed state or an already-valid game object. Avoid creating startup UI, assuming the player is in a world, or throwing if runtime objects do not exist yet.

## 7. Complete integration example

```csharp
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Configuration;
using SpiritVale.ModConfig;
using UnityEngine;

namespace ExampleMod;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ModConfigPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "example.author.my-mod";
    public const string PluginName = "My Mod";
    public const string PluginVersion = "1.0.0";

    private ConfigEntry<bool> _enabled;
    private ConfigEntry<int> _maxRows;
    private ConfigEntry<float> _scale;
    private ConfigEntry<string> _style;
    private ConfigEntry<KeyboardShortcut> _toggleHotkey;

    public override void Load()
    {
        _enabled = Config.Bind("General", "Enabled", true, "Enable My Mod.");
        _maxRows = Config.Bind("Display", "MaxRows", 8, "Maximum rows shown.");
        _scale = Config.Bind("Display", "Scale", 1f, "Overlay scale.");
        _style = Config.Bind("Display", "Style", "Compact", "Display style.");
        _toggleHotkey = Config.Bind(
            "Hotkeys", "Toggle", new KeyboardShortcut(KeyCode.F8),
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
        page.BindIntegerSlider(_maxRows, "Maximum rows", 1, 20);
        page.BindFloatSlider(
            _scale, "Overlay scale", 0.5f, 1.5f, 0.05f, "0.00x",
            "Resize the overlay.", value => ApplyScale(value));
        page.BindChoice(
            _style, "Display style",
            new[] { "Compact", "Detailed", "Minimal" });

        page.AddHeader(
            "HOTKEYS",
            "Hold the desired keys, press the main key last, then release.");
        page.BindHotkey(_toggleHotkey, "Toggle overlay");

        page.AddHeader("ACTIONS");
        page.AddButton(
            "ResetLayout", "Reset overlay layout", ResetLayout,
            "Restore the default position and size.");
    }

    private void ApplyScale(float value)
    {
        // Apply only when the overlay exists and the player is in a valid world.
    }

    private void ResetLayout()
    {
        // Restore the mod's persisted layout values and refresh the live overlay.
    }
}
```

## 8. Page structure and copy guidelines

- Use one page per plugin GUID.
- Keep the page title equal to the public mod name.
- Group settings with short headers such as `GENERAL`, `DISPLAY`, `HOTKEYS`, and `ACTIONS`.
- Use sentence-case setting labels.
- Describe the result, not the implementation detail.
- State when a setting applies only after re-entering the world or restarting.
- Do not expose raw internal coordinates when a clear reset action is safer.
- Prefer bounded sliders over unrestricted numeric text fields.
- Keep destructive actions clearly named and separate from ordinary settings.
- Do not add a second custom settings window when the MCM page covers the same controls.

## 9. Lifecycle and compatibility

- Register from the plugin's `Load()` method after binding configuration.
- Use a hard dependency to guarantee load order.
- Call `ModConfigMenu.Unregister(PluginGuid)` if the plugin supports unloading.
- Do not modify MCM's registry from worker threads.
- Do not repeatedly register or populate the page from per-frame methods.
- Preserve stable page and setting keys across releases.
- When changing a setting's type, back up and test migration against a real existing config.
- Log integration success and failures with the integrating mod's normal log prefix.
- Keep MCM integration failures isolated from unrelated gameplay behavior where practical.

## 10. Validation checklist

Before releasing an integration:

1. Build with zero warnings and confirm MCM is not copied into the mod output.
2. Preserve the previously installed DLL and the mod's current configuration.
3. Launch SpiritVale and confirm both plugins finish loading without exceptions.
4. Open **ESC > Settings > MODS** and select the mod page.
5. Exercise every control and confirm the live mod reacts correctly.
6. Restart the game and verify values persist.
7. Test **RESET PAGE** and every action button.
8. Capture a single-key hotkey and a multi-key chord.
9. Confirm hotkeys do not fire while MCM is capturing input or while the mod's normal world gate is closed.
10. Test at the smallest supported resolution and UI scale.
11. Verify all mod-authored logs use the mod's identifying prefix.

## 11. Troubleshooting

### The mod page does not appear

- Confirm `SpiritVale.ModConfigMenu.dll` is installed once under `BepInEx\plugins`.
- Confirm the integrating mod declares an MCM dependency.
- Check that `ModConfigMenu.Register` runs during `Load()`.
- Search `BepInEx\LogOutput.log` for `[ModConfigMenu]` and the integrating mod's prefix.
- Confirm the page ID and every non-header setting key are non-empty and unique.

### The mod fails to load after adding the reference

- Confirm the referenced MCM DLL is version 1.1.0 or newer.
- Confirm the project targets .NET 6 and references the same BepInEx installation as MCM.
- Keep `<Private>false</Private>` on the MCM reference.
- Do not place backup DLLs inside `BepInEx\plugins`.

### Values change in MCM but not in the mod

- Bind the original `ConfigEntry<T>` instead of creating a separate MCM-owned value.
- Read the current entry value at the point of use, or subscribe to `Changed`.
- If a live Unity object must update, guard against it being null or outside the world.

### A hotkey does not trigger

- Use `KeyboardShortcut.IsDown()` instead of `Input.GetKeyDown(MainKey)`.
- Remember that BepInEx shortcuts require the exact modifier combination.
- Process the shortcut only in the mod's valid gameplay update path.
- Ensure another UI is not intentionally suppressing gameplay input.
