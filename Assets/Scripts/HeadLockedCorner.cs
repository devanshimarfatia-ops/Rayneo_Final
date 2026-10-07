using UnityEngine;

/// <summary>
/// Keeps this object anchored to a corner of the viewer's field of view,
/// following the camera (head) with optional smoothing so it doesn't feel
/// perfectly rigid. Attach this to the cube/button/icon you want to follow
/// the head on RayNeo X3 Pro or any AR camera rig.
/// </summary>
public class HeadLockedCorner : MonoBehaviour
{
    [Header("Camera reference")]
    [Tooltip("Leave empty to auto-find Camera.main")]
    public Transform headCamera;

    [Header("Corner offset (local to camera)")]
    [Tooltip("X: negative = left, positive = right")]
    public float horizontalOffset = -0.25f;
    [Tooltip("Y: negative = bottom, positive = top")]
    public float verticalOffset = 0.18f;
    [Tooltip("Distance in front of the camera")]
    public float forwardDistance = 1.0f;

    [Header("Follow behavior")]
    [Tooltip("0 = instant rigid head-lock, higher = more lag/smoothing")]
    [Range(0f, 20f)]
    public float positionSmoothing = 8f;
    [Range(0f, 20f)]
    public float rotationSmoothing = 8f;

    [Tooltip("If true, object also rotates to always face the camera")]
    public bool faceCamera = true;

    void Start()
    {
        if (headCamera == null && Camera.main != null)
            headCamera = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (headCamera == null) return;

        // Compute the target position relative to the camera's current facing
        Vector3 targetPosition = headCamera.position
            + headCamera.forward * forwardDistance
            + headCamera.right * horizontalOffset
            + headCamera.up * verticalOffset;

        // Smoothly move toward that target (set positionSmoothing to 0 for a rigid lock)
        if (positionSmoothing <= 0f)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                1f - Mathf.Exp(-positionSmoothing * Time.deltaTime)
            );
        }

        if (faceCamera)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                transform.position - headCamera.position
            );

            if (rotationSmoothing <= 0f)
            {
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime)
                );
            }
        }
    }
}