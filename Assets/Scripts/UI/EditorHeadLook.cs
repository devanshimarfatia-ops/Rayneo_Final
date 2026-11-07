using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Editor-only stand-in for turning the head (no glasses): Q = look forward, E = look right at the map.
    /// Snaps only (arrow keys belong to InputAdapter). Disabled on device builds.
    /// Yaw of E matches TreasureMapView.mapYawDeg (default 55). Also sets the camera to the glasses' 30 deg view.
    /// </summary>
    public class EditorHeadLook : MonoBehaviour
    {
        [SerializeField] private float lookRightYawDeg = 55f;

        private Transform head;

        private void Start()
        {
            if (!Application.isEditor) { enabled = false; return; }
            head = UiFactory.FindHead();
            var cam = head != null ? head.GetComponent<Camera>() : null;
            if (cam != null) cam.fieldOfView = UiFactory.VerticalFovDeg(); // same 30 deg view as the glasses
        }

        private void Update()
        {
            if (head == null) return;
            if (Input.GetKeyDown(KeyCode.Q)) head.rotation = Quaternion.identity;
            if (Input.GetKeyDown(KeyCode.E)) head.rotation = Quaternion.Euler(0, lookRightYawDeg, 0);
        }
    }
}
