using System.Collections.Generic;
using OpenMinimap.Config;
using OpenMinimap.Tracking;
using OpenMinimap.Util;
using UnityEngine;
using UnityEngine.UI;

namespace OpenMinimap.UI
{
    // The radar window: a masked circle/square in the top-right corner. A rotating container
    // holds one icon per marker; in RotateWithPlayer mode the container spins with camera yaw
    // while icons counter-rotate to stay upright, and the player arrow is fixed pointing up.
    // Positions are fixed-scale (pixels per meter) relative to the player — no bounding-box
    // normalization, so icons never slide when distant objects enter the scan.
    public sealed class MinimapView
    {
        private readonly GameObject _root;
        private readonly RectTransform _rootRect;
        private readonly CanvasGroup _group;
        private readonly RectTransform _container;
        private readonly RectTransform _playerArrow;
        private readonly Image _backdrop;
        private readonly Image _maskImage;
        private readonly Image _border;
        private readonly Dictionary<int, MarkerIcon.Instance> _icons = new Dictionary<int, MarkerIcon.Instance>(256);
        private readonly Dictionary<int, RectTransform> _chargeRings = new Dictionary<int, RectTransform>(4);

        private static Sprite _circleSprite;
        private static Sprite _circleRingSprite;
        private static Sprite _squareRingSprite;
        private static Sprite _whiteSprite;

        public MinimapView(Transform parent)
        {
            EnsureSprites();

            _root = new GameObject("OpenMinimap.Minimap", typeof(RectTransform), typeof(CanvasGroup));
            _root.layer = 5;
            _rootRect = (RectTransform)_root.transform;
            _rootRect.SetParent(parent, false);
            _rootRect.anchorMin = new Vector2(1f, 1f);
            _rootRect.anchorMax = new Vector2(1f, 1f);
            _rootRect.pivot = new Vector2(1f, 1f);
            _rootRect.sizeDelta = new Vector2(280f, 280f);
            _group = _root.GetComponent<CanvasGroup>();

            _backdrop = AddStretched(_rootRect, "Backdrop", _whiteSprite, new Color(0.04f, 0.05f, 0.08f, 0.7f));

            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.layer = 5;
            var maskRect = (RectTransform)maskGo.transform;
            maskRect.SetParent(_rootRect, false);
            Stretch(maskRect);
            _maskImage = maskGo.GetComponent<Image>();
            _maskImage.raycastTarget = false;
            maskGo.GetComponent<Mask>().showMaskGraphic = false;

            var containerGo = new GameObject("Container", typeof(RectTransform));
            containerGo.layer = 5;
            _container = (RectTransform)containerGo.transform;
            _container.SetParent(maskRect, false);
            _container.anchorMin = new Vector2(0.5f, 0.5f);
            _container.anchorMax = new Vector2(0.5f, 0.5f);
            _container.pivot = new Vector2(0.5f, 0.5f);
            _container.sizeDelta = Vector2.zero;

            var arrowGo = new GameObject("PlayerArrow", typeof(RectTransform), typeof(Image));
            arrowGo.layer = 5;
            _playerArrow = (RectTransform)arrowGo.transform;
            _playerArrow.SetParent(_rootRect, false);
            _playerArrow.anchorMin = new Vector2(0.5f, 0.5f);
            _playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
            _playerArrow.pivot = new Vector2(0.5f, 0.5f);
            _playerArrow.sizeDelta = new Vector2(26f, 26f);
            var arrowImg = arrowGo.GetComponent<Image>();
            arrowImg.raycastTarget = false;
            arrowImg.sprite = IconLibrary.Load("Textures/MiscIcons/texOptionsArrowLeft");
            arrowImg.color = new Color(1f, 1f, 1f, 0.95f);
            _playerArrow.localRotation = Quaternion.Euler(0f, 0f, -90f); // the sprite points left; -90° turns it up

            _border = AddStretched(_rootRect, "Border", _circleRingSprite, new Color(0.85f, 0.85f, 0.9f, 0.8f));
        }

        public void SetVisible(bool visible)
        {
            if (_root.activeSelf != visible)
                _root.SetActive(visible);
        }

        public void CreateIcon(TrackedMarker m)
        {
            bool teleporter = m.Kind == MapKind.Teleporter;
            _icons[m.Id] = MarkerIcon.Create(_container, m, teleporter);
            if (teleporter)
                _chargeRings[m.Id] = MakeChargeRing();
        }

