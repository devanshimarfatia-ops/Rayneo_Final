using System.Linq;
using System.Text;
using RayneoIdle.Cards;
using RayneoIdle.Idle;
using RayneoIdle.Save;
using UnityEngine;

namespace RayneoIdle.Game
{
    /// <summary>
    /// Editor test harness for milestone M1 (core loop with the keyboard, no HUD yet).
    /// Shows a text overlay (OnGUI, Editor / development use only, not RayNeo-styled) and logs every event.
    ///
    /// Keys:
    ///   T = finish a To-do (+todoReward)     U = buy Coin Rate level
    ///   G = open tutorial chest               S = start chest search (end of onboarding)
    ///   C = open a waiting chest              P = open a stored pack
    ///   1 / 2 / 3 = demo tier 1x / 60x / 1800x
    ///   K = demo "skip offline 8 h"           F5 = save now
    ///   R x3 (ConfirmGate) = Reset slot       D = demo mode on / off (switches slot)
    ///   M = one hidden-toggle tap (5 within 3 s toggles demo mode)
    ///   With a ZoneTodoService in the scene: V = voice create, F = finish first visible todo,
    ///   Z = next zone (manual, stands in for ModeManager)
    /// </summary>
    public class IdleDebugKeys : MonoBehaviour
    {
        private const string LogTag = "[IdleDebug]";

        [SerializeField] private bool showOverlay = true;
        [SerializeField] private double skipHours = 8;

        private readonly ConfirmGate resetGate = new ConfirmGate(3, 5);
        private string lastAction = "";
        private GameSession session;
        private DemoMode demo;               // optional
        private Todo.ZoneTodoService todos;  // optional

        private void Start()
        {
            session = GameSession.Instance;
            if (session == null) { Debug.LogError($"{LogTag} no GameSession in scene"); enabled = false; return; }
            demo = FindObjectOfType<DemoMode>();
            todos = FindObjectOfType<Todo.ZoneTodoService>();
            if (todos != null) todos.OnTodoCreated += t => lastAction = $"Created [{t.zone}] {t.displayText}";

            var core = session.Core;
            core.OnCoinsAdded += (amount, src) => Debug.Log($"{LogTag} +{amount:F2} coins ({src})");
            core.OnCoinsSpent += amount => Debug.Log($"{LogTag} -{amount:F0} coins");
            core.OnCoinRateUpgraded += lvl => Debug.Log($"{LogTag} Coin Rate -> level {lvl} ({core.CoinsPerHour}/h)");
            core.OnChestFound += n => Debug.Log($"{LogTag} chest found, {n} waiting");
            core.OnDemoTierChanged += t => Debug.Log($"{LogTag} demo tier {t} = {core.DemoMultiplier}x");
            core.OnSettled += (reason, secs, coins, chests) =>
                Debug.Log($"{LogTag} settle {reason}: {secs:F0}s -> +{coins:F10} coins, +{chests} chests");
            session.Cards.OnCardDrawn += c =>
                Debug.Log($"{LogTag} card {c.card.id} ({c.card.rarity}) {(c.isNew ? "NEW" : $"dup +{c.duplicateCoins}")}{(c.fromPity ? " [pity]" : "")}");
            session.OnLaunchSettled += info =>
                lastAction = $"Welcome back: away {info.awaySeconds:F0}s, +{info.coins:F10} coins, +{info.chestsFound} chests";
            if (session.LastLaunchSettle.hadSave)
                lastAction = $"Welcome back: +{session.LastLaunchSettle.coins:F10} coins";
        }

        private void Update()
        {
            var core = session.Core;
            var cards = session.Cards;

            if (Input.GetKeyDown(KeyCode.T)) { core.RewardTodo(); lastAction = "To-do finished"; }
            if (Input.GetKeyDown(KeyCode.U)) lastAction = core.TryBuyCoinRate() ? $"Bought Coin Rate {core.State.coinRateLevel}" : "Cannot buy";
            if (Input.GetKeyDown(KeyCode.S)) { core.StartChestSearch(); session.Island.OnSearchStarted(); lastAction = "Chest search started"; }
            if (Input.GetKeyDown(KeyCode.G)) { int a = session.Island.PlaceTutorialChest(); var r = session.Island.OpenAt(a); lastAction = r == null ? "No free area" : $"Area {a}: " + Describe(r); }
            if (Input.GetKeyDown(KeyCode.C)) { int a = FirstChestArea(); var r = session.Island.OpenAt(a); lastAction = r == null ? "No chest waiting" : $"Area {a}: " + Describe(r); }
            if (Input.GetKeyDown(KeyCode.P))
            {
                var list = cards.OpenPack();
                lastAction = list == null ? "No pack" : "Pack: " + string.Join(", ", list.Select(CardText));
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) core.SetDemoTier(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) core.SetDemoTier(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) core.SetDemoTier(2);
            if (Input.GetKeyDown(KeyCode.K)) lastAction = $"Skip {skipHours} h: +{core.DemoSkipHours(skipHours):F0} coins";
            if (Input.GetKeyDown(KeyCode.F5)) { session.Save(); lastAction = "Saved"; }
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (demo != null) demo.SetActive(!demo.IsActive);
                else session.SwitchSlot(session.IsDemoSlot ? SaveSystem.MainSlot : SaveSystem.DemoSlot);
                lastAction = $"Slot {session.Slot}";
            }
            // M = one secret tap (DemoMode hidden toggle: secretTapCount taps within the window).
            if (Input.GetKeyDown(KeyCode.M) && demo != null)
                lastAction = demo.RegisterSecretTap() ? $"Demo {(demo.IsActive ? "ON" : "OFF")}" : "Secret tap";
            if (Input.GetKeyDown(KeyCode.R))
            {
                // Trigger: ConfirmGate, 3 presses within 5 s each (Reset + two confirmations, D10).
                if (resetGate.Press(Time.unscaledTime)) { session.ResetSlot(); lastAction = "Reset done"; }
                else lastAction = resetGate.Step == 1 ? "Reset? press R to confirm" : "Sure? press R again";
            }

