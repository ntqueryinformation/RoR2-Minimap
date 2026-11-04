using System.Collections.Generic;
using OpenMinimap.Config;
using OpenMinimap.Tracking;
using OpenMinimap.Util;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenMinimap.UI
{
    // Horizontal compass strip at the top-center of the screen. Cardinal and intercardinal
    // labels plus 15-degree ticks slide with camera yaw; object markers are projected by
    // bearing delta and pin to the ends of the strip when behind the player.
    public sealed class CompassView
    {
        private const float SpanDeg = 90f;  // total bearing range shown across the strip
        private const float Height = 30f;

        private static readonly string[] Names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        private static readonly float[] Angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        private readonly GameObject _root;
        private readonly RectTransform _rootRect;
        private readonly CanvasGroup _group;
        private readonly RectTransform _markerContainer;
        private readonly List<Label> _labels = new List<Label>(8);
        private readonly List<RectTransform> _ticks = new List<RectTransform>(24);
        private readonly Dictionary<int, MarkerIcon.Instance> _icons = new Dictionary<int, MarkerIcon.Instance>(256);

        private readonly struct Label
        {
            public readonly float Angle;
            public readonly bool Cardinal;
            public readonly RectTransform Rect;

            public Label(float angle, bool cardinal, RectTransform rect)
            {
                Angle = angle;
                Cardinal = cardinal;
                Rect = rect;
            }
        }

        public CompassView(Transform parent)
        {
            var white = TextureGen.MakeSprite(TextureGen.SolidAlpha(4));

            _root = new GameObject("OpenMinimap.Compass", typeof(RectTransform), typeof(CanvasGroup));
            _root.layer = 5;
            _rootRect = (RectTransform)_root.transform;
            _rootRect.SetParent(parent, false);
            _rootRect.anchorMin = new Vector2(0.5f, 1f);
            _rootRect.anchorMax = new Vector2(0.5f, 1f);
            _rootRect.pivot = new Vector2(0.5f, 1f);
            _rootRect.sizeDelta = new Vector2(480f, Height);
            _rootRect.anchoredPosition = new Vector2(0f, -24f);
            _group = _root.GetComponent<CanvasGroup>();

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdropGo.layer = 5;
            var backdropRect = (RectTransform)backdropGo.transform;
            backdropRect.SetParent(_rootRect, false);
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            var backdropImg = backdropGo.GetComponent<Image>();
            backdropImg.sprite = white;
            backdropImg.color = new Color(0.04f, 0.05f, 0.08f, 0.55f);
            backdropImg.raycastTarget = false;

            for (int i = 0; i < 8; i++)
                _labels.Add(MakeLabel(Names[i], Angles[i], i == 0));

            for (int i = 0; i < 24; i++)
                _ticks.Add(MakeTick());

            var containerGo = new GameObject("Markers", typeof(RectTransform));
            containerGo.layer = 5;
            _markerContainer = (RectTransform)containerGo.transform;
            _markerContainer.SetParent(_rootRect, false);
            _markerContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _markerContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _markerContainer.pivot = new Vector2(0.5f, 0.5f);
            _markerContainer.sizeDelta = Vector2.zero;
        }

        private Label MakeLabel(string text, float angle, bool cardinal)
        {
            var go = new GameObject("cardinal_" + text, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(_rootRect, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(30f, Height);
            var tmp = go.AddComponent<HGTextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = cardinal ? 15f : 11f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.color = cardinal
                ? new Color(0.95f, 0.85f, 0.7f, 1f)
                : new Color(0.9f, 0.9f, 0.95f, 0.7f);
            tmp.fontStyle = cardinal ? FontStyles.Bold : FontStyles.Normal;
            return new Label(angle, cardinal, rt);
        }

        private RectTransform MakeTick()
        {
            var go = new GameObject("tick", typeof(RectTransform), typeof(Image));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(_rootRect, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1.5f, 5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(0.9f, 0.9f, 0.95f, 0.35f);
            return rt;
        }

        public void SetVisible(bool visible)
        {
            if (_root.activeSelf != visible)
                _root.SetActive(visible);
        }

        public void CreateIcon(TrackedMarker m)
        {
            if (!_icons.ContainsKey(m.Id))
                _icons[m.Id] = MarkerIcon.Create(_markerContainer, m);
        }

        public void DestroyIcon(TrackedMarker m)
        {
            if (_icons.TryGetValue(m.Id, out var entry))
            {
                var inst = entry;
                MarkerIcon.Destroy(ref inst);
                _icons.Remove(m.Id);
            }
        }

        public void Refresh(Transform cam)
        {
            var cfg = Plugin.Config;

            float width = cfg.CompassWidth.Value;
            _rootRect.sizeDelta = new Vector2(width, Height);
            _rootRect.anchoredPosition = new Vector2(0f, -cfg.CompassOffsetY.Value);
            _group.alpha = cfg.Opacity.Value / 100f;

            float yaw = cam.eulerAngles.y;
            float pxPerDeg = width / SpanDeg;
            float halfSpan = SpanDeg * 0.5f;
            // Inverted scrolling flips which way markers and labels travel when you turn.
            float dir = cfg.CompassInvert.Value ? -1f : 1f;

            bool showIntercardinals = cfg.CompassIntercardinals.Value;
            bool showTicks = cfg.CompassTicks.Value;

            foreach (var label in _labels)
            {
                bool active = label.Cardinal || showIntercardinals;
                if (active)
                {
                    float d = dir * Mathf.DeltaAngle(label.Angle, yaw);
                    active = Mathf.Abs(d) <= halfSpan;
                    if (active)
                        label.Rect.anchoredPosition = new Vector2(d * pxPerDeg, 0f);
                }
                if (label.Rect.gameObject.activeSelf != active)
                    label.Rect.gameObject.SetActive(active);
            }

            for (int i = 0; i < _ticks.Count; i++)
            {
                var tick = _ticks[i];
                bool active = showTicks;
                if (active)
                {
                    float d = dir * Mathf.DeltaAngle(i * 15f, yaw);
                    active = Mathf.Abs(d) <= halfSpan;
                    if (active)
                        tick.anchoredPosition = new Vector2(d * pxPerDeg, -Height * 0.5f + 3.5f);
                }
                if (tick.gameObject.activeSelf != active)
                    tick.gameObject.SetActive(active);
            }

            bool clamp = cfg.CompassClampMarkers.Value;
            float edgeInset = 12f;
            foreach (var m in MapScanner.Visible)
            {
                if (!_icons.TryGetValue(m.Id, out var e) || e.Root == null)
                    continue;
                if (!m.Show)
                {
                    if (e.Root.activeSelf)
                        e.Root.SetActive(false);
                    continue;
                }

                float d = dir * m.BearingDelta;
                float x;
                bool clamped = false;
                if (Mathf.Abs(d) <= halfSpan)
                {
                    x = d * pxPerDeg;
                }
                else if (clamp)
                {
                    x = (d > 0f ? 1f : -1f) * (width * 0.5f - edgeInset);
                    clamped = true;
                }
                else
                {
                    if (e.Root.activeSelf)
                        e.Root.SetActive(false);
                    continue;
                }

                if (!e.Root.activeSelf)
                    e.Root.SetActive(true);
                e.Root.transform.localPosition = new Vector3(x, 0f, 0f);
                e.Root.transform.localRotation = Quaternion.identity;

                var rt = e.Image.rectTransform;
                float iconSize = IconLibrary.SizeFor(m.Kind) * cfg.IconScale.Value * (clamped ? 0.85f : 1f);
                if (!Mathf.Approximately(rt.sizeDelta.x, iconSize))
                    rt.sizeDelta = new Vector2(iconSize, iconSize);
                if ((Sprite)e.Image.sprite != (Sprite)m.Sprite)
                    e.Image.sprite = m.Sprite;

                Color c = IconLibrary.GetColor(m);
                if (m.Purchase != null && !m.Purchase.available)
                    c.a = 0.35f;
                if (clamped)
                    c.a *= 0.55f;
                e.Image.color = c;

                bool showElev = cfg.ElevationArrows.Value && Mathf.Abs(m.YOffset) > cfg.ElevationThreshold.Value;
                if (e.Elevation.gameObject.activeSelf != showElev)
                    e.Elevation.gameObject.SetActive(showElev);
                if (showElev)
                    e.ElevationRect.localRotation = Quaternion.Euler(0f, 0f, m.YOffset > 0f ? -90f : 90f);
            }
        }
    }
}
