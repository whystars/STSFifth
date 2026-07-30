using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace STSFifth
{
    public sealed class StsTranslation
    {
        [YamlMember(Description = "各自定义职位显示给玩家的中文名称，用于 CustomInfo 和 HUD 占位符。")]
        public Dictionary<StsRole, string> RoleDisplayNames { get; set; } = StsTranslationDefaults.CreateRoleDisplayNames();

        [YamlMember(Description = "各自定义职位复活后常驻显示的 HUD 文案，可使用 {RoleName} 占位符。")]
        public Dictionary<StsRole, string> RoleHudTexts { get; set; } = StsTranslationDefaults.CreateRoleHudTexts();

        [YamlMember(Description = "入场时全体玩家收到的 CASSIE 公告字幕文本。")]
        public string EntryCassieText { get; set; } = "所有单位注意，经<color=red>O5议会指令</color><color=#00FFFF>第五特别行动组</color>已进入设施，他们授权启动Omega核弹";

        [YamlMember(Description = "Omega 核弹启动时的 CASSIE 公告字幕文本，{Seconds} 会被替换为配置的倒计时总秒数。")]
        public string NukeStartCassieText { get; set; } = "注意，<color=yellow>Omega核弹</color>正在合法引爆，距离引爆还剩{Seconds}秒";

        [YamlMember(Description = "Omega 核弹启动时游戏原生 CASSIE 朗读的英文文本，使用 CASSIE 语法，{Seconds} 会被替换为倒计时秒数。")]
        public string NukeStartCassieAnnouncement { get; set; } = "ATTENTION . OMEGA WARHEAD DETONATION SEQUENCE ACTIVATED . T MINUS {Seconds} SECONDS";

        [YamlMember(Description = "Omega 核弹关闭时的 CASSIE 公告字幕文本。")]
        public string NukeStopCassieText { get; set; } = "Omega核弹遭到关闭，请重新开启";

        [YamlMember(Description = "Omega 核弹关闭时游戏原生 CASSIE 朗读的英文文本，使用 CASSIE 语法。")]
        public string NukeStopCassieAnnouncement { get; set; } = "OMEGA WARHEAD DETONATION CANCELLED . REACTIVATION AUTHORIZED";

        [YamlMember(Description = "Omega 核弹倒计时期间屏幕上方显示的文案模板，{Seconds} 会被替换为剩余秒数。")]
        public string NukeCountdownHudText { get; set; } = "Omega核弹正在被引爆，距离设施被炸毁还剩余:{Seconds}秒";

        [YamlMember(Description = "Omega 核弹关闭后显示的提示文案。")]
        public string NukeClosedNotificationText { get; set; } = "Omega核弹已被关闭，可重新开启";

        [YamlMember(Description = "Omega 核弹爆炸时非基金会阵营的死亡原因文案。")]
        public string NukeDeathReasonText { get; set; } = "你在Omega核弹爆炸中消失了";

        [YamlMember(Description = "Omega 核弹爆炸后全服显示的秘密提示文案。")]
        public string NukeSecretHintText { get; set; } = "核辐射下的秘密";

        [YamlMember(Description = "管理员 stsrole / stsnuke 命令返回文本。")]
        public StsCommandTranslation CommandResponses { get; set; } = new StsCommandTranslation();

        public static StsTranslation CreateDefault()
        {
            return new StsTranslation();
        }

        public void Validate(Action<string> warn)
        {
            warn = warn ?? delegate { };
            StsTranslation defaults = CreateDefault();

            ValidateRoleDisplayNames(warn);
            ValidateRoleHudTexts(warn);
            EntryCassieText = RequireText(EntryCassieText, defaults.EntryCassieText, "EntryCassieText", warn);
            NukeStartCassieText = RequireText(NukeStartCassieText, defaults.NukeStartCassieText, "NukeStartCassieText", warn);
            NukeStartCassieAnnouncement = RequireText(NukeStartCassieAnnouncement, defaults.NukeStartCassieAnnouncement, "NukeStartCassieAnnouncement", warn);
            NukeStopCassieText = RequireText(NukeStopCassieText, defaults.NukeStopCassieText, "NukeStopCassieText", warn);
            NukeStopCassieAnnouncement = RequireText(NukeStopCassieAnnouncement, defaults.NukeStopCassieAnnouncement, "NukeStopCassieAnnouncement", warn);
            NukeCountdownHudText = RequireText(NukeCountdownHudText, defaults.NukeCountdownHudText, "NukeCountdownHudText", warn);
            NukeClosedNotificationText = RequireText(NukeClosedNotificationText, defaults.NukeClosedNotificationText, "NukeClosedNotificationText", warn);
            NukeDeathReasonText = RequireText(NukeDeathReasonText, defaults.NukeDeathReasonText, "NukeDeathReasonText", warn);
            NukeSecretHintText = RequireText(NukeSecretHintText, defaults.NukeSecretHintText, "NukeSecretHintText", warn);

            if (CommandResponses == null)
            {
                warn("CommandResponses 缺失，已使用默认命令返回文本。");
                CommandResponses = new StsCommandTranslation();
            }

            CommandResponses.Validate(warn);
        }

        private void ValidateRoleDisplayNames(Action<string> warn)
        {
            Dictionary<StsRole, string> defaults = StsTranslationDefaults.CreateRoleDisplayNames();

            if (RoleDisplayNames == null)
            {
                warn("RoleDisplayNames 缺失，已使用默认职位显示名。");
                RoleDisplayNames = defaults;
                return;
            }

            if (RoleDisplayNames.ContainsKey(StsRole.None))
            {
                warn("RoleDisplayNames.None 不会被使用，已从运行时翻译中移除。");
                RoleDisplayNames.Remove(StsRole.None);
            }

            foreach (StsRole role in StsConfigDefaults.ConfigurableRoles)
            {
                if (!RoleDisplayNames.TryGetValue(role, out string displayName) || string.IsNullOrWhiteSpace(displayName))
                {
                    warn($"RoleDisplayNames.{role} 缺失或为空，已使用默认显示名。");
                    RoleDisplayNames[role] = defaults[role];
                }
                else
                {
                    RoleDisplayNames[role] = displayName.Trim();
                }
            }
        }

        private void ValidateRoleHudTexts(Action<string> warn)
        {
            Dictionary<StsRole, string> defaults = StsTranslationDefaults.CreateRoleHudTexts();

            if (RoleHudTexts == null)
            {
                warn("RoleHudTexts 缺失，已使用默认职位 HUD 文案。");
                RoleHudTexts = defaults;
                return;
            }

            if (RoleHudTexts.ContainsKey(StsRole.None))
            {
                warn("RoleHudTexts.None 不会被使用，已从运行时翻译中移除。");
                RoleHudTexts.Remove(StsRole.None);
            }

            foreach (StsRole role in StsConfigDefaults.ConfigurableRoles)
            {
                if (!RoleHudTexts.TryGetValue(role, out string text) || string.IsNullOrWhiteSpace(text))
                {
                    warn($"RoleHudTexts.{role} 缺失或为空，已使用默认 HUD 文案。");
                    RoleHudTexts[role] = defaults[role];
                }
            }
        }

        internal static string RequireText(string value, string defaultValue, string path, Action<string> warn)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            warn($"{path} 为空，已使用默认文本。");
            return defaultValue;
        }
    }

    public sealed class StsCommandTranslation
    {
        public string NoPermission { get; set; } = "你没有权限执行该命令。";

        public string CommandsDisabled { get; set; } = "STSFifth 管理员命令当前已关闭。";

        public string InvalidUsage { get; set; } = "命令格式错误。";

        public string PlayerNotFound { get; set; } = "未找到玩家：{PlayerId}";

        public string RoleAssigned { get; set; } = "已将 {PlayerName} 设置为 {RoleName}（{RoleUid}）。";

        public string InvalidRole { get; set; } = "未知第五特别行动组职位：{Role}";

        public string NukeStarted { get; set; } = "已强制启动 Omega 核弹。";

        public string NukeStopped { get; set; } = "已强制关闭 Omega 核弹。";

        public string NukeInvalidAction { get; set; } = "未知核弹操作：{Action}，请使用 start 或 stop。";

        public void Validate(Action<string> warn)
        {
            StsCommandTranslation defaults = new StsCommandTranslation();

            NoPermission = StsTranslation.RequireText(NoPermission, defaults.NoPermission, "CommandResponses.NoPermission", warn);
            CommandsDisabled = StsTranslation.RequireText(CommandsDisabled, defaults.CommandsDisabled, "CommandResponses.CommandsDisabled", warn);
            InvalidUsage = StsTranslation.RequireText(InvalidUsage, defaults.InvalidUsage, "CommandResponses.InvalidUsage", warn);
            PlayerNotFound = StsTranslation.RequireText(PlayerNotFound, defaults.PlayerNotFound, "CommandResponses.PlayerNotFound", warn);
            RoleAssigned = StsTranslation.RequireText(RoleAssigned, defaults.RoleAssigned, "CommandResponses.RoleAssigned", warn);
            InvalidRole = StsTranslation.RequireText(InvalidRole, defaults.InvalidRole, "CommandResponses.InvalidRole", warn);
            NukeStarted = StsTranslation.RequireText(NukeStarted, defaults.NukeStarted, "CommandResponses.NukeStarted", warn);
            NukeStopped = StsTranslation.RequireText(NukeStopped, defaults.NukeStopped, "CommandResponses.NukeStopped", warn);
            NukeInvalidAction = StsTranslation.RequireText(NukeInvalidAction, defaults.NukeInvalidAction, "CommandResponses.NukeInvalidAction", warn);
        }
    }

    internal static class StsTranslationDefaults
    {
        internal static Dictionary<StsRole, string> CreateRoleDisplayNames()
        {
            return new Dictionary<StsRole, string>
            {
                [StsRole.Commander] = "第五特别行动组 队长",
                [StsRole.Suppressor] = "第五特别行动组 压制者",
                [StsRole.Specialist] = "第五特别行动组 特种干员",
                [StsRole.Elite] = "第五特别行动组 精英",
                [StsRole.Soldier] = "第五特别行动组 士兵"
            };
        }

        internal static Dictionary<StsRole, string> CreateRoleHudTexts()
        {
            const string commanderTask = "你经<color=red>O5议会</color><color=yellow>指令</color>前往地下核弹启动Omega核弹，炸掉整个设施，<color=red>这个设施没有存在的必要了，让他们在核辐射中消失吧</color>";
            const string suppressorTask = "你经<color=red>O5议会</color><color=yellow>指令</color>负责压制设施内的抵抗力量，掩护小队前往地下核弹启动Omega核弹";
            const string specialistTask = "你经<color=red>O5议会</color><color=yellow>指令</color>负责突破设施防线，为小队打通前往地下核弹的道路";
            const string eliteTask = "你经<color=red>O5议会</color><color=yellow>指令</color>担任小队精锐火力，确保Omega核弹顺利启动";
            const string soldierTask = "你经<color=red>O5议会</color><color=yellow>指令</color>协助小队前往地下核弹启动Omega核弹，炸掉整个设施";

            return new Dictionary<StsRole, string>
            {
                [StsRole.Commander] = "你是\n[<color=#00FFFF>{RoleName}</color>]\n" + commanderTask,
                [StsRole.Suppressor] = "你是\n[<color=#00FFFF>{RoleName}</color>]\n" + suppressorTask,
                [StsRole.Specialist] = "你是\n[<color=#00FFFF>{RoleName}</color>]\n" + specialistTask,
                [StsRole.Elite] = "你是\n[<color=#00FFFF>{RoleName}</color>]\n" + eliteTask,
                [StsRole.Soldier] = "你是\n[<color=#00FFFF>{RoleName}</color>]\n" + soldierTask
            };
        }
    }
}
