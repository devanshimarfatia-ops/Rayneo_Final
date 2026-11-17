using UnityEngine;

// Keeps a World Space canvas (the level's Gallery UI: To-Do card, artwork, gallery grid) centred in front
// of the player, without locking it to the head.
//   - Every time it is shown (level started, back from the menu) it snaps straight in front of you
//     (again one frame later, once the head pose for that frame is known).
//   - While you look around it stays completely still. Once you turn more than Angle Threshold away, or
//     walk more than Distance Tolerance away, it picks ONE new spot in front of you and glides there,
//     then stops again. It never chases the live head pose, so head-tracking noise cannot shake it.
//   - Recenter() snaps it immediately (hook it to the Gallery / Back buttons).
// Only position + yaw are changed; the canvas keeps its own scale. Put this on the canvas, not on the island.
public class KeepInFront : MonoBehaviour
{
    [Tooltip("Head camera (XR Plugin > CameraOffset > Head). Empty = Camera.main.")]
    public Transform head;
    [Tooltip("Distance in front of the eyes (m). The X3 Pro is comfortable at about 1.5 - 2 m.")]
    public float distance = 1.5f;
    [Tooltip("Height relative to eye level (m). Negative = a little lower.")]
    public float heightOffset = 0f;

    [Header("Lazy follow")]
    public bool lazyFollow = true;
    [Tooltip("Start moving back when the UI is more than this many degrees off to the side.")]
    public float angleThreshold = 25f;
    [Tooltip("Start moving back when you have walked this far (m) from where the UI expects you.")]
    public float distanceTolerance = 0.6f;
    [Tooltip("Time (s) the glide back takes, roughly.")]
    public float glideTime = 0.35f;

    private Vector3 targetPos, velocity;
    private Quaternion targetRot;
    private bool gliding;
    private int recenterNextFrame;

    void OnEnable()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        Recenter();
        recenterNextFrame = 2; // the head pose of the activation frame can still be the old one
    }

    /// <summary>Snap the UI straight in front of the player now.</summary>
    public void Recenter()
    {
        if (head == null) return;
        ComputeTarget();
        transform.SetPositionAndRotation(targetPos, targetRot);
        velocity = Vector3.zero;
        gliding = false;
    }

    void LateUpdate()
    {
        if (head == null) return;
        if (recenterNextFrame > 0 && --recenterNextFrame == 0) { Recenter(); return; }
        if (!lazyFollow) return;

        if (gliding)
        {
            // Glide to the spot chosen when the glide started (fixed target = no shaking).
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, glideTime);
            float k = 1f - Mathf.Exp(-Time.deltaTime * 3f / Mathf.Max(0.05f, glideTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, k);
            if ((transform.position - targetPos).sqrMagnitude < 0.0001f)
            {
                transform.SetPositionAndRotation(targetPos, targetRot);
                gliding = false;
            }
            return;
        }

        // Resting: check whether the player has turned / walked away far enough.
        Vector3 toUi = transform.position - head.position;
        Vector3 toUiFlat = new Vector3(toUi.x, 0f, toUi.z);
        float angle = Vector3.Angle(toUiFlat, FlatForward());
        float distError = Mathf.Abs(toUiFlat.magnitude - distance);
        if (angle > angleThreshold || distError > distanceTolerance)
        {
            ComputeTarget(); // one new spot, then glide there
            gliding = true;
        }
    }

    void ComputeTarget()
    {
        Vector3 fwd = FlatForward();
        targetPos = head.position + fwd * distance + Vector3.up * heightOffset;
        targetRot = Quaternion.LookRotation(fwd, Vector3.up); // upright, facing away from the eyes = readable
    }

    // Head forward with the up/down part removed, so the UI stays upright at eye height.
    Vector3 FlatForward()
    {
        Vector3 f = head.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) { f = head.up; f.y = 0f; } // looking straight down/up
        return f.normalized;
    }
}