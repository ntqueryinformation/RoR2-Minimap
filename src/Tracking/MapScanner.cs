using System;
using System.Collections.Generic;
using System.Reflection;
using RoR2;
using UnityEngine;

namespace OpenMinimap.Tracking
{
    // Finds and classifies everything worth showing. Detection is component-based first
    // (ChestBehavior, ShrineBehavior base class, SummonMasterBehavior, ...), then refined by
    // PurchaseInteraction context/display tokens and finally by GameObject name hints — so
    // new DLC content is either classified correctly or at least stays visible under "Other".
    // Rescans are throttled; per-frame work is only availability/tint refresh and pruning.
    public static class MapScanner
    {
        private const float RescanInterval = 2f;
        private const float EventRescanCooldown = 0.5f;
        private const int MaxEnemyMarkers = 200;
        private const float TripleShopClusterRadius = 4f;    // terminals within this range form one triple shop
        private const float ShrineHideGraceSeconds = 10f;    // chance shrines go unavailable between paid uses
        private const float UsedHideGraceSeconds = 0.75f;    // everything else disappears shortly after being used

        private static readonly string[] RelicHints =
        {
            "Hex", "Reliquary", "Gong", "Nkuhana", "Hallowed", "Veil", "Compulsion", "Descent", "Concept",
        };

        private static readonly Dictionary<int, TrackedMarker> Markers = new Dictionary<int, TrackedMarker>(256);
        private static readonly List<TrackedMarker> VisibleList = new List<TrackedMarker>(256);
        private static readonly List<TrackedMarker> StaleBuffer = new List<TrackedMarker>(32);
        private static readonly Dictionary<Type, MemberInfo[]> PickupIndexMembers = new Dictionary<Type, MemberInfo[]>(64);
        private static float _nextRescan;
        private static float _eventRescanAt = float.MaxValue;
        private static bool _scannedOnce;

        public static event Action<TrackedMarker> MarkerAdded;
        public static event Action<TrackedMarker> MarkerRemoved;

        public static IReadOnlyList<TrackedMarker> Visible => VisibleList;

        public static IEnumerable<TrackedMarker> All => Markers.Values;

        public static void OnStageStarted()
        {
            Clear();
            ForceScan();
        }

        public static void QueueEventRescan()
        {
            _eventRescanAt = Time.unscaledTime + EventRescanCooldown;
        }

        public static void Update()
        {
            if (!_scannedOnce || Time.unscaledTime >= _nextRescan || Time.unscaledTime >= _eventRescanAt)
            {
                ForceScan();
                _nextRescan = Time.unscaledTime + RescanInterval;
                _eventRescanAt = float.MaxValue;
                _scannedOnce = true;
            }
            Refresh();
        }

        public static void ForceScan()
        {
            if (!SceneInfo.instance)
                return;
            var seen = new HashSet<int>(Markers.Count * 2);
            ScanPurchasables(seen);
            ScanTeleporter(seen);
            ScanGenericInteractions(seen);
            ScanPickups(seen);
            ScanCharacters(seen);
            RemoveUnseen(seen);
        }

        public static void Clear()
        {
            foreach (var m in Markers.Values)
                MarkerRemoved?.Invoke(m);
            Markers.Clear();
            VisibleList.Clear();
            _scannedOnce = false;
        }

        private static void RemoveUnseen(HashSet<int> seen)
        {
            StaleBuffer.Clear();
            foreach (var kv in Markers)
            {
                if (!seen.Contains(kv.Key))
                    StaleBuffer.Add(kv.Value);
            }
            foreach (var m in StaleBuffer)
            {
                Markers.Remove(m.Id);
                MarkerRemoved?.Invoke(m);
            }
        }

        private static TrackedMarker GetOrCreate(int id, Component source, MapKind kind)
        {
            if (Markers.TryGetValue(id, out var existing))
            {
                existing.Kind = kind;
                return existing;
            }
            var m = new TrackedMarker
            {
                Id = id,
                Source = source,
                Transform = source.transform,
                Kind = kind,
            };
            Markers[id] = m;
            MarkerAdded?.Invoke(m);
            return m;
        }

