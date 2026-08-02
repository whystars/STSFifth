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
        private readonly Dictionary<Room, Color> originalLightColors = new Dictionary<Room, Color>();

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

        public void HandleItemThrown(Player player, ItemPickupBase pickup)
        {
            if (!config.Nuke.IsEnabled)
                return;

            // 检查是否为硬币
            if (pickup?.Info.ItemId != ItemType.Coin)
                return;

            // 检查是否为 STS-5 成员
            if (!stsService.IsStsRole(player))
                return;

            // 检查是否在核弹室
            if (!IsPlayerInNukeRoom(player))
                return;

            // 检查是否处于暂停状态（可重启）
            if (stsService.RoundState.IsOmegaPaused)
            {
                RestartOmegaNuke(player);
                return;
            }

            // 检查核弹是否已启动
            if (stsService.RoundState.IsOmegaArmed)
            {
                Logger.Info($"{LogPrefix} {player.Nickname} 尝试启动 Omega 核弹，但核弹已在运行中。");
                return;
            }

            // 启动核弹
            StartOmegaNuke(player);
        }

        public void HandleWarheadStarting(WarheadStartingEventArgs ev)
        {
            // 如果本局 Alpha 核弹已被锁定，阻止启动
            if (stsService.RoundState.IsAlphaLockedThisRound)
            {
                ev.IsAllowed = false;
                Logger.Info($"{LogPrefix} Alpha 核弹已被 Omega 核弹锁定，无法启动。");

                // 向尝试启动的玩家发送提示
                if (ev.Player != null)
                {
                    ev.Player.ShowHint("<color=red>Alpha 核弹已被 Omega 核弹锁定！</color>", 3f);
                }
            }
        }

        public void HandleWarheadStopping(WarheadStoppingEventArgs ev)
        {
            RoundStsState state = stsService.RoundState;

            // 如果是停止 Alpha 核弹
            if (!state.IsOmegaArmed)
            {
                // 检查是否本局已锁定 Alpha
                if (state.IsAlphaLockedThisRound)
                {
                    ev.IsAllowed = false; // 阻止停止（保持锁定）
                    Logger.Info($"{LogPrefix} Alpha 核弹已被 Omega 核弹锁定，无法操作。");
                }
                return;
            }

            // 如果是停止 Omega 核弹
            StopOmegaNuke(ev.Player);
        }

        public void HandleRoundEnding(RoundEndingEventArgs ev)
        {
            // 核弹爆炸逻辑将在这里处理
        }

        public void StopAll(string reason)
        {
            RoundStsState state = stsService.RoundState;

            if (state.IsOmegaArmed || state.IsOmegaPaused)
            {
                // 停止倒计时协程
                if (state.OmegaCoroutineHandle.IsRunning)
                    Timing.KillCoroutines(state.OmegaCoroutineHandle);

                // 停止音频
                audioService?.StopOmegaNukeAudio();

                // 恢复灯光
                RestoreFacilityLights();

                // 清除所有玩家的倒计时 HUD
                ClearAllPlayersCountdownHud();

                // 重置状态
                state.IsOmegaArmed = false;
                state.IsOmegaPaused = false;
                state.OmegaRemainingSeconds = 0f;
            }

            Logger.Info($"{LogPrefix} StsNukeService.StopAll 已执行，原因：{reason}");
        }

        public void ForceStartNuke()
        {
            // 管理员命令强制启动
            StartOmegaNuke(null);
        }

        public void ForceStopNuke()
        {
            // 管理员命令强制停止
            StopOmegaNuke(null);
        }

        private void StartOmegaNuke(Player activator)
        {
            if (stsService.RoundState.IsOmegaArmed)
            {
                Logger.Warn($"{LogPrefix} Omega 核弹已在运行，无法重复启动。");
                return;
            }

            Logger.Info($"{LogPrefix} {activator?.Nickname ?? "系统"} 启动了 Omega 核弹！");

            // 更新状态
            RoundStsState state = stsService.RoundState;
            state.IsOmegaArmed = true;
            state.IsOmegaPaused = false;
            state.OmegaRemainingSeconds = config.Nuke.DetonationSeconds;
            state.OmegaStartedByPlayerId = activator?.PlayerId ?? -1;
            state.IsOmegaDetonated = false;

            // 强制停止并锁定 Alpha 核弹
            LockAlphaWarhead();

            // 播放 CASSIE 公告
            audioService?.PlayOmegaNukeStartCassie();

            // 启动核弹音频
            audioService?.StartOmegaNukeAudio();

            // 变色灯光
            if (config.Nuke.EnableLightEffect)
                SetFacilityLightsColor(true);

            // 启动倒计时协程
            state.OmegaCoroutineHandle = Timing.RunCoroutine(OmegaNukeCountdownCoroutine());
        }

        private void RestartOmegaNuke(Player activator)
        {
            RoundStsState state = stsService.RoundState;

            // 只有在暂停状态才能重启
            if (!state.IsOmegaPaused)
            {
                Logger.Warn($"{LogPrefix} Omega 核弹未处于暂停状态，无法重启。");
                return;
            }

            Logger.Info($"{LogPrefix} {activator.Nickname} 重启了 Omega 核弹！");

            // 重置倒计时（设计要求）
            state.OmegaRemainingSeconds = config.Nuke.DetonationSeconds;
            state.IsOmegaArmed = true;
            state.IsOmegaPaused = false;

            // 播放重启 CASSIE 公告
            audioService?.PlayOmegaNukeRestartCassie();

            // 重新启动音频
            audioService?.StartOmegaNukeAudio();

            // 重新变色灯光
            if (config.Nuke.EnableLightEffect)
                SetFacilityLightsColor(true);

            // 重启倒计时协程
            state.OmegaCoroutineHandle = Timing.RunCoroutine(OmegaNukeCountdownCoroutine());
        }

        private void StopOmegaNuke(Player stopper)
        {
            RoundStsState state = stsService.RoundState;

            if (!state.IsOmegaArmed)
                return;

            Logger.Info($"{LogPrefix} {stopper?.Nickname ?? "系统"} 停止了 Omega 核弹。");

            // 暂停状态（而非完全终止）
            state.IsOmegaPaused = true;
            state.IsOmegaArmed = false;

            // 停止协程
            if (state.OmegaCoroutineHandle.IsRunning)
                Timing.KillCoroutines(state.OmegaCoroutineHandle);

            // 停止音频
            audioService?.StopOmegaNukeAudio();

            // 恢复灯光
            RestoreFacilityLights();

            // 清除所有玩家的倒计时 HUD
            ClearAllPlayersCountdownHud();

            // 播放停止 CASSIE 公告
            audioService?.PlayOmegaNukeStopCassie();
        }

        private IEnumerator<float> OmegaNukeCountdownCoroutine()
        {
            RoundStsState state = stsService.RoundState;

            while (state.OmegaRemainingSeconds > 0)
            {
                // 检查是否被外部停止
                if (!state.IsOmegaArmed || state.IsOmegaPaused)
                {
                    Logger.Info($"{LogPrefix} Omega 核弹倒计时被中断。");
                    yield break;
                }

                // 更新所有玩家的 HUD
                UpdateAllPlayersCountdownHud(state.OmegaRemainingSeconds);

                // 等待1秒
                yield return Timing.WaitForSeconds(1f);

                // 递减时间
                state.OmegaRemainingSeconds--;
            }

            // 倒计时结束，触发爆炸
            DetonateOmegaNuke();
        }

        private void DetonateOmegaNuke()
        {
            Logger.Info($"{LogPrefix} Omega 核弹爆炸！");

            RoundStsState state = stsService.RoundState;
            state.IsOmegaDetonated = true;
            state.IsOmegaArmed = false;

            // TODO: 实现爆炸逻辑
            // - 杀死非基金会阵营
            // - 传送基金会到地表
            // - 强制结束回合，基金会胜利
        }

        private void LockAlphaWarhead()
        {
            // 检查 Alpha 核弹是否正在运行
            if (Warhead.IsDetonationInProgress)
            {
                // 强制停止 Alpha 核弹
                Warhead.Stop();
                Logger.Info($"{LogPrefix} 已强制停止正在运行的 Alpha 核弹。");
            }

            // 标记本局 Alpha 已锁定
            stsService.RoundState.IsAlphaLockedThisRound = true;

            Logger.Info($"{LogPrefix} 已锁定 Alpha 核弹本局。");
        }

        private bool IsPlayerInNukeRoom(Player player)
        {
            if (player?.CurrentRoom == null)
                return false;

            return player.CurrentRoom.Type == RoomType.HczNuke;
        }

        private void SetFacilityLightsColor(bool enable)
        {
            if (!config.Nuke.EnableLightEffect)
                return;

            Color targetColor = new Color(
                config.Nuke.LightColorR / 255f,
                config.Nuke.LightColorG / 255f,
                config.Nuke.LightColorB / 255f
            );

            foreach (Room room in Room.List)
            {
                if (room?.LightController == null)
                    continue;

                if (enable)
                {
                    // 保存原始颜色
                    if (!originalLightColors.ContainsKey(room))
                        originalLightColors[room] = room.LightController.NetworkLightColor;

                    // 设置为目标颜色
                    room.LightController.NetworkLightColor = targetColor;
                }
                else
                {
                    // 恢复原始颜色
                    if (originalLightColors.TryGetValue(room, out Color originalColor))
                    {
                        room.LightController.NetworkLightColor = originalColor;
                        originalLightColors.Remove(room);
                    }
                }
            }

            Logger.Info($"{LogPrefix} 已{(enable ? "设置" : "恢复")}设施灯光颜色。");
        }

        private void RestoreFacilityLights()
        {
            SetFacilityLightsColor(false);
        }

        private void UpdateAllPlayersCountdownHud(float remainingSeconds)
        {
            foreach (Player player in Player.GetPlayers())
            {
                if (player != null && player.IsAlive)
                {
                    hudService?.ShowOmegaNukeCountdown(player, remainingSeconds);
                }
            }
        }

        private void ClearAllPlayersCountdownHud()
        {
            foreach (Player player in Player.GetPlayers())
            {
                if (player != null)
                {
                    hudService?.ClearOmegaNukeCountdown(player);
                }
            }
        }
    }
}
