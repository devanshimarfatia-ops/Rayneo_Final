using System;
using UnityEngine;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Semantic label for a real-world area.
    /// To add a new zone: add a value here, then map it in ModeManager.MapZoneToMode()
    /// and add it to AnchorPlacementInput.MenuOrder.
    /// The int values are saved in zone_anchors.json, so never renumber existing entries.
    /// </summary>
    public enum ZoneType
    {
        Work = 0,    // e.g. office desk
        Bedroom = 1, // e.g. bed / sofa (was "Rest")
        Kitchen = 2
    }

    /// <summary>
    /// App mode that gameplay reacts to (via ModeManager.OnModeChanged).
    /// ZoneType -> AppMode mapping lives in ModeManager.MapZoneToMode().
    /// </summary>
    public enum AppMode
    {
        Default = 0, // outside any zone
        Work = 1,
        Bedroom = 2,
        Kitchen = 3
    }

    /// <summary>
    /// Serializable anchor record. Position is in SLAM world space,
    /// which is RELATIVE TO THE SESSION ORIGIN (see README: persistence caveat).
    /// </summary>
    [Serializable]
    public class ZoneAnchorData
    {
        public string id;
        public ZoneType zoneType;
        public Vector3 position;
        public long createdUnixSeconds;

        public ZoneAnchorData(ZoneType type, Vector3 pos)
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 8);
            zoneType = type;
            position = pos;
            createdUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
