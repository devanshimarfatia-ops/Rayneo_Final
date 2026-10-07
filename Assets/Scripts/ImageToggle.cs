using UnityEngine;

public class ImageToggle : MonoBehaviour
{
    [SerializeField] private GameObject image;

    void Start()
    {
        image.SetActive(false);
    }

    public void ShowImage()
    {
        image.SetActive(true);
    }
}