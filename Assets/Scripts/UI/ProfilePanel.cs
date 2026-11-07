using System;
using RayneoIdle.GameInput;
using RayneoIdle.Profile;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Profile (character) tool, device test 4: three sections in the three-box layout.
    ///   Skills: upgrades (MVP: Coin Rate; Explorer speed / Luck "coming soon").
    ///   Look:   outfit (incl. male / female clothes) and clothing color; free ones + ones bought with coins.
    ///   Pets:   a pet that follows the explorer on the map; bought with coins.
    /// Look / Pets: Prev / Next change the option on the focused row and preview it on the right; unlocked
    /// options are worn at once, locked ones show their price and a tap buys + wears them. Leaving the section
    /// drops a locked preview. Shop skins only, no stat effect (GDD).
    /// </summary>
    public class ProfilePanel : PanelBase
    {
        private enum View { Overview, Skills, Look, Pets }

        public override string Name => "Profile";
        public override string Hint
        {
            get
            {
                switch (view)
                {
                    case View.Skills: return "Tap: buy   Swipe: move   Double tap: back";
                    case View.Look: return "Swipe: change   Tap: buy / wear   Double tap: back";
                    case View.Pets: return "Swipe: choose   Tap: buy / take   Double tap: back";
                    default: return "Swipe: choose   Tap: open   Double tap: back";
                }
            }
        }

        private View view;
        private Text title;
        private int overviewFocus, focusRow;

        // overview
        private RectTransform overviewRt;
        private Box[] boxes;
        private RawImage[] boxImages = new RawImage[3];

        // section views share rows + a preview on the right
        private RectTransform sectionRt;
        private readonly System.Collections.Generic.List<Row> rows = new System.Collections.Generic.List<Row>();
        private RawImage preview;
        private Text previewLabel;

        // Look browsing (index into the catalog lists, may point at a locked item = preview only)
        private int outfitIndex, colorIndex, petIndex;

        private ProfileSystem Profile => session.Profile;

        protected override void Build()
        {
            title = Title("Profile");

            overviewRt = UiFactory.CreateRect(root, "Overview", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            boxes = CreateBoxes(overviewRt);
            string[] names = { "Skills", "Look", "Pets" };
            for (int i = 0; i < 3; i++)
            {
                boxes[i].big.fontSize = UiFactory.BodyFont;
                boxes[i].big.text = names[i];
                boxes[i].name.text = "";
                boxImages[i] = UiFactory.CreatePixelImage(boxes[i].rt, "Image", null, 6f,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6));
            }

            sectionRt = UiFactory.CreateRect(root, "Section", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            for (int i = 0; i < 5; i++)
            {
                var r = CreateRow(i);
                r.text.rectTransform.sizeDelta = new Vector2(330, RowHeight); // leave the right side to the preview
                rows.Add(r);
            }
            preview = UiFactory.CreatePixelImage(sectionRt, "Preview", null, 8f,
                new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-90, 10));
            previewLabel = UiFactory.CreateText(sectionRt, "Preview label", UiFactory.AuxFont, Palette.Secondary, TextAnchor.UpperCenter,
                new Vector2(1, 0.5f), new Vector2(0.5f, 1), new Vector2(-90, -60), new Vector2(170, 48));

            Profile.OnChanged += () => { if (IsOpen) Refresh(); };
        }

        public override void Open()
        {
            view = View.Overview;
            overviewFocus = 0;
            base.Open();
        }

        private static void SetImage(RawImage img, Texture2D tex, float px)
        {
            img.texture = tex;
            img.enabled = tex != null;
            if (tex != null) img.rectTransform.sizeDelta = new Vector2(tex.width, tex.height) * px;
        }

        public override void Refresh()
        {
            overviewRt.gameObject.SetActive(view == View.Overview);
            sectionRt.gameObject.SetActive(view != View.Overview);
            if (view == View.Overview) { RefreshOverview(); foreach (var r in rows) r.Hide(); return; }
            switch (view)
            {
                case View.Skills: RefreshSkills(); break;
                case View.Look: RefreshLook(); break;
                case View.Pets: RefreshPets(); break;
            }
        }

        private void RefreshOverview()
        {
            var core = session.Core;
            var p = Profile.State;
            title.text = "Profile";
            for (int i = 0; i < 3; i++) boxes[i].SetFocused(i == overviewFocus, BoxTex, BoxFocusTex);

            SetImage(boxImages[0], PixelCanvas.FromSpriteCached(PixelSprites.Coin), 6f);
            boxes[0].status.text = $"Coin Rate Lv {core.State.coinRateLevel}\n{core.CoinsPerHour:0}/h";

            SetImage(boxImages[1], CharacterArt.Character(p, 0), 6f);
            boxes[1].status.text = $"{ProfileSystem.Find(CosmeticKind.Outfit, p.outfit).name}\n{ProfileSystem.Find(CosmeticKind.Color, p.color).name}";

            SetImage(boxImages[2], CharacterArt.Pet(p.pet), 8f);
            boxes[2].status.text = $"{ProfileSystem.Find(CosmeticKind.Pet, p.pet).name}\n{Profile.UnlockedCount(CosmeticKind.Pet) - 1}/{ProfileSystem.Pets.Length - 1} owned";
            for (int i = 0; i < 3; i++) boxes[i].status.color = Palette.Main;
        }

        // ---------------------------------------------------------------- Skills

        private enum Skill { CoinRate, ExplorerSpeed, Luck }
        private static readonly Skill[] Skills = { Skill.CoinRate, Skill.ExplorerSpeed, Skill.Luck };

        private void RefreshSkills()
        {
            var core = session.Core;
            title.text = "Skills";
            SetImage(preview, CharacterArt.Character(Profile.State, 0), 8f);
            previewLabel.text = $"{core.CoinsPerHour:0} coins/h";
            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= Skills.Length) { rows[i].Hide(); continue; }
                bool f = i == focusRow;
                switch (Skills[i])
                {
                    case Skill.CoinRate:
                        string price = core.CoinRateMaxed ? "MAX" : $"{core.NextCoinRateCost:N0}";
                        rows[i].Set($"Coin Rate  Lv {core.State.coinRateLevel}   {price}", f,
                            core.CanBuyCoinRate ? (Color?)null : (Color)Palette.Secondary);
                        break;
                    case Skill.ExplorerSpeed: rows[i].Set("Explorer speed   soon", f, Palette.Secondary); break;
                    case Skill.Luck: rows[i].Set("Luck   soon", f, Palette.Secondary); break;
                }
            }
        }

        // ---------------------------------------------------------------- Look

        private Cosmetic OutfitShown => ProfileSystem.Outfits[outfitIndex];
        private Cosmetic ColorShown => ProfileSystem.Colors[colorIndex];

        private void RefreshLook()
        {
            title.text = "Look";
            SetImage(preview, CharacterArt.Character(OutfitShown.id, ColorShown.id, 0), 8f);
            bool previewLocked = !Profile.IsUnlocked(OutfitShown) || !Profile.IsUnlocked(ColorShown);
            var focused = focusRow == 0 ? OutfitShown : ColorShown;
            // Price of the focused locked option sits under the preview (rows stay short, left of it).
            previewLabel.text = !Profile.IsUnlocked(focused) ? $"{focused.price:N0} coins\nTap to buy" : previewLocked ? "Preview" : "Wearing";
            rows[0].Set($"Outfit  < {OutfitShown.name} >", focusRow == 0,
                Profile.IsUnlocked(OutfitShown) ? (Color?)null : (Color)Palette.Secondary);
            rows[1].Set($"Color  < {ColorShown.name} >", focusRow == 1,
                Profile.IsUnlocked(ColorShown) ? (Color?)null : (Color)Palette.Secondary);
            for (int i = 2; i < rows.Count; i++) rows[i].Hide();
        }


        // ---------------------------------------------------------------- Pets

        private void RefreshPets()
        {
            title.text = "Pets";
            var shown = ProfileSystem.Pets[petIndex];
            SetImage(preview, CharacterArt.Pet(shown.id), 10f);
            previewLabel.text = shown.id == Profile.State.pet ? "With you" : Profile.IsUnlocked(shown) ? "Owned" : "Preview";
            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= ProfileSystem.Pets.Length) { rows[i].Hide(); continue; }
                var c = ProfileSystem.Pets[i];
                string tag = c.id == Profile.State.pet ? "with you" : Profile.IsUnlocked(c) ? "owned" : $"{c.price:N0}";
                rows[i].Set($"{c.name}   {tag}", i == petIndex, Profile.IsUnlocked(c) ? (Color?)null : (Color)Palette.Secondary);
            }
        }

        // ---------------------------------------------------------------- input

        protected override void OnAction(GameAction action)
        {
            switch (view)
            {
                case View.Overview:
                    if (action == GameAction.Prev || action == GameAction.Up) overviewFocus = Mathf.Max(0, overviewFocus - 1);
                    else if (action == GameAction.Next || action == GameAction.Down) overviewFocus = Mathf.Min(2, overviewFocus + 1);
                    else if (action == GameAction.Confirm) EnterSection((View)(overviewFocus + 1));
                    break;
                case View.Skills:
                    if (action == GameAction.Up || action == GameAction.Prev) focusRow = Mathf.Max(0, focusRow - 1);
                    else if (action == GameAction.Down || action == GameAction.Next) focusRow = Mathf.Min(Skills.Length - 1, focusRow + 1);
                    else if (action == GameAction.Confirm) BuySkill(Skills[focusRow]);
                    break;
                case View.Look:
                    if (action == GameAction.Up) focusRow = 0;
                    else if (action == GameAction.Down) focusRow = 1;
                    else if (action == GameAction.Prev || action == GameAction.Next) CycleLook(action == GameAction.Next ? 1 : -1);
                    else if (action == GameAction.Confirm) BuyShown(focusRow == 0 ? OutfitShown : ColorShown);
                    break;
                case View.Pets:
                    if (action == GameAction.Up || action == GameAction.Prev) petIndex = Mathf.Max(0, petIndex - 1);
                    else if (action == GameAction.Down || action == GameAction.Next) petIndex = Mathf.Min(ProfileSystem.Pets.Length - 1, petIndex + 1);
                    else if (action == GameAction.Confirm) BuyShown(ProfileSystem.Pets[petIndex]);
                    break;
            }
            Refresh();
        }

        protected override bool OnBack()
        {
            if (view == View.Overview) return false;
            view = View.Overview; // a locked preview is simply dropped: the equipped look never changed
            Refresh();
            return true;
        }

        private void EnterSection(View v)
        {
            view = v;
            focusRow = 0;
            var p = Profile.State;
            outfitIndex = Math.Max(0, Array.FindIndex(ProfileSystem.Outfits, c => c.id == p.outfit));
            colorIndex = Math.Max(0, Array.FindIndex(ProfileSystem.Colors, c => c.id == p.color));
            petIndex = Math.Max(0, Array.FindIndex(ProfileSystem.Pets, c => c.id == p.pet));
        }

        /// <summary>Trigger: swipe on a Look row -> next option; worn at once if unlocked, else preview only.</summary>
        private void CycleLook(int step)
        {
            if (focusRow == 0)
            {
                outfitIndex = (outfitIndex + step + ProfileSystem.Outfits.Length) % ProfileSystem.Outfits.Length;
                Profile.Equip(OutfitShown); // no-op when locked
            }
            else
            {
                colorIndex = (colorIndex + step + ProfileSystem.Colors.Length) % ProfileSystem.Colors.Length;
                Profile.Equip(ColorShown);
            }
        }

        private void BuyShown(Cosmetic c)
        {
            if (Profile.IsUnlocked(c)) { Profile.Equip(c); return; }
            if (Profile.BuyAndEquip(c)) { hud.Toast($"Unlocked: {c.name}"); Debug.Log($"[Shop] bought {c.Key} for {c.price:N0}"); }
            else hud.Toast($"Need {c.price:N0} coins");
        }

        private void BuySkill(Skill s)
        {
            var core = session.Core;
            if (s != Skill.CoinRate) { hud.Toast("Coming soon"); return; }
            if (!core.TryBuyCoinRate())
                hud.Toast(core.CoinRateMaxed ? "Coin Rate max" : $"Need {core.NextCoinRateCost:N0} coins");
        }

        /// <summary>For tests / snapshots.</summary>
        public void ShowSectionForTest(int section)
        {
            EnterSection((View)(Mathf.Clamp(section, 0, 2) + 1));
            Refresh();
        }
    }
}
