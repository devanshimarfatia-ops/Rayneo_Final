using UnityEngine;

public class RecenterContent : MonoBehaviour
{
    public Transform head;        // drag XR Plugin → CameraOffset → Head here
    public Transform content;     // drag the Content object here

    private Vector3 startOffset;  // where the content sat relative to you at launch
    private float startYaw;

    void Start()
    {
        startYaw = head.eulerAngles.y;
        startOffset = Quaternion.Euler(0, -startYaw, 0) * (content.position - head.position);
    }

    // Hook this to a button's On Click
    public void Recenter()
    {
        float yaw = head.eulerAngles.y;   // turn only left/right, ignore head tilt
        content.position = head.position + Quaternion.Euler(0, yaw, 0) * startOffset;
        content.rotation = Quaternion.Euler(0, yaw - startYaw, 0);
    }
}