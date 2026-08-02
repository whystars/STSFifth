using MEC;

namespace STSFifth
{
    public sealed class RoundStsState
    {
        public int RoundId { get; private set; }

        public bool HasSummonedSts { get; set; }

        public bool IsSummonInProgress { get; set; }

        // Omega 核弹状态
        public bool IsOmegaArmed { get; set; }
        public bool IsOmegaPaused { get; set; }
        public float OmegaRemainingSeconds { get; set; }
        public bool IsOmegaDetonated { get; set; }
        public int OmegaStartedByPlayerId { get; set; }
        public bool IsAlphaLockedThisRound { get; set; }
        public CoroutineHandle OmegaCoroutineHandle { get; set; }

        public void Reset(int roundId)
        {
            RoundId = roundId;
            HasSummonedSts = false;
            IsSummonInProgress = false;

            // 重置 Omega 核弹状态
            IsOmegaArmed = false;
            IsOmegaPaused = false;
            OmegaRemainingSeconds = 0f;
            IsOmegaDetonated = false;
            OmegaStartedByPlayerId = -1;
            IsAlphaLockedThisRound = false;

            if (OmegaCoroutineHandle.IsRunning)
                Timing.KillCoroutines(OmegaCoroutineHandle);

            OmegaCoroutineHandle = default;
        }
    }
}
