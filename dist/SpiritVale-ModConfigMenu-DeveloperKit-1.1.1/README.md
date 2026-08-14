# SpiritVale Mod Configuration Menu Developer Kit

This kit contains everything needed to add a SpiritVale BepInEx 6 IL2CPP mod to Mod Configuration Menu.

## Kit contents

- `lib/SpiritVale.ModConfigMenu.dll`: MCM 1.1.1, public API version 2
- `docs/INTEGRATION_GUIDE.md`: complete API, lifecycle, testing, and troubleshooting guide
- `examples/ExamplePlugin.cs`: minimal integration using existing BepInEx configuration entries
- `examples/SpiritVale.McmExample.csproj`: buildable .NET 6 IL2CPP example project
- `SHA256SUMS.txt`: checksums for all distributed files

## Requirements

- SpiritVale
- BepInEx 6 IL2CPP installed in the game directory
- .NET 6 SDK for building a mod
- Mod Configuration Menu 1.1.0 or newer installed at runtime

## Integration steps

1. Add a compile-time reference to `lib/SpiritVale.ModConfigMenu.dll` or the installed MCM DLL.
2. Set `<Private>false</Private>` on the reference. This prevents MCM from being copied into your mod's output.
3. Add a hard dependency on `ModConfigPlugin.PluginGuid`.
4. Register one page from your plugin's `Load()` method.
5. Prefer the `Bind...` methods when your mod already has `ConfigEntry<T>` settings.
6. Test every control in `ESC > Settings > MODS`, then restart the game and confirm values persist.
7. Publish MCM as a separate requirement. Do not include its DLL in your own mod archive.

Read `docs/INTEGRATION_GUIDE.md` before releasing an integration.

## Build the example

Pass the SpiritVale installation directory through the `GameDir` property:

```powershell
dotnet build examples\SpiritVale.McmExample.csproj -p:GameDir="D:\Games\SpiritVale"
```

The example expects BepInEx assemblies under `BepInEx\core` and generated game assemblies under `BepInEx\interop`.

## Runtime testing

Install `SpiritVale.ModConfigMenu.dll` once at:

```text
<SpiritVale>\BepInEx\plugins\ModConfigMenu\SpiritVale.ModConfigMenu.dll
```

Install your own compiled plugin in a separate directory under `BepInEx\plugins`. Enter the world and open `ESC > Settings > MODS`.

If a page does not appear, inspect `BepInEx\LogOutput.log` and search for `[ModConfigMenu]` and your plugin's log prefix.
