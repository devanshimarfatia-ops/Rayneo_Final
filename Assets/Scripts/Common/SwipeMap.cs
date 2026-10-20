namespace RayneoIdle
{
    /// <summary>
    /// One project-wide mapping for horizontal temple swipes, so every left / right list behaves the same.
    ///
    /// Convention: swipe FORWARD (toward the eyes' front) = move LEFT / previous (-1),
    ///             swipe BACK = move RIGHT / next (+1).
    ///
    /// Device fact (X3 Pro, ARDK 1.1.2, 2026-10-19): a forward swipe fires SimpleTouchForLite.OnSwipeRight
    /// (the ARDK maps LEFT_SWIPE_STEP -> OnSwipeRight). The ARDK has no system setting for swipe direction.
    /// Flip ForwardMovesLeft to change the convention everywhere; per-component invertSwipe flips it locally.
    /// </summary>
    public static class SwipeMap
    {
        public const bool ForwardMovesLeft = true;

        /// <summary>Step (-1 / +1) for SimpleTouchForLite.OnSwipeRight (= physical forward swipe).</summary>
        public static int OnSwipeRightStep(bool invert) => Apply(ForwardMovesLeft ? -1 : +1, invert);

        /// <summary>Step (-1 / +1) for SimpleTouchForLite.OnSwipeLeft (= physical back swipe).</summary>
        public static int OnSwipeLeftStep(bool invert) => Apply(ForwardMovesLeft ? +1 : -1, invert);

        private static int Apply(int step, bool invert) => invert ? -step : step;
    }
}
