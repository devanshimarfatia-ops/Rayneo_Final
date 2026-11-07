using FfalconXR.InputModule; // XRGraphicRaycaster (ARDK UnityXRSDKCore.dll)
using RayNeo;                // HeadTrackedPoseDriver
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Builds world-space canvases, text and pixel images in code (same approach as ZoneDebugHUD).
    /// Layout units are canvas px of the RayNeo 640x480 reference canvas.
    /// </summary>
    public static class UiFactory
    {
        // ---- RayNeo design specs ----
        public const float CanvasWidthPx = 640f;
        public const float CanvasHeightPx = 480f;
        /// <summary>Display FOV, treated as the diagonal of the 640x480 canvas.</summary>
        public const float DisplayFovDeg = 30f;
        /// <summary>Black safety margin, spec 16-30 px.</summary>
        public const float MarginPx = 24f;

        // Font sizes: title 28-40, body 20-24, aux 16-18 (min 16), Bold.
        public const int TitleFont = 30;
        public const int BodyFont = 22;
        public const int AuxFont = 18;

        /// <summary>Meters per canvas px for a canvas at <paramref name="distanceM"/>, so 640x480 px fill the FOV.</summary>
        public static float MetersPerPx(float distanceM)
        {
            float diagPx = Mathf.Sqrt(CanvasWidthPx * CanvasWidthPx + CanvasHeightPx * CanvasHeightPx);
            return 2f * distanceM * Mathf.Tan(DisplayFovDeg * 0.5f * Mathf.Deg2Rad) / diagPx;
        }

        /// <summary>Vertical camera FOV that shows exactly the 640x480 canvas (Editor snapshots).</summary>
        public static float VerticalFovDeg()
        {
            float halfDiag = Mathf.Tan(DisplayFovDeg * 0.5f * Mathf.Deg2Rad);
            float halfV = halfDiag * CanvasHeightPx / Mathf.Sqrt(CanvasWidthPx * CanvasWidthPx + CanvasHeightPx * CanvasHeightPx);
            return 2f * Mathf.Atan(halfV) * Mathf.Rad2Deg;
        }

        /// <summary>XR Plugin head camera (HeadTrackedPoseDriver), else Camera.main.</summary>
        public static Transform FindHead()
        {
            var driver = Object.FindObjectOfType<HeadTrackedPoseDriver>();
            if (driver != null) return driver.transform;
            return Camera.main != null ? Camera.main.transform : null;
        }

        public static RectTransform CreateCanvas(string name, Transform parent, Vector2 sizePx, float distanceM, Camera eventCamera)
        {
            var go = new GameObject(name, typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera;
            go.AddComponent<XRGraphicRaycaster>(); // RayNeo replacement for GraphicRaycaster

            rt.sizeDelta = sizePx;
            rt.localScale = Vector3.one * MetersPerPx(distanceM);
            return rt;
        }

        public static Text CreateText(RectTransform parent, string name, int fontSize, Color color, TextAnchor align,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = CreateRect(parent, name, anchor, pivot, pos, size);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;    // spec: Bold / Medium only
            text.fontSize = Mathf.Max(16, fontSize); // spec: min 16 px
            text.color = color;
            text.alignment = align;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = "";
            return text;
        }

        /// <summary>Pixel image: texture shown at <paramref name="pxPerTexel"/> canvas px per texel.</summary>
        public static RawImage CreatePixelImage(RectTransform parent, string name, Texture2D tex, float pxPerTexel,
            Vector2 anchor, Vector2 pivot, Vector2 pos)
        {
            var size = tex != null ? new Vector2(tex.width, tex.height) * pxPerTexel : Vector2.zero;
            var rt = CreateRect(parent, name, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            return img;
        }

        public static RectTransform CreateRect(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Camera setup required on RayNeo: solid black clear (= transparent), no skybox.</summary>
        public static void ApplyBlackClear(Camera cam)
        {
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }
    }
}
