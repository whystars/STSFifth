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

        [YamlMember(Description = "Omega核弹倒计时HUD显示文本，{time}为剩余秒数")]
        public string OmegaNukeCountdownHud { get; set; } = "<color=yellow>Omega核弹</color>正在被<color=#00FFFF>STS-5小队</color>引爆，时间还剩余<color=red>{time}</color>秒";

        [YamlMember(Description = "Omega核弹启动时的CASSIE字幕文本")]
        public string OmegaNukeStartedCassieText { get; set; } = "所有人员注意，<color=yellow>Omega核弹</color>正在被<color=red>合法引爆</color>请所有人员立即前往地表撤离";

        [YamlMember(Description = "Omega核弹启动时的CASSIE语音（需包含钟声）")]
        public string OmegaNukeStartedCassieVoice { get; set; } = "bell_start bell_start bell_start . . Attention all personnel . the Omega nuclear is being detonated . All personnel must immediately proceed to the surface";

        [YamlMember(Description = "Omega核弹停止时的CASSIE字幕文本")]
        public string OmegaNukeStoppedCassieText { get; set; } = "<color=yellow>Omega核弹</color>被<color=green>关闭</color>，请重新开启";

        [YamlMember(Description = "Omega核弹停止时的CASSIE语音")]
        public string OmegaNukeStoppedCassieVoice { get; set; } = "bell_start bell_start bell_start . . The Omega nuclear has been shut down . Please start it";

        [YamlMember(Description = "Omega核弹重启时的CASSIE字幕文本")]
        public string OmegaNukeRestartedCassieText { get; set; } = "<color=yellow>Omega核弹</color>被<color=red>重新开启</color>请所有人员按原定计划执行";

        [YamlMember(Description = "Omega核弹重启时的CASSIE语音")]
        public string OmegaNukeRestartedCassieVoice { get; set; } = "bell_start bell_start bell_start . . The Omega nuclear has been restarted . All personnel are to execute";

        [YamlMember(Description = "CASSIE字幕显示时长（秒）")]
        public float CassieSubtitleDurationSeconds { get; set; } = 20.0f;

        [YamlMember(Description = "管理员 stsrole 命令返回文本。")]
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

            OmegaNukeCountdownHud = RequireText(OmegaNukeCountdownHud, defaults.OmegaNukeCountdownHud, "OmegaNukeCountdownHud", warn);
            OmegaNukeStartedCassieText = RequireText(OmegaNukeStartedCassieText, defaults.OmegaNukeStartedCassieText, "OmegaNukeStartedCassieText", warn);
            OmegaNukeStartedCassieVoice = RequireText(OmegaNukeStartedCassieVoice, defaults.OmegaNukeStartedCassieVoice, "OmegaNukeStartedCassieVoice", warn);
            OmegaNukeStoppedCassieText = RequireText(OmegaNukeStoppedCassieText, defaults.OmegaNukeStoppedCassieText, "OmegaNukeStoppedCassieText", warn);
            OmegaNukeStoppedCassieVoice = RequireText(OmegaNukeStoppedCassieVoice, defaults.OmegaNukeStoppedCassieVoice, "OmegaNukeStoppedCassieVoice", warn);
            OmegaNukeRestartedCassieText = RequireText(OmegaNukeRestartedCassieText, defaults.OmegaNukeRestartedCassieText, "OmegaNukeRestartedCassieText", warn);
            OmegaNukeRestartedCassieVoice = RequireText(OmegaNukeRestartedCassieVoice, defaults.OmegaNukeRestartedCassieVoice, "OmegaNukeRestartedCassieVoice", warn);

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

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        //public string NukeStarted { get; set; } = "已强制启动 Omega 核弹。";

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        //public string NukeStopped { get; set; } = "已强制关闭 Omega 核弹。";

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        //public string NukeInvalidAction { get; set; } = "未知核弹操作：{Action}，请使用 start 或 stop。";

        public void Validate(Action<string> warn)
        {
            StsCommandTranslation defaults = new StsCommandTranslation();

            NoPermission = StsTranslation.RequireText(NoPermission, defaults.NoPermission, "CommandResponses.NoPermission", warn);
            CommandsDisabled = StsTranslation.RequireText(CommandsDisabled, defaults.CommandsDisabled, "CommandResponses.CommandsDisabled", warn);
            InvalidUsage = StsTranslation.RequireText(InvalidUsage, defaults.InvalidUsage, "CommandResponses.InvalidUsage", warn);
            PlayerNotFound = StsTranslation.RequireText(PlayerNotFound, defaults.PlayerNotFound, "CommandResponses.PlayerNotFound", warn);
            RoleAssigned = StsTranslation.RequireText(RoleAssigned, defaults.RoleAssigned, "CommandResponses.RoleAssigned", warn);
            InvalidRole = StsTranslation.RequireText(InvalidRole, defaults.InvalidRole, "CommandResponses.InvalidRole", warn);
            // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
            //NukeStarted = StsTranslation.RequireText(NukeStarted, defaults.NukeStarted, "CommandResponses.NukeStarted", warn);
            //NukeStopped = StsTranslation.RequireText(NukeStopped, defaults.NukeStopped, "CommandResponses.NukeStopped", warn);
            //NukeInvalidAction = StsTranslation.RequireText(NukeInvalidAction, defaults.NukeInvalidAction, "CommandResponses.NukeInvalidAction", warn);
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
