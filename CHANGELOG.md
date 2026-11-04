# Changelog

## 1.2.0

- Skyrim-style compass: markers near the center of the strip are brighter and slightly larger, fading toward the edges, and every marker shows a faint distance label underneath (e.g. 42m). Both toggleable.
- Minimap extras (inspired by OmniMap): camera view cone, distance rings (configurable spacing), a north marker pinned to the rim, and a stage name / teleporter distance line under the window. All toggleable in the Map tab.

## 1.1.0

- Full per-kind icon customization: every object kind can be switched off and given its own icon (24 to choose from — mostly the game's own icons plus generated Dot/Triangle/Ring), color, and size, all in Risk of Options.
- Distinct kinds for everything: regular chests, large chests (red), category chests (tier-colored: damage red / healing green / utility blue), shrines, Newt Altars, drones, broken drones, scrappers, printers/cauldrons, multishops (tinted by the item tier actually on offer), equipment barrels (orange), money barrels, teleporter, portals, void potentials, command cubes, ground items, enemies, elites, bosses, minions, other players.
- Bosses show their real portrait; elites and bosses are their own kinds and enemies upgrade between them live mid-fight.
- Dropped items always use their real pickup icon (small dot fallback); pickups without a valid item are skipped, so nothing shows as a fake chest anymore.
- Teleporter markers show the holdout-zone charge radius ring and a live charge percentage while charging.
- Compass scroll direction can be inverted; compass and minimap offsets are exact (both views anchor to the canvas root) and compass offset accepts negative values.
- Fixed ghost minion icons from transient ability spawns (dash decoys): minion markers now require real ownership.

## 1.0.0

- Initial release.
- Minimap (top-right, circle or square, rotates with the player or north-up) with zoom, size, offset and opacity options.
- Compass mode: top-of-screen strip with N/NE/E/SE/S/SW/W/NW, degree ticks, and edge-clamped markers.
- Elevation arrows for objects above/below the player (configurable threshold).
- In-game icons sourced from the game's own texture set; unknown content falls back to the vanilla ping icon.
- Full Risk of Options integration: toggle keybind (default M), display mode, live-adjustable display options.
- Component-based detection covering Survivors of the Void, Seekers of the Storm, Alloyed Collective and Hallowed Concepts content without hardcoded lists.
