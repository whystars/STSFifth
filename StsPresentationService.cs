using System;
using System.Collections.Generic;
using InventorySystem.Items;
using LabApi.Features.Wrappers;
using Logger = LabApi.Features.Console.Logger;

namespace STSFifth
{
    public sealed class StsPresentationService
    {
        private const string LogPrefix = "[STSFifth]";
        private const global::PlayerInfoArea NativeRoleMetadataFlags = global::PlayerInfoArea.Role;
        private const global::PlayerInfoArea OwnedDisplayFlags =
            global::PlayerInfoArea.CustomInfo | NativeRoleMetadataFlags;

        private readonly StsConfig config;
        private readonly StsTranslation translation;
        private readonly StsHudService hudService;

        public StsPresentationService(StsConfig config, StsTranslation translation, StsHudService hudService)
        {
            this.config = config ?? StsConfig.CreateDefault();
            this.translation = translation ?? StsTranslation.CreateDefault();
            this.hudService = hudService ?? new StsHudService(this.config, this.translation);
        }

        public void ApplyPresentation(Player player, StsMemberData data)
        {
            if (player == null || data == null)
            {
                return;
            }

            string roleName = GetRoleDisplayName(data.Role);

            if (!data.PresentationApplied)
            {
                if (ApplyConfiguredLoadout(player, data.Role))
                {
                    data.PresentationApplied = true;
                    Logger.Info($"{LogPrefix} 已为 {FormatPlayer(player)} 应用 {roleName} 的装备和弹药。");
                }
            }

            EnsureCustomInfo(player, data, roleName);
            EnsureMaxHealth(player, data);
            hudService.ShowRoleHud(player, data.Role, roleName);
        }

        public void ShowNotification(Player player, string text)
        {
            hudService.ShowNotification(player, text, config.Hud.NotificationDurationSeconds);
        }

        public void ClearPlayerPresentation(Player player, StsMemberData data, string reason)
        {
            if (player == null)
            {
                return;
            }

            hudService.ClearPlayer(player);

            if (data == null || !data.HasOriginalDisplayState)
            {
                return;
            }

            try
            {
                global::PlayerInfoArea currentInfoArea = player.InfoArea;
                if (player.CustomInfo == data.AppliedCustomInfo)
                {
                    player.CustomInfo = data.OriginalCustomInfo ?? string.Empty;
                    player.InfoArea = RestoreOwnedDisplayFlags(currentInfoArea, data.OriginalInfoArea);
                    Logger.Info($"{LogPrefix} 已恢复 {FormatPlayer(player)} 的原始职位显示。原因：{reason}");
                }
                else
                {
                    player.InfoArea = RestoreNativeRoleMetadataFlags(currentInfoArea, data.OriginalInfoArea);
                    Logger.Warn(
                        $"{LogPrefix} {FormatPlayer(player)} 的 CustomInfo 已被其他插件改写；" +
                        $"已保留当前 CustomInfo，仅恢复本插件修改的原生 Role 显示位。原因：{reason}");
                }
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 清理 {FormatPlayer(player)} 的自定义职位显示失败。原因：{reason}，错误：{exception.Message}");
            }
            finally
            {
                data.HasOriginalDisplayState = false;
                data.AppliedCustomInfo = null;
                data.OriginalCustomInfo = null;
            }
        }

        private bool ApplyConfiguredLoadout(Player player, StsRole role)
        {
            try
            {
                player.ClearInventory(true, true);
            }
            catch (Exception exception)
            {
                Logger.Error($"{LogPrefix} 清空 {FormatPlayer(player)} 的原生库存失败，已跳过本次装备发放。职位：{role}，错误：{exception}");
                return false;
            }

            foreach (string itemName in GetEquipment(role))
            {
                if (!TryParseItemType(itemName, role, "Equipment", out ItemType itemType))
                {
                    continue;
                }

                try
                {
                    player.AddItem(itemType, ItemAddReason.AdminCommand);
                }
                catch (Exception exception)
                {
                    Logger.Warn($"{LogPrefix} 发放物品失败：玩家={FormatPlayer(player)}，职位={role}，物品={itemType}，错误：{exception.Message}");
                }
            }

            foreach (KeyValuePair<string, int> ammoEntry in GetAmmo(role))
            {
                if (!TryParseItemType(ammoEntry.Key, role, "Ammo", out ItemType ammoType))
                {
                    continue;
                }

                if (ammoEntry.Value < 0)
                {
                    Logger.Warn($"{LogPrefix} Ammo.{role}.{ammoEntry.Key} 数量小于 0，已跳过。");
                    continue;
                }

                int clampedAmount = ammoEntry.Value;
                if (clampedAmount > ushort.MaxValue)
                {
                    Logger.Warn($"{LogPrefix} Ammo.{role}.{ammoEntry.Key} 数量超过 {ushort.MaxValue}，已夹紧。原值={clampedAmount}");
                    clampedAmount = ushort.MaxValue;
                }

                try
                {
                    player.SetAmmo(ammoType, (ushort)clampedAmount);
                }
                catch (Exception exception)
                {
                    Logger.Warn($"{LogPrefix} 设置弹药失败：玩家={FormatPlayer(player)}，职位={role}，弹药={ammoType}，数量={clampedAmount}，错误：{exception.Message}");
                }
            }

            return true;
        }