        private RectTransform MakeChargeRing()
        {
            var go = new GameObject("charge_radius", typeof(RectTransform), typeof(Image));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(_container, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localRotation = Quaternion.identity;
            var img = go.GetComponent<Image>();
            img.sprite = _circleRingSprite;
            img.raycastTarget = false;
            go.SetActive(false);
            return rt;
        }

        public void DestroyIcon(TrackedMarker m)
        {
            if (_icons.TryGetValue(m.Id, out var entry))
            {
                var inst = entry;
                MarkerIcon.Destroy(ref inst);
                _icons.Remove(m.Id);
            }
            if (_chargeRings.TryGetValue(m.Id, out var ring))
            {
                if (ring)
                    Object.Destroy(ring.gameObject);
                _chargeRings.Remove(m.Id);
            }
        }

        public void Refresh(Transform cam)
        {
            var cfg = Plugin.Config;

            int size = cfg.MapSize.Value;
            _rootRect.sizeDelta = new Vector2(size, size);
            _rootRect.anchoredPosition = new Vector2(-cfg.MapOffsetX.Value, -cfg.MapOffsetY.Value);
            _group.alpha = cfg.Opacity.Value / 100f;

            bool circle = cfg.Shape.Value == MapShape.Circle;
            Sprite maskSprite = circle ? _circleSprite : _whiteSprite;
            if (_maskImage.sprite != maskSprite)
            {
                _maskImage.sprite = maskSprite;
                _backdrop.sprite = maskSprite;
            }
            Sprite borderSprite = circle ? _circleRingSprite : _squareRingSprite;
            if (_border.sprite != borderSprite)
                _border.sprite = borderSprite;

            float yaw = cam.eulerAngles.y;
            bool rotate = cfg.Orientation.Value == MapOrientation.RotateWithPlayer;
            _container.localRotation = rotate ? Quaternion.Euler(0f, 0f, yaw) : Quaternion.identity;

            // North-up mode: rotate the player arrow to show camera facing.
            if (!rotate)
            {
                float r = yaw * Mathf.Deg2Rad;
                float arrowDeg = Mathf.Atan2(-Mathf.Cos(r), -Mathf.Sin(r)) * Mathf.Rad2Deg;
                _playerArrow.localRotation = Quaternion.Euler(0f, 0f, arrowDeg);
            }
            else
            {
                _playerArrow.localRotation = Quaternion.Euler(0f, 0f, -90f);
            }

            float half = size * 0.5f;
            float maxR = Mathf.Max(10f, half - 10f);
            float ppm = cfg.Zoom.Value;
            float iconScale = cfg.IconScale.Value;
            bool clamp = cfg.ClampToEdge.Value;
            bool elevEnabled = cfg.ElevationArrows.Value;
            float elevThreshold = cfg.ElevationThreshold.Value;
            Quaternion upright = rotate ? Quaternion.Euler(0f, 0f, -yaw) : Quaternion.identity;

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

                Vector2 pos = m.Delta * ppm;
                float r2 = pos.magnitude;
                bool clamped = false;
                if (r2 > maxR)
                {
                    if (!clamp)
                    {
                        if (e.Root.activeSelf)
                            e.Root.SetActive(false);
                        continue;
                    }
                    pos *= maxR / r2;
                    clamped = true;
                }

                if (!e.Root.activeSelf)
                    e.Root.SetActive(true);
                e.Root.transform.localPosition = pos;
                e.Root.transform.localRotation = upright;

                var rt = e.Image.rectTransform;
                float iconSize = cfg.Kinds[m.Kind].Size.Value * iconScale * (clamped ? 0.85f : 1f);
                if (!Mathf.Approximately(rt.sizeDelta.x, iconSize))
                    rt.sizeDelta = new Vector2(iconSize, iconSize);
                if ((Sprite)e.Image.sprite != (Sprite)m.Sprite)
                    e.Image.sprite = m.Sprite;

                Color c = IconLibrary.GetColor(m, cfg.Kinds[m.Kind].Color.Value);
                if (m.Purchase != null && !m.Purchase.available)
                    c.a = 0.35f; // opened/purchased interactables fade instead of disappearing
                e.Image.color = c;

                bool showElev = elevEnabled && Mathf.Abs(m.YOffset) > elevThreshold;
                if (e.Elevation.gameObject.activeSelf != showElev)
                    e.Elevation.gameObject.SetActive(showElev);
                if (showElev)
                {
                    // the chevron sprite points left: -90° up, +90° down
                    e.ElevationRect.localRotation = Quaternion.Euler(0f, 0f, m.YOffset > 0f ? -90f : 90f);
                }

                // Teleporter extras: holdout-zone radius ring and charge percentage.
                bool isTeleporter = m.Kind == MapKind.Teleporter && m.Teleporter != null;
                bool inRange = !clamped;
                if (_chargeRings.TryGetValue(m.Id, out var ring))
                {
                    float radius = isTeleporter && m.Teleporter.holdoutZoneController
                        ? m.Teleporter.holdoutZoneController.currentRadius
                        : 0f;
                    bool showRing = isTeleporter && inRange && radius > 1f && ppm * radius * 2f <= size * 3f;
                    if (ring.gameObject.activeSelf != showRing)
                        ring.gameObject.SetActive(showRing);
                    if (showRing)
                    {
                        ring.localPosition = pos;
                        float ringSize = radius * 2f * ppm;
                        if (!Mathf.Approximately(ring.sizeDelta.x, ringSize))
                            ring.sizeDelta = new Vector2(ringSize, ringSize);
                        var ringImg = ring.GetComponent<Image>();
                        Color rc = c;
                        rc.a = 0.3f;
                        if (ringImg.color != rc)
                            ringImg.color = rc;
                    }
                }
                if (e.ChargeText != null)
                {
                    bool charging = isTeleporter && inRange && m.Teleporter.isCharging;
                    if (e.ChargeRect.gameObject.activeSelf != charging)
                        e.ChargeRect.gameObject.SetActive(charging);
                    if (charging)
                    {
                        int pct = Mathf.RoundToInt(Mathf.Clamp01(m.Teleporter.chargeFraction) * 100f);
                        string text = pct + "%";
                        if (e.ChargeText.text != text)
                            e.ChargeText.text = text;
                    }
                }
            }
        }

        private static void EnsureSprites()
        {
            if (_circleSprite != null)
                return;
            _circleSprite = TextureGen.MakeSprite(TextureGen.CircleAlpha(256));
            _circleRingSprite = TextureGen.MakeSprite(TextureGen.RingAlpha(256, 3f));
            _squareRingSprite = TextureGen.MakeSprite(TextureGen.RectRingAlpha(256, 3f));
            _whiteSprite = TextureGen.MakeSprite(TextureGen.SolidAlpha(4));
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Image AddStretched(RectTransform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
