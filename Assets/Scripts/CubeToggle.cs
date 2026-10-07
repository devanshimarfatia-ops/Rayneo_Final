using UnityEngine;

public class CubeToggle : MonoBehaviour
{
    [SerializeField] private GameObject cube;

    void Start()
    {
        // Make sure the cube is hidden at the start
        cube.SetActive(false);
    }

    public void ShowCube()
    {
        cube.SetActive(true);
    }
}