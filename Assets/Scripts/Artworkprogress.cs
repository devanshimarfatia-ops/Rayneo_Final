using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The artwork preview in the HUD. Starts blocky and sharpens as progress goes from 0 to 1.
// Also drives a simple progress bar and label.
public class ArtworkProgress : MonoBehaviour
{
    public RawImage previewImage;           // RawImage showing the artwork
    public AspectRatioFitter aspectFitter;  // optional: keeps the artwork's proportions
    public RectTransform progressFill;      // bar fill (Pivot X = 0, stretched in its background)
    public Text label;                      // e.g. "1 / 3  ·  tap for gallery"

    public int startWidth = 6;              // "big pixels" across at 0% progress
    public int sharpWidth = 160;            // pixels across just before 100% (about the preview's size)
    public float animTime = 1.2f;           // seconds to animate between steps

    public bool IsAnimating { get; private set; }

    private Texture source;
    private float shown;                    // progress currently displayed (0..1)
    private int lastWidth = -1;
    private RenderTexture rt;

    public void SetArtwork(Sprite artwork)
    {
        StopAllCoroutines();
        IsAnimating = false;
        source = artwork != null ? artwork.texture : null;
        shown = 0f;
        lastWidth = -1;

        previewImage.enabled = source != null;
        if (source != null)
        {
            if (aspectFitter != null) aspectFitter.aspectRatio = (float)source.width / source.height;
            Render(0f);
        }
        SetBar(0f);
    }

    public void SetProgress(int done, int total)
    {
        float target = total > 0 ? Mathf.Clamp01((float)done / total) : 0f;
        StopAllCoroutines();
        StartCoroutine(AnimateTo(target));
    }

    public void SetLabel(string text)
    {
        if (label != null) label.text = text;
    }

    IEnumerator AnimateTo(float target)
    {
        IsAnimating = true;
        float from = shown;
        float t = 0f;
        while (t < animTime)
        {
            t += Time.deltaTime;
            shown = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / animTime));
            Render(shown);
            SetBar(shown);
            yield return null;
        }
        shown = target;
        Render(shown);
        SetBar(shown);
        IsAnimating = false;
    }

    void Render(float p)
    {
        if (source == null) return;

        if (p >= 0.999f)
        {
            // fully revealed: show the original, sharp image
            previewImage.texture = source;
            ReleaseRT();
            lastWidth = source.width;
            return;
        }

        int w = Mathf.RoundToInt(startWidth * Mathf.Pow((float)sharpWidth / startWidth, p));
        if (w == lastWidth) return;   // only re-render when the pixel size changes
        lastWidth = w;

        int h = Mathf.Max(1, Mathf.RoundToInt(w * (float)source.height / source.width));
        ReleaseRT();
        rt = new RenderTexture(w, h, 0);
        rt.filterMode = FilterMode.Point;
        Graphics.Blit(source, rt);
        previewImage.texture = rt;
    }

    void SetBar(float p)
    {
        if (progressFill != null)
            progressFill.localScale = new Vector3(Mathf.Clamp01(p), 1f, 1f);
    }

    void ReleaseRT()
    {
        if (rt == null) return;
        if (previewImage != null && previewImage.texture == rt) previewImage.texture = null;
        rt.Release();
        Destroy(rt);
        rt = null;
    }

    void OnDestroy() => ReleaseRT();
}