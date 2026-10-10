using UnityEngine;

public class LazyFollow : MonoBehaviour
{
    public Transform head;              // drag XR Plugin → CameraOffset → Head here
    public float angleThreshold = 25f;  // how far you can turn before it follows
    public float followSpeed = 3f;      // how quickly it catches up

    private Vector3 offset;             // position relative to your head at launch
    private Quaternion startRotation;
    private float currentYaw;
    private bool following;

    void Start()
    {
        currentYaw = head.eulerAngles.y;
        offset = Quaternion.Euler(0, -currentYaw, 0) * (transform.position - head.position);
        startRotation = Quaternion.Euler(0, -currentYaw, 0) * transform.rotation;
    }

    void LateUpdate()
    {
        float headYaw = head.eulerAngles.y;
        float diff = Mathf.Abs(Mathf.DeltaAngle(currentYaw, headYaw));

        if (diff > angleThreshold) following = true;

        if (following)
        {
            currentYaw = Mathf.LerpAngle(currentYaw, headYaw, followSpeed * Time.deltaTime);
            if (Mathf.Abs(Mathf.DeltaAngle(currentYaw, headYaw)) < 2f) following = false;
        }

        Quaternion yawRot = Quaternion.Euler(0, currentYaw, 0);
        transform.position = head.position + yawRot * offset;
        transform.rotation = yawRot * startRotation;
    }
}