            if (todos != null)
            {
                if (Input.GetKeyDown(KeyCode.V)) lastAction = todos.StartVoiceCreate() ? "Voice create started" : "Voice create failed";
                if (Input.GetKeyDown(KeyCode.F))
                {
                    if (todos.Visible.Count == 0) lastAction = "No todo to finish";
                    else { var t = todos.Visible[0]; lastAction = $"Finished '{t.displayText}': +{todos.Finish(t)}"; }
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    todos.SetZone((Todo.TodoZone)(((int)todos.CurrentZone + 1) % 4));
                    lastAction = $"Zone {todos.CurrentZone} (manual)";
                }
            }
        }

        private int FirstChestArea()
        {
            var spots = session.Island.State.spots;
            for (int i = 0; i < spots.Count; i++) if (spots[i].hasChest) return i;
            return -1;
        }

        private static string CardText(CardDrawResult c) =>
            $"{c.card.id}{(c.isNew ? " NEW" : $" dup+{c.duplicateCoins}")}{(c.fromPity ? " pity" : "")}";

        private static string Describe(ChestOpenResult r)
        {
            var sb = new StringBuilder(r.isTutorial ? "Tutorial chest: " : "Chest: ");
            foreach (var s in r.slots)
            {
                switch (s.kind)
                {
                    case ChestSlotKind.Coins: sb.Append($"[{s.coins} coins] "); break;
                    case ChestSlotKind.Card: sb.Append($"[{CardText(s.cards[0])}] "); break;
                    case ChestSlotKind.Pack: sb.Append("[pack] "); break;
                }
            }
            return sb.ToString();
        }

        private void OnGUI()
        {
            if (!showOverlay || session == null) return;
            var core = session.Core;
            var st = core.State;
            var cs = session.Cards.State;

            var sb = new StringBuilder();
            sb.AppendLine($"Slot {session.Slot}{(session.IsDemoSlot ? " (DEMO)" : "")}   Tier {core.DemoMultiplier}x   anim {core.AnimationSpeed}x");
            sb.AppendLine($"Coins {System.Math.Floor(st.coins)}  ({st.coins:F4})   {core.CoinsPerHour}/h");
            sb.AppendLine($"Coin Rate L{st.coinRateLevel}   next {(core.CoinRateMaxed ? "MAX" : core.NextCoinRateCost.ToString("F0"))}");
            sb.AppendLine($"Search {(st.chestSearchActive ? "on" : "off")}   chests {st.chestsWaiting}/{core.Config.chestSlots}   next {core.ChestProgress01:P1}   searching area {session.Island.State.searchArea}   chests at [{string.Join(",", session.Island.State.spots.Select(s => s.hasChest ? (s.isTutorial ? "T" : "C") : "-"))}]");
            sb.AppendLine($"Packs {cs.unopenedPacks}   cards {cs.OwnedDistinct()}/{session.Cards.Catalog.Count}   draws {cs.totalDraws}   pity {cs.drawsSinceNewSpecial}");
            if (demo != null) sb.AppendLine($"Demo {(demo.IsActive ? "ON" : "off")}   badge '{demo.BadgeText}'   reset step {resetGate.Step}");
            if (todos != null)
                sb.AppendLine($"Zone {todos.CurrentZone}   todos: {string.Join(" | ", todos.Visible.Select(t => $"[{t.zone}] {t.displayText}"))}");
            sb.AppendLine($"> {lastAction}");
            sb.AppendLine("T todo U upgrade G tutorial S search C chest P pack 1/2/3 tier K skip F5 save RRR reset D demo M secret V voice F finish Z zone");

            GUI.Box(new Rect(8, 8, 860, 190), GUIContent.none);
            GUI.Label(new Rect(16, 12, 850, 185), sb.ToString());
        }
    }
}
