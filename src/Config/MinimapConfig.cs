using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using OpenMinimap.Tracking;
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

    public sealed class KindSettings
    {
        public ConfigEntry<bool> Enabled;
        public ConfigEntry<MapIcon> Icon;
        public ConfigEntry<Color> Color;
        public ConfigEntry<float> Size;
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

        // Enemies (master gate; per-kind toggles live in the Icons section)
        public readonly ConfigEntry<bool> ShowEnemies;

        // Per-kind icon/color/size/visibility
        public IReadOnlyDictionary<MapKind, KindSettings> Kinds { get; }

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
                "Global scale multiplier applied on top of every icon's own size.");
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

            ShowEnemies = file.Bind("Enemies", "ShowEnemies", true,
                "Master switch for hostile enemies (regular, elite and boss). Hides all of them when off.");

            // One enabled/icon/color/size group per object kind. Sizes in pixels at
            // IconScale 1; colors multiply with dynamic tints (item tiers, charge state).
            var kinds = new Dictionary<MapKind, KindSettings>();
            foreach (MapKind kind in System.Enum.GetValues(typeof(MapKind)))
            {
                var d = IconLibrary.DefaultFor(kind);
                string label = IconLibrary.Label(kind);
                kinds[kind] = new KindSettings
                {
                    Enabled = file.Bind("Icons", kind + ".Enabled", true, $"Show {label} on the map."),
                    Icon = file.Bind("Icons", kind + ".Icon", d.Icon, $"Icon used for {label}."),
                    Color = file.Bind("Icons", kind + ".Color", d.Color, $"Color of the {label} icon."),
                    Size = file.Bind("Icons", kind + ".Size", d.Size, $"Size in pixels of the {label} icon."),
                };
            }
            Kinds = kinds;
        }

        public bool IsKindVisible(MapKind kind)
        {
            if (!Kinds[kind].Enabled.Value)
                return false;
            if (kind == MapKind.Enemy || kind == MapKind.EnemyElite || kind == MapKind.EnemyBoss)
                return ShowEnemies.Value;
            return true;
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

            // Per-kind options in stable enum order: enabled, icon, color, size.
            foreach (MapKind kind in System.Enum.GetValues(typeof(MapKind)))
            {
                var s = Kinds[kind];
                string label = IconLibrary.Label(kind);
                Add(new CheckBoxOption(s.Enabled, new CheckBoxConfig { category = "Icons", name = label }), modGuid, modName);
                Add(new ChoiceOption(s.Icon, new ChoiceConfig { category = "Icons", name = label + " Icon" }), modGuid, modName);
                Add(new ColorOption(s.Color, new ColorOptionConfig { category = "Icons", name = label + " Color" }), modGuid, modName);
                Add(new StepSliderOption(s.Size, new StepSliderConfig { category = "Icons", min = 8f, max = 48f, increment = 1f, FormatString = "{0:0} px", name = label + " Size" }), modGuid, modName);
            }

            Add(new CheckBoxOption(ShowEnemies, new CheckBoxConfig { category = "Enemies", name = "Show All Enemies" }), modGuid, modName);
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
