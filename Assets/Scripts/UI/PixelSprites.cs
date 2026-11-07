namespace RayneoIdle.UI
{
    /// <summary>
    /// Placeholder pixel sprites drawn in code (top row first). Codes: S = sand, I = ink, A = accent, . = clear.
    /// Line art only (outlines, few fills) to keep brightness low. Replace with final art later (Trello: Art).
    /// </summary>
    public static class PixelSprites
    {
        // ---------------- island ----------------

        /// <summary>Closed chest, 11x8, outline + lock.</summary>
        public static readonly string[] Chest =
        {
            "..AAAAAAA..",
            ".A.......A.",
            "A.........A",
            "AAAAAAAAAAA",
            "A....S....A",
            "A...SSS...A",
            "A.........A",
            "AAAAAAAAAAA",
        };

        /// <summary>Tutorial chest: same shape with a small spark on top, 11x10.</summary>
        public static readonly string[] TutorialChest =
        {
            ".....A.....",
            "....A.A....",
            "..AAAAAAA..",
            ".A.......A.",
            "A.........A",
            "AAAAAAAAAAA",
            "A....S....A",
            "A...SSS...A",
            "A.........A",
            "AAAAAAAAAAA",
        };

        /// <summary>Explorer, 7x10, two walk frames.</summary>
        public static readonly string[] ExplorerA =
        {
            "..AAA..",
            ".AAAAA.",
            "..SSS..",
            "..S.S..",
            ".SSSSS.",
            "S.SSS.S",
            "..SSS..",
            "..S.S..",
            ".S...S.",
            ".S...S.",
        };

        public static readonly string[] ExplorerB =
        {
            "..AAA..",
            ".AAAAA.",
            "..SSS..",
            "..S.S..",
            ".SSSSS.",
            "S.SSS.S",
            "..SSS..",
            "..S.S..",
            "..S.S..",
            "..S.S..",
        };

        /// <summary>"?" mark for the searched area, 5x7.</summary>
        public static readonly string[] Question =
        {
            ".AAA.",
            "A...A",
            "....A",
            "..AA.",
            "..A..",
            ".....",
            "..A..",
        };

        /// <summary>Focus brackets around a chest, 15x12 (chest sits in the middle).</summary>
        public static readonly string[] FocusBrackets =
        {
            "AAA.........AAA",
            "A.............A",
            "A.............A",
            "...............",
            "...............",
            "...............",
            "...............",
            "...............",
            "...............",
            "A.............A",
            "A.............A",
            "AAA.........AAA",
        };

        // ---------------- HUD ----------------

        /// <summary>Coin, 8x8.</summary>
        public static readonly string[] Coin =
        {
            "..AAAA..",
            ".A....A.",
            "A..SS..A",
            "A.S..S.A",
            "A.S..S.A",
            "A..SS..A",
            ".A....A.",
            "..AAAA..",
        };

        /// <summary>Arrow pointing right (look right to the map), 6x7.</summary>
        public static readonly string[] ArrowRight =
        {
            "..A...",
            "...A..",
            "....A.",
            ".....A",
            "....A.",
            "...A..",
            "..A...",
        };

        // Toolbar icons, 12x12 each.
        public static readonly string[] IconTodo =
        {
            "............",
            ".SSSSSSSSSS.",
            ".S........S.",
            ".S......A.S.",
            ".S.SSS.A..S.",
            ".S....AA..S.",
            ".S...A.A..S.",
            ".S.SSS....S.",
            ".S........S.",
            ".S.SSSSSS.S.",
            ".SSSSSSSSSS.",
            "............",
        };

        public static readonly string[] IconMap =
        {
            "............",
            ".SSSSSSSSSS.",
            ".S...S....S.",
            ".S...S.A.AS.",
            ".S...S..A.S.",
            ".S...S.A.AS.",
            ".S...S....S.",
            ".S.II.S...S.",
            ".S....S...S.",
            ".S....S...S.",
            ".SSSSSSSSSS.",
            "............",
        };

        public static readonly string[] IconCollection =
        {
            "............",
            "...SSSSSSS..",
            "...S.....S..",
            ".SSSSSSS.S..",
            ".S.....S.S..",
            ".S..A..S.S..",
            ".S.AAA.S.S..",
            ".S..A..S.S..",
            ".S.....SSS..",
            ".S.....S....",
            ".SSSSSSS....",
            "............",
        };

        public static readonly string[] IconUpgrade =
        {
            "............",
            ".....AA.....",
            "....AAAA....",
            "...AA..AA...",
            "..AA....AA..",
            "....S..S....",
            "....S..S....",
            "....S..S....",
            "....S..S....",
            "..SSSSSSSS..",
            "............",
            "............",
        };

        public static readonly string[] IconPomodoro =
        {
            "....SSSS....",
            "............",
            "...SSSSSS...",
            "..S......S..",
            ".S...A....S.",
            ".S...A....S.",
            ".S...AAA..S.",
            ".S........S.",
            "..S......S..",
            "...SSSSSS...",
            "............",
            "............",
        };

        public static readonly string[] IconSettings =
        {
            "............",
            ".....SS.....",
            "..SS.SS.SS..",
            "..SSSSSSSS..",
            "...SS..SS...",
            ".SSS....SSS.",
            ".SSS....SSS.",
            "...SS..SS...",
            "..SSSSSSSS..",
            "..SS.SS.SS..",
            ".....SS.....",
            "............",
        };
    }
}
