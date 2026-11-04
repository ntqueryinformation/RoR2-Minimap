using OpenMinimap.Tracking;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenMinimap.UI
{
    // Shared icon construction for both views: an Image for the object plus a small
    // child chevron (the game's own options-arrow, rotated) for elevation above/below.
    // Teleporter markers additionally get a charge-percentage label below the icon.
    internal static class MarkerIcon
    {
        public readonly struct Instance
        {
            public readonly GameObject Root;
            public readonly Image Image;
            public readonly Image Elevation;
            public readonly RectTransform ElevationRect;
            public readonly RectTransform ChargeRect;
            public readonly HGTextMeshProUGUI ChargeText;

            public Instance(GameObject root, Image image, Image elevation, RectTransform elevationRect,
                RectTransform chargeRect, HGTextMeshProUGUI chargeText)
            {
                Root = root;
                Image = image;
                Elevation = elevation;
                ElevationRect = elevationRect;
                ChargeRect = chargeRect;
                ChargeText = chargeText;
            }
        }

        public static Instance Create(RectTransform parent, TrackedMarker m, bool withChargeLabel = false)
        {
            var go = new GameObject("icon_" + m.Kind, typeof(RectTransform), typeof(Image));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.maskable = true;
            img.sprite = m.Sprite;

            var chev = new GameObject("elevation", typeof(RectTransform), typeof(Image));
            chev.layer = 5;
            var crt = (RectTransform)chev.transform;
            crt.SetParent(rt, false);
            crt.anchorMin = new Vector2(0.5f, 1f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(9f, 9f);
            crt.anchoredPosition = new Vector2(0f, 7f);
            var cimg = chev.GetComponent<Image>();
            cimg.raycastTarget = false;
            cimg.sprite = IconLibrary.ElevationChevron;
            cimg.color = new Color(1f, 0.95f, 0.55f, 0.95f);
            chev.SetActive(false);

            RectTransform chargeRect = null;
            HGTextMeshProUGUI chargeText = null;
            if (withChargeLabel)
            {
                var charge = new GameObject("charge", typeof(RectTransform));
                charge.layer = 5;
                chargeRect = (RectTransform)charge.transform;
                chargeRect.SetParent(rt, false);
                chargeRect.anchorMin = new Vector2(0.5f, 0f);
                chargeRect.anchorMax = new Vector2(0.5f, 0f);
                chargeRect.pivot = new Vector2(0.5f, 1f);
                chargeRect.sizeDelta = new Vector2(80f, 16f);
                chargeRect.anchoredPosition = new Vector2(0f, -4f);
                chargeText = charge.AddComponent<HGTextMeshProUGUI>();
                chargeText.fontSize = 11f;
                chargeText.alignment = TextAlignmentOptions.Center;
                chargeText.raycastTarget = false;
                chargeText.color = new Color(1f, 1f, 1f, 0.95f);
                chargeText.text = "0%";
                charge.SetActive(false);
            }

            return new Instance(go, img, cimg, crt, chargeRect, chargeText);
        }

        public static void Destroy(ref Instance instance)
        {
            if (instance.Root)
                Object.Destroy(instance.Root);
        }
    }
}
