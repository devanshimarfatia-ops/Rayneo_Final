using UnityEngine;

public class CubeRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private bool isRotating = false;

    void Update()
    {
        if (isRotating)
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    public void ToggleRotation() => isRotating = !isRotating;
    public void StartRotation()  => isRotating = true;
    public void StopRotation()   => isRotating = false;
}