using FfalconXR.InputModule; // XRGraphicRaycaster (ARDK UnityXRSDKCore.dll)
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Builds the head-locked (0DoF) HUD canvas used by ZoneDebugHUD, following the RayNeo specs:
    /// 640x480 reference covering the display FOV, 24 px black margin, Bold legacy Text,
    /// cyan / green palette, XRGraphicRaycaster instead of GraphicRaycaster.
    /// Works at runtime (AnchorZoneBootstrap) and in Edit mode (scene setup menu).
    /// </summary>
    public static class ZoneHudBuilder
    {
        public struct HudTexts
        {
            public RectTransform canvas;
            public Text mode, toast, status, menu;
        }

        // Canvas reference 640x480 px covering a 30 deg FOV (treated as the diagonal).
        public const float CanvasWidthPx = 640f;
        public const float CanvasHeightPx = 480f;
        public const float DisplayFovDeg = 30f;
        // HUD distance in front of the eyes. Scale is derived so the 640x480 canvas fills the FOV.
        public const float HudDistanceM = 2f;
        // Black safety margin, spec says 16-30 px.
        public const float MarginPx = 24f;
        // Font sizes: title 28-40, body 20-24, aux 16-18 (min 16).
        public const int ModeFontSize = 32;
        public const int ToastFontSize = 22;
        public const int StatusFontSize = 18;
        public const int MenuFontSize = 24;
        // Gap between the center gaze dot and the bottom of the anchor menu.
        public const float MenuGapPx = 30f;

        public static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        public static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);

        public static HudTexts Build(Transform head)
        {
            var hud = new HudTexts { canvas = CreateCanvas(head) };
            float innerWidth = CanvasWidthPx - 2 * MarginPx;

            // top zone = toasts
            hud.toast = CreateText(hud.canvas, "ToastText", ToastFontSize, Cyan, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -MarginPx), new Vector2(innerWidth, 32f));
            // top-left, second row (first row belongs to toasts)
            hud.mode = CreateText(hud.canvas, "ModeText", ModeFontSize, Cyan, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(MarginPx, -(MarginPx + 40f)), new Vector2(innerWidth, 44f));
            // center zone, bottom edge MenuGapPx above the gaze dot so the dot stays visible
            hud.menu = CreateText(hud.canvas, "MenuText", MenuFontSize, Green, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, MenuGapPx), new Vector2(innerWidth, 70f));
            // bottom zone = hints, two lines
            hud.status = CreateText(hud.canvas, "StatusText", StatusFontSize, Green, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(MarginPx, MarginPx), new Vector2(innerWidth, 52f));
            return hud;
        }

        private static RectTransform CreateCanvas(Transform head)
        {
            var go = new GameObject("Zone HUD Canvas", typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(head, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = head.GetComponent<Camera>();
            // RayNeo replacement for GraphicRaycaster. No plain GraphicRaycaster is added.
            go.AddComponent<XRGraphicRaycaster>();

            // Visible diagonal at HudDistanceM = 2 * d * tan(FOV/2); divide by the canvas diagonal in px.
            float diagPx = Mathf.Sqrt(CanvasWidthPx * CanvasWidthPx + CanvasHeightPx * CanvasHeightPx);
            float metersPerPx = 2f * HudDistanceM * Mathf.Tan(DisplayFovDeg * 0.5f * Mathf.Deg2Rad) / diagPx;

            rt.sizeDelta = new Vector2(CanvasWidthPx, CanvasHeightPx);
            rt.localPosition = new Vector3(0f, 0f, HudDistanceM);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * metersPerPx;
            return rt;
        }

        private static Text CreateText(RectTransform parent, string name, int fontSize, Color color, TextAnchor align,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;          // spec: Bold/Medium only
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = align;
            text.supportRichText = true;              // menu uses <color>/<size>
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;               // display only, keeps UI raycasts cheap
            text.text = "";
            return text;
        }
    }
}
