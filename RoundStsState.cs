namespace STSFifth
{
    public sealed class RoundStsState
    {
        public int RoundId { get; private set; }

        public bool HasSummonedSts { get; set; }

        public bool IsSummonInProgress { get; set; }

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        //public bool IsOmegaArmed { get; set; }
        //public bool IsOmegaPaused { get; set; }
        //public float OmegaRemainingSeconds { get; set; }
        //public bool IsOmegaDetonated { get; set; }
        //public bool IsVanillaWarheadActive { get; set; }
        //public int OmegaAudioSessionId { get; set; } = -1;

        public void Reset(int roundId)
        {
            RoundId = roundId;
            HasSummonedSts = false;
            IsSummonInProgress = false;
            // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
            //IsOmegaArmed = false;
            //IsOmegaPaused = false;
            //OmegaRemainingSeconds = 0f;
            //IsOmegaDetonated = false;
            //IsVanillaWarheadActive = false;
            //OmegaAudioSessionId = -1;
        }
    }
}
