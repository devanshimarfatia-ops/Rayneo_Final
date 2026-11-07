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

            // Launch flow: offline earnings window (if a save existed) + Start screen; tutorial waits for Start.
            Check("launch shows the Start screen", host.StartScreen != null && host.StartScreen.IsOpen && !ob.Active);
            for (int i = 0; i < 3 && host.StartScreen.IsOpen; i++) yield return Send(GameAction.Confirm);
            Check("Start pressed: game begins", !host.StartScreen.IsOpen && host.StartScreen.Started);

            // Fresh demo slot -> onboarding restarts at Welcome (intro card).
            demo.SetActive(true);
            session.ResetSlot();
            yield return null;
            Check("fresh save starts at Welcome with the intro card", ob.Step == OnboardingStep.Welcome && ob.CardShown, ob.Step.ToString());
            yield return Frames(3);
            Shot("ob_1_welcome");

            yield return Send(GameAction.Confirm);
            Check("tap -> Tap step, card gone", ob.Step == OnboardingStep.Tap && !ob.CardShown);
            Shot("ob_1b_tap");
            yield return Send(GameAction.Confirm);
            Check("tap -> Swipe step", ob.Step == OnboardingStep.Swipe);
            yield return Send(GameAction.Next);
            Check("swipe -> DoubleTap step", ob.Step == OnboardingStep.DoubleTap);
            yield return Send(GameAction.Back);
            Check("double tap -> SetAnchor: Anchors panel on slot 1", ob.Step == OnboardingStep.SetAnchor
                && host.Current is AnchorPanel ap && ap.InSlotView && host.AnyOpen);
            yield return Frames(2);
            Shot("ob_2_anchor");

            // No anchor system in this test scene: Place reports "not available", closing the panel continues.
            yield return Send(GameAction.Confirm);   // Place anchor here
            yield return Send(GameAction.Back);      // slot -> boxes
            yield return Send(GameAction.Back);      // close
            yield return Frames(2);
            Check("anchor panel closed -> Todo step, panel NOT auto-opened", ob.Step == OnboardingStep.Todo && !host.AnyOpen
                && FindObjectOfType<MainHud>().FocusedTool == MainHud.ToolId.Todo);
            Shot("ob_3_todo_pointer");
            yield return Send(GameAction.Confirm);   // open To-do from the toolbar
            Check("tap opens To-do", host.Current is TodoPanel && host.AnyOpen);
            Shot("ob_3b_todo");

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
            Check("open tutorial chest -> OpenPack, reward window open", ob.Step == OnboardingStep.OpenPack
                && session.Data.tutorialChestOpened && session.Cards.State.unopenedPacks == 1 && host.Current is RewardWindow && host.AnyOpen);
            yield return new WaitForSecondsRealtime(1.5f);   // rewards appear one by one
            Shot("ob_7_rewards");
            yield return Send(GameAction.Confirm);          // tap the pack: open it in the window
            Check("pack opened in the window -> OpenCollection", ob.Step == OnboardingStep.OpenCollection
                && session.Cards.State.unopenedPacks == 0 && host.AnyOpen);
            yield return new WaitForSecondsRealtime(2.5f);
            Shot("ob_8_pack");
            yield return Send(GameAction.Confirm);          // close the window
            Check("reward window closed by tap", !host.AnyOpen);

            head.rotation = Quaternion.identity;
            yield return Frames(3);
            yield return Send(GameAction.Confirm);          // toolbar is focused on Collection
            Check("collection opens -> BrowseCollection", ob.Step == OnboardingStep.BrowseCollection && host.Current is CollectionPanel);
            Shot("ob_9_collection");
            yield return Send(GameAction.Back);             // close the collection
            yield return Frames(2);
            Check("onboarding done, search started", !ob.Active && session.Data.onboardingDone && session.Core.State.chestSearchActive);
            Check("end card shown", ob.CardShown, ob.CurrentHint);
            yield return Frames(2);
            Shot("ob_10_end");
            yield return Send(GameAction.Confirm);
            Check("tap closes the end card", !ob.CardShown);

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
