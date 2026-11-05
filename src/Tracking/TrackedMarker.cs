using RoR2;
using UnityEngine;

namespace OpenMinimap.Tracking
{
    // One entry per tracked world object, shared by every view (minimap + compass).
    // Per-frame relative data (Delta, Bearing...) is recomputed by MinimapController
    // for the local player before the views read it.
    public sealed class TrackedMarker
    {
        public int Id;
        public Component Source;
        public Transform Transform;
        public MapKind Kind;

        // Optional fast-path references used for classification, availability and tinting.
        public PurchaseInteraction Purchase;
        public BarrelInteraction Barrel;
        public TeleporterInteraction Teleporter;
        public GenericPickupController Pickup;
        public CharacterBody Body;
        public bool Hidden; // opened barrels and the like are dropped from the visible list entirely

        public bool Show; // per-frame result of the per-kind config toggles

        // Dynamic tint applied on top of the kind's configured color (multishop tier,
        // category chest, teleporter charge state). Color.clear means "no tint".
        public Color TintColor = Color.clear;

        // Relative to the viewing player:
        public Vector2 Delta;      // world (x, z) offset
        public float Distance;     // horizontal distance in meters
        public float YOffset;      // vertical offset in meters
        public float Bearing;      // absolute bearing, degrees, 0 = north (+z)
        public float BearingDelta; // bearing relative to the camera yaw
        public bool Available = true;
        public float UnavailableSince = -1f; // used-hide grace timing (chance shrines toggle this between paid uses)

        public Sprite Sprite; // re-resolved every frame by the controller (cheap lookups)
    }
}
