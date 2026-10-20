using UnityEngine;
using UnityEngine.UI;

public class GalleryGrid : MonoBehaviour
{
    [System.Serializable]
    public class Tile
    {
        public Button button;        // the tile itself
        public Image artworkImage;   // child Image that shows the artwork
        public GameObject lockIcon;  // child Image with the lock sprite
        public Sprite artwork;       // the artwork earned for this tile
        public bool unlocked;        // tick to start unlocked
    }

    [Header("Gallery")]
    public GameObject galleryPanel;  // the 3x3 grid panel
    public GameObject hud;           // to-do + artwork preview, hidden while the gallery is open
    public Tile[] tiles = new Tile[9];

    [Header("Full-size viewer (optional)")]
    public GameObject viewerPanel;
    public Image viewerImage;

    void Start()
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            int index = i; // capture for the click callback
            if (tiles[i].button != null)
                tiles[i].button.onClick.AddListener(() => OnTileClicked(index));
        }

        galleryPanel.SetActive(false);
        if (viewerPanel != null) viewerPanel.SetActive(false);
        Refresh();
    }

    // Hook to the artwork preview button
    public void OpenGallery()
    {
        if (hud != null) hud.SetActive(false);
        galleryPanel.SetActive(true);
        Refresh();
    }

    // Hook to the gallery's Back button
    public void CloseGallery()
    {
        galleryPanel.SetActive(false);
        if (viewerPanel != null) viewerPanel.SetActive(false);
        if (hud != null) hud.SetActive(true);
    }

    // Hook to the viewer's Back button
    public void CloseViewer()
    {
        if (viewerPanel != null) viewerPanel.SetActive(false);
        galleryPanel.SetActive(true);
    }

    public void UnlockTile(int index)
    {
        if (index < 0 || index >= tiles.Length) return;
        tiles[index].unlocked = true;
        Refresh();
    }

    // First tile that has artwork but isn't unlocked yet, or -1 if all are collected
    public int NextLockedIndex()
    {
        for (int i = 0; i < tiles.Length; i++)
            if (tiles[i] != null && tiles[i].artwork != null && !tiles[i].unlocked) return i;
        return -1;
    }

    void OnTileClicked(int index)
    {
        Tile t = tiles[index];
        if (!t.unlocked || t.artwork == null || viewerPanel == null) return;

        viewerImage.sprite = t.artwork;
        viewerImage.preserveAspect = true;
        galleryPanel.SetActive(false);
        viewerPanel.SetActive(true);
    }

    void Refresh()
    {
        foreach (Tile t in tiles)
        {
            if (t == null) continue;
            bool showArt = t.unlocked && t.artwork != null;

            if (t.artworkImage != null)
            {
                t.artworkImage.sprite = t.artwork;
                t.artworkImage.preserveAspect = true;
                t.artworkImage.enabled = showArt;
            }
            if (t.lockIcon != null) t.lockIcon.SetActive(!t.unlocked);
        }
    }
}