using System.Collections.Generic;
using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Hides the RayNeo SDK gaze pointer (the dot in the middle of the view) while <see cref="Hide"/> is true.
    /// The pointer lives in the XR Plugin prefab: LaserBeam > LaserBeamDot (MeshRenderer) and BeamGraphic
    /// (LineRenderer). Only the renderers are switched off; the SDK LaserBeam / input module keep running.
    ///
    /// Trigger: Camera.onPreCull, i.e. right before every camera draws. The closed-source LaserBeam may switch its
    /// renderers back on in its own Update / LateUpdate (script order not guaranteed), so unticking them once is
    /// not enough (same method as AnchorZone/ZoneDebugHUD). Built-in render pipeline.
    /// Renderers are looked up by name once at start and again if any of them is destroyed.
    /// </summary>
    public class GazeDotHider : MonoBehaviour
    {
        private const string LogTag = "[GazeDot]";
        private static readonly string[] PointerNames = { "LaserBeamDot", "BeamGraphic" };

        [Tooltip("True = SDK gaze dot hidden. Default on: the game uses its own reticle where aiming is needed.")]
        [SerializeField] private bool hide = true;

        /// <summary>Setting false switches the SDK renderers back on (the SDK then drives them again).</summary>
        public bool Hide
        {
            get => hide;
            set
            {
                if (hide && !value) foreach (var r in renderers) if (r != null) r.enabled = true;
                hide = value;
            }
        }

        private readonly List<Renderer> renderers = new List<Renderer>();

        private void Start() => FindRenderers();

        private void FindRenderers()
        {
            renderers.Clear();
            foreach (var r in FindObjectsOfType<Renderer>(true))
                if (System.Array.IndexOf(PointerNames, r.gameObject.name) >= 0) renderers.Add(r);
            if (renderers.Count == 0) Debug.LogWarning($"{LogTag} SDK pointer renderers (LaserBeamDot / BeamGraphic) not found");
            else Debug.Log($"{LogTag} found {renderers.Count} SDK pointer renderers, hide={hide}");
        }

        private void OnEnable() => Camera.onPreCull += BeforeCull;
        private void OnDisable() => Camera.onPreCull -= BeforeCull;

        private void BeforeCull(Camera cam)
        {
            if (!hide) return;
            bool missing = false;
            foreach (var r in renderers)
            {
                if (r == null) { missing = true; continue; }
                if (r.enabled) r.enabled = false;
            }
            if (missing) FindRenderers();
        }
    }
}
