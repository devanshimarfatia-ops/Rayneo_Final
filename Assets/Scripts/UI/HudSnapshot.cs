using System.Collections;
using System.IO;
using RayneoIdle.Game;
using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Editor tool: plays a short scripted scene in the DEMO slot and renders the head camera to PNG files
    /// (Temp/hud_forward.png, hud_map.png, hud_map_open.png) at the glasses' 30 deg view, for layout review.
    /// Started by SmokeTestLauncher (Tools > RayneoIdle > HUD Snapshot, or Temp/hud-snapshot.request).
    /// </summary>
    public class HudSnapshot : MonoBehaviour
    {
        private const string LogTag = "[HudSnapshot]";
        private const int Width = 960, Height = 720; // 4:3 like the 640x480 canvas, 1.5x for readability

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            yield return null;
            yield return null;

            var session = GameSession.Instance;
            var demo = FindObjectOfType<DemoMode>();
            var map = FindObjectOfType<TreasureMapView>();
            var head = UiFactory.FindHead();
            var cam = head != null ? head.GetComponent<Camera>() : null;
            if (session == null || demo == null || map == null || cam == null)
            {
                Debug.LogError($"{LogTag} missing GameSession / DemoMode / TreasureMapView / camera");
                Finish();
                yield break;
            }
            cam.fieldOfView = UiFactory.VerticalFovDeg();

            // Scripted state in the demo slot (never touches the real save).
            demo.SetActive(true);
            session.ResetSlot();
            session.Core.RewardTodo();
            session.Core.AddCoins(1000, Idle.CoinSource.Debug);
            session.Core.TryBuyCoinRate();
            session.Island.PlaceTutorialChest();
            session.Core.StartChestSearch();
            session.Island.OnSearchStarted();
            demo.SetTier(2); // 1800x: first chest in ~10 s
            float t0 = Time.realtimeSinceStartup;
            while (session.Core.State.chestsWaiting < 1 && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            // Forward view: capture while the map arrow blink is "on".
            yield return WaitForBlink(0.6f);
            Capture(cam, "hud_forward");

            // Look right at the map.
            head.rotation = Quaternion.Euler(0, 55f, 0);
            yield return null;
            yield return null;
            yield return WaitForBlink(0.8f);
            Capture(cam, "hud_map");

            map.OpenFocused();
            yield return null;
            Capture(cam, "hud_map_open");

            head.rotation = Quaternion.identity;
            demo.SetActive(false);
            Debug.Log($"{LogTag} DONE");
            Finish();
        }

        // Waits until Repeat(t, 2*half) is in the first half (the blink "on" phase), plus a small offset.
        private static IEnumerator WaitForBlink(float half)
        {
            while (Mathf.Repeat(Time.unscaledTime, half * 2) > half * 0.6f) yield return null;
        }

        private static void Capture(Camera cam, string name)
        {
            var rt = new RenderTexture(Width, Height, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", name + ".png"));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
            Debug.Log($"{LogTag} saved {path}");
        }

        private static void Finish()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }
    }
}
