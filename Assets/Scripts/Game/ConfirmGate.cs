namespace RayneoIdle.Game
{
    /// <summary>
    /// Multi-step confirmation for destructive actions (Reset, D10: two confirmations).
    /// Each Press() moves one step; the action fires on the last step. The gate falls back to
    /// step 0 if the next press does not come within <see cref="TimeoutSeconds"/>, or on Cancel().
    /// Plain C#: the caller passes the current time so it can be tested.
    /// </summary>
    public class ConfirmGate
    {
        /// <summary>Presses needed: 1 = open the prompt, 2 = "Are you sure?", 3 = do it.</summary>
        public int StepsRequired { get; }

        /// <summary>Seconds a step stays armed before the gate resets.</summary>
        public double TimeoutSeconds { get; }

        /// <summary>0 = idle; 1..StepsRequired-1 = waiting for the next confirm.</summary>
        public int Step { get; private set; }

        private double armedUntil;

        public ConfirmGate(int stepsRequired = 3, double timeoutSeconds = 5)
        {
            StepsRequired = stepsRequired;
            TimeoutSeconds = timeoutSeconds;
        }

        /// <summary>Returns true when this press completed the last step (caller runs the action).</summary>
        public bool Press(double now)
        {
            if (Step > 0 && now > armedUntil) Step = 0;
            Step++;
            if (Step >= StepsRequired)
            {
                Step = 0;
                return true;
            }
            armedUntil = now + TimeoutSeconds;
            return false;
        }

        public void Cancel() => Step = 0;

        /// <summary>Call each frame (or before reading Step) so a stale prompt closes by itself.</summary>
        public void Update(double now)
        {
            if (Step > 0 && now > armedUntil) Step = 0;
        }
    }
}
