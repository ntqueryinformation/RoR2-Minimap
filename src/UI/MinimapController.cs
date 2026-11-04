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
            var parent = _hud && _hud.mainUIPanel ? _hud.mainUIPanel.transform : transform;
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
            IconLibrary.ResolveSprite(m);
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
                m.Show = Plugin.Config.IsKindVisible(m.Kind);
            }
        }
    }
}
