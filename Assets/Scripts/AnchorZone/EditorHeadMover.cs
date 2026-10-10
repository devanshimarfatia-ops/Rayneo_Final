using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
using RayNeo; // HeadTrackedPoseDriver
#endif

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Editor-only: walk the head around so ZoneDetector can be tested without glasses.
    ///   WASD = walk (relative to where the head faces), Q / E = turn left / right.
    /// Attach to the camera. Does nothing in device builds.
    ///
    /// In the Editor the ARDK's HeadTrackedPoseDriver overwrites the camera transform every
    /// frame with its debug pose, so this script feeds that pose through
    /// HeadTrackedPoseDriver.SetPosition / SetQuaternion instead of moving the transform.
    /// </summary>
    public class EditorHeadMover : MonoBehaviour
    {
#pragma warning disable 0414 // only read in the Editor
        [SerializeField] private float speed = 1.2f;      // roughly walking pace, m/s
        [SerializeField] private float turnSpeed = 90f;   // deg/s for Q / E
#pragma warning restore 0414

#if UNITY_EDITOR
        private Vector3 position;
        private float yaw;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float turn = (kb.eKey.isPressed ? 1f : 0f) - (kb.qKey.isPressed ? 1f : 0f);
            yaw += turn * turnSpeed * Time.deltaTime;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

            Vector3 dir = new Vector3(
                (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
                0f,
                (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));
            position += rot * dir.normalized * speed * Time.deltaTime;

            HeadTrackedPoseDriver.SetPosition(position);
            HeadTrackedPoseDriver.SetQuaternion(rot);
        }
#endif
    }
}