        private static void ScanPurchasables(HashSet<int> seen)
        {
            // First pass: classify everything so multishop clusters can be grouped.
            var classified = new List<(PurchaseInteraction pi, MapKind kind, bool tinted)>(64);
            foreach (var pi in UnityEngine.Object.FindObjectsOfType<PurchaseInteraction>())
            {
                if (!pi)
                    continue;
                var kind = Classify(pi, out bool tinted);
                classified.Add((pi, kind, tinted));
            }

            // Terminals with 2+ other terminals within range are the triple-shop structures —
            // they should collapse into a single marker instead of three stacked icons.
            var triples = new HashSet<int>();
            foreach (var entry in classified)
            {
                if (entry.kind != MapKind.Multishop)
                    continue;
                int near = 0;
                foreach (var other in classified)
                {
                    if (other.kind == MapKind.Multishop && !ReferenceEquals(other.pi, entry.pi) &&
                        WithinXZ(entry.pi.transform.position, other.pi.transform.position, TripleShopClusterRadius))
                        near++;
                }
                if (near >= 2)
                    triples.Add(entry.pi.GetInstanceID());
            }

            foreach (var entry in classified)
            {
                PurchaseInteraction pi = entry.pi;
                seen.Add(pi.GetInstanceID());
                var kind = entry.kind;
                if (kind == MapKind.Multishop && triples.Contains(pi.GetInstanceID()))
                {
                    if (MergeTripleShop(pi))
                        continue;
                    kind = MapKind.MultishopTriple;
                }
                var m = GetOrCreate(pi.GetInstanceID(), pi, kind);
                m.Purchase = pi;
                m.TintColor = Color.clear;
                if (kind == MapKind.Barrel)
                    m.Barrel = pi.GetComponent<BarrelInteraction>();
                if (entry.tinted)
                    m.TintColor = TintFor(pi);
            }
        }

        // Reuses an existing marker for a triple-shop cluster if one is nearby, preferring an
        // available terminal as its anchor. Returns true when this terminal was merged and
        // should not get a marker of its own.
        private static bool MergeTripleShop(PurchaseInteraction pi)
        {
            Vector3 pos = pi.transform.position;
            TrackedMarker rep = null;
            foreach (var kv in Markers)
            {
                var m = kv.Value;
                if (m.Kind != MapKind.MultishopTriple || !m.Transform)
                    continue;
                if (WithinXZ(pos, m.Transform.position, TripleShopClusterRadius))
                {
                    rep = m;
                    break;
                }
            }
            if (rep == null)
                return false;

            bool piAvailable = pi.available;
            bool repAvailable = rep.Purchase && rep.Purchase.available;
            if (piAvailable && !repAvailable)
            {
                rep.Source = pi;
                rep.Transform = pi.transform;
                rep.Purchase = pi;
                rep.Hidden = false;
                rep.Available = true;
                rep.UnavailableSince = -1f;
                rep.TintColor = TintFor(pi);
            }
            // Retire this terminal's own marker if a previous scan gave it one.
            int id = pi.GetInstanceID();
            if (Markers.TryGetValue(id, out var stale) && !ReferenceEquals(stale, rep))
            {
                Markers.Remove(id);
                MarkerRemoved?.Invoke(stale);
            }
            return true;
        }

        private static bool WithinXZ(Vector3 a, Vector3 b, float r)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz <= r * r;
        }

        private static void ScanTeleporter(HashSet<int> seen)
        {
            foreach (var tp in UnityEngine.Object.FindObjectsOfType<TeleporterInteraction>())
            {
                if (!tp)
                    continue;
                seen.Add(tp.GetInstanceID());
                var m = GetOrCreate(tp.GetInstanceID(), tp, MapKind.Teleporter);
                m.Teleporter = tp;
            }
        }

