using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace STSFifth
{
    public sealed class StsService
    {
        private const string LogPrefix = "[STSFifth]";
        private const float PresentationStabilizationDelaySeconds = 0.2f;

        private static readonly StsRole[] AssignableStsRoles =
        {
            StsRole.Commander,
            StsRole.Suppressor,
            StsRole.Specialist,
            StsRole.Elite,
            StsRole.Soldier
        };

        private readonly StsConfig config;
        private readonly StsTranslation translation;
        private readonly StsPresentationService presentationService;
        private readonly StsSpawnService spawnService;
        private readonly StsAudioService audioService;
        private readonly Dictionary<int, StsMemberData> memberDataByPlayerId = new Dictionary<int, StsMemberData>();
        private readonly System.Random random = new System.Random();
        private readonly RoundStsState roundState = new RoundStsState();

        private int roundSequence;
        private bool allowDelayedStsSpawn;

        public StsService(
            StsConfig config,
            StsTranslation translation,
            StsAudioService audioService,
            StsPresentationService presentationService,
            StsSpawnService spawnService)
        {
            this.config = config ?? StsConfig.CreateDefault();
            this.translation = translation ?? StsTranslation.CreateDefault();
            this.presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));
            this.spawnService = spawnService ?? throw new ArgumentNullException(nameof(spawnService));
            this.audioService = audioService;
        }

        public RoundStsState RoundState => roundState;

        public void HandleRoundStarted()
        {
            roundSequence++;
            allowDelayedStsSpawn = true;
            audioService?.StopAll("新回合开始前重置");
            int clearedCount = ClearAllPlayerPresentation("新回合开始前重置");
            roundState.Reset(roundSequence);
            int scheduledRoundId = roundState.RoundId;

            Logger.Info($"{LogPrefix} 回合开始，已重置第五特别行动组状态。RoundId={roundState.RoundId}，清理玩家数：{clearedCount}，将在 {config.SpawnDelayMinutes:0.0} 分钟后触发生成。");

            float delaySeconds = config.SpawnDelayMinutes * 60f;
            Timing.CallDelayed(delaySeconds, () => TrySpawnStsForRound(scheduledRoundId));
        }

        public void HandleRoundEnded()
        {
            ClearRoundState("回合结束");
        }

        public void ClearRoundState(string reason)
        {
            allowDelayedStsSpawn = false;
            audioService?.StopAll(reason);
            int clearedCount = ClearAllPlayerPresentation(reason);
            roundState.Reset(roundSequence);

            Logger.Info($"{LogPrefix} 已清理第五特别行动组回合状态。原因：{reason}，清理玩家数：{clearedCount}");
        }

        public void HandlePlayerDeath(PlayerDeathEventArgs ev)
        {
            if (ev?.Player != null)
            {
                ClearPlayerData(ev.Player, "玩家死亡");
            }
        }

        public void HandlePlayerLeft(PlayerLeftEventArgs ev)
        {
            if (ev?.Player != null)
            {
                ClearPlayerData(ev.Player, "玩家离开");
            }
        }

        public void HandlePlayerChangedRole(PlayerChangedRoleEventArgs ev)
        {
            if (ev?.Player == null)
            {
                return;
            }

            Player player = ev.Player;
            if (!memberDataByPlayerId.TryGetValue(player.PlayerId, out StsMemberData data))
            {
                return;
            }

            if (!TryResolveCarrierRole(data.Role, out RoleTypeId expectedCarrierRole))
            {
                ClearPlayerData(player, "承载角色配置无法解析");
                return;
            }

            RoleTypeId newRole = ev.NewRole?.RoleTypeId ?? player.Role;
            if (newRole != expectedCarrierRole)
            {
                ClearPlayerData(player, $"玩家切换为非预期承载角色 {newRole}");
                return;
            }

            SchedulePresentationRefresh(player, data);
        }

        public void HandlePlayerReceivedLoadout(PlayerReceivedLoadoutEventArgs ev)
        {
            TryApplyPresentationForActivePlayer(ev?.Player);
        }

        public void HandlePlayerSpawning(PlayerSpawningEventArgs ev)
        {
            if (ev?.Player == null || !ev.IsAllowed)
            {
                return;
            }

            Player player = ev.Player;
            if (!TryGetActiveStsMemberData(player, false, out StsMemberData data, out RoleTypeId expectedCarrierRole))
            {
                return;
            }

            if (ev.Role == null || ev.Role.RoleTypeId != expectedCarrierRole || !data.HasReservedSpawn || data.ReservedSpawnApplied)
            {
                return;
            }

            try
            {
                ev.SetSpawnpoint(data.ReservedSpawnPosition, data.ReservedSpawnHorizontalRotation);
                data.ReservedSpawnApplied = true;
                Logger.Info(
                    $"{LogPrefix} 已为 {FormatPlayer(player)} 设置第五特别行动组安全生成点：{FormatVector(data.ReservedSpawnPosition)}。");
            }
            catch (Exception exception)
            {
                Logger.Warn(
                    $"{LogPrefix} 为 {FormatPlayer(player)} 设置第五特别行动组安全生成点失败，将保留原生出生位置。错误：{exception.Message}");
            }
        }

        public void HandlePlayerSpawned(PlayerSpawnedEventArgs ev)
        {
            TryApplyReservedSpawnAfterSpawn(ev?.Player);
            TryApplyPresentationForActivePlayer(ev?.Player);
        }

        public bool TryAssignStsRoleForCommand(Player target, StsRole role, string executorName, out string reason)
        {
            reason = string.Empty;
            if (target == null)
            {
                reason = "目标玩家不存在。";
                return false;
            }

            if (!AssignableStsRoles.Contains(role))
            {
                reason = $"目标职位不是可分配的第五特别行动组成员职位：{role}。";
                return false;
            }

            if (memberDataByPlayerId.ContainsKey(target.PlayerId))
            {
                ClearPlayerData(target, "管理员命令重新指定第五特别行动组职位");
            }

            StsSpawnPoint spawnPoint = spawnService.CreateSpawnPlans(1).FirstOrDefault();
            bool success = AssignStsRole(target, role, spawnPoint);
            if (success)
            {
                reason = $"已设置第五特别行动组职位：{FormatPlayer(target)} -> {GetRoleDisplayName(role)}。";
                Logger.Info($"{LogPrefix} 管理员命令设置第五特别行动组职位成功。执行者={executorName}，目标={FormatPlayer(target)}，职位={role}");
                return true;
            }

            reason = $"设置第五特别行动组职位失败：{role}。";
            Logger.Warn($"{LogPrefix} 管理员命令设置第五特别行动组职位失败。执行者={executorName}，目标={FormatPlayer(target)}，职位={role}");
            return false;
        }

        public bool IsActiveStsMemberForAudio(Player player)
        {
            if (player == null || !memberDataByPlayerId.TryGetValue(player.PlayerId, out StsMemberData data))
            {
                return false;
            }

            if (!data.IsStsMember || data.RoundId != roundState.RoundId || !player.IsAlive)
            {
                return false;
            }

            if (!TryResolveCarrierRole(data.Role, out RoleTypeId expectedCarrierRole))
            {
                return false;
            }

            return player.Role == expectedCarrierRole;
        }

        public bool IsActiveStsMember(Player player)
        {
            return TryGetActiveStsMemberData(player, true, out _, out _);
        }

        private void TrySpawnStsForRound(int scheduledRoundId)
        {
            if (!allowDelayedStsSpawn || roundState.RoundId != scheduledRoundId)
            {
                Logger.Info($"{LogPrefix} 已跳过过期的第五特别行动组延迟生成。ScheduledRoundId={scheduledRoundId}，CurrentRoundId={roundState.RoundId}");
                return;
            }

            if (roundState.HasSummonedSts)
            {
                Logger.Info($"{LogPrefix} 本回合已生成过第五特别行动组，跳过定时触发。RoundId={roundState.RoundId}");
                return;
            }

            TrySpawnSts("定时触发", out _);
        }

        private bool TrySpawnSts(string triggerDescription, out string reason)
        {
            reason = string.Empty;
            if (roundState.IsSummonInProgress)
            {
                reason = "已有召唤流程正在执行。";
                Logger.Info($"{LogPrefix} {triggerDescription} 触发召唤时已有召唤流程正在执行，已忽略本次触发。");
                return false;
            }

            roundState.IsSummonInProgress = true;

            try
            {
                CandidatePool candidatePool = BuildCandidatePool();
                int roleCapacity = BuildRoleAllocation(config.MaximumSummonCount).Count;

                Logger.Info(
                    $"{LogPrefix} 候选玩家池：真实旁观者={candidatePool.RealSpectatorCount}，Dummy={candidatePool.DummySpectatorCount}，" +
                    $"去重后总数={candidatePool.Candidates.Count}，最低人数={config.MinimumSummonCount}，最高人数={config.MaximumSummonCount}，" +
                    $"允许Dummy={config.AllowSpectatorDummies}，职位容量={roleCapacity}。");

                int requiredCandidateCount = config.MinimumSummonCount;

                if (candidatePool.Candidates.Count < requiredCandidateCount)
                {
                    reason = $"候选人数不足。候选={candidatePool.Candidates.Count}，最低={requiredCandidateCount}。";
                    Logger.Info($"{LogPrefix} 第五特别行动组召唤失败：候选人数不足。候选={candidatePool.Candidates.Count}，最低={requiredCandidateCount}");
                    return false;
                }

                if (roleCapacity < requiredCandidateCount)
                {
                    reason = $"可分配职位容量不足。容量={roleCapacity}，最低={requiredCandidateCount}。";
                    Logger.Error($"{LogPrefix} 第五特别行动组召唤失败：可分配职位容量不足。容量={roleCapacity}，最低={requiredCandidateCount}");
                    return false;
                }

                List<Player> selectedPlayers = SelectCandidates(candidatePool.Candidates, roleCapacity);
                List<StsRole> roleAllocation = BuildRoleAllocation(selectedPlayers.Count);
                List<StsSpawnPoint> spawnPlans = spawnService.CreateSpawnPlans(selectedPlayers.Count);

                Logger.Info($"{LogPrefix} 本次召唤选中玩家：{FormatPlayers(selectedPlayers)}。");

                int assignedCount = 0;
                for (int i = 0; i < selectedPlayers.Count && i < roleAllocation.Count; i++)
                {
                    StsSpawnPoint spawnPoint = i < spawnPlans.Count ? spawnPlans[i] : null;
                    if (AssignStsRole(selectedPlayers[i], roleAllocation[i], spawnPoint))
                    {
                        assignedCount++;
                    }
                }

                if (assignedCount <= 0)
                {
                    reason = "没有任何候选玩家成功分配职位。";
                    Logger.Error($"{LogPrefix} 第五特别行动组召唤失败：没有任何候选玩家成功分配职位，本回合召唤锁未消耗。");
                    return false;
                }

                roundState.HasSummonedSts = true;
                audioService?.PlaySummonAnnouncement(IsActiveStsMemberForAudio);
                reason = $"第五特别行动组召唤成功。成功分配={assignedCount}，计划分配={selectedPlayers.Count}。";
                Logger.Info($"{LogPrefix} 第五特别行动组召唤成功，已锁定本回合召唤。成功分配={assignedCount}，计划分配={selectedPlayers.Count}");
                return true;
            }
            catch (Exception exception)
            {
                reason = $"召唤流程发生异常：{exception.Message}";
                Logger.Error($"{LogPrefix} 第五特别行动组召唤流程发生异常，本回合召唤锁未消耗。错误：{exception}");
                return false;
            }
            finally
            {
                roundState.IsSummonInProgress = false;
            }
        }

        private CandidatePool BuildCandidatePool()
        {
            List<Player> realSpectators = Player.List
                .Where(player => player != null && !player.IsDummy && player.Role == RoleTypeId.Spectator)
                .ToList();

            List<Player> dummySpectators = config.AllowSpectatorDummies
                ? Player.DummyList.Where(player => player != null && player.Role == RoleTypeId.Spectator).ToList()
                : new List<Player>();

            Dictionary<int, Player> candidatesByPlayerId = new Dictionary<int, Player>();
            foreach (Player player in realSpectators)
            {
                candidatesByPlayerId[player.PlayerId] = player;
            }

            foreach (Player player in dummySpectators)
            {
                candidatesByPlayerId[player.PlayerId] = player;
            }

            return new CandidatePool(
                realSpectators.Count,
                dummySpectators.Count,
                candidatesByPlayerId.Values.ToList());
        }

        private List<Player> SelectCandidates(List<Player> candidates, int roleCapacity)
        {
            List<Player> shuffled = new List<Player>(candidates);
            Shuffle(shuffled);

            int selectedCount = Math.Min(shuffled.Count, config.MaximumSummonCount);
            selectedCount = Math.Min(selectedCount, roleCapacity);
            return shuffled.Take(selectedCount).ToList();
        }

        private List<StsRole> BuildRoleAllocation(int desiredCount)
        {
            List<StsRole> allocation = new List<StsRole>();
            foreach (StsRole role in AssignableStsRoles
                .Select(role => new { Role = role, Setting = GetRoleSetting(role) })
                .Where(entry => entry.Setting.MaxCount > 0)
                .OrderBy(entry => entry.Setting.Priority)
                .ThenBy(entry => (int)entry.Role)
                .Select(entry => entry.Role))
            {
                StsRoleConfig setting = GetRoleSetting(role);
                for (int i = 0; i < setting.MaxCount && allocation.Count < desiredCount; i++)
                {
                    allocation.Add(role);
                }

                if (allocation.Count >= desiredCount)
                {
                    break;
                }
            }

            return allocation;
        }

        private bool AssignStsRole(Player player, StsRole role, StsSpawnPoint spawnPoint)
        {
            if (player == null)
            {
                return false;
            }

            if (!TryResolveCarrierRole(role, out RoleTypeId carrierRole))
            {
                Logger.Error($"{LogPrefix} 无法为 {FormatPlayer(player)} 分配 {role}：承载角色无法解析。");
                return false;
            }

            StsMemberData data = CreateMemberData(player, role);
            data.IsStsMember = true;
            ApplyReservedSpawn(data, spawnPoint);
            memberDataByPlayerId[player.PlayerId] = data;

            try
            {
                player.SetRole(carrierRole, RoleChangeReason.Respawn, RoleSpawnFlags.All);
                EnsureCarrierRoleApplied(player, role, carrierRole);
                StabilizePresentationAfterRoleAssignment(player, data);

                Logger.Info($"{LogPrefix} 已将 {FormatPlayer(player)} 分配为 {GetRoleDisplayName(role)}，承载角色：{carrierRole}。");
                return true;
            }
            catch (Exception exception)
            {
                presentationService.ClearPlayerPresentation(player, data, "分配第五特别行动组职位失败");
                memberDataByPlayerId.Remove(player.PlayerId);
                Logger.Error($"{LogPrefix} 分配第五特别行动组职位失败：{FormatPlayer(player)} -> {role}，错误：{exception}");
                return false;
            }
        }

        private void ClearPlayerData(Player player, string reason)
        {
            if (player == null || !memberDataByPlayerId.TryGetValue(player.PlayerId, out StsMemberData data))
            {
                return;
            }

            presentationService.ClearPlayerPresentation(player, data, reason);
            memberDataByPlayerId.Remove(player.PlayerId);

            if (data.IsStsMember)
            {
                Logger.Info($"{LogPrefix} 第五特别行动组成员 {FormatPlayer(player)} 已清理身份。职位：{GetRoleDisplayName(data.Role)}，原因：{reason}");
            }
        }

        private void TryApplyReservedSpawnAfterSpawn(Player player)
        {
            if (!TryGetActiveStsMemberData(player, true, out StsMemberData data, out RoleTypeId expectedCarrierRole) ||
                player.Role != expectedCarrierRole ||
                !data.HasReservedSpawn)
            {
                return;
            }

            Vector3 delta = player.Position - data.ReservedSpawnPosition;
            if (data.ReservedSpawnApplied && delta.sqrMagnitude <= 1.5f * 1.5f)
            {
                return;
            }

            try
            {
                player.Position = data.ReservedSpawnPosition;
                data.ReservedSpawnApplied = true;
                Logger.Info($"{LogPrefix} 已在生成完成后校正 {FormatPlayer(player)} 的第五特别行动组生成点。");
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 生成完成后校正 {FormatPlayer(player)} 的第五特别行动组生成点失败：{exception.Message}");
            }
        }

        private bool TryGetActiveStsMemberData(Player player, bool requireAlive, out StsMemberData data, out RoleTypeId expectedCarrierRole)
        {
            data = null;
            expectedCarrierRole = RoleTypeId.None;

            if (player == null || !memberDataByPlayerId.TryGetValue(player.PlayerId, out data))
            {
                return false;
            }

            if (!data.IsStsMember || data.RoundId != roundState.RoundId)
            {
                return false;
            }

            if (requireAlive && !player.IsAlive)
            {
                return false;
            }

            return TryResolveCarrierRole(data.Role, out expectedCarrierRole);
        }

        private static void ApplyReservedSpawn(StsMemberData data, StsSpawnPoint spawnPoint)
        {
            if (data == null || spawnPoint == null)
            {
                return;
            }

            data.HasReservedSpawn = true;
            data.ReservedSpawnPosition = spawnPoint.Position;
            data.ReservedSpawnHorizontalRotation = spawnPoint.HorizontalRotation;
            data.ReservedSpawnApplied = false;
        }

        private void StabilizePresentationAfterRoleAssignment(Player player, StsMemberData data)
        {
            TryApplyPresentationForActivePlayer(player);
            SchedulePresentationRefresh(player, data);
        }

        private void SchedulePresentationRefresh(Player player, StsMemberData data)
        {
            if (player == null || data == null)
            {
                return;
            }

            int playerId = player.PlayerId;
            int refreshSequence = ++data.PresentationRefreshSequence;
            Timing.CallDelayed(PresentationStabilizationDelaySeconds, () =>
            {
                if (!memberDataByPlayerId.TryGetValue(playerId, out StsMemberData currentData) ||
                    !ReferenceEquals(currentData, data) ||
                    currentData.PresentationRefreshSequence != refreshSequence)
                {
                    return;
                }

                TryApplyPresentationForActivePlayer(FindPlayerById(playerId));
            });
        }

        private static void EnsureCarrierRoleApplied(Player player, StsRole role, RoleTypeId expectedCarrierRole)
        {
            if (player == null || player.Role != expectedCarrierRole)
            {
                RoleTypeId actualRole = player?.Role ?? RoleTypeId.None;
                throw new InvalidOperationException(
                    $"{role} 承载角色设置未生效。预期={expectedCarrierRole}，实际={actualRole}。");
            }
        }

        private void TryApplyPresentationForActivePlayer(Player player)
        {
            if (player == null || !memberDataByPlayerId.TryGetValue(player.PlayerId, out StsMemberData data))
            {
                return;
            }

            if (data.RoundId != roundState.RoundId || !data.IsStsMember)
            {
                return;
            }

            if (!player.IsAlive)
            {
                return;
            }

            if (!TryResolveCarrierRole(data.Role, out RoleTypeId expectedCarrierRole) || player.Role != expectedCarrierRole)
            {
                return;
            }

            presentationService.ApplyPresentation(player, data);
        }

        private int ClearAllPlayerPresentation(string reason)
        {
            List<KeyValuePair<int, StsMemberData>> entries = memberDataByPlayerId.ToList();
            foreach (KeyValuePair<int, StsMemberData> entry in entries)
            {
                Player player = FindPlayerById(entry.Key);
                if (player != null)
                {
                    presentationService.ClearPlayerPresentation(player, entry.Value, reason);
                }
            }

            memberDataByPlayerId.Clear();
            return entries.Count;
        }

        private StsMemberData CreateMemberData(Player player, StsRole role)
        {
            return new StsMemberData
            {
                PlayerId = player.PlayerId,
                Role = role,
                RoundId = roundState.RoundId
            };
        }

        private bool TryResolveCarrierRole(StsRole role, out RoleTypeId carrierRole)
        {
            StsRoleConfig setting = GetRoleSetting(role);
            if (Enum.TryParse(setting.CarrierRole, true, out carrierRole))
            {
                return true;
            }

            StsRoleConfig defaultSetting = StsConfigDefaults.CreateRoleSettings()[role];
            Logger.Warn($"{LogPrefix} RoleSettings.{role}.CarrierRole={setting.CarrierRole} 不是有效 RoleTypeId，尝试回退为 {defaultSetting.CarrierRole}。");
            return Enum.TryParse(defaultSetting.CarrierRole, true, out carrierRole);
        }

        private StsRoleConfig GetRoleSetting(StsRole role)
        {
            Dictionary<StsRole, StsRoleConfig> defaults = StsConfigDefaults.CreateRoleSettings();
            if (config.RoleSettings == null ||
                !config.RoleSettings.TryGetValue(role, out StsRoleConfig setting) ||
                setting == null)
            {
                return defaults[role];
            }

            return setting;
        }

        private string GetRoleDisplayName(StsRole role)
        {
            if (translation.RoleDisplayNames != null &&
                translation.RoleDisplayNames.TryGetValue(role, out string displayName) &&
                !string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            return role.ToString();
        }

        private void Shuffle<T>(IList<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = values[i];
                values[i] = values[j];
                values[j] = temp;
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

        private static string FormatPlayers(IEnumerable<Player> players)
        {
            return string.Join(", ", players.Select(FormatPlayer));
        }

        private static string FormatVector(Vector3 position)
        {
            return $"({position.x:0.00}, {position.y:0.00}, {position.z:0.00})";
        }

        private static Player FindPlayerById(int playerId)
        {
            return Player.List.FirstOrDefault(player => player != null && player.PlayerId == playerId) ??
                   Player.DummyList.FirstOrDefault(player => player != null && player.PlayerId == playerId);
        }

        private sealed class CandidatePool
        {
            public CandidatePool(int realSpectatorCount, int dummySpectatorCount, List<Player> candidates)
            {
                RealSpectatorCount = realSpectatorCount;
                DummySpectatorCount = dummySpectatorCount;
                Candidates = candidates;
            }

            public int RealSpectatorCount { get; }

            public int DummySpectatorCount { get; }

            public List<Player> Candidates { get; }
        }
    }
}
