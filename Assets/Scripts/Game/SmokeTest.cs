using System;
using System.Collections;
using System.IO;
using System.Linq;
using RayneoIdle.Save;
using RayneoIdle.Todo;
using UnityEngine;

namespace RayneoIdle.Game
{
    /// <summary>
    /// Automated Editor smoke test of the core loop (milestone M1/M2 logic, no HUD).
    /// Started by the Editor menu Tools > RayneoIdle > Run Smoke Test (SmokeTestLauncher), which builds a
    /// temporary scene with GameSession + DemoMode + ZoneTodoService + this component.
    ///
    /// Phase 1: Reset slot 0, tutorial chest, pack, voice todo in the Work zone, finish it, Coin Rate,
    ///          chest search at 1800x (first chest ~10 s, 3 areas full), open a chest, save, exit Play.
    /// Phase 2: (launcher waits a few seconds, re-enters Play) launch settle = real exit,
    ///          state kept, demo slot on / off, then exit Play.
    /// WARNING: wipes the Editor save in slot 0 and slot 1.
    /// Results: "[SmokeTest] PASS/FAIL ..." lines and a final "[SmokeTest] DONE" line in the Console / Editor.log.
    /// </summary>
    public class SmokeTest : MonoBehaviour
    {
        private const string LogTag = "[SmokeTest]";

        [Tooltip("1 = first run, 2 = relaunch check. Set by the launcher.")]
        public int phase = 1;

        [Tooltip("Max real seconds to wait for the first chest at 1800x (expected ~10 s).")]
        [SerializeField] private float firstChestTimeout = 20f;

        private int passed, failed;

        [Serializable]
        private class Snapshot
        {
            public double coins;
            public int coinRateLevel;
            public int cardsOwned;
            public int chestsWaiting;
            public string spots;
            public long savedUtcMs;
        }

        private static string SnapshotPath => Path.Combine(Application.temporaryCachePath, "smoketest_snapshot.json");

        private IEnumerator Start()
        {
            Application.runInBackground = true; // keep running while the Editor window is not focused
            yield return null;
            var session = GameSession.Instance;
            if (session == null) { Fail("setup", "no GameSession"); Finish(); yield break; }

            if (phase == 1) yield return Phase1(session);
            else yield return Phase2(session);
            Finish();
        }

