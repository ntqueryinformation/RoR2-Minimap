using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace OpenMinimap.Tracking
{
    // Finds and classifies everything worth showing. Detection is component-based first
    // (ChestBehavior, ShrineBehavior base class, SummonMasterBehavior, ...), then refined by
    // PurchaseInteraction context/display tokens and finally by GameObject name hints — so
    // new DLC content is either classified correctly or at least stays visible under "Other".
    // Rescans are throttled; per-frame work is only availability/elite refresh and pruning.
    public static class MapScanner
    {
        private const float RescanInterval = 2f;
        private const float EventRescanCooldown = 0.5f;
        private const int MaxEnemyMarkers = 200;

        private static readonly Dictionary<int, TrackedMarker> Markers = new Dictionary<int, TrackedMarker>(256);
        private static readonly List<TrackedMarker> VisibleList = new List<TrackedMarker>(256);
        private static readonly List<TrackedMarker> StaleBuffer = new List<TrackedMarker>(32);
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
            foreach (var pi in UnityEngine.Object.FindObjectsOfType<PurchaseInteraction>())
            {
                if (!pi)
                    continue;
                seen.Add(pi.GetInstanceID());
                var kind = Classify(pi, out bool gold);
                var m = GetOrCreate(pi.GetInstanceID(), pi, kind);
                m.Purchase = pi;
                m.Gold = gold;
                if (kind == MapKind.Barrel)
                    m.Barrel = pi.GetComponent<BarrelInteraction>();
            }
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
            foreach (var gi in UnityEngine.Object.FindObjectsOfType<GenericInteraction>())
            {
                if (!gi)
                    continue;
                string token = gi.contextToken;
                if (!IsPortalToken(token))
                    continue;
                seen.Add(gi.GetInstanceID());
                GetOrCreate(gi.GetInstanceID(), gi, MapKind.Portal);
            }
        }

        private static void ScanPickups(HashSet<int> seen)
        {
            foreach (var gpc in UnityEngine.Object.FindObjectsOfType<GenericPickupController>())
            {
                if (!gpc)
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
                        kind = MapKind.Enemy;
                        break;
                    case TeamIndex.Player:
                    {
                        var b = tc.GetComponent<CharacterBody>();
                        if (!b)
                            continue;
                        kind = b.isPlayerControlled ? MapKind.OtherPlayer : MapKind.Minion;
                        break;
                    }
                    default:
                        continue;
                }
                seen.Add(tc.GetInstanceID());
                var m = GetOrCreate(tc.GetInstanceID(), tc, kind);
                if (kind == MapKind.Enemy)
                    enemies++;
                m.Body = tc.GetComponent<CharacterBody>();
                m.IsBoss = m.Body && m.Body.isBoss;
                m.IsElite = m.Body && m.Body.isElite;
            }
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
                    m.Available = m.Purchase.available;
                if (m.Body != null)
                {
                    m.IsBoss = m.Body.isBoss;
                    m.IsElite = m.Body.isElite;
                }
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

        private static MapKind Classify(PurchaseInteraction pi, out bool gold)
        {
            gold = false;
            GameObject go = pi.gameObject;
            StringComparison ig = StringComparison.OrdinalIgnoreCase;

            var barrel = pi.GetComponent<BarrelInteraction>();
            if (barrel != null)
                return MapKind.Barrel;

            // Command cubes and void potentials — before the plain chest check,
            // since OptionChestBehavior derives from ChestBehavior.
            if (pi.GetComponent<OptionChestBehavior>())
                return go.name.IndexOf("Potential", ig) >= 0 ? MapKind.VoidPotential : MapKind.CommandCube;

            if (pi.GetComponent<ChestBehavior>())
            {
                if (go.name.IndexOf("GoldChest", ig) >= 0)
                {
                    gold = true;
                    return MapKind.GoldChest;
                }
                return MapKind.Chest;
            }

            // Shrines: the base class covers every DLC shrine; explicit checks catch the
            // SotS specials that don't derive from it.
            if (pi.GetComponent<ShrineBehavior>())
            {
                if (pi.GetComponent<ShrineBossBehavior>())
                    gold = true;
                return MapKind.Shrine;
            }
            if (pi.GetComponent<HalcyoniteShrineInteractable>())
            {
                gold = true;
                return MapKind.Shrine;
            }
            if (pi.GetComponent<ShrineRebirthController>())
                return MapKind.Shrine;
            if (pi.GetComponent<GeodeController>())
                return MapKind.Shrine;

            var statue = pi.GetComponent<PortalStatueBehavior>();
            if (statue != null && statue.portalType == PortalStatueBehavior.PortalType.Shop)
                return MapKind.NewtAltar;

            if (pi.GetComponent<SummonMasterBehavior>())
                return MapKind.Drone;
            if (pi.GetComponent<ScrapperController>())
                return MapKind.Scrapper;
            if (pi.isGoldShrine)
            {
                gold = true;
                return MapKind.Shrine;
            }

            string token = pi.contextToken;
            if (!string.IsNullOrEmpty(token))
            {
                if (token.IndexOf("DUPLICATOR", ig) >= 0) return MapKind.Printer;
                if (token.IndexOf("TERMINAL", ig) >= 0) return MapKind.Multishop;
                if (IsPortalToken(token)) return MapKind.Portal;
            }

            // Name hints last — they catch DLC content with new components but familiar names.
            string n = go.name;
            if (n.IndexOf("Duplicator", ig) >= 0 || n.IndexOf("Printer", ig) >= 0) return MapKind.Printer;
            if (n.IndexOf("Multishop", ig) >= 0 || n.IndexOf("ShopTerminal", ig) >= 0) return MapKind.Multishop;
            if (n.IndexOf("Shrine", ig) >= 0) return MapKind.Shrine;
            if (n.IndexOf("Scrapper", ig) >= 0) return MapKind.Scrapper;
            if (n.IndexOf("Potential", ig) >= 0) return MapKind.VoidPotential;
            if (n.IndexOf("CommandCube", ig) >= 0) return MapKind.CommandCube;
            if (IsPortalToken(n)) return MapKind.Portal;
            if (n.IndexOf("Drone", ig) >= 0) return MapKind.Drone;

            return MapKind.Other;
        }
    }
}