        private void EnsureCustomInfo(Player player, StsMemberData data, string roleName)
        {
            try
            {
                if (!Player.ValidateCustomInfo(roleName, out string reason))
                {
                    Logger.Warn($"{LogPrefix} 职位显示文本未通过 CustomInfo 校验：玩家={FormatPlayer(player)}，职位={roleName}，原因={reason}。已跳过头顶/观察信息显示。");
                    return;
                }

                if (!data.HasOriginalDisplayState)
                {
                    data.OriginalCustomInfo = player.CustomInfo ?? string.Empty;
                    data.OriginalInfoArea = player.InfoArea;
                    data.HasOriginalDisplayState = true;
                }

                data.AppliedCustomInfo = roleName;
                player.CustomInfo = roleName;
                player.InfoArea = ApplyRoleDisplayFlags(player.InfoArea);

                if (!IsRoleDisplayApplied(player.CustomInfo, player.InfoArea, roleName))
                {
                    Logger.Warn(
                        $"{LogPrefix} 职位显示写入后状态不符合预期：玩家={FormatPlayer(player)}，" +
                        $"预期职位={roleName}，当前CustomInfo={player.CustomInfo ?? "<null>"}，InfoArea={player.InfoArea}。");
                }
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 设置自定义职位显示失败：玩家={FormatPlayer(player)}，职位={roleName}，错误：{exception.Message}");
            }
        }

        private void EnsureMaxHealth(Player player, StsMemberData data)
        {
            if (!config.RoleSettings.TryGetValue(data.Role, out StsRoleConfig roleConfig) || roleConfig == null)
            {
                return;
            }

            try
            {
                player.MaxHealth = roleConfig.MaxHealth;
                player.Health = roleConfig.MaxHealth;
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 设置 {FormatPlayer(player)} 的最大生命值失败：目标={roleConfig.MaxHealth}，错误：{exception.Message}");
            }
        }

        internal static global::PlayerInfoArea ApplyRoleDisplayFlags(global::PlayerInfoArea currentInfoArea)
        {
            return (currentInfoArea | global::PlayerInfoArea.CustomInfo) & ~NativeRoleMetadataFlags;
        }

        internal static global::PlayerInfoArea RestoreOwnedDisplayFlags(
            global::PlayerInfoArea currentInfoArea,
            global::PlayerInfoArea originalInfoArea)
        {
            return (currentInfoArea & ~OwnedDisplayFlags) | (originalInfoArea & OwnedDisplayFlags);
        }

        internal static global::PlayerInfoArea RestoreNativeRoleMetadataFlags(
            global::PlayerInfoArea currentInfoArea,
            global::PlayerInfoArea originalInfoArea)
        {
            return (currentInfoArea & ~NativeRoleMetadataFlags) |
                   (originalInfoArea & NativeRoleMetadataFlags);
        }

        internal static bool IsRoleDisplayApplied(
            string currentCustomInfo,
            global::PlayerInfoArea currentInfoArea,
            string expectedRoleName)
        {
            return string.Equals(currentCustomInfo, expectedRoleName, StringComparison.Ordinal) &&
                   (currentInfoArea & global::PlayerInfoArea.CustomInfo) != 0 &&
                   (currentInfoArea & NativeRoleMetadataFlags) == 0;
        }

        private IEnumerable<string> GetEquipment(StsRole role)
        {
            if (config.Equipment != null && config.Equipment.TryGetValue(role, out List<string> equipment) && equipment != null)
            {
                return equipment;
            }

            Dictionary<StsRole, List<string>> defaults = StsConfigDefaults.CreateEquipment();
            return defaults.TryGetValue(role, out List<string> defaultEquipment) ? defaultEquipment : Array.Empty<string>();
        }

        private IEnumerable<KeyValuePair<string, int>> GetAmmo(StsRole role)
        {
            if (config.Ammo != null && config.Ammo.TryGetValue(role, out Dictionary<string, int> ammo) && ammo != null)
            {
                return ammo;
            }

            Dictionary<StsRole, Dictionary<string, int>> defaults = StsConfigDefaults.CreateAmmo();
            return defaults.TryGetValue(role, out Dictionary<string, int> defaultAmmo)
                ? defaultAmmo
                : Array.Empty<KeyValuePair<string, int>>();
        }

        private bool TryParseItemType(string rawValue, StsRole role, string configRoot, out ItemType itemType)
        {
            itemType = ItemType.None;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                Logger.Warn($"{LogPrefix} {configRoot}.{role} 包含空 ItemType，已跳过。");
                return false;
            }

            if (!Enum.TryParse(rawValue.Trim(), true, out itemType) || itemType == ItemType.None)
            {
                Logger.Warn($"{LogPrefix} {configRoot}.{role} 包含无效 ItemType：{rawValue}，已跳过。");
                return false;
            }

            return true;
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
