# Stardew Valley: Romanian Language Mod

Adds **"Română"** as a selectable language on Stardew Valley's title screen language menu, using a Romanian translation of the game's dialogue, data, and string assets.

## About

Stardew Valley does not ship an official Romanian translation. This mod adds one as a new entry in `Data/AdditionalLanguages`, the same mechanism the base game uses for German, Turkish, and other supported languages. Once installed, "Română" appears as its own button on the language selection screen, right alongside the vanilla languages.

The mod is split into two parts that ship together and depend on each other:

- **`[CP] Romanian Translation`**, a [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) content pack. It registers the new language and loads the Romanian text, data, and image files whenever that language is selected.
- **`[Harmony] Romanian Font Fix`**, a small SMAPI code mod. The Romanian alphabet needs six diacritic characters (ă, Ă, ș, Ș, ț, Ț) that the base game's fonts cannot draw. This mod patches the game's fonts at runtime so those characters render correctly. See "How it works" below for details.

## Disclaimer

This is not a professional translation and not a professional mod. It is a personal project by someone learning Romanian, who translated the game with the help of large language models and wrote this mod to make that translation playable. There is no guarantee that the translation is accurate or natural, and no guarantee that the mod runs without issues. Use it at your own risk, and please report anything that looks wrong or breaks.

## Preview

| Title screen | Item tooltip and save info | Dialogue |
| --- | --- | --- |
| ![Title screen fully translated into Romanian](docs/screenshots/04-title-romanian.png) | ![Item tooltip and save info shown in Romanian](docs/screenshots/05-farm-tooltip-romanian.png) | ![Dialogue with a villager shown in Romanian](docs/screenshots/06-dialogue-romanian.png) |

## Requirements

