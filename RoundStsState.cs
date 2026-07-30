namespace STSFifth
{
    public sealed class RoundStsState
    {
        public int RoundId { get; private set; }

        public bool HasSummonedSts { get; set; }

        public bool IsSummonInProgress { get; set; }

        public bool IsOmegaArmed { get; set; }

        public bool IsOmegaPaused { get; set; }

        public float OmegaRemainingSeconds { get; set; }

        public bool IsOmegaDetonated { get; set; }

        public bool IsVanillaWarheadActive { get; set; }

        public int OmegaAudioSessionId { get; set; } = -1;

        public void Reset(int roundId)
        {
            RoundId = roundId;
            HasSummonedSts = false;
            IsSummonInProgress = false;
            IsOmegaArmed = false;
            IsOmegaPaused = false;
            OmegaRemainingSeconds = 0f;
            IsOmegaDetonated = false;
            IsVanillaWarheadActive = false;
            OmegaAudioSessionId = -1;
        }
    }
}
