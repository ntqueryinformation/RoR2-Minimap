using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using OpenMinimap.Util;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;

namespace OpenMinimap.Config
{
    public enum DisplayMode
    {
        Minimap,
        Compass,
        Both,
    }

    public enum MapOrientation
    {
        RotateWithPlayer,
        NorthUp,
    }

    public enum MapShape
    {
        Circle,
        Square,
    }

    public sealed class MinimapConfig
    {
        // General
        public readonly ConfigEntry<bool> Enabled;
        public readonly ConfigEntry<KeyboardShortcut> ToggleKey;
        public readonly ConfigEntry<DisplayMode> Display;

        // Map
        public readonly ConfigEntry<MapShape> Shape;
        public readonly ConfigEntry<MapOrientation> Orientation;
        public readonly ConfigEntry<int> MapSize;
        public readonly ConfigEntry<int> MapOffsetX;
        public readonly ConfigEntry<int> MapOffsetY;
        public readonly ConfigEntry<float> Zoom;               // pixels per meter
        public readonly ConfigEntry<int> Opacity;              // percent
        public readonly ConfigEntry<float> IconScale;
        public readonly ConfigEntry<bool> ClampToEdge;
        public readonly ConfigEntry<bool> ElevationArrows;
        public readonly ConfigEntry<float> ElevationThreshold; // meters

        // Compass
        public readonly ConfigEntry<int> CompassWidth;
        public readonly ConfigEntry<int> CompassOffsetY;
        public readonly ConfigEntry<bool> CompassIntercardinals;
        public readonly ConfigEntry<bool> CompassTicks;
        public readonly ConfigEntry<bool> CompassClampMarkers;
        public readonly ConfigEntry<bool> CompassInvert;

        // Icons (per-kind visibility)
        public readonly ConfigEntry<bool> ShowChests;
        public readonly ConfigEntry<bool> ShowGoldChests;
        public readonly ConfigEntry<bool> ShowShrines;
        public readonly ConfigEntry<bool> ShowNewtAltars;
        public readonly ConfigEntry<bool> ShowDrones;
        public readonly ConfigEntry<bool> ShowScrapper;
        public readonly ConfigEntry<bool> ShowPrinters;
        public readonly ConfigEntry<bool> ShowMultishops;
        public readonly ConfigEntry<bool> ShowBarrels;
        public readonly ConfigEntry<bool> ShowTeleporter;
        public readonly ConfigEntry<bool> ShowPortals;
        public readonly ConfigEntry<bool> ShowVoidPotentials;
        public readonly ConfigEntry<bool> ShowCommandCubes;
        public readonly ConfigEntry<bool> ShowGroundItems;
        public readonly ConfigEntry<bool> ShowOtherPlayers;
        public readonly ConfigEntry<bool> ShowMinions;
        public readonly ConfigEntry<bool> ShowOther;

        // Enemies
        public readonly ConfigEntry<bool> ShowEnemies;
        public readonly ConfigEntry<bool> HighlightElites;

        public MinimapConfig(ConfigFile file)
        {
            Enabled = file.Bind("General", "Enabled", true,
                "Show the minimap/compass overlay.");
            ToggleKey = file.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.M),
                "Press this key in-game to toggle the overlay on/off.");
            Display = file.Bind("General", "DisplayMode", DisplayMode.Minimap,
                "Minimap: radar window in the top-right corner. Compass: directional strip at the top of the screen. Both: show both.");

            Shape = file.Bind("Map", "Shape", MapShape.Circle,
                "Window shape of the minimap.");
            Orientation = file.Bind("Map", "Orientation", MapOrientation.RotateWithPlayer,
                "RotateWithPlayer: the map spins with your camera and your arrow always points up. NorthUp: north is always up and your arrow shows your facing.");
            MapSize = file.Bind("Map", "MapSize", 280,
                "Size of the minimap window in pixels.");
            MapOffsetX = file.Bind("Map", "OffsetFromRight", 40,
                "Distance in pixels between the minimap and the right edge of the screen.");
            MapOffsetY = file.Bind("Map", "OffsetFromTop", 330,
                "Distance in pixels between the minimap and the top edge of the screen. The default clears the vanilla objective panel.");
            Zoom = file.Bind("Map", "Zoom", 4f,
                "Zoom level in pixels per meter. Higher values zoom in.");
            Opacity = file.Bind("Map", "Opacity", 80,
                "Opacity of the whole minimap in percent.");
            IconScale = file.Bind("Map", "IconScale", 1f,
                "Global scale multiplier for all icons.");
            ClampToEdge = file.Bind("Map", "ClampToEdge", true,
                "Keep icons of far-away objects pinned to the rim of the minimap instead of hiding them.");
            ElevationArrows = file.Bind("Map", "ElevationArrows", true,
                "Show a tiny arrow on icons of objects that are much higher or lower than you.");
            ElevationThreshold = file.Bind("Map", "ElevationThreshold", 5f,
                "Height difference in meters at which elevation arrows appear.");

