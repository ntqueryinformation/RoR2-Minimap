using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace OpenMinimap.Tracking
{
    // Loads the game's own sprites (the same MiscIcons set the vanilla ping system uses)
    // through LegacyResourcesAPI, with a cache. Unknown objects fall back to the game's
    // own ping-icon resolver, so new DLC content always gets a sensible icon.
    public static class IconLibrary
    {
        private const string MysteryIcon = "Textures/MiscIcons/texMysteryIcon";
        private const string ElevationIcon = "Textures/MiscIcons/texOptionsArrowLeft";

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> Warned = new HashSet<string>();

        public static Sprite ElevationChevron => Load(ElevationIcon);

        public static string PathFor(MapKind kind)
        {
            switch (kind)
            {
                case MapKind.Chest:
                case MapKind.GoldChest:
                case MapKind.Multishop:
                    return "Textures/MiscIcons/texInventoryIconOutlined";
                case MapKind.Shrine: return "Textures/MiscIcons/texShrineIconOutlined";
                case MapKind.NewtAltar: return "Textures/MiscIcons/texLunarPillarIcon";
                case MapKind.Drone:
                case MapKind.Minion:
                    return "Textures/MiscIcons/texDroneIconOutlined";
                case MapKind.Scrapper: return "Textures/MiscIcons/texWIPIcon";
                case MapKind.Printer: return "Textures/MiscIcons/texEditIcon";
                case MapKind.Barrel: return "Textures/MiscIcons/texBarrelIcon";
                case MapKind.Teleporter: return "Textures/MiscIcons/texTeleporterIconOutlined";
                case MapKind.Portal: return "Textures/MiscIcons/texQuickplay";
                case MapKind.VoidPotential:
                case MapKind.CommandCube:
                case MapKind.GroundItem:
                    return "Textures/MiscIcons/texLootIconOutlined";
                case MapKind.Enemy: return "Textures/MiscIcons/texAttackIcon";
                case MapKind.OtherPlayer: return "Textures/MiscIcons/texSprintIcon";
                default: return MysteryIcon;
            }
        }

        public static Sprite Load(string path)
        {
            if (Cache.TryGetValue(path, out var cached))
                return cached;
            Sprite sprite = null;
            try { sprite = LegacyResourcesAPI.Load<Sprite>(path); }
            catch (System.Exception) { /* treat as missing */ }
            if (sprite == null && Warned.Add(path))
                Plugin.Log.LogWarning($"Sprite not found: '{path}' (the game may have moved it; falling back to the ping icon).");
            Cache[path] = sprite; // cache misses too, to avoid repeated lookups
            return sprite;
        }

        public static void ResolveSprite(TrackedMarker m)
        {
            Sprite sprite = null;
            if (m.Kind == MapKind.GroundItem && m.Pickup != null)
            {
                var def = PickupCatalog.GetPickupDef(m.Pickup.pickupIndex);
                if (def != null)
                    sprite = def.iconSprite;
            }
            if (sprite == null)
                sprite = Load(PathFor(m.Kind));
            if (sprite == null && m.Source != null)
            {
                try { sprite = RoR2.UI.PingIndicator.GetInteractableIcon(m.Source.gameObject); }
                catch (System.Exception) { /* game's resolver can throw on odd objects */ }
            }
            m.Sprite = sprite != null ? sprite : Load(MysteryIcon);
        }

        public static float SizeFor(MapKind kind)
        {
            switch (kind)
            {
                case MapKind.Chest:
                case MapKind.GoldChest:
                case MapKind.Shrine:
                case MapKind.NewtAltar:
                    return 24f;
                case MapKind.Teleporter: return 28f;
                case MapKind.Multishop:
                case MapKind.Printer:
                case MapKind.Scrapper:
                case MapKind.Portal:
                    return 22f;
                case MapKind.Drone:
                case MapKind.Barrel:
                case MapKind.VoidPotential:
                case MapKind.CommandCube:
                    return 20f;
                case MapKind.Enemy: return 16f;
                case MapKind.Minion:
                case MapKind.OtherPlayer:
                case MapKind.GroundItem:
                    return 18f;
                default: return 20f;
            }
        }

        public static Color GetColor(TrackedMarker m)
        {
            switch (m.Kind)
            {
                case MapKind.Teleporter:
                    return m.Teleporter != null ? GetTeleporterColor(m.Teleporter) : Color.white;
                case MapKind.GoldChest: return new Color(1f, 0.82f, 0.25f);
                case MapKind.Shrine: return m.Gold ? new Color(1f, 0.85f, 0.3f) : Color.white;
                case MapKind.NewtAltar: return new Color(0.75f, 0.8f, 1f);
                case MapKind.Multishop: return new Color(0.6f, 1f, 0.75f);
                case MapKind.VoidPotential: return new Color(0.8f, 0.55f, 1f);
                case MapKind.CommandCube: return new Color(1f, 0.95f, 0.45f);
                case MapKind.Minion: return new Color(0.45f, 1f, 0.9f);
                case MapKind.OtherPlayer: return new Color(0.5f, 0.9f, 1f);
                case MapKind.Enemy:
                    if (m.IsBoss) return new Color(1f, 0.12f, 0.1f);
                    if (m.IsElite) return new Color(1f, 0.35f, 0.15f);
                    return new Color(1f, 0.45f, 0.4f);
                default: return Color.white;
            }
        }

        public static Color GetTeleporterColor(TeleporterInteraction tp)
        {
            if (tp.activationState >= TeleporterInteraction.ActivationState.Charged)
                return new Color(0.35f, 1f, 0.45f);
            if (tp.activationState >= TeleporterInteraction.ActivationState.Charging)
                return new Color(1f, 0.85f, 0.35f);
            return Color.white;
        }
    }
}
