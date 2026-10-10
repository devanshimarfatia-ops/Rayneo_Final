using System;
using UnityEngine;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Semantic label for a real-world area.
    /// To add a new zone: add a value here, then map it in ModeManager.MapZoneToMode().
    /// </summary>
    public enum ZoneType
    {
        Work = 0,   // e.g. office desk
        Rest = 1,   // e.g. sofa / bed
        Kitchen = 2 // placeholder for future modes
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