            CompassWidth = file.Bind("Compass", "CompassWidth", 480,
                "Width of the compass strip in pixels.");
            CompassOffsetY = file.Bind("Compass", "CompassOffsetFromTop", 24,
                "Distance in pixels between the compass and the top edge of the screen. Negative values move it further up.");
            CompassIntercardinals = file.Bind("Compass", "ShowIntercardinals", true,
                "Also show NE, SE, SW and NW on the compass.");
            CompassTicks = file.Bind("Compass", "ShowDegreeTicks", true,
                "Show a small tick mark every 15 degrees.");
            CompassClampMarkers = file.Bind("Compass", "ClampMarkers", true,
                "Pin markers of off-screen objects to the ends of the compass strip.");
            CompassInvert = file.Bind("Compass", "InvertScrollDirection", false,
                "Flip which way the compass scrolls when you turn the camera.");

            ShowChests = file.Bind("Icons", "ShowChests", true, "Show regular and timed chests.");
            ShowGoldChests = file.Bind("Icons", "ShowGoldChests", true, "Show large (legendary) chests.");
            ShowShrines = file.Bind("Icons", "ShowShrines", true, "Show shrines (all types, including DLC shrines).");
            ShowNewtAltars = file.Bind("Icons", "ShowNewtAltars", true, "Show Newt Altars (bazaar portals).");
            ShowDrones = file.Bind("Icons", "ShowDrones", true, "Show purchasable drones and turrets.");
            ShowScrapper = file.Bind("Icons", "ShowScrapper", true, "Show item scrappers.");
            ShowPrinters = file.Bind("Icons", "ShowPrinters", true, "Show 3D printers.");
            ShowMultishops = file.Bind("Icons", "ShowMultishops", true, "Show multishop terminals.");
            ShowBarrels = file.Bind("Icons", "ShowBarrels", true, "Show unopened barrels.");
            ShowTeleporter = file.Bind("Icons", "ShowTeleporter", true, "Show the teleporter (color changes while charging/charged).");
            ShowPortals = file.Bind("Icons", "ShowPortals", true, "Show portals (blue, gold, celestial, void, etc.).");
            ShowVoidPotentials = file.Bind("Icons", "ShowVoidPotentials", true, "Show void potentials.");
            ShowCommandCubes = file.Bind("Icons", "ShowCommandCubes", true, "Show Command essence cubes.");
            ShowGroundItems = file.Bind("Icons", "ShowGroundItems", true, "Show dropped items lying on the ground.");
            ShowOtherPlayers = file.Bind("Icons", "ShowOtherPlayers", true, "Show other players in multiplayer.");
            ShowMinions = file.Bind("Icons", "ShowMinions", true, "Show your allied drones and turrets in the field.");
            ShowOther = file.Bind("Icons", "ShowOther", true, "Show interactables that don't match a known category (keeps new DLC content visible with the game's own ping icon).");

