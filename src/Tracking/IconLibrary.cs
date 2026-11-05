using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace OpenMinimap.Tracking
{
    // Icon source for the map. Every kind has a configurable icon (one of the MapIcon
    // choices — mostly the game's own MiscIcons set — plus generated Dot/Triangle/Ring),
    // color and size, with dynamic tints layered on top (multishop tier, category chest,
    // teleporter charge state, real pickup icons for dropped items, boss portraits).
    public static class IconLibrary
    {
        private const string MysteryIcon = "Textures/MiscIcons/texMysteryIcon";
        private const string ElevationIcon = "Textures/MiscIcons/texOptionsArrowLeft";

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<int, Sprite> PortraitCache = new Dictionary<int, Sprite>();
        private static readonly HashSet<string> Warned = new HashSet<string>();
        private static Sprite _dotSprite;
        private static Sprite _triangleSprite;
        private static Sprite _ringSprite;
        private static Sprite _hexSprite;

        public static Sprite ElevationChevron => Load(ElevationIcon);

        private static string IconPath(MapIcon icon)
        {
            switch (icon)
            {
                case MapIcon.ChestOutlined: return "Textures/MiscIcons/texInventoryIconOutlined";
                case MapIcon.Chest: return "Textures/MiscIcons/texInventoryIcon";
                case MapIcon.Shrine: return "Textures/MiscIcons/texShrineIconOutlined";
                case MapIcon.Teleporter: return "Textures/MiscIcons/texTeleporterIconOutlined";
                case MapIcon.TeleporterAlt: return "Textures/MiscIcons/texTeleporterIcon";
                case MapIcon.Drone: return "Textures/MiscIcons/texDroneIconOutlined";
                case MapIcon.Barrel: return "Textures/MiscIcons/texBarrelIcon";
                case MapIcon.Loot: return "Textures/MiscIcons/texLootIconOutlined";
                case MapIcon.Mystery: return MysteryIcon;
                case MapIcon.WIP: return "Textures/MiscIcons/texWIPIcon";
                case MapIcon.Attack: return "Textures/MiscIcons/texAttackIcon";
                case MapIcon.Sprint: return "Textures/MiscIcons/texSprintIcon";
                case MapIcon.Quickplay: return "Textures/MiscIcons/texQuickplay";
                case MapIcon.LunarPillar: return "Textures/MiscIcons/texLunarPillarIcon";
                case MapIcon.Edit: return "Textures/MiscIcons/texEditIcon";
                case MapIcon.RescueShip: return "Textures/MiscIcons/texRescueshipIcon";
                case MapIcon.Hurt: return "Textures/MiscIcons/texCriticallyHurtIcon";
                case MapIcon.Unlock: return "Textures/MiscIcons/texUnlockIcon";
                case MapIcon.Arrow: return "Textures/MiscIcons/texOptionsArrowLeft";
                case MapIcon.Randomize: return "Textures/MiscIcons/texRandomizeIcon";
                case MapIcon.RandomMap: return "Textures/MiscIcons/texRuleMapIsRandom";
                default: return null; // generated shapes
            }
        }

        public static Sprite Load(string path)
        {
            if (path == null)
                return null;
            if (Cache.TryGetValue(path, out var cached))
                return cached;
            Sprite sprite = null;
            try { sprite = LegacyResourcesAPI.Load<Sprite>(path); }
            catch (System.Exception) { /* treat as missing */ }
            if (sprite == null && Warned.Add(path))
                Plugin.Log.LogWarning($"Sprite not found: '{path}'.");
            Cache[path] = sprite; // cache misses too, to avoid repeated lookups
            return sprite;
        }

        public static Sprite GetSprite(MapIcon icon)
        {
            switch (icon)
            {
                case MapIcon.Dot:
                    if (_dotSprite == null)
                        _dotSprite = Util.TextureGen.MakeSprite(Util.TextureGen.CircleAlpha(64));
                    return _dotSprite;
                case MapIcon.Triangle:
                    if (_triangleSprite == null)
                        _triangleSprite = Util.TextureGen.MakeSprite(Util.TextureGen.TriangleAlpha(64));
                    return _triangleSprite;
                case MapIcon.Ring:
                    if (_ringSprite == null)
                        _ringSprite = Util.TextureGen.MakeSprite(Util.TextureGen.RingAlpha(64, 6f));
                    return _ringSprite;
                case MapIcon.Hex:
                    if (_hexSprite == null)
                        _hexSprite = Util.TextureGen.MakeSprite(Util.TextureGen.HexAlpha(64));
                    return _hexSprite;
                default:
                {
                    var sprite = Load(IconPath(icon));
                    return sprite != null ? sprite : Load(MysteryIcon);
                }
            }
        }

        // Chooses the sprite a marker should currently display: real pickup icons for
        // dropped items, the boss portrait for bosses, otherwise the kind's configured icon.
        public static Sprite ResolveSprite(TrackedMarker m, MapIcon iconChoice)
        {
            if (m.Kind == MapKind.GroundItem && m.Pickup != null && m.Pickup.pickupIndex.isValid)
            {
                var def = PickupCatalog.GetPickupDef(m.Pickup.pickupIndex);
                if (def != null && def.iconSprite != null)
                    return def.iconSprite;
                return GetSprite(MapIcon.Dot);
            }
            if (m.Kind == MapKind.EnemyBoss && m.Body != null)
            {
                var portrait = GetPortraitSprite(m.Body);
                if (portrait != null)
                    return portrait;
            }
            var chosen = GetSprite(iconChoice);
            if (chosen != null)
                return chosen;
            if (m.Source != null)
            {
                try
                {
                    var ping = RoR2.UI.PingIndicator.GetInteractableIcon(m.Source.gameObject);
                    if (ping != null)
                        return ping;
                }
                catch (System.Exception) { /* game's resolver can throw on odd objects */ }
            }
            return Load(MysteryIcon);
        }

        private static Sprite GetPortraitSprite(CharacterBody body)
        {
            if (!(body.portraitIcon is Texture2D tex) || tex == null)
                return null;
            int key = tex.GetInstanceID();
            if (PortraitCache.TryGetValue(key, out var sprite))
                return sprite;
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            PortraitCache[key] = sprite;
            return sprite;
        }

        // Final marker color = the kind's configured color multiplied by any dynamic tint.
        public static Color GetColor(TrackedMarker m, Color baseColor)
        {
            if (m.TintColor == Color.clear)
                return baseColor;
            Color tint = m.TintColor; // implicit Color32 -> Color
            return baseColor * tint;
        }

        public static Color GetTeleporterTint(TeleporterInteraction tp)
        {
            if (tp.activationState >= TeleporterInteraction.ActivationState.Charged)
                return new Color(0.35f, 1f, 0.45f);
            if (tp.activationState >= TeleporterInteraction.ActivationState.Charging)
                return new Color(1f, 0.85f, 0.35f);
            return Color.clear;
        }

        public readonly struct KindDefault
        {
            public readonly MapIcon Icon;
            public readonly Color Color;
            public readonly float Size;

            public KindDefault(MapIcon icon, Color color, float size)
            {
                Icon = icon;
                Color = color;
                Size = size;
            }
        }

        private static readonly Dictionary<MapKind, KindDefault> Defaults = new Dictionary<MapKind, KindDefault>
        {
            { MapKind.Chest, new KindDefault(MapIcon.ChestOutlined, Color.white, 24f) },
            { MapKind.GoldChest, new KindDefault(MapIcon.ChestOutlined, new Color(1f, 0.3f, 0.25f), 26f) },
            { MapKind.CategoryChest, new KindDefault(MapIcon.Chest, Color.white, 24f) },
            { MapKind.Shrine, new KindDefault(MapIcon.Shrine, Color.white, 24f) },
            { MapKind.NewtAltar, new KindDefault(MapIcon.LunarPillar, new Color(0.75f, 0.8f, 1f), 24f) },
            { MapKind.Drone, new KindDefault(MapIcon.Drone, Color.white, 20f) },
            { MapKind.DroneBroken, new KindDefault(MapIcon.Drone, new Color(0.62f, 0.62f, 0.68f), 20f) },
            { MapKind.Scrapper, new KindDefault(MapIcon.WIP, Color.white, 22f) },
            { MapKind.Printer, new KindDefault(MapIcon.Edit, Color.white, 22f) },
            { MapKind.Multishop, new KindDefault(MapIcon.Chest, Color.white, 22f) },
            { MapKind.MultishopTriple, new KindDefault(MapIcon.Loot, new Color(0.45f, 0.95f, 0.7f), 24f) },
            { MapKind.EquipmentBarrel, new KindDefault(MapIcon.Barrel, new Color(1f, 0.62f, 0.18f), 22f) },
            { MapKind.Barrel, new KindDefault(MapIcon.Barrel, Color.white, 20f) },
            { MapKind.Teleporter, new KindDefault(MapIcon.Teleporter, Color.white, 28f) },
            { MapKind.Portal, new KindDefault(MapIcon.Quickplay, Color.white, 22f) },
            { MapKind.VoidPotential, new KindDefault(MapIcon.Loot, new Color(0.8f, 0.55f, 1f), 20f) },
            { MapKind.CommandCube, new KindDefault(MapIcon.Loot, new Color(1f, 0.95f, 0.45f), 20f) },
            { MapKind.Relic, new KindDefault(MapIcon.Hex, new Color(0.55f, 1f, 0.8f), 22f) },
            { MapKind.GroundItem, new KindDefault(MapIcon.Dot, Color.white, 14f) },
            { MapKind.Enemy, new KindDefault(MapIcon.Attack, new Color(1f, 0.45f, 0.4f), 16f) },
            { MapKind.EnemyElite, new KindDefault(MapIcon.Attack, new Color(1f, 0.32f, 0.12f), 17f) },
            { MapKind.EnemyBoss, new KindDefault(MapIcon.Attack, new Color(1f, 0.12f, 0.1f), 20f) },
            { MapKind.Minion, new KindDefault(MapIcon.Drone, new Color(0.45f, 1f, 0.9f), 18f) },
            { MapKind.OtherPlayer, new KindDefault(MapIcon.Sprint, new Color(0.5f, 0.9f, 1f), 18f) },
            { MapKind.Other, new KindDefault(MapIcon.Mystery, Color.white, 20f) },
        };

        public static KindDefault DefaultFor(MapKind kind)
        {
            return Defaults.TryGetValue(kind, out var d) ? d : new KindDefault(MapIcon.Mystery, Color.white, 20f);
        }

        public static string Label(MapKind kind)
        {
            switch (kind)
            {
                case MapKind.Chest: return "Chest";
                case MapKind.GoldChest: return "Large Chest";
                case MapKind.CategoryChest: return "Category Chest";
                case MapKind.Shrine: return "Shrine";
                case MapKind.NewtAltar: return "Newt Altar";
                case MapKind.Drone: return "Drone";
                case MapKind.DroneBroken: return "Broken Drone";
                case MapKind.Scrapper: return "Scrapper";
                case MapKind.Printer: return "Printer / Cauldron";
                case MapKind.Multishop: return "Multishop";
                case MapKind.MultishopTriple: return "Multishop (Triple)";
                case MapKind.EquipmentBarrel: return "Equipment Barrel";
                case MapKind.Barrel: return "Barrel";
                case MapKind.Teleporter: return "Teleporter";
                case MapKind.Portal: return "Portal";
                case MapKind.VoidPotential: return "Void Potential";
                case MapKind.CommandCube: return "Command Cube";
                case MapKind.Relic: return "Hallowed Relic";
                case MapKind.GroundItem: return "Ground Item";
                case MapKind.Enemy: return "Enemy";
                case MapKind.EnemyElite: return "Elite Enemy";
                case MapKind.EnemyBoss: return "Boss";
                case MapKind.Minion: return "Minion";
                case MapKind.OtherPlayer: return "Other Player";
                default: return "Other";
            }
        }
    }
}
