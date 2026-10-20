using UnityEngine;
using UnityEngine.UI;

public class GalleryController : MonoBehaviour
{
    public GameObject galleryPanel;   // the panel to show/hide
    public GameObject galleryButton;  // the button that opens it
    public Image displayImage;        // where the artwork appears
    public Text captionText;          // optional title text
    public Sprite[] artworks;         // your artwork images
    public string[] captions;         // optional: one title per artwork

    private int index = 0;

    void Start()
    {
        galleryPanel.SetActive(false);
    }

    public void OpenGallery()
    {
        galleryPanel.SetActive(true);
        galleryButton.SetActive(false);
        Show(0);
    }

    public void CloseGallery()
    {
        galleryPanel.SetActive(false);
        galleryButton.SetActive(true);
    }

    public void Next()     => Show((index + 1) % artworks.Length);
    public void Previous() => Show((index - 1 + artworks.Length) % artworks.Length);

    void Show(int i)
    {
        if (artworks.Length == 0) return;
        index = i;
        displayImage.sprite = artworks[index];
        displayImage.preserveAspect = true;

        if (captionText != null && captions != null && index < captions.Length)
            captionText.text = captions[index];
    }
}