        private IEnumerator Phase1(GameSession s)
        {
            var core = s.Core;
            var cards = s.Cards;
            var island = s.Island;
            var todos = FindObjectOfType<ZoneTodoService>();

            // Fresh start
            var demo = FindObjectOfType<DemoMode>();
            if (demo != null && demo.IsActive) demo.SetActive(false);
            SaveSystem.Delete(SaveSystem.DemoSlot);
            s.ResetSlot();
            Check("reset: empty", core.State.coins < 0.01 && core.State.coinRateLevel == 0 && cards.State.OwnedDistinct() == 0
                                  && core.State.chestsWaiting == 0, $"coins {core.State.coins}");

            // Tutorial chest
            int area = island.PlaceTutorialChest();
            Check("tutorial: placed", area >= 0 && island.State.spots[area].isTutorial, $"area {area}");
            var tut = island.OpenAt(area);
            Check("tutorial: 500 + 1 Common + 1 pack",
                tut != null && tut.isTutorial && Math.Abs(core.State.coins - 500) < 0.5 && cards.State.OwnedDistinct() == 1
                && cards.State.unopenedPacks == 1, $"coins {core.State.coins:F2}, cards {cards.State.OwnedDistinct()}, packs {cards.State.unopenedPacks}");

            // Pack
            double beforePack = core.State.coins;
            var pack = cards.OpenPack();
            double dup = pack?.Sum(c => c.duplicateCoins) ?? -1;
            Check("pack: 5 cards, duplicates paid", pack != null && pack.Count == 5 && cards.State.unopenedPacks == 0
                && Math.Abs(core.State.coins - beforePack - dup) < 0.5, $"dup {dup}, owned {cards.State.OwnedDistinct()}");

            // Todo in the Work zone
            if (todos == null) Fail("todo", "no ZoneTodoService");
            else
            {
                todos.SetZone(TodoZone.Work);
                bool started = todos.StartVoiceCreate();
                var created = todos.Visible.FirstOrDefault(t => t.zone == TodoZone.Work);
                Check("todo: voice create tagged (Work)-", started && created.raw != null && created.raw.text.StartsWith("(Work)-"),
                    created.raw?.text ?? "none");
                todos.SetZone(TodoZone.Kitchen);
                Check("todo: hidden in Kitchen", todos.Visible.All(t => t.zone != TodoZone.Work), $"{todos.Visible.Count} visible");
                todos.SetZone(TodoZone.Work);

                double beforeTodo = core.State.coins;
                double paid = created.raw != null ? todos.Finish(created) : 0;
                Check("todo: finish pays 320 once", Math.Abs(paid - 320) < 0.01 && core.State.coins - beforeTodo >= 319.99
                    && s.Data.rewardedTodoTimestamps.Contains(created.timestamp), $"paid {paid}");
                Check("todo: gone after finish", todos.Visible.All(t => t.timestamp != created.timestamp));
            }

            // Upgrade
            Check("upgrade: Coin Rate 1 -> 105/h", core.TryBuyCoinRate() && core.State.coinRateLevel == 1 && Math.Abs(core.CoinsPerHour - 105) < 0.01);

            // Chest search at 1800x
            core.StartChestSearch();
            island.OnSearchStarted();
            int searchArea = island.State.searchArea;
            Check("search: area chosen", searchArea >= 0, $"area {searchArea}");

            core.SetDemoTier(2);
            float t0 = Time.realtimeSinceStartup;
            while (core.State.chestsWaiting < 1 && Time.realtimeSinceStartup - t0 < firstChestTimeout) yield return null;
            float firstChest = Time.realtimeSinceStartup - t0;
            Check("search: first chest in ~10 s at 1800x", core.State.chestsWaiting >= 1 && firstChest > 8.5f && firstChest < 12f, $"{firstChest:F1} s");
            Check("search: chest lands in the searched area", searchArea >= 0 && island.State.spots[searchArea].hasChest);
            Check("search: moved to another area", island.State.searchArea >= 0 && island.State.searchArea != searchArea,
                $"now {island.State.searchArea}");

            t0 = Time.realtimeSinceStartup;
            while (core.State.chestsWaiting < 3 && Time.realtimeSinceStartup - t0 < 2 * firstChestTimeout + 5) yield return null;
            Check("search: 3 areas full, search stops", core.State.chestsWaiting == 3 && island.State.spots.All(p => p.hasChest)
                && island.State.searchArea == -1 && core.ChestProgress01 == 0, $"waiting {core.State.chestsWaiting}");
            core.SetDemoTier(0);

            // Open a chest
            int ownedBefore = cards.State.OwnedDistinct();
            var opened = island.OpenAt(1);
            Check("chest: opened, area 1 free, search resumes there", opened != null && opened.slots.Count == 3
                && core.State.chestsWaiting == 2 && !island.State.spots[1].hasChest && island.State.searchArea == 1,
                opened == null ? "null" : string.Join(",", opened.slots.Select(x => x.kind)));

            // Snapshot for phase 2, then a real exit
            s.Save();
            var snap = new Snapshot
            {
                coins = core.State.coins, coinRateLevel = core.State.coinRateLevel, cardsOwned = cards.State.OwnedDistinct(),
                chestsWaiting = core.State.chestsWaiting, spots = SpotsText(s), savedUtcMs = core.State.lastCreditUtcMs,
            };
            File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snap));
            Log($"phase 1 snapshot: coins {snap.coins:F4}, level {snap.coinRateLevel}, cards {snap.cardsOwned}, chests {snap.chestsWaiting} {snap.spots}");
        }

        private IEnumerator Phase2(GameSession s)
        {
            var core = s.Core;
            if (!File.Exists(SnapshotPath)) { Fail("relaunch", "no phase 1 snapshot"); yield break; }
            var snap = JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath));
            var info = s.LastLaunchSettle;

            Check("relaunch: settle shown (save existed)", info.hadSave && !s.IsFreshSave);
            double expected = 105.0 * info.awaySeconds / 3600.0;
            Check("relaunch: away pays 105/h x away time at 1x", info.awaySeconds >= 3 && Math.Abs(info.coins - expected) < 0.01,
                $"away {info.awaySeconds:F1} s, +{info.coins:F10} (expected {expected:F10})");
            Check("relaunch: state kept", core.State.coinRateLevel == snap.coinRateLevel && s.Cards.State.OwnedDistinct() == snap.cardsOwned
                && core.State.chestsWaiting == snap.chestsWaiting && SpotsText(s) == snap.spots
                && core.State.coins >= snap.coins - 0.001,
                $"coins {core.State.coins:F4} vs {snap.coins:F4}, spots {SpotsText(s)} vs {snap.spots}");

            // Demo slot on / off
            var demo = FindObjectOfType<DemoMode>();
            if (demo == null) { Fail("demo", "no DemoMode"); yield break; }
            double mainCoins = core.State.coins;
            demo.SetActive(true);
            Check("demo: on = fresh demo slot", demo.IsActive && s.IsDemoSlot && core.State.coins < 0.01, $"coins {core.State.coins}");
            demo.SetTier(1);
            Check("demo: badge 60x DEMO", demo.BadgeText == "60x DEMO", demo.BadgeText);
            yield return new WaitForSecondsRealtime(1f);
            Check("demo: 60x income", core.State.coins > 1.5 && core.State.coins < 2.5, $"{core.State.coins:F3} after ~1 s"); // 100/h x 60 = 1.67/s
            demo.SetActive(false);
            Check("demo: off = main slot back, tier 1x", !demo.IsActive && !s.IsDemoSlot && core.DemoTier == 0
                && core.State.coins >= mainCoins - 0.001 && core.State.coinRateLevel == snap.coinRateLevel, $"coins {core.State.coins:F4}");
        }

        private static string SpotsText(GameSession s) =>
            string.Join(",", s.Island.State.spots.Select(p => p.hasChest ? (p.isTutorial ? "T" : "C") : "-"));

        // ------------------------------------------------------------------

        private void Check(string name, bool ok, string detail = "")
        {
            if (ok) { passed++; Debug.Log($"{LogTag} PASS {name} {detail}"); }
            else Fail(name, detail);
        }

        private void Fail(string name, string detail)
        {
            failed++;
            Debug.LogWarning($"{LogTag} FAIL {name} {detail}");
        }

        private static void Log(string msg) => Debug.Log($"{LogTag} {msg}");

        private void Finish()
        {
            Debug.Log($"{LogTag} DONE phase {phase}: {passed} passed, {failed} failed");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }
    }
}
