using RayNeo; // HeadTrackedPoseDriver
using UnityEngine;

namespace RayneoIdle.Todo
{
    /// <summary>
    /// Drop-in component for the voice todo test scene: add it next to the RayNeo "XR Plugin" prefab.
    /// On Awake it creates VoiceTodoTestPanel (with its head-locked canvas), wired.
    /// Voice input goes through the RayNeo system To-do app (see SystemTodoClient.StartVoiceCreate).
    /// All tuning lives on this component.
    /// </summary>
    [DisallowMultipleComponent]
    public class VoiceTodoTestBootstrap : MonoBehaviour
    {
        [Tooltip("Head camera. Leave empty to use the XR Plugin's HeadTrackedPoseDriver camera (or Camera.main).")]
        [SerializeField] private Transform head;

        [Header("Voice (system To-do app)")]
        [Tooltip("OPEN TIMEOUT (s): after a tap, the system voice screen must take focus within this time, " +
                 "otherwise the tap is reset and a 'did not open' toast is shown.")]
        [SerializeField] private float systemOpenTimeoutSeconds = 3f;

        [Header("Input / display")]
        [Tooltip("Flip if swiping forward on the temple moves the selection the wrong way.")]
        [SerializeField] private bool invertSwipe = false;
        [Tooltip("Force the camera to clear to solid black (#000000 = transparent on the glasses).")]
        [SerializeField] private bool forceBlackBackground = true;

        private void Awake()
        {
            if (FindObjectOfType<VoiceTodoTestPanel>() != null)
            {
                Debug.LogWarning("[VoiceTodoTestBootstrap] A VoiceTodoTestPanel already exists in the scene, skipping.");
                return;
            }

            if (head == null) head = FindHead();
            if (head == null)
            {
                Debug.LogError("[VoiceTodoTestBootstrap] No head camera found. Add the RayNeo 'XR Plugin' prefab to the scene.");
                return;
            }

            var cam = head.GetComponent<Camera>();
            if (forceBlackBackground && cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }

            // Build inactive, wire, then activate, so OnEnable runs with references set.
            var system = new GameObject("VoiceTodoSystem");
            system.SetActive(false);
            system.transform.SetParent(transform, false);

            var panel = system.AddComponent<VoiceTodoTestPanel>();
            panel.Configure(head, invertSwipe, systemOpenTimeoutSeconds);

            system.SetActive(true);
            Debug.Log("[VoiceTodoTestBootstrap] Voice todo test created (system To-do voice flow)");
        }

        private static Transform FindHead()
        {
            var driver = FindObjectOfType<HeadTrackedPoseDriver>();
            if (driver != null) return driver.transform;
            return Camera.main != null ? Camera.main.transform : null;
        }
    }
}