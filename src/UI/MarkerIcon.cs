using OpenMinimap.Tracking;
using UnityEngine;
using UnityEngine.UI;

namespace OpenMinimap.UI
{
    // Shared icon construction for both views: an Image for the object plus a small
    // child chevron (the game's own options-arrow, rotated) for elevation above/below.
    internal static class MarkerIcon
    {
        public readonly struct Instance
        {
            public readonly GameObject Root;
            public readonly Image Image;
            public readonly Image Elevation;
            public readonly RectTransform ElevationRect;

            public Instance(GameObject root, Image image, Image elevation, RectTransform elevationRect)
            {
                Root = root;
                Image = image;
                Elevation = elevation;
                ElevationRect = elevationRect;
            }
        }

        public static Instance Create(RectTransform parent, TrackedMarker m)
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

            return new Instance(go, img, cimg, crt);
        }

        public static void Destroy(ref Instance instance)
        {
            if (instance.Root)
                Object.Destroy(instance.Root);
        }
    }
}
