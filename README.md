# OpenMinimap

A minimap and compass HUD for **Risk of Rain 2** that shows every interactable, enemy, and player around you using **the game's own icons**, with every category selectable and every setting exposed through **Risk of Options**.

- **Minimap** — a circular (or square) radar in the top-right corner. It rotates with your camera, keeps you at the center, and pins far-away objects to the rim.
- **Compass** — prefer the top of the screen? Switch to a horizontal compass strip with N/NE/E/SE/S/SW/W/NW and degree ticks; markers slide along it as you turn, with behind-you objects pinned to the ends. (Or enable both.)
- **Elevation arrows** — objects well above or below you get a tiny up/down chevron.
- **DLC-proof** — detection is component- and token-based, not a hardcoded list: everything from Survivors of the Void, Seekers of the Storm, Alloyed Collective and Hallowed Concepts is picked up automatically, and anything unrecognized still shows using the game's own ping icon.

## Features

### Object categories — every one individually configurable
Every kind below can be **switched off**, given **another icon** (24 choices, mostly the game's own icons plus generated dot/triangle/ring), recolored, and resized — all from Risk of Options:

Chests (regular/timed) · Large chests (red) · Category chests (tier-colored) · Shrines (all DLC types) · Newt Altars · Drones for sale · Broken drones · Scrapper · 3D printers & cauldrons · Multishops (tinted by the item tier on offer) · Triple shops (merged into one marker) · Equipment barrels (orange) · Money barrels · Teleporter (yellow while charging with a live % label and the holdout-zone radius ring, green when charged) · Portals · Void potentials · Command cubes · Hallowed Relics (hex icon) · Ground items (their real pickup icons) · Enemies · Elite enemies (own icon & color) · Bosses (real portrait) · Your minions · Other players · Other/unknown

### Enemies
- One master switch hides **all** hostiles (regular, elite and boss).
- Elites and bosses are separate kinds with their own icons, colors and sizes; bosses show their actual portrait. Enemies upgrade between kinds live (e.g. an elite buff mid-fight).

### Options (Risk of Options, no file editing required)
| Category | Options |
|---|---|
| **General** | Enabled, toggle keybind (default `M`), display mode (Minimap / Compass / Both) |
| **Map** | Shape (circle/square), orientation (rotate with player / north-up), size, offset from corner, zoom (pixels-per-meter), opacity, icon scale, clamp-to-edge, elevation arrows + height threshold, view cone, distance rings + spacing, north marker, stage info line |
| **Compass** | Width, offset from top (negative = higher), intercardinals, degree ticks, clamp markers, invert scroll direction, distance labels, center highlight (Skyrim-style: markers fade and shrink toward the edges) |
| **Icons** | For each of the 23 object kinds: show toggle, icon choice (24), color picker, size slider |
| **Enemies** | Master switch for all hostiles |

Everything applies live — no restart needed. If Risk of Options isn't installed, all settings still work through the regular BepInEx config file.

## Install

Install with **r2modman** or the Thunderstore mod manager (recommended: it also installs Risk of Options for the in-game menu). Manual install: drop `OpenMinimap.dll` into `BepInEx/plugins/`.

## Building from source

Requires .NET SDK and a Risk of Rain 2 install:

```powershell
dotnet build src/OpenMinimap.csproj -c Release -p:GameDir="C:\...\Risk of Rain 2"
```

With r2modman, `tools/deploy.ps1` builds and copies the DLL into a profile, and `tools/launch-modded.ps1` launches the game with that profile. Both default to the paths used during development; override with parameters.

## How it works

The mod is read-only: it never hooks or patches game code. It polls the scene on a 2-second cadence (plus faster resamples on interaction/death events), classifies objects by their components (`ChestBehavior`, `ShrineBehavior`, `SummonMasterBehavior`, team index, ...) with token/name hints for DLC-specific content, and renders icons on a uGUI overlay under the vanilla HUD. Opened/purchased objects fade to 35% opacity; barrels disappear when opened; dead enemies are removed immediately.

## Credits

Built from scratch; inspired by [MiniMapMod](https://thunderstore.io/c/riskofrain2/p/DekuDesu/MiniMapMod/) by DekuDesu/artyredd. Icons are loaded from the game's own texture set. MIT licensed.
