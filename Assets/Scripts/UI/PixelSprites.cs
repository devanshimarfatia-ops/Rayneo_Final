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

        /// <summary>Blind card pack: two stacked cards with a mark, 12x12.</summary>
        public static readonly string[] Pack =
        {
            "...IIIIIIII.",
            "...I......I.",
            "SSSSSSSS..I.",
            "S......S..I.",
            "S..AA..S..I.",
            "S.AAAA.S..I.",
            "S.AAAA.S..I.",
            "S..AA..S..I.",
            "S......SIII.",
            "S......S....",
            "SSSSSSSS....",
            "............",
        };

        /// <summary>Tutorial pointer: double chevron pointing down, 9x5.</summary>
        public static readonly string[] ArrowDown =
        {
            "AA.....AA",
            ".AA...AA.",
            "..AA.AA..",
            "...AAA...",
            "....A....",
        };

        /// <summary>Single chevron, 6x7: focus marker in panel rows.</summary>
        public static readonly string[] Chevron =
        {
            "..A...",
            "...A..",
            "....A.",
            ".....A",
            "....A.",
            "...A..",
            "..A...",
        };

        /// <summary>Arrow pointing right (look right to the map): double chevron, 9x7.</summary>
        public static readonly string[] ArrowRight =
        {
            "A...A....",
            ".A...A...",
            "..A...A..",
            "...A...A.",
            "..A...A..",
            ".A...A...",
            "A...A....",
        };

        // Toolbar icons, 12x12 each (first device test: icons must read at a glance, RayNeo-like shapes).

        /// <summary>Anchors: diamond with a filled core.</summary>
        public static readonly string[] IconAnchor =
        {
            ".....SS.....",
            "....S..S....",
            "...S....S...",
            "..S......S..",
            ".S...AA...S.",
            "S...AAAA...S",
            "S...AAAA...S",
            ".S...AA...S.",
            "..S......S..",
            "...S....S...",
            "....S..S....",
            ".....SS.....",
        };

        /// <summary>To-do: notepad with lines and a pen (like the RayNeo To-do app icon).</summary>
        public static readonly string[] IconTodo =
        {
            ".SSSSSSSS...",
            ".S......S.AA",
            ".S.SSSS.SAAA",
            ".S......AAA.",
            ".S.SSS.AAA..",
            ".S....AAA...",
            ".S.SSAAA....",
            ".S...AA.S...",
            ".S...A..S...",
            ".S......S...",
            ".SSSSSSSS...",
            "............",
        };

        /// <summary>Collection: treasure chest.</summary>
        public static readonly string[] IconChest =
        {
            "............",
            "..SSSSSSSS..",
            ".S........S.",
            "S..........S",
            "SSSSSSSSSSSS",
            "S....AA....S",
            "S...A..A...S",
            "S....AA....S",
            "S..........S",
            "SSSSSSSSSSSS",
            "............",
            "............",
        };

        /// <summary>Profile: character bust (upgrades, later character looks).</summary>
        public static readonly string[] IconProfile =
        {
            "....SSSS....",
            "...S....S...",
            "...S....S...",
            "...S....S...",
            "....SSSS....",
            ".....SS.....",
            "...SSSSSS...",
            "..S......S..",
            ".S........S.",
            ".S...AA...S.",
            ".S........S.",
            ".SSSSSSSSSS.",
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
