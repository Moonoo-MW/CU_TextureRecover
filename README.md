# GuiReplacer

GuiReplacer is a **Unity Mono + BepInEx 5** runtime texture replacement framework. It replaces existing `Texture2D` pixel data in memory so UI components such as `Image`, `RawImage`, and `Sprite` can keep referencing the original texture object. It does **not** modify game asset files.

## Requirements

- Unity Mono game, not IL2CPP
- BepInEx 5.x already installed and working
- Visual Studio with .NET Framework 4.7.2 targeting pack
- Game-managed assemblies available from the target game

## Project Structure

```text
GuiReplacer.sln
GuiReplacer.csproj
Source/
  Plugin.cs
  TextureManager.cs
  TextureLoader.cs
  TextureReplacer.cs
  HotReload.cs
  Config.cs
  Logger.cs
README.md
```

## Build

1. Open `GuiReplacer.sln` in Visual Studio.
2. Set the `GameRoot` MSBuild property to your game root folder, or keep the solution inside the game root.
3. If needed, set `GameName` so `$(GameName)_Data/Managed` points to the Unity managed assemblies.
4. Build the project as `Release | Any CPU`.
5. Copy `GuiReplacer.dll` to:

```text
<GameRoot>/BepInEx/plugins/GuiReplacer.dll
```

The project references BepInEx and Unity assemblies from the target game instead of bundling them.

## Installation

After installing the plugin DLL, start the game once. GuiReplacer automatically creates:

```text
<GameRoot>/BepInEx/config/GuiReplacer.cfg
<GameRoot>/Mods/GUI/
```

Default configuration:

```ini
Enable = true
EnableHotReload = true
RecursiveScan = true
IgnoreCase = true
AllowSizeMismatch = false
EnableLog = true
ModsFolder = Mods/GUI
EnableDump = false
```

## Adding Replacement PNG Files

Put PNG files under:

```text
<GameRoot>/Mods/GUI/
```

The PNG filename without extension is matched against `Texture.name`.

Examples:

```text
Mods/GUI/button.png        -> Texture.name == "button"
Mods/GUI/title.png         -> Texture.name == "title"
Mods/GUI/icon/sword.png    -> Texture.name == "sword"
Mods/GUI/enemy/boss.png    -> Texture.name == "boss"
```

When `IgnoreCase=true`, filename matching is case-insensitive.

## Hot Reload

Press **F8** in game to:

1. Rebuild the live `Texture2D` lookup table.
2. Rescan `Mods/GUI` for PNG files.
3. Reapply all matching replacements.

This lets artists iterate on PNG files without restarting the game.

## Debug Dump Mode

Set the following option in `BepInEx/config/GuiReplacer.cfg`:

```ini
EnableDump = true
```

On the next startup, GuiReplacer exports discovered textures to:

```text
<GameRoot>/Mods/GUI_Dump/
```

Dumped filenames use `Texture.name.png`. If a file already exists, GuiReplacer appends the texture `InstanceID`. After the dump completes, `EnableDump` is automatically set back to `false`.

## Notes and Limitations

- GuiReplacer mutates existing texture pixel data and avoids replacing texture object references.
- Size mismatches are logged as `Texture Size Mismatch`.
- If `AllowSizeMismatch=false`, mismatched PNG files are skipped.
- If `AllowSizeMismatch=true`, mismatched PNG files are scaled to the original texture dimensions before writing.
- Some engine or platform textures may reject CPU-side writes. GuiReplacer tries `Graphics.CopyTexture` first and falls back to `SetPixels32` when possible.
- The plugin scans on startup and only scans again when F8 is pressed; it does not scan every frame.
- No Harmony patching is used.
