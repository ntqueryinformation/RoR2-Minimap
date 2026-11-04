using OpenMinimap.Config;
using OpenMinimap.Tracking;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace OpenMinimap.UI
{
    // Per-HUD driver: reads config live every frame, computes per-marker data relative to the
    // local player, and feeds the minimap and compass views. The scanner itself is shared.
    [DisallowMultipleComponent]
    public sealed class MinimapController : MonoBehaviour
    {
        private HUD _hud;
        private MinimapView _minimap;
        private CompassView _compass;

        private void Awake()
        {
            _hud = GetComponent<HUD>();
            // Parent to the HUD's canvas root, not mainUIPanel — the latter has a built-in
            // inset that would make every offset configurable only by guesswork.
            var parent = transform;
            if (_hud && _hud.mainUIPanel)
            {
                var canvas = _hud.mainUIPanel.GetComponentInParent<Canvas>();
                if (canvas)
                    parent = canvas.transform;
            }
            _minimap = new MinimapView(parent);
            _compass = new CompassView(parent);
            MapScanner.MarkerAdded += OnMarkerAdded;
            MapScanner.MarkerRemoved += OnMarkerRemoved;
            foreach (var m in MapScanner.All)
                OnMarkerAdded(m);
        }

        private void OnDestroy()
        {
            MapScanner.MarkerAdded -= OnMarkerAdded;
            MapScanner.MarkerRemoved -= OnMarkerRemoved;
        }

        private void OnMarkerAdded(TrackedMarker m)
        {
            m.Sprite = IconLibrary.ResolveSprite(m, Plugin.Config.Kinds[m.Kind].Icon.Value);
            _minimap.CreateIcon(m);
            _compass.CreateIcon(m);
        }

        private void OnMarkerRemoved(TrackedMarker m)
        {
            _minimap.DestroyIcon(m);
            _compass.DestroyIcon(m);
        }

        private void Update()
        {
            var cfg = Plugin.Config;

            var key = cfg.ToggleKey.Value;
            if (key.MainKey != KeyCode.None && key.IsDown())
                cfg.Enabled.Value = !cfg.Enabled.Value;

            MapScanner.Update();

            var rig = _hud && _hud.cameraRigController ? _hud.cameraRigController : null;
            var cam = rig && rig.sceneCam ? rig.sceneCam.transform : null;
            var body = rig ? rig.targetBody : null;
            bool active = cfg.Enabled.Value && cam != null && body != null;

            if (active)
                ComputeFrameData(cam, body);

            var mode = cfg.Display.Value;
            _minimap.SetVisible(active && mode != DisplayMode.Compass);
            _compass.SetVisible(active && mode != DisplayMode.Minimap);

            if (active)
            {
                _minimap.Refresh(cam);
                _compass.Refresh(cam);
            }
        }

        private void ComputeFrameData(Transform cam, CharacterBody body)
        {
            var cfg = Plugin.Config;
            float yaw = cam.eulerAngles.y;
            Vector3 origin = body.transform.position;
            foreach (var m in MapScanner.Visible)
            {
                if (!m.Transform)
                {
                    m.Show = false;
                    continue;
                }
                Vector3 d = m.Transform.position - origin;
                m.Delta = new Vector2(d.x, d.z);
                m.Distance = m.Delta.magnitude;
                m.YOffset = d.y;
                m.Bearing = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                m.BearingDelta = Mathf.DeltaAngle(m.Bearing, yaw);
                // Re-resolve every frame: kind upgrades (enemy → elite → boss portrait) and
                // pickup icons must follow immediately, and the lookup is a cheap dictionary hit.
                m.Sprite = IconLibrary.ResolveSprite(m, cfg.Kinds[m.Kind].Icon.Value);
                // You are the arrow at the center — don't render your own marker on top of it.
                m.Show = cfg.IsKindVisible(m.Kind) && !ReferenceEquals(m.Body, body);
            }
        }
    }
}
