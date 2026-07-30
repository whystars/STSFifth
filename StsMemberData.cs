namespace STSFifth
{
    public sealed class StsMemberData
    {
        public int PlayerId { get; set; }

        public bool IsStsMember { get; set; }

        public StsRole Role { get; set; } = StsRole.None;

        public int RoundId { get; set; }

        public bool PresentationApplied { get; set; }

        public int PresentationRefreshSequence { get; set; }

        public bool HasReservedSpawn { get; set; }

        public UnityEngine.Vector3 ReservedSpawnPosition { get; set; }

        public float ReservedSpawnHorizontalRotation { get; set; }

        public bool ReservedSpawnApplied { get; set; }

        public string AppliedCustomInfo { get; set; }

        public string OriginalCustomInfo { get; set; }

        public global::PlayerInfoArea OriginalInfoArea { get; set; }

        public bool HasOriginalDisplayState { get; set; }
    }
}
