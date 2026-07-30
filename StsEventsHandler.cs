using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.CustomHandlers;

namespace STSFifth
{
    public sealed class StsEventsHandler : CustomEventsHandler
    {
        private readonly StsService stsService;
        private readonly StsNukeService nukeService;

        public StsEventsHandler(StsService stsService, StsNukeService nukeService)
        {
            this.stsService = stsService;
            this.nukeService = nukeService;
        }

        public override void OnServerRoundStarted()
        {
            stsService.HandleRoundStarted();
        }

        public override void OnServerRoundEnded(RoundEndedEventArgs ev)
        {
            stsService.HandleRoundEnded();
        }

        public override void OnServerRoundEnding(RoundEndingEventArgs ev)
        {
            nukeService.HandleRoundEnding(ev);
        }

        public override void OnPlayerDeath(PlayerDeathEventArgs ev)
        {
            stsService.HandlePlayerDeath(ev);
        }

        public override void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            stsService.HandlePlayerLeft(ev);
        }

        public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev)
        {
            stsService.HandlePlayerChangedRole(ev);
        }

        public override void OnPlayerReceivedLoadout(PlayerReceivedLoadoutEventArgs ev)
        {
            stsService.HandlePlayerReceivedLoadout(ev);
        }

        public override void OnPlayerSpawning(PlayerSpawningEventArgs ev)
        {
            stsService.HandlePlayerSpawning(ev);
        }

        public override void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
        {
            stsService.HandlePlayerSpawned(ev);
        }

        public override void OnWarheadStarting(WarheadStartingEventArgs ev)
        {
            nukeService.HandleWarheadStarting(ev);
        }

        public override void OnWarheadStopping(WarheadStoppingEventArgs ev)
        {
            nukeService.HandleWarheadStopping(ev);
        }
    }
}