- Stardew Valley, installed with [SMAPI](https://smapi.io/) (the mod loader).
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915), installed as its own mod.
- Both parts of this mod, `Romanian Translation` and `Romanian Font Fix`. Neither works without the other. Both come prebuilt in the [latest release](https://github.com/Kerndevelop/stardew-valley-romanian-language-mod/releases/latest); nothing needs to be compiled to play.

## Installing the mod

There is nothing to compile. [Download the latest release](https://github.com/Kerndevelop/stardew-valley-romanian-language-mod/releases/latest), which ships both halves of the mod with the font fix already built, and copy the two folders into Stardew Valley's `Mods` folder. Building from source is only needed if you want to change the mod itself.

Both parts are installed the same way: one folder each inside `Mods`, next to Content Patcher. When you are done, `Mods` should contain these three entries, alongside `ConsoleCommands` and `SaveBackup`, which SMAPI installs itself:

```
Stardew Valley/
└── Mods/
    ├── ContentPatcher/               # from Nexus Mods, a separate mod
    ├── [CP] Romanian Translation/    # this repo's content pack
    │   ├── manifest.json
    │   ├── content.json
    │   └── assets/
    └── RomanianFontFix/              # this repo's compiled font fix
        ├── manifest.json
        ├── RomanianFontFix.dll
        └── assets/
```

### Step 1: find your `Mods` folder

The `Mods` folder sits directly inside the Stardew Valley game folder. SMAPI prints its exact location in the second line of its console window every time it starts (`Mods go here: ...`), which is the most reliable way to find it. Common default locations:

| Platform | Typical path |
| --- | --- |
| Linux (Steam) | `~/.steam/steam/steamapps/common/Stardew Valley/Mods` |
| Linux (Steam, alternate library) | `~/Games/Steam/steamapps/common/Stardew Valley/Mods` |
| macOS (Steam) | `~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods` |
| Windows (Steam) | `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods` |
| Windows (GOG) | `C:\Program Files (x86)\GOG Galaxy\Games\Stardew Valley\Mods` |

In Steam you can also right-click Stardew Valley in your library and choose **Manage → Browse local files**.

The rest of these instructions refer to that folder as `<GAME>/Mods`, and to the game folder itself as `<GAME>`.

### Step 2: install SMAPI and Content Patcher

1. Install [SMAPI](https://smapi.io/) following the instructions on its site, and start the game through SMAPI once to confirm it works. This creates the `Mods` folder if it does not exist yet.
2. Download [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) from Nexus Mods and unzip it so that `<GAME>/Mods/ContentPatcher/manifest.json` exists. Content Patcher 2.0.0 or newer is required.

### Step 3: install both mod folders

Download `RomanianLanguageMod-1.0.0.zip` from the [latest release](https://github.com/Kerndevelop/stardew-valley-romanian-language-mod/releases/latest) and unzip it. It contains two folders:

- `[CP] Romanian Translation` — the translation itself, plain JSON and image files.
- `RomanianFontFix` — the font fix, with `RomanianFontFix.dll` already compiled.

Copy both into `<GAME>/Mods/`, keeping the folder names exactly as they are, square brackets included. Afterwards both `<GAME>/Mods/[CP] Romanian Translation/manifest.json` and `<GAME>/Mods/RomanianFontFix/manifest.json` must exist.

Both folders are required. The content pack declares the font fix as a dependency, so SMAPI refuses to load it if the font fix is missing, and without the content pack the font fix has nothing to do.

If you would rather work from a clone of this repository than a release zip, the font fix has to be compiled first; see [Building from source](#building-from-source) below, then come back to the next step.

### Step 4: verify that both mods loaded

Start the game the way you normally do with SMAPI installed (the Steam **Play** button, or running `StardewModdingAPI` from the game folder). SMAPI's console window lists everything it loaded at startup; you should see both parts, one as a mod and one as a content pack:

```
[SMAPI] Loaded 4 mods:
[SMAPI]    Console Commands 4.5.2 by SMAPI | ...
[SMAPI]    Content Patcher 2.9.1 by Pathoschild | ...
[SMAPI]    Romanian Font Fix 1.0.0 by kerngesund | ...
[SMAPI]    Save Backup 4.5.2 by SMAPI | ...

[SMAPI] Loaded 1 content packs:
[SMAPI]    Romanian Translation 1.0.0 by kerngesund | for Content Patcher | ...
```

A `Patched game code` warning naming Romanian Font Fix is expected and harmless; that is just SMAPI noting that the mod uses Harmony, which it must in order to draw the diacritics.

If something is missing, the usual causes are:

- **Neither one appears.** They went into the wrong folder. Check the `Mods go here:` line at the top of the SMAPI console and compare it to where you copied them.
- **Only one appears, and SMAPI reports a missing dependency.** The two halves require each other. Redo whichever step above is missing.
- **A folder is nested one level too deep**, for example `Mods/RomanianFontFix/RomanianFontFix/manifest.json`. SMAPI only looks one level down, so move the inner folder up.
- **"Română" is missing from the language menu** even though both loaded. Make sure Content Patcher itself is installed and at least version 2.0.0.

Then switch the language from the title screen, as shown below.

### Selecting "Română" in-game

Once the game is running, switch the language from the title screen. It only takes a few clicks.

1. On the title screen, click the language button in the bottom right corner, circled in red below.

   ![Title screen with the language button circled in the bottom right corner](docs/screenshots/01-title-english.png)

2. The language menu opens showing the vanilla languages. "Română" is on the next page, so click the arrow on the right edge of the menu, circled in red below, to page forward.

   ![Language selection menu with the next-page arrow circled on the right edge](docs/screenshots/02-language-menu.png)

3. The second page shows a single option, "Română". Click it.

   ![Second page of the language menu showing the Română option](docs/screenshots/03-romana-selected.png)

4. The title screen, its buttons, and the rest of the game now display in Romanian.

   ![Title screen fully translated into Romanian](docs/screenshots/04-title-romanian.png)

## Building from source

Players do not need this section: the [latest release](https://github.com/Kerndevelop/stardew-valley-romanian-language-mod/releases/latest) ships the font fix already compiled. Build only if you want to change the mod itself.

Only the font fix needs compiling. The content pack is plain JSON and image files that SMAPI reads directly, so it is installed by copying, exactly as in [step 3](#step-3-install-both-mod-folders) above:

```bash
cp -r "mod/[CP] Romanian Translation" "/path/to/your/Stardew Valley/Mods/"
```

Its `scripts/` subfolder is only used to regenerate `content.json` during development; it is harmless to copy along, and equally fine to delete from the installed copy.

### Building the font fix

Install the [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or newer; the project targets `net6.0`), then build in Release mode:

```bash
cd "mod/[Harmony] Romanian Font Fix"
dotnet build -c Release -p:GamePath="/path/to/your/Stardew Valley"
```

The project uses [SMAPI's mod build package](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig). On a successful Release build it copies the finished mod into `<GAME>/Mods/RomanianFontFix/` and writes a distributable zip next to the build output, so there is nothing to copy by hand. Confirm it landed in the right place by looking for this line:

```
[mod build package] Copying the mod files to .../Stardew Valley/Mods/RomanianFontFix...
```

Then verify the result in-game, as in [step 4](#step-4-verify-that-both-mods-loaded) above.

### Setting `GamePath`

`GamePath` is where builds usually go wrong. It must point at the game folder, the one containing `Stardew Valley.dll`, not at `Mods`. `RomanianFontFix.csproj` hardcodes a default:

```xml
<GamePath Condition="'$(GamePath)' == ''">/home/kerngesund/Games/Steam/steamapps/common/Stardew Valley</GamePath>
```

That is the author's own Steam library path and will not match your machine. Either edit that line, or leave the file alone and override it per build, which is what the `Condition` is there for:

```bash
dotnet build -c Release -p:GamePath="/home/yourname/.steam/steam/steamapps/common/Stardew Valley"
```

If the path is wrong, the build fails with an error from the mod build package saying it could not find the game folder.

On **Windows** you can build from a command prompt, or open `RomanianFontFix.csproj` in Visual Studio and build the Release configuration:

```powershell
cd "mod\[Harmony] Romanian Font Fix"
dotnet build -c Release -p:GamePath="C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley"
```

There the build package can usually detect the game by itself through the registry or a nearby Steam or GOG install, so you can drop `-p:GamePath` and delete the hardcoded `<GamePath>` line from the `.csproj` entirely. Only set it explicitly if the build reports that it cannot find the game.

## Repo layout

```
.
└── mod/
    ├── [CP] Romanian Translation/    # the installable Content Patcher content pack
    │   ├── manifest.json             # SMAPI content-pack manifest
    │   ├── content.json              # generated, do not hand-edit, see below
    │   ├── assets/
    │   │   ├── button.png            # hand-drawn language-select button
    │   │   └── content_ro/           # Romanian translation files, shipped as-is
    │   └── scripts/
    │       └── generate_content_json.py  # generates content.json, see below
    └── [Harmony] Romanian Font Fix/  # C# SMAPI mod, required dependency of the content pack
        ├── manifest.json
        ├── RomanianFontFix.csproj
        ├── ModEntry.cs               # Harmony patch teaching font_bold to draw ă/Ă/ș/Ș/ț/Ț
        ├── DiacriticSpriteFontPatcher.cs
        ├── SpriteFont1Patcher.cs
        ├── SmallFontPatcher.cs
        └── assets/                  # glyph PNGs composited onto SpriteFont1/SmallFont at runtime
```

## How it works

### Registering the language

The content pack adds an entry to `Data/AdditionalLanguages`, the same native mechanism vanilla Stardew Valley uses for languages like German, Turkish, or Portuguese. That entry points at the translated files under `assets/content_ro/`, so the game loads them the moment "Română" is selected, the same way it loads any other language.

### Rendering the diacritics

Stardew Valley draws text through four independent font systems, and each one needed to be checked separately for whether it could display the Romanian diacritics ă, Ă, ș, Ș, ț, and Ț.

1. **`LooseSprites/font_bold`** (plus `font_colored`). This is the classic chunky pixel font. `StardewValley.BellsAndWhistles.SpriteText`, which draws almost all dialogue text, most menu labels, and things like the save-slot farmer name, uses this font whenever `LocalizedContentManager.CurrentLanguageLatin` is `true`. That flag is `true` for English, Spanish, German, Portuguese, French, Italian, Turkish, and Hungarian, and for a modded language it is whatever `UseLatinFont` is set to in `Data/AdditionalLanguages`. Diacritics are handled here, see below.
2. **`Fonts/SpriteFont1`** (`Game1.dialogueFont`). This is a real compiled XNA `SpriteFont`. Some menus draw directly with it, bypassing `SpriteText` entirely; for example, `LoadGameMenu` and `SaveGameMenu` use it for the farm name, in-game date, gold, and playtime on each save slot. Diacritics are handled here, see below.
3. **`Fonts/tinyFont`** (`Game1.tinyFont`). This is the same kind of compiled XNA `SpriteFont` as `SpriteFont1`, but checked against every `Game1.tinyFont` reference in the decompiled game code across `Toolbar.cs`, `InventoryMenu.cs`, and `CraftingRecipe.cs`, it turns out to be used exclusively for hotbar slot labels (1 through 9, 0, minus, equals) and numeric quantity or width measurement, never a translated string. No work was needed for this font.
4. **`Fonts/SmallFont`** (`Game1.smallFont`). This is another compiled XNA `SpriteFont`, and by far the highest traffic of the non `SpriteText` paths. `IClickableMenu.drawHoverText` and `drawToolTip` use it for the description body of every item tooltip, `FishingRod.cs` uses it for the catch and size popup, and it also backs `HUDMessage` toasts, the chat box, and several menus. Diacritics are handled here, see below.

The mod's `content.json` sets `"UseLatinFont": true`, which makes `SpriteText` use `font_bold` everywhere, so ordinary text looks pixel identical to the English version. All six Romanian diacritics render correctly through it: â and Î were already present in vanilla `font_bold`, and ă, Ă, ș, Ș, ț, and Ț were hand-drawn and composited into previously blank cells in the font atlas. Since `SpriteText.getSourceRectForChar`, the method that maps a character to its atlas cell, is hardcoded in C# rather than data-driven, teaching the game to find those six new glyphs required a small Harmony patch. That patch ships as `mod/[Harmony] Romanian Font Fix/`, a required dependency of the content pack, declared in its manifest.

The same six diacritics also render correctly on the Load and Save Game screens (`Fonts/SpriteFont1`). That font is a compiled XNA `SpriteFont`, not a plain texture, and there is no public API to add a glyph to it after the XNB has loaded. To work around this, `RomanianFontFix` hooks `Content.AssetRequested` for that asset and, on load, composites the same six hand-drawn glyphs onto the vanilla texture using a `RenderTarget2D` (needed because the vanilla texture is not in a plain `Color` format that `GetData` can read directly), then rebuilds the `SpriteFont` through its public constructor and copies its private fields onto the live instance, so every existing reference, including `Game1.dialogueFont`, sees the change immediately.

The same six diacritics also render correctly through `Fonts/SmallFont`, covering item tooltips, the fishing catch popup, HUD messages, chat, and more. It is patched the same way as `SpriteFont1`: `RomanianFontFix` hooks `Content.AssetRequested` for that asset too and runs the same composite-then-swap-private-fields logic, implemented in `SmallFontPatcher.cs` and shared with `SpriteFont1Patcher.cs` through `DiacriticSpriteFontPatcher.cs`.

## Regenerating the content pack

If any file under `mod/[CP] Romanian Translation/assets/content_ro/` changes, regenerate `content.json` to match:

```bash
cd "mod/[CP] Romanian Translation"
python3 scripts/generate_content_json.py
```

Do not hand-edit `content.json` directly; it is a generated file and will be overwritten the next time the script runs.
