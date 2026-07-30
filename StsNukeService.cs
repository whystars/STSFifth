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
    public sealed class StsNukeService
    {
        private const string LogPrefix = "[STSFifth]";

        private readonly StsConfig config;
        private readonly StsTranslation translation;
        private readonly StsAudioService audioService;
        private readonly StsHudService hudService;
        private readonly StsSpawnService spawnService;
        private readonly StsService stsService;

        private CoroutineHandle countdownCoroutine;

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

        public void HandleWarheadStarting(WarheadStartingEventArgs ev)
        {
            if (ev == null || ev.Player == null)
            {
                return;
            }

            RoundStsState state = stsService.RoundState;

            if (state.IsOmegaArmed || Warhead.IsDetonationInProgress)
            {
                ev.IsAllowed = false;
                return;
            }

            if (!stsService.IsActiveStsMember(ev.Player))
            {
                ev.IsAllowed = false;
                return;
            }

            ev.IsAllowed = true;
            StartOmegaNuke();
        }

        public void HandleWarheadStopping(WarheadStoppingEventArgs ev)
        {
            RoundStsState state = stsService.RoundState;

            if (!state.IsOmegaArmed)
            {
                ev.IsAllowed = false;
                return;
            }

            ev.IsAllowed = true;
            StopOmegaNuke();
        }

        public void HandleRoundEnding(RoundEndingEventArgs ev)
        {
            RoundStsState state = stsService.RoundState;

            if (state.IsOmegaDetonated && ev != null)
            {
                ev.LeadingTeam = RoundSummary.LeadingTeam.FacilityForces;
                ev.IsAllowed = true;
            }
        }

        public void StopAll(string reason)
        {
            if (countdownCoroutine.IsRunning)
            {
                Timing.KillCoroutines(countdownCoroutine);
                Logger.Info($"{LogPrefix} 已停止 Omega 核弹倒计时协程。原因：{reason}");
            }

            ClearAllNukeCountdownHud();
        }

        public void ForceStartNuke()
        {
            StartOmegaNuke();
        }

        public void ForceStopNuke()
        {
            StopOmegaNuke();
        }

        private void StartOmegaNuke()
        {
            RoundStsState state = stsService.RoundState;

            if (state.IsOmegaArmed && !state.IsOmegaPaused)
            {
                Logger.Warn($"{LogPrefix} Omega 核弹已在运行，忽略重复启动请求。");
                return;
            }

            if (!state.IsOmegaArmed)
            {
                state.IsOmegaArmed = true;
                state.OmegaRemainingSeconds = config.Nuke.CountdownSeconds;
            }

            state.IsOmegaPaused = false;

            audioService?.PlayNukeStartAnnouncement();

            if (countdownCoroutine.IsRunning)
            {
                Timing.KillCoroutines(countdownCoroutine);
            }

            countdownCoroutine = Timing.RunCoroutine(CountdownCoroutine());

            Logger.Info($"{LogPrefix} Omega 核弹已启动，剩余时间：{state.OmegaRemainingSeconds:0.0} 秒。");
        }

        private void StopOmegaNuke()
        {
            RoundStsState state = stsService.RoundState;

            if (!state.IsOmegaArmed)
            {
                Logger.Warn($"{LogPrefix} Omega 核弹未启动，忽略关闭请求。");
                return;
            }

            state.IsOmegaPaused = true;

            if (countdownCoroutine.IsRunning)
            {
                Timing.KillCoroutines(countdownCoroutine);
            }

            ClearAllNukeCountdownHud();

            // 先停止正在循环播放的核弹启动音乐
            audioService?.StopAll("核弹关闭");

            try
            {
                string closedText = translation.NukeClosedNotificationText ?? "Omega核弹已被关闭，可重新开启";
                foreach (Player player in Player.List)
                {
                    if (player != null)
                    {
                        hudService.ShowNotification(player, closedText, config.Nuke.NotificationDurationSeconds);
                    }
                }

                string announcement = translation.NukeStopCassieAnnouncement ?? "OMEGA WARHEAD CANCELLED";
                LabApi.Features.Wrappers.Announcer.Message(
                    announcement,
                    translation.NukeStopCassieText ?? "Omega核弹遭到关闭，请重新开启",
                    false,
                    0f,
                    0f);
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 播放核弹关闭通知失败：{exception.Message}");
            }

            Logger.Info($"{LogPrefix} Omega 核弹已关闭，剩余时间：{state.OmegaRemainingSeconds:0.0} 秒。");
        }

        private IEnumerator<float> CountdownCoroutine()
        {
            RoundStsState state = stsService.RoundState;

            while (state.OmegaRemainingSeconds > 0f)
            {
                UpdateAllNukeCountdownHud(state.OmegaRemainingSeconds);
                yield return Timing.WaitForSeconds(1f);

                state.OmegaRemainingSeconds -= 1f;

                if (state.IsOmegaPaused)
                {
                    Logger.Info($"{LogPrefix} Omega 核弹倒计时已暂停。");
                    yield break;
                }
            }

            TriggerDetonation();
        }

        private void TriggerDetonation()
        {
            RoundStsState state = stsService.RoundState;
            state.IsOmegaDetonated = true;

            Logger.Info($"{LogPrefix} Omega 核弹倒计时归零，开始爆炸结算。");

            ClearAllNukeCountdownHud();

            List<Player> foundationPlayers = new List<Player>();
            List<Player> nonFoundationPlayers = new List<Player>();

            foreach (Player player in Player.List)
            {
                if (player == null || !player.IsAlive)
                {
                    continue;
                }

                if (player.Faction == Faction.FoundationStaff)
                {
                    foundationPlayers.Add(player);
                }
                else
                {
                    nonFoundationPlayers.Add(player);
                }
            }

            Logger.Info($"{LogPrefix} 爆炸结算：基金会={foundationPlayers.Count}，非基金会={nonFoundationPlayers.Count}。");

            List<StsSpawnPoint> escapePoints = spawnService.GetEscapeZoneSpawnPlans(foundationPlayers.Count);
            for (int i = 0; i < foundationPlayers.Count; i++)
            {
                Player player = foundationPlayers[i];
                StsSpawnPoint escapePoint = i < escapePoints.Count ? escapePoints[i] : escapePoints.LastOrDefault();

                if (escapePoint != null)
                {
                    try
                    {
                        player.Position = escapePoint.Position;
                        Logger.Info($"{LogPrefix} 已传送基金会玩家到逃生区：{FormatPlayer(player)}。");
                    }
                    catch (Exception exception)
                    {
                        Logger.Warn($"{LogPrefix} 传送基金会玩家失败：{FormatPlayer(player)}，错误：{exception.Message}");
                    }
                }
            }

            try
            {
                Warhead.Shake();
                Warhead.OpenBlastDoors();
                Logger.Info($"{LogPrefix} 已播放爆炸特效（震屏+开门）。");
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 播放爆炸特效失败：{exception.Message}");
            }

            string deathReason = translation.NukeDeathReasonText ?? "你在Omega核弹爆炸中消失了";
            foreach (Player player in nonFoundationPlayers)
            {
                try
                {
                    bool killed = player.Kill(deathReason, string.Empty);
                    if (!killed)
                    {
                        Logger.Warn($"{LogPrefix} 杀死非基金会玩家失败（返回false）：{FormatPlayer(player)}。");
                    }
                }
                catch (Exception exception)
                {
                    Logger.Warn($"{LogPrefix} 杀死非基金会玩家时发生异常：{FormatPlayer(player)}，错误：{exception.Message}");
                }
            }

            try
            {
                string secretText = translation.NukeSecretHintText ?? "核辐射下的秘密";
                foreach (Player player in Player.List)
                {
                    if (player != null)
                    {
                        hudService.ShowNotification(player, secretText, config.Nuke.SecretHintDurationSeconds);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 显示\"核辐射下的秘密\"提示失败：{exception.Message}");
            }

            Timing.CallDelayed(config.Nuke.EndRoundDelaySeconds, () =>
            {
                try
                {
                    bool ended = Round.End(true);
                    Logger.Info($"{LogPrefix} 已请求强制结束回合，Round.End 返回：{ended}。");
                }
                catch (Exception exception)
                {
                    Logger.Error($"{LogPrefix} 强制结束回合失败：{exception.Message}");
                }
            });

            Logger.Info($"{LogPrefix} Omega 核弹爆炸结算完成，{config.Nuke.EndRoundDelaySeconds:0.0} 秒后结束回合。");
        }

        private void UpdateAllNukeCountdownHud(float remainingSeconds)
        {
            try
            {
                foreach (Player player in Player.List)
                {
                    if (player != null)
                    {
                        hudService.ShowNukeCountdown(player, remainingSeconds);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 更新核弹倒计时 HUD 失败：{exception.Message}");
            }
        }

        private void ClearAllNukeCountdownHud()
        {
            try
            {
                foreach (Player player in Player.List)
                {
                    if (player != null)
                    {
                        hudService.HideNukeCountdown(player);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 清理核弹倒计时 HUD 失败：{exception.Message}");
            }
        }

        private static string FormatPlayer(Player player)
        {
            if (player == null)
            {
                return "<null>";
            }

            string nickname = string.IsNullOrWhiteSpace(player.Nickname) ? "<无昵称>" : player.Nickname;
            return $"{nickname}({player.PlayerId})";
        }
    }
}