        private static void ScanGenericInteractions(HashSet<int> seen)
        {
            // Every GenericInteraction is tracked: portal tokens become Portals, Hallowed
            // Concepts-style names become Relics (hex icon), and anything else stays visible
            // as "Other" with the game's own ping icon — unknown content is never invisible.
            foreach (var gi in UnityEngine.Object.FindObjectsOfType<GenericInteraction>())
            {
                if (!gi)
                    continue;
                seen.Add(gi.GetInstanceID());
                string token = gi.contextToken;
                string name = gi.gameObject.name;
                MapKind kind;
                if (IsPortalToken(token) || IsPortalToken(name))
                    kind = MapKind.Portal;
                else if (MatchesRelic(token) || MatchesRelic(name))
                    kind = MapKind.Relic;
                else
                    kind = MapKind.Other;
                GetOrCreate(gi.GetInstanceID(), gi, kind);
            }
        }

        private static bool MatchesRelic(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            foreach (var hint in RelicHints)
            {
                if (s.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static void ScanPickups(HashSet<int> seen)
        {
            foreach (var gpc in UnityEngine.Object.FindObjectsOfType<GenericPickupController>())
            {
                if (!gpc)
                    continue;
                // Skip drops whose content hasn't been assigned yet — they'd otherwise
                // fall back to a generic icon and look like misc loot.
                if (!gpc.pickupIndex.isValid || gpc.pickupIndex == PickupIndex.none)
                    continue;
                seen.Add(gpc.GetInstanceID());
                var m = GetOrCreate(gpc.GetInstanceID(), gpc, MapKind.GroundItem);
                m.Pickup = gpc;
            }
        }

        private static void ScanCharacters(HashSet<int> seen)
        {
            int enemies = 0;
            foreach (var tc in UnityEngine.Object.FindObjectsOfType<TeamComponent>())
            {
                if (!tc)
                    continue;
                MapKind kind;
                switch (tc.teamIndex)
                {
                    case TeamIndex.Monster:
                    case TeamIndex.Lunar:
                    case TeamIndex.Void:
                        if (enemies >= MaxEnemyMarkers)
                            continue;
                        var enemyBody = tc.GetComponent<CharacterBody>();
                        kind = ClassifyEnemy(enemyBody);
                        break;
                    case TeamIndex.Player:
                    {
                        var b = tc.GetComponent<CharacterBody>();
                        if (!b)
                            continue;
                        if (b.isPlayerControlled)
                        {
                            kind = MapKind.OtherPlayer;
                        }
                        else
                        {
                            // Only count owned minions (purchased drones/turrets). Ability
                            // spawns like dash decoys also present player-team bodies, but
                            // without MinionOwnership they are skipped so they never leave
                            // ghost icons behind on the map.
                            var master = b.master;
                            var ownership = master ? master.GetComponent<MinionOwnership>() : null;
                            if (!ownership || ownership.ownerMaster == null)
                                continue;
                            kind = MapKind.Minion;
                        }
                        break;
                    }
                    default:
                        continue;
                }
                seen.Add(tc.GetInstanceID());
                var m = GetOrCreate(tc.GetInstanceID(), tc, kind);
                if (IsEnemyKind(kind))
                    enemies++;
                m.Body = tc.GetComponent<CharacterBody>();
            }
        }

        private static bool IsEnemyKind(MapKind kind)
        {
            return kind == MapKind.Enemy || kind == MapKind.EnemyElite || kind == MapKind.EnemyBoss;
        }

        private static MapKind ClassifyEnemy(CharacterBody body)
        {
            if (body && body.isBoss)
                return MapKind.EnemyBoss;
            if (body && body.isElite)
                return MapKind.EnemyElite;
            return MapKind.Enemy;
        }

        private static void Refresh()
        {
            VisibleList.Clear();
            StaleBuffer.Clear();
            foreach (var m in Markers.Values)
            {
                if (!m.Source)
                {
                    StaleBuffer.Add(m);
                    continue;
                }
                if (m.Barrel != null && m.Barrel.Networkopened)
                    m.Hidden = true;
                if (m.Purchase != null)
                {
                    // Used interactables disappear: once a purchase becomes unavailable and
                    // stays that way past its grace window, the marker is hidden. Shrines get
                    // a long grace because chance shrines lock for a few seconds between paid
                    // uses; everything else (chests, terminals, drones, printers, barrels)
                    // disappears almost immediately. Teleporter-locked shops reappear when
                    // they become available again.
                    bool avail = m.Purchase.available;
                    m.Available = avail;
                    if (avail)
                    {
                        m.UnavailableSince = -1f;
                        if (m.Hidden && !(m.Barrel != null && m.Barrel.Networkopened))
                            m.Hidden = false;
                    }
                    else
                    {
                        if (m.UnavailableSince < 0f)
                            m.UnavailableSince = Time.unscaledTime;
                        float grace = m.Kind == MapKind.Shrine ? ShrineHideGraceSeconds : UsedHideGraceSeconds;
                        if (Time.unscaledTime - m.UnavailableSince >= grace)
                            m.Hidden = true;
                    }
                }
                if (m.Body != null && IsEnemyKind(m.Kind))
                {
                    // Elites/bosses change mid-fight — keep their kind in sync so their
                    // icons (portrait) and colors follow.
                    var wanted = ClassifyEnemy(m.Body);
                    if (wanted != m.Kind)
                        m.Kind = wanted;
                }
                if (m.Teleporter != null)
                    m.TintColor = IconLibrary.GetTeleporterTint(m.Teleporter);
                if (!m.Hidden)
                    VisibleList.Add(m);
            }
            foreach (var m in StaleBuffer)
            {
                Markers.Remove(m.Id);
                MarkerRemoved?.Invoke(m);
            }
        }

        private static bool IsPortalToken(string token)
        {
            return !string.IsNullOrEmpty(token) &&
                   (token.IndexOf("PORTAL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    token.IndexOf("GOLDSHORE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    token.IndexOf("TOTEM", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static MapKind Classify(PurchaseInteraction pi, out bool tinted)
        {
            tinted = false;
            GameObject go = pi.gameObject;
            StringComparison ig = StringComparison.OrdinalIgnoreCase;

            var barrel = pi.GetComponent<BarrelInteraction>();
            if (barrel != null)
            {
                string bt = pi.contextToken;
                if ((!string.IsNullOrEmpty(bt) && bt.IndexOf("EQUIPMENTBARREL", ig) >= 0) ||
                    go.name.IndexOf("EquipmentBarrel", ig) >= 0)
                {
                    tinted = false; // equipment barrels are orange via their default kind color
                    return MapKind.EquipmentBarrel;
                }
                return MapKind.Barrel;
            }

            // Command cubes and void potentials — before the plain chest check,
            // since OptionChestBehavior derives from ChestBehavior.
            if (pi.GetComponent<OptionChestBehavior>())
                return go.name.IndexOf("Potential", ig) >= 0 ? MapKind.VoidPotential : MapKind.CommandCube;

            if (pi.GetComponent<ChestBehavior>())
            {
                if (go.name.IndexOf("GoldChest", ig) >= 0)
                    return MapKind.GoldChest;
                if (go.name.IndexOf("CategoryChest", ig) >= 0)
                {
                    tinted = true; // damage/healing/utility tier color
                    return MapKind.CategoryChest;
                }
                return MapKind.Chest;
            }

            // Shrines: the base class covers every DLC shrine; explicit checks catch the
            // SotS specials that don't derive from it.
            if (pi.GetComponent<ShrineBehavior>())
            {
                return MapKind.Shrine;
            }
            if (pi.GetComponent<HalcyoniteShrineInteractable>())
                return MapKind.Shrine;
            if (pi.GetComponent<ShrineRebirthController>())
                return MapKind.Shrine;
            if (pi.GetComponent<GeodeController>())
                return MapKind.Shrine;

            var statue = pi.GetComponent<PortalStatueBehavior>();
            if (statue != null && statue.portalType == PortalStatueBehavior.PortalType.Shop)
                return MapKind.NewtAltar;

            if (pi.GetComponent<SummonMasterBehavior>())
                return go.name.IndexOf("Broken", ig) >= 0 ? MapKind.DroneBroken : MapKind.Drone;
            if (pi.GetComponent<ScrapperController>())
                return MapKind.Scrapper;
            if (pi.isGoldShrine)
                return MapKind.Shrine;

            string token = pi.contextToken;
            if (!string.IsNullOrEmpty(token))
            {
                if (token.IndexOf("DUPLICATOR", ig) >= 0) return MapKind.Printer;
                if (token.IndexOf("TERMINAL", ig) >= 0)
                {
                    tinted = true; // tint by the item tier actually on offer
                    return MapKind.Multishop;
                }
                if (IsPortalToken(token)) return MapKind.Portal;
            }

            // Name hints last — they catch DLC content with new components but familiar names.
            string n = go.name;
            if (n.IndexOf("Duplicator", ig) >= 0 || n.IndexOf("Printer", ig) >= 0 || n.IndexOf("Cauldron", ig) >= 0) return MapKind.Printer;
            if (n.IndexOf("Multishop", ig) >= 0 || n.IndexOf("ShopTerminal", ig) >= 0)
            {
                tinted = true;
                return MapKind.Multishop;
            }
            if (n.IndexOf("Shrine", ig) >= 0) return MapKind.Shrine;
            if (n.IndexOf("Scrapper", ig) >= 0) return MapKind.Scrapper;
            if (n.IndexOf("Potential", ig) >= 0) return MapKind.VoidPotential;
            if (n.IndexOf("CommandCube", ig) >= 0) return MapKind.CommandCube;
            if (MatchesRelic(token) || MatchesRelic(n)) return MapKind.Relic;
            if (IsPortalToken(n)) return MapKind.Portal;
            if (n.IndexOf("Broken", ig) >= 0) return MapKind.DroneBroken;
            if (n.IndexOf("Drone", ig) >= 0) return MapKind.Drone;

            return MapKind.Other;
        }

        // Dynamic tint for markers classified as tier-dependent (category chests, multishops).
        private static Color32 TintFor(PurchaseInteraction pi)
        {
            string n = pi.gameObject.name;
            StringComparison ig = StringComparison.OrdinalIgnoreCase;
            if (n.IndexOf("Damage", ig) >= 0) return new Color32(255, 64, 64, 255);
            if (n.IndexOf("Healing", ig) >= 0) return new Color32(96, 255, 128, 255);
            if (n.IndexOf("Utility", ig) >= 0) return new Color32(96, 176, 255, 255);

            // Multishops: find the item actually on offer. Vanilla hides the pickup index in
            // private fields synced over the network, so sweep component members reflectively
            // (cached per type). This also survives DLC shop variants.
            var pickupIndex = ReadPickupIndex(pi.gameObject);
            if (pickupIndex.isValid)
            {
                var def = PickupCatalog.GetPickupDef(pickupIndex);
                if (def != null && def.baseColor != default)
                    return def.baseColor;
            }
            return Color.clear;
        }

        private static PickupIndex ReadPickupIndex(GameObject go)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var mb in go.GetComponents<MonoBehaviour>())
            {
                if (!mb)
                    continue;
                var type = mb.GetType();
                if (!PickupIndexMembers.TryGetValue(type, out var members))
                {
                    var found = new List<MemberInfo>(4);
                    foreach (var f in type.GetFields(Flags))
                        if (f.FieldType == typeof(PickupIndex))
                            found.Add(f);
                    foreach (var p in type.GetProperties(Flags))
                        if (p.PropertyType == typeof(PickupIndex) && p.CanRead && p.GetIndexParameters().Length == 0)
                            found.Add(p);
                    members = found.ToArray();
                    PickupIndexMembers[type] = members;
                }
                foreach (var member in members)
                {
                    try
                    {
                        var value = member is FieldInfo field
                            ? (PickupIndex)field.GetValue(mb)
                            : (PickupIndex)((PropertyInfo)member).GetValue(mb);
                        if (value.isValid)
                            return value;
                    }
                    catch (System.Exception) { /* skip members that fail to read */ }
                }
            }
            return PickupIndex.none;
        }
    }
}
