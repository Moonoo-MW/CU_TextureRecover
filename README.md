# CU_TextureRecover

CU_TextureRecover is a **Unity Mono + BepInEx 5** runtime texture replacement framework. It replaces existing `Texture2D` pixel data in memory so UI components such as `Image`, `RawImage`, and `Sprite` can keep referencing the original texture object. It does **not** modify game asset files.

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
  Overlay.cs
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

After installing the plugin DLL, start the game once. CU_TextureRecover automatically creates:

```text
<GameRoot>/BepInEx/plugins/GuiReplacer/Config/CU_TextureRecover.cfg
<GameRoot>/BepInEx/plugins/GuiReplacer/GUI/
<GameRoot>/BepInEx/plugins/GuiReplacer/Cache/
```

Default configuration:

```ini
Enable = true
EnableHotReload = true
RecursiveScan = true
IgnoreCase = true
AllowSizeMismatch = false
EnableLog = true
ModsFolder = GuiReplacer/GUI
EnableOverlay = true
OverlayDuration = 3
OverlayFadeTime = 0.3
EnableDump = false
```

## Adding Replacement PNG Files

Put PNG files under:

```text
<GameRoot>/BepInEx/plugins/GuiReplacer/GUI/
```

The PNG filename without extension is matched against `Texture.name`.

Examples:

```text
GuiReplacer/GUI/button.png        -> Texture.name == "button"
GuiReplacer/GUI/title.png         -> Texture.name == "title"
GuiReplacer/GUI/icon/sword.png    -> Texture.name == "sword"
GuiReplacer/GUI/enemy/boss.png    -> Texture.name == "boss"
```

When `IgnoreCase=true`, filename matching is case-insensitive.

## Hot Reload

Press **F8** in game to:

1. Rebuild the live `Texture2D` lookup table.
2. Rescan `GuiReplacer/GUI` for PNG files.
3. Reapply all matching replacements.

This lets artists iterate on PNG files without restarting the game.

## Debug Dump Mode

Set the following option in `BepInEx/plugins/GuiReplacer/Config/CU_TextureRecover.cfg`:

```ini
EnableDump = true
```

On the next startup, CU_TextureRecover exports discovered textures to:

```text
<GameRoot>/BepInEx/plugins/GuiReplacer/Cache/GUI_Dump/
```

Dumped filenames use `Texture.name.png`. If a file already exists, CU_TextureRecover appends the texture `InstanceID`. After the dump completes, `EnableDump` is automatically set back to `false`.

## Notes and Limitations

- CU_TextureRecover mutates existing texture pixel data and avoids replacing texture object references.
- Size mismatches are logged as `Texture Size Mismatch`.
- If `AllowSizeMismatch=false`, mismatched PNG files are skipped.
- If `AllowSizeMismatch=true`, mismatched PNG files are scaled to the original texture dimensions before writing.
- Some engine or platform textures may reject CPU-side writes. GuiReplacer tries `Graphics.CopyTexture` first and falls back to `SetPixels32` when possible.
- On startup, a non-interactive IMGUI overlay shows `TextureReplaceMod By Moonoo` once when `EnableOverlay=true`.
- Pressing F8 after a successful hot reload shows `Reload Texture Success` for two seconds when `EnableOverlay=true`.
- The plugin scans on startup and only scans again when F8 is pressed; it does not scan every frame.
- No Harmony patching is used.
