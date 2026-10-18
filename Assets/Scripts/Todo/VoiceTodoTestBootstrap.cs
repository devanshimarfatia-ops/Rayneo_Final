using RayNeo; // HeadTrackedPoseDriver
using UnityEngine;

namespace RayneoIdle.Todo
{
    /// <summary>
    /// Drop-in component for the voice todo test scene: add it next to the RayNeo "XR Plugin" prefab.
    /// On Awake it creates VoiceInput + VoiceTodoTestPanel (with its head-locked canvas), all wired.
    /// All tuning lives on this component.
    /// </summary>
    [DisallowMultipleComponent]
    public class VoiceTodoTestBootstrap : MonoBehaviour
    {
        [Tooltip("Head camera. Leave empty to use the XR Plugin's HeadTrackedPoseDriver camera (or Camera.main).")]
        [SerializeField] private Transform head;

        [Header("Voice")]
        [Tooltip("Recognizer language. The game is English-only, keep en-US. Empty = system language.")]
        [SerializeField] private string language = "en-US";
        [Tooltip("SAFETY TIMEOUT (s): a session with no result/error after this long is cancelled.")]
        [SerializeField] private float sessionTimeoutSeconds = 15f;

        [Header("Input / display")]
        [Tooltip("Flip if swiping forward on the temple moves the selection the wrong way.")]
        [SerializeField] private bool invertSwipe = false;
        [Tooltip("Force the camera to clear to solid black (#000000 = transparent on the glasses).")]
        [SerializeField] private bool forceBlackBackground = true;

        private void Awake()
        {
            if (FindObjectOfType<VoiceInput>() != null)
            {
                Debug.LogWarning("[VoiceTodoTestBootstrap] A VoiceInput already exists in the scene, skipping.");
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

            var voice = system.AddComponent<VoiceInput>();
            var panel = system.AddComponent<VoiceTodoTestPanel>();
            voice.Configure(language, sessionTimeoutSeconds);
            panel.Configure(voice, head, invertSwipe);

            system.SetActive(true);
            Debug.Log($"[VoiceTodoTestBootstrap] Voice todo test created (language '{language}')");
        }

        private static Transform FindHead()
        {
            var driver = FindObjectOfType<HeadTrackedPoseDriver>();
            if (driver != null) return driver.transform;
            return Camera.main != null ? Camera.main.transform : null;
        }
    }
}
