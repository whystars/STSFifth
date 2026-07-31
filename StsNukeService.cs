using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace STSFifth
{
    // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
    // 当前所有核弹相关功能已暂时移除，保留类框架以便后续恢复
    public sealed class StsNukeService
    {
        private const string LogPrefix = "[STSFifth]";

        private readonly StsConfig config;
        private readonly StsTranslation translation;
        private readonly StsAudioService audioService;
        private readonly StsHudService hudService;
        private readonly StsSpawnService spawnService;
        private readonly StsService stsService;

        public StsNukeService(
            StsConfig config,
            StsTranslation translation,
            StsAudioService audioService,
            StsHudService hudService,
            StsSpawnService spawnService,
            StsService stsService)
        {
            this.config = config ?? StsConfig.CreateDefault();
            this.translation = translation ?? StsTranslation.CreateDefault();
            this.audioService = audioService;
            this.hudService = hudService;
            this.spawnService = spawnService;
            this.stsService = stsService;
        }

        // TODO: 待后续设计文档完善后重新实现
        public void HandleWarheadStarting(WarheadStartingEventArgs ev)
        {
            // 核弹功能已暂时移除
        }

        // TODO: 待后续设计文档完善后重新实现
        public void HandleWarheadStopping(WarheadStoppingEventArgs ev)
        {
            // 核弹功能已暂时移除
        }

        // TODO: 待后续设计文档完善后重新实现
        public void HandleRoundEnding(RoundEndingEventArgs ev)
        {
            // 核弹功能已暂时移除
        }

        public void StopAll(string reason)
        {
            // 核弹功能已暂时移除，保留方法签名
            Logger.Info($"{LogPrefix} StsNukeService.StopAll 被调用，原因：{reason}（核弹功能已暂时移除）");
        }

        // TODO: 待后续设计文档完善后重新实现
        public void ForceStartNuke()
        {
            // 核弹功能已暂时移除
        }

        // TODO: 待后续设计文档完善后重新实现
        public void ForceStopNuke()
        {
            // 核弹功能已暂时移除
        }
    }
}