            ShowEnemies = file.Bind("Enemies", "ShowEnemies", true,
                "Show hostile enemies. When off, ALL enemies are hidden — including teleporter bosses and elites.");
            HighlightElites = file.Bind("Enemies", "HighlightElites", true,
                "Tint elite and boss enemies a brighter red than regular enemies.");
        }

        public bool IsKindVisible(Tracking.MapKind kind)
        {
            switch (kind)
            {
                case Tracking.MapKind.Chest: return ShowChests.Value;
                case Tracking.MapKind.GoldChest: return ShowGoldChests.Value;
                case Tracking.MapKind.Shrine: return ShowShrines.Value;
                case Tracking.MapKind.NewtAltar: return ShowNewtAltars.Value;
                case Tracking.MapKind.Drone: return ShowDrones.Value;
                case Tracking.MapKind.Scrapper: return ShowScrapper.Value;
                case Tracking.MapKind.Printer: return ShowPrinters.Value;
                case Tracking.MapKind.Multishop: return ShowMultishops.Value;
                case Tracking.MapKind.Barrel: return ShowBarrels.Value;
                case Tracking.MapKind.Teleporter: return ShowTeleporter.Value;
                case Tracking.MapKind.Portal: return ShowPortals.Value;
                case Tracking.MapKind.VoidPotential: return ShowVoidPotentials.Value;
                case Tracking.MapKind.CommandCube: return ShowCommandCubes.Value;
                case Tracking.MapKind.GroundItem: return ShowGroundItems.Value;
                case Tracking.MapKind.Enemy: return ShowEnemies.Value;
                case Tracking.MapKind.Minion: return ShowMinions.Value;
                case Tracking.MapKind.OtherPlayer: return ShowOtherPlayers.Value;
                default: return ShowOther.Value;
            }
        }

        // Settings registration is deferred to Start() by Plugin: after every plugin's Awake, so
        // Risk of Options' own initialization has settled before we hand it our config entries.
        public void RegisterRiskOfOptions(string modName, string description, string modGuid)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                return; // soft dependency: mod still works, settings stay in the BepInEx config file
            try
            {
                RegisterInternal(modName, description, modGuid);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[{modGuid}] Risk of Options registration failed ({e.Message}); settings remain available in the BepInEx config file.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RegisterInternal(string modName, string description, string modGuid)
        {
            ModSettingsManager.SetModDescription(description, modGuid, modName);
            ModSettingsManager.SetModIcon(Sprite.Create(MiscAssets.ModIconTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f)), modGuid, modName);

            Add(new CheckBoxOption(Enabled, new CheckBoxConfig { category = "General" }), modGuid, modName);
            Add(new KeyBindOption(ToggleKey, new KeyBindConfig { category = "General" }), modGuid, modName);
            Add(new ChoiceOption(Display, new ChoiceConfig { category = "General" }), modGuid, modName);

            Add(new ChoiceOption(Shape, new ChoiceConfig { category = "Map" }), modGuid, modName);
            Add(new ChoiceOption(Orientation, new ChoiceConfig { category = "Map" }), modGuid, modName);
            Add(new IntSliderOption(MapSize, new IntSliderConfig { category = "Map", min = 150, max = 600, formatString = "{0} px" }), modGuid, modName);
            Add(new IntSliderOption(MapOffsetX, new IntSliderConfig { category = "Map", min = 0, max = 800, formatString = "{0} px" }), modGuid, modName);
            Add(new IntSliderOption(MapOffsetY, new IntSliderConfig { category = "Map", min = 0, max = 800, formatString = "{0} px" }), modGuid, modName);
            Add(new StepSliderOption(Zoom, new StepSliderConfig { category = "Map", min = 1f, max = 15f, increment = 0.5f, FormatString = "{0:0.0} px/m" }), modGuid, modName);
            Add(new IntSliderOption(Opacity, new IntSliderConfig { category = "Map", min = 10, max = 100, formatString = "{0}%" }), modGuid, modName);
            Add(new StepSliderOption(IconScale, new StepSliderConfig { category = "Map", min = 0.5f, max = 2f, increment = 0.05f, FormatString = "{0:0.00}x" }), modGuid, modName);
            Add(new CheckBoxOption(ClampToEdge, new CheckBoxConfig { category = "Map" }), modGuid, modName);
            Add(new CheckBoxOption(ElevationArrows, new CheckBoxConfig { category = "Map" }), modGuid, modName);
            Add(new SliderOption(ElevationThreshold, new SliderConfig { category = "Map", min = 2f, max = 30f, FormatString = "{0:0} m" }), modGuid, modName);

            Add(new IntSliderOption(CompassWidth, new IntSliderConfig { category = "Compass", min = 200, max = 1000, formatString = "{0} px" }), modGuid, modName);
            Add(new IntSliderOption(CompassOffsetY, new IntSliderConfig { category = "Compass", min = -100, max = 400, formatString = "{0} px" }), modGuid, modName);
            Add(new CheckBoxOption(CompassIntercardinals, new CheckBoxConfig { category = "Compass" }), modGuid, modName);
            Add(new CheckBoxOption(CompassTicks, new CheckBoxConfig { category = "Compass" }), modGuid, modName);
            Add(new CheckBoxOption(CompassClampMarkers, new CheckBoxConfig { category = "Compass" }), modGuid, modName);
            Add(new CheckBoxOption(CompassInvert, new CheckBoxConfig { category = "Compass" }), modGuid, modName);

            Add(new CheckBoxOption(ShowChests, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowGoldChests, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowShrines, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowNewtAltars, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowDrones, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowScrapper, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowPrinters, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowMultishops, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowBarrels, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowTeleporter, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowPortals, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowVoidPotentials, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowCommandCubes, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowGroundItems, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowOtherPlayers, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowMinions, new CheckBoxConfig { category = "Icons" }), modGuid, modName);
            Add(new CheckBoxOption(ShowOther, new CheckBoxConfig { category = "Icons" }), modGuid, modName);

            Add(new CheckBoxOption(ShowEnemies, new CheckBoxConfig { category = "Enemies" }), modGuid, modName);
            Add(new CheckBoxOption(HighlightElites, new CheckBoxConfig { category = "Enemies" }), modGuid, modName);
        }

        private static void Add(BaseOption option, string modGuid, string modName)
        {
            ModSettingsManager.AddOption(option, modGuid, modName);
        }
    }

    // Procedural fallback textures/sprites that don't come from the game (window shapes, mod icon).
    internal static class MiscAssets
    {
        private static Texture2D _modIconTexture;

        public static Texture2D ModIconTexture
        {
            get
            {
                if (_modIconTexture == null)
                    _modIconTexture = TextureGen.FilledCircle(128, new Color(0.12f, 0.14f, 0.2f, 1f), new Color(0.9f, 0.75f, 0.2f, 1f), 6);
                return _modIconTexture;
            }
        }
    }
}
