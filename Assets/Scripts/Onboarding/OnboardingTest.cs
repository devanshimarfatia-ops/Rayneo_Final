using System.Collections;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using RayneoIdle.UI;
using UnityEngine;

namespace RayneoIdle.Onboarding
{
    /// <summary>
    /// Editor test: walks the whole onboarding in the DEMO slot with injected gestures (InputAdapter.Send, same
    /// path as the touchpad) and head turns, checks every step, and saves screenshots Temp/ob_*.png.
    /// Started by SmokeTestLauncher (Temp/onboarding-test.request). Logs "[OnboardingTest] PASS/FAIL" + DONE.
    /// Runs without the anchor system, so the Work zone uses the manual fallback (anchors: test on device).
    /// </summary>
    public class OnboardingTest : MonoBehaviour
    {
        private const string LogTag = "[OnboardingTest]";
        private int passed, failed;
        private OnboardingController ob;
        private InputAdapter input;
        private Camera cam;

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            yield return null;
            yield return null;

            var session = GameSession.Instance;
            var demo = FindObjectOfType<DemoMode>();
            var host = FindObjectOfType<PanelHost>();
            ob = FindObjectOfType<OnboardingController>();
            input = InputAdapter.Instance;
            var head = UiFactory.FindHead();
            cam = head != null ? head.GetComponent<Camera>() : null;
            if (session == null || demo == null || host == null || ob == null || input == null || cam == null)
            {
                Fail("setup", "missing component");
                Finish();
                yield break;
            }
            cam.fieldOfView = UiFactory.VerticalFovDeg();

            // Fresh demo slot -> onboarding restarts at Welcome.
            demo.SetActive(true);
            session.ResetSlot();
            yield return null;
            Check("fresh save starts at Welcome", ob.Step == OnboardingStep.Welcome, ob.Step.ToString());
            yield return Frames(3);
            Shot("ob_1_welcome");

            yield return Send(GameAction.Confirm);
            Check("tap -> Tap step", ob.Step == OnboardingStep.Tap);
            yield return Send(GameAction.Confirm);
            Check("tap -> Swipe step", ob.Step == OnboardingStep.Swipe);
            yield return Send(GameAction.Next);
            Check("swipe -> DoubleTap step", ob.Step == OnboardingStep.DoubleTap);
            yield return Send(GameAction.Back);
            Check("double tap -> SetAnchor step", ob.Step == OnboardingStep.SetAnchor);
            Shot("ob_2_anchor");

            yield return Send(GameAction.Confirm);
            Check("anchor -> Todo step, panel open", ob.Step == OnboardingStep.Todo && host.Current is TodoPanel && host.AnyOpen);
            yield return Frames(2);
            Shot("ob_3_todo");

            double before = session.Core.State.coins;
            yield return Send(GameAction.Down);      // focus the first todo
            yield return Send(GameAction.Confirm);   // finish it (D5 path: existing todo, no voice)
            Check("finish todo -> Toolbar step, +320", ob.Step == OnboardingStep.Toolbar && session.Core.State.coins - before >= 319.9,
                $"{ob.Step}, +{session.Core.State.coins - before:F1}");
            yield return Send(GameAction.Back);      // close the To-do panel
            Check("panel closed by double tap", !host.AnyOpen);
            Shot("ob_4_toolbar");
            yield return Send(GameAction.Next);
            Check("swipe toolbar -> TurnToMap", ob.Step == OnboardingStep.TurnToMap);

            head.rotation = Quaternion.Euler(0, 55f, 0);
            yield return Frames(3);
            Check("map in view -> TutorialSearch", ob.Step == OnboardingStep.TutorialSearch);
            yield return new WaitForSecondsRealtime(4f);
            Shot("ob_5_search");
            float t0 = Time.realtimeSinceStartup;
            while (ob.Step == OnboardingStep.TutorialSearch && Time.realtimeSinceStartup - t0 < 15f) yield return null;
            Check("tutorial chest found after the fixed delay", ob.Step == OnboardingStep.OpenChest, ob.Step.ToString());
            yield return Frames(20);
            Shot("ob_6_chest");

            yield return Send(GameAction.Confirm);
            Check("open tutorial chest -> OpenCollection", ob.Step == OnboardingStep.OpenCollection
                && session.Data.tutorialChestOpened && session.Cards.State.unopenedPacks == 1);

            head.rotation = Quaternion.identity;
            yield return Frames(3);
            yield return Send(GameAction.Confirm);   // toolbar is focused on Collection
            Check("collection opens -> OpenPack", ob.Step == OnboardingStep.OpenPack && host.Current is CollectionPanel);
            Shot("ob_7_collection");
            yield return Send(GameAction.Down);
            yield return Send(GameAction.Down);
            yield return Send(GameAction.Down);      // grid row 3 -> "Open pack"
            yield return Send(GameAction.Confirm);
            Check("pack opened -> BrowseCollection", ob.Step == OnboardingStep.BrowseCollection && session.Cards.State.unopenedPacks == 0);
            Shot("ob_8_pack");
            yield return Send(GameAction.Back);      // leave the reveal view
            yield return Send(GameAction.Back);      // close the collection
            yield return Frames(2);
            Check("onboarding done, search started", !ob.Active && session.Data.onboardingDone && session.Core.State.chestSearchActive);
            Shot("ob_9_done");

            // Skip path: reset, skip at once, tutorial chest must stay on the map (not granted).
            session.ResetSlot();
            yield return Frames(2);
            yield return Send(GameAction.Back);
            Check("double tap shows skip prompt", ob.CurrentHint.StartsWith("Skip"), ob.CurrentHint);
            yield return Send(GameAction.Confirm);
            bool chestLeft = session.Island.State.spots.Exists(s => s.hasChest && s.isTutorial);
            Check("skip: done, tutorial chest left on the map, no reward", !ob.Active && chestLeft && session.Core.State.coins < 1,
                $"chest {chestLeft}, coins {session.Core.State.coins:F2}");

            demo.SetActive(false);
            Finish();
        }

        private IEnumerator Send(GameAction a)
        {
            input.Send(a);
            yield return null; // InputAdapter dispatches in Update
            yield return null;
        }

        private static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        private void Shot(string name) => HudSnapshot.Capture(cam, name);

        private void Check(string name, bool ok, string detail = "")
        {
            if (ok) { passed++; Debug.Log($"{LogTag} PASS {name} {detail}"); }
            else Fail(name, $"{detail} (step {ob?.Step})");
        }

        private void Fail(string name, string detail) { failed++; Debug.LogWarning($"{LogTag} FAIL {name} {detail}"); }

        private void Finish()
        {
            Debug.Log($"{LogTag} DONE: {passed} passed, {failed} failed");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }
    }
}
