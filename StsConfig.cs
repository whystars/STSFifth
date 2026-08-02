using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace STSFifth
{
    public sealed class StsConfig
    {
        private const float MinimumSpawnSpreadRadius = 1f;
        private const float MinimumCassieSubtitleDurationSeconds = 1f;
        private const float MaximumCassieSubtitleDurationSeconds = 300f;
        private const float ZeroPointToleranceSqr = 0.01f;

        [YamlMember(Description = "是否启用 STSFifth 插件功能。关闭后插件会加载配置但不注册玩法事件。")]
        public bool IsEnabled { get; set; } = true;

        [YamlMember(Description = "是否允许旁观状态的 Dummy 进入第五特别行动组候选玩家池。")]
        public bool AllowSpectatorDummies { get; set; } = true;

        [YamlMember(Description = "回合开始后延迟多少分钟触发第五特别行动组生成。")]
        public float SpawnDelayMinutes { get; set; } = 15f;

        [YamlMember(Description = "生成第五特别行动组所需的最低候选人数。低于该人数则跳过本回合生成。")]
        public int MinimumSummonCount { get; set; } = 3;

        [YamlMember(Description = "单次最多生成的第五特别行动组人数。")]
        public int MaximumSummonCount { get; set; } = 6;

        [YamlMember(Description = "一局游戏最多触发生成的次数。")]
        public int MaximumSummonsPerRound { get; set; } = 1;

        [YamlMember(Description = "各自定义职位的人数上限、分配优先级、承载角色和最大生命值。")]
        public Dictionary<StsRole, StsRoleConfig> RoleSettings { get; set; } = StsConfigDefaults.CreateRoleSettings();

        [YamlMember(Description = "第五特别行动组生成位置配置。默认使用地表九尾原生出生点。")]
        public StsSpawnConfig Spawn { get; set; } = new StsSpawnConfig();

        [YamlMember(Description = "HintServiceMeow HUD 布局配置。")]
        public StsHudConfig Hud { get; set; } = new StsHudConfig();

        [YamlMember(Description = "CASSIE 公告音频、字幕时长和入场音频配置。")]
        public StsAudioConfig Audio { get; set; } = new StsAudioConfig();

        [YamlMember(Description = "Omega 核弹系统配置。")]
        public StsNukeConfig Nuke { get; set; } = new StsNukeConfig();

        [YamlMember(Description = "是否启用 RemoteAdmin 管理员命令 stsrole。")]
        public bool EnableTestCommands { get; set; } = true;

        [YamlMember(Description = "各自定义职位发放的物品 ItemType 名称列表。")]
        public Dictionary<StsRole, List<string>> Equipment { get; set; } = StsConfigDefaults.CreateEquipment();

        [YamlMember(Description = "各自定义职位设置的弹药 ItemType 名称和数量。")]
        public Dictionary<StsRole, Dictionary<string, int>> Ammo { get; set; } = StsConfigDefaults.CreateAmmo();

        public static StsConfig CreateDefault()
        {
            return new StsConfig();
        }

        public void Validate(Action<string> warn)
        {
            warn = warn ?? delegate { };
            StsConfig defaults = CreateDefault();

            if (MinimumSummonCount < 1)
            {
                warn("MinimumSummonCount 小于 1，已回退为默认值。");
                MinimumSummonCount = defaults.MinimumSummonCount;
            }

            if (MaximumSummonCount < MinimumSummonCount)
            {
                int corrected = Math.Max(defaults.MaximumSummonCount, MinimumSummonCount);
                warn($"MaximumSummonCount 小于 MinimumSummonCount，已调整为 {corrected}。");
                MaximumSummonCount = corrected;
            }

            if (MaximumSummonsPerRound < 0)
            {
                warn("MaximumSummonsPerRound 小于 0，已回退为默认值。");
                MaximumSummonsPerRound = defaults.MaximumSummonsPerRound;
            }

            if (SpawnDelayMinutes < 0f || !IsFinite(SpawnDelayMinutes))
            {
                warn("SpawnDelayMinutes 非法，已回退为默认值。");
                SpawnDelayMinutes = defaults.SpawnDelayMinutes;
            }

            ValidateRoleSettings(warn);
            ValidateSpawn(warn);
            ValidateHud(warn);
            ValidateAudio(warn);
            ValidateNuke(warn);
            ValidateEquipment(warn);
            ValidateAmmo(warn);
        }

        private void ValidateRoleSettings(Action<string> warn)
        {
            Dictionary<StsRole, StsRoleConfig> defaults = StsConfigDefaults.CreateRoleSettings();

            if (RoleSettings == null)
            {
                warn("RoleSettings 缺失，已使用默认职位配置。");
                RoleSettings = defaults;
                return;
            }

            if (RoleSettings.ContainsKey(StsRole.None))
            {
                warn("RoleSettings.None 不会被使用，已从运行时配置中移除。");
                RoleSettings.Remove(StsRole.None);
            }

            foreach (StsRole role in StsConfigDefaults.ConfigurableRoles)
            {
                StsRoleConfig defaultSetting = defaults[role];
                if (!RoleSettings.TryGetValue(role, out StsRoleConfig setting) || setting == null)
                {
                    warn($"RoleSettings.{role} 缺失，已使用默认值。");
                    RoleSettings[role] = defaultSetting.Clone();
                    continue;
                }

                if (string.IsNullOrWhiteSpace(setting.CarrierRole))
                {
                    warn($"RoleSettings.{role}.CarrierRole 为空，已回退为 {defaultSetting.CarrierRole}。");
                    setting.CarrierRole = defaultSetting.CarrierRole;
                }

                if (setting.MaxCount < 0)
                {
                    warn($"RoleSettings.{role}.MaxCount 小于 0，已回退为默认值 {defaultSetting.MaxCount}。");
                    setting.MaxCount = defaultSetting.MaxCount;
                }

                if (setting.Priority < 0)
                {
                    warn($"RoleSettings.{role}.Priority 小于 0，已回退为默认值 {defaultSetting.Priority}。");
                    setting.Priority = defaultSetting.Priority;
                }

                if (setting.MaxHealth <= 0)
                {
                    warn($"RoleSettings.{role}.MaxHealth 非法，已回退为默认值 {defaultSetting.MaxHealth}。");
                    setting.MaxHealth = defaultSetting.MaxHealth;
                }
            }
        }
        private void ValidateSpawn(Action<string> warn)
        {
            StsSpawnConfig defaults = new StsSpawnConfig();

            if (Spawn == null)
            {
                warn("Spawn 配置缺失，已使用默认生成点配置。");
                Spawn = defaults;
                return;
            }

            if (Spawn.Position == null)
            {
                warn("Spawn.Position 缺失，已使用默认坐标。");
                Spawn.Position = defaults.Position;
            }
            else if (!IsFinite(Spawn.Position.X) || !IsFinite(Spawn.Position.Y) || !IsFinite(Spawn.Position.Z))
            {
                warn("Spawn.Position 包含非法坐标值，已回退为默认坐标。");
                Spawn.Position = defaults.Position;
            }
            else if (Spawn.UseConfiguredPosition && IsObviousFallbackPoint(Spawn.Position))
            {
                warn("Spawn.UseConfiguredPosition 已开启，但 Spawn.Position 接近 Vector3.zero，疑似非法回退点。已关闭配置坐标并改用默认地表九尾生成点。");
                Spawn.UseConfiguredPosition = false;
                Spawn.Position = defaults.Position;
            }

            if (Spawn.SpreadRadius < MinimumSpawnSpreadRadius || !IsFinite(Spawn.SpreadRadius))
            {
                warn($"Spawn.SpreadRadius 非法，已回退为默认值 {defaults.SpreadRadius}。");
                Spawn.SpreadRadius = defaults.SpreadRadius;
            }
        }

        private void ValidateHud(Action<string> warn)
        {
            StsHudConfig defaults = new StsHudConfig();

            if (Hud == null)
            {
                warn("Hud 配置缺失，已使用默认 HUD 配置。");
                Hud = defaults;
                return;
            }

            if (!IsFinite(Hud.RoleHintX) || !IsFinite(Hud.RoleHintY))
            {
                warn("Hud 角色提示坐标非法，已回退为默认坐标。");
                Hud.RoleHintX = defaults.RoleHintX;
                Hud.RoleHintY = defaults.RoleHintY;
            }

            // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
            //if (!IsFinite(Hud.NukeCountdownX) || !IsFinite(Hud.NukeCountdownY))
            //{
            //    warn("Hud 核弹倒计时坐标非法，已回退为默认坐标。");
            //    Hud.NukeCountdownX = defaults.NukeCountdownX;
            //    Hud.NukeCountdownY = defaults.NukeCountdownY;
            //}

            if (!IsFinite(Hud.NotificationHintX) || !IsFinite(Hud.NotificationHintY))
            {
                warn("Hud 临时提示坐标非法，已回退为默认坐标。");
                Hud.NotificationHintX = defaults.NotificationHintX;
                Hud.NotificationHintY = defaults.NotificationHintY;
            }

            if (Hud.FontSize <= 0)
            {
                warn($"Hud.FontSize 非法，已回退为默认值 {defaults.FontSize}。");
                Hud.FontSize = defaults.FontSize;
            }

            if (Hud.NotificationDurationSeconds < 0f || !IsFinite(Hud.NotificationDurationSeconds))
            {
                warn($"Hud.NotificationDurationSeconds 非法，已回退为默认值 {defaults.NotificationDurationSeconds}。");
                Hud.NotificationDurationSeconds = defaults.NotificationDurationSeconds;
            }
        }
        private void ValidateAudio(Action<string> warn)
        {
            StsAudioConfig defaults = new StsAudioConfig();

            if (Audio == null)
            {
                warn("Audio 配置缺失，已使用默认音频配置。");
                Audio = defaults;
                return;
            }

            if (string.IsNullOrWhiteSpace(Audio.CassieAudioKey))
            {
                warn("Audio.CassieAudioKey 为空，将跳过入场 CASSIE 公告音频播放。");
            }
            else
            {
                Audio.CassieAudioKey = Audio.CassieAudioKey.Trim();
            }

            if (!IsFinite(Audio.CassieSubtitleDurationSeconds))
            {
                warn($"Audio.CassieSubtitleDurationSeconds 数值非法，已回退为默认值 {defaults.CassieSubtitleDurationSeconds} 秒。");
                Audio.CassieSubtitleDurationSeconds = defaults.CassieSubtitleDurationSeconds;
            }
            else if (Audio.CassieSubtitleDurationSeconds < 1f)
            {
                warn($"Audio.CassieSubtitleDurationSeconds 小于 1 秒，已夹紧为 1。原值={Audio.CassieSubtitleDurationSeconds}");
                Audio.CassieSubtitleDurationSeconds = 1f;
            }
            else if (Audio.CassieSubtitleDurationSeconds > 300f)
            {
                warn($"Audio.CassieSubtitleDurationSeconds 大于 300 秒，已夹紧为 300。原值={Audio.CassieSubtitleDurationSeconds}");
                Audio.CassieSubtitleDurationSeconds = 300f;
            }

            if (string.IsNullOrWhiteSpace(Audio.EntryAudioKey))
            {
                warn("Audio.EntryAudioKey 为空，将跳过第五特别行动组专属入场音频播放。");
            }
            else
            {
                Audio.EntryAudioKey = Audio.EntryAudioKey.Trim();
            }

            // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
            //if (string.IsNullOrWhiteSpace(Audio.NukeStartAudioKey))
            //{
            //    warn("Audio.NukeStartAudioKey 为空，将跳过 Omega 核弹启动音频播放。");
            //}
            //else
            //{
            //    Audio.NukeStartAudioKey = Audio.NukeStartAudioKey.Trim();
            //}

            Audio.CassieVolume = ClampVolume(Audio.CassieVolume, defaults.CassieVolume, "Audio.CassieVolume", warn);
            Audio.EntryVolume = ClampVolume(Audio.EntryVolume, defaults.EntryVolume, "Audio.EntryVolume", warn);
        }

        private void ValidateNuke(Action<string> warn)
        {
            StsNukeConfig defaults = new StsNukeConfig();

            if (Nuke == null)
            {
                warn("Nuke 配置缺失，已使用默认核弹配置。");
                Nuke = defaults;
                return;
            }

            if (Nuke.DetonationSeconds <= 0f || !IsFinite(Nuke.DetonationSeconds))
            {
                warn($"Nuke.DetonationSeconds 非法，已回退为默认值 {defaults.DetonationSeconds} 秒。");
                Nuke.DetonationSeconds = defaults.DetonationSeconds;
            }

            if (Nuke.CoinPickupScale <= 0f || !IsFinite(Nuke.CoinPickupScale))
            {
                warn($"Nuke.CoinPickupScale 非法，已回退为默认值 {defaults.CoinPickupScale}。");
                Nuke.CoinPickupScale = defaults.CoinPickupScale;
            }

            Nuke.NukeAudioVolume = ClampVolume(Nuke.NukeAudioVolume, defaults.NukeAudioVolume, "Nuke.NukeAudioVolume", warn);

            // 验证 RGB 颜色值
            if (Nuke.LightColorR < 0 || Nuke.LightColorR > 255 ||
                Nuke.LightColorG < 0 || Nuke.LightColorG > 255 ||
                Nuke.LightColorB < 0 || Nuke.LightColorB > 255)
            {
                warn($"Nuke 灯光 RGB 值已自动限制在 0-255 范围内。原值: R={Nuke.LightColorR}, G={Nuke.LightColorG}, B={Nuke.LightColorB}");
                Nuke.LightColorR = Math.Max(0, Math.Min(255, Nuke.LightColorR));
                Nuke.LightColorG = Math.Max(0, Math.Min(255, Nuke.LightColorG));
                Nuke.LightColorB = Math.Max(0, Math.Min(255, Nuke.LightColorB));
            }

            if (Nuke.CountdownHudFontSize <= 0)
            {
                warn($"Nuke.CountdownHudFontSize 非法，已回退为默认值 {defaults.CountdownHudFontSize}。");
                Nuke.CountdownHudFontSize = defaults.CountdownHudFontSize;
            }
        }

        private void ValidateEquipment(Action<string> warn)
        {
            Dictionary<StsRole, List<string>> defaults = StsConfigDefaults.CreateEquipment();

            if (Equipment == null)
            {
                warn("Equipment 缺失，已使用默认装备表。");
                Equipment = defaults;
                return;
            }

            foreach (StsRole role in StsConfigDefaults.ConfigurableRoles)
            {
                if (!Equipment.TryGetValue(role, out List<string> items) || items == null)
                {
                    warn($"Equipment.{role} 缺失，已使用默认装备。");
                    Equipment[role] = new List<string>(defaults[role]);
                    continue;
                }

                List<string> sanitized = new List<string>();
                foreach (string item in items)
                {
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        warn($"Equipment.{role} 包含空物品名称，已跳过。");
                        continue;
                    }

                    sanitized.Add(item.Trim());
                }

                Equipment[role] = sanitized;
            }
        }

        private void ValidateAmmo(Action<string> warn)
        {
            Dictionary<StsRole, Dictionary<string, int>> defaults = StsConfigDefaults.CreateAmmo();

            if (Ammo == null)
            {
                warn("Ammo 缺失，已使用默认弹药表。");
                Ammo = defaults;
                return;
            }

            foreach (StsRole role in StsConfigDefaults.ConfigurableRoles)
            {
                if (!Ammo.TryGetValue(role, out Dictionary<string, int> ammoByType) || ammoByType == null)
                {
                    warn($"Ammo.{role} 缺失，已使用默认弹药。");
                    Ammo[role] = new Dictionary<string, int>(defaults[role]);
                    continue;
                }

                Dictionary<string, int> sanitized = new Dictionary<string, int>();
                foreach (KeyValuePair<string, int> entry in ammoByType)
                {
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        warn($"Ammo.{role} 包含空弹药名称，已跳过。");
                        continue;
                    }

                    if (entry.Value < 0)
                    {
                        warn($"Ammo.{role}.{entry.Key} 数量小于 0，已跳过。");
                        continue;
                    }

                    sanitized[entry.Key.Trim()] = entry.Value;
                }

                Ammo[role] = sanitized;
            }
        }

        private static float ClampVolume(float value, float defaultValue, string path, Action<string> warn)
        {
            if (!IsFinite(value))
            {
                warn($"{path} 不是有效数值，已回退为默认值 {defaultValue}。");
                return defaultValue;
            }

            if (value < 0f)
            {
                warn($"{path} 小于 0，已夹紧为 0。");
                return 0f;
            }

            if (value > 1f)
            {
                warn($"{path} 大于 1，已夹紧为 1。");
                return 1f;
            }

            return value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsObviousFallbackPoint(StsVector3Config position)
        {
            if (position == null)
            {
                return true;
            }

            float magnitudeSqr = (position.X * position.X) + (position.Y * position.Y) + (position.Z * position.Z);
            return magnitudeSqr <= ZeroPointToleranceSqr;
        }
    }
    public sealed class StsRoleConfig
    {
        [YamlMember(Description = "该职位的人数上限。")]
        public int MaxCount { get; set; }

        [YamlMember(Description = "候选人数不足 6 人时的分配优先级，数值越小越优先保留。")]
        public int Priority { get; set; }

        [YamlMember(Description = "承载该自定义职位的原生 RoleTypeId 名称（如 NtfCaptain）。")]
        public string CarrierRole { get; set; }

        [YamlMember(Description = "该职位的最大生命值。")]
        public int MaxHealth { get; set; }

        public StsRoleConfig Clone()
        {
            return new StsRoleConfig
            {
                MaxCount = MaxCount,
                Priority = Priority,
                CarrierRole = CarrierRole,
                MaxHealth = MaxHealth
            };
        }
    }

    public sealed class StsVector3Config
    {
        [YamlMember(Description = "X 坐标。")]
        public float X { get; set; }

        [YamlMember(Description = "Y 坐标。")]
        public float Y { get; set; }

        [YamlMember(Description = "Z 坐标。")]
        public float Z { get; set; }

        public StsVector3Config()
        {
        }

        public StsVector3Config(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public sealed class StsSpawnConfig
    {
        [YamlMember(Description = "是否使用下方 Position 配置的固定坐标生成第五特别行动组。关闭时使用地表九尾原生出生点。")]
        public bool UseConfiguredPosition { get; set; } = false;

        [YamlMember(Description = "固定生成坐标（仅在 UseConfiguredPosition 为 true 时使用）。")]
        public StsVector3Config Position { get; set; } = new StsVector3Config(0f, 0f, 0f);

        [YamlMember(Description = "多人生成时的分散半径（米），避免多人叠在同一位置。")]
        public float SpreadRadius { get; set; } = 1.5f;
    }

    public sealed class StsHudConfig
    {
        [YamlMember(Description = "职位常驻提示 X 坐标。")]
        public float RoleHintX { get; set; } = 0f;

        [YamlMember(Description = "职位常驻提示 Y 坐标（越大越靠屏幕下方）。")]
        public float RoleHintY { get; set; } = 850f;

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        //[YamlMember(Description = "Omega 核弹倒计时提示 X 坐标。")]
        //public float NukeCountdownX { get; set; } = 0f;

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        //[YamlMember(Description = "Omega 核弹倒计时提示 Y 坐标（负值越大越靠屏幕上方）。")]
        //public float NukeCountdownY { get; set; } = -850f;

        [YamlMember(Description = "临时通知提示的 X 坐标。")]
        public float NotificationHintX { get; set; } = 0f;

        [YamlMember(Description = "临时通知提示的 Y 坐标。")]
        public float NotificationHintY { get; set; } = -850f;

        [YamlMember(Description = "HUD 提示字号。")]
        public int FontSize { get; set; } = 25;

        [YamlMember(Description = "临时通知提示的默认显示时长（秒）。")]
        public float NotificationDurationSeconds { get; set; } = 5f;
    }

    public sealed class StsAudioConfig
    {
        [YamlMember(Description = "全服可听的入场 CASSIE 公告音频注册 Key，留空则跳过播放。")]
        public string CassieAudioKey { get; set; } = "STS5_EntryCassie";

        [YamlMember(Description = "入场 CASSIE 自定义字幕显示秒数。会按向上取整后的秒数生成同数量的英文句号作为 CASSIE 停顿，每个句号约 1 秒；允许短于音频文件时长。范围 1 到 300 秒。")]
        public float CassieSubtitleDurationSeconds { get; set; } = 20f;

        [YamlMember(Description = "仅第五特别行动组成员可听的专属入场音频注册 Key，留空则跳过播放。")]
        public string EntryAudioKey { get; set; } = "STS5_EntryMember";

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        //[YamlMember(Description = "Omega 核弹启动时全服可听的音频注册 Key，留空则跳过播放。")]
        //public string NukeStartAudioKey { get; set; } = "STS5_NukeStart";

        [YamlMember(Description = "入场 CASSIE 公告音量（0~1）。")]
        public float CassieVolume { get; set; } = 1f;

        [YamlMember(Description = "第五特别行动组专属入场音频音量（0~1）。")]
        public float EntryVolume { get; set; } = 1f;

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        //[YamlMember(Description = "Omega 核弹启动音频音量（0~1）。")]
        //public float NukeStartVolume { get; set; } = 1f;
    }

    public sealed class StsNukeConfig
    {
        [YamlMember(Description = "是否启用 Omega 核弹功能")]
        public bool IsEnabled { get; set; } = true;

        [YamlMember(Description = "Omega 核弹引爆倒计时（秒）")]
        public float DetonationSeconds { get; set; } = 130f;

        [YamlMember(Description = "是否给队长发放启动硬币")]
        public bool GiveCoinToCommander { get; set; } = true;

        [YamlMember(Description = "硬币掉落物缩放倍数（方便识别）")]
        public float CoinPickupScale { get; set; } = 1.8f;

        [YamlMember(Description = "核弹音频音量（0.0-1.0）")]
        public float NukeAudioVolume { get; set; } = 0.8f;

        [YamlMember(Description = "核弹音频是否循环播放")]
        public bool LoopNukeAudio { get; set; } = false;

        [YamlMember(Description = "启动时是否改变设施灯光颜色")]
        public bool EnableLightEffect { get; set; } = true;

        [YamlMember(Description = "设施灯光颜色 - 红色分量（0-255）")]
        public int LightColorR { get; set; } = 0;

        [YamlMember(Description = "设施灯光颜色 - 绿色分量（0-255）")]
        public int LightColorG { get; set; } = 128;

        [YamlMember(Description = "设施灯光颜色 - 蓝色分量（0-255）")]
        public int LightColorB { get; set; } = 255;

        [YamlMember(Description = "HUD 倒计时显示 X 坐标")]
        public int CountdownHudX { get; set; } = 0;

        [YamlMember(Description = "HUD 倒计时显示 Y 坐标")]
        public int CountdownHudY { get; set; } = 200;

        [YamlMember(Description = "HUD 倒计时字体大小")]
        public int CountdownHudFontSize { get; set; } = 30;
    }

    internal static class StsConfigDefaults
    {
        internal static readonly StsRole[] ConfigurableRoles =
        {
            StsRole.Commander,
            StsRole.Suppressor,
            StsRole.Specialist,
            StsRole.Elite,
            StsRole.Soldier
        };

        internal static Dictionary<StsRole, StsRoleConfig> CreateRoleSettings()
        {
            return new Dictionary<StsRole, StsRoleConfig>
            {
                [StsRole.Commander] = new StsRoleConfig { MaxCount = 1, Priority = 0, CarrierRole = "NtfCaptain", MaxHealth = 150 },
                [StsRole.Suppressor] = new StsRoleConfig { MaxCount = 1, Priority = 1, CarrierRole = "NtfSergeant", MaxHealth = 120 },
                [StsRole.Specialist] = new StsRoleConfig { MaxCount = 1, Priority = 2, CarrierRole = "NtfSergeant", MaxHealth = 120 },
                [StsRole.Elite] = new StsRoleConfig { MaxCount = 1, Priority = 3, CarrierRole = "NtfSergeant", MaxHealth = 120 },
                [StsRole.Soldier] = new StsRoleConfig { MaxCount = 2, Priority = 4, CarrierRole = "NtfPrivate", MaxHealth = 120 }
            };
        }

        internal static Dictionary<StsRole, List<string>> CreateEquipment()
        {
            return new Dictionary<StsRole, List<string>>
            {
                [StsRole.Commander] = new List<string>
                {
                    "GunE11SR", "GunCom45", "Adrenaline", "Medkit", "ArmorHeavy", "Radio", "GrenadeHE", "KeycardO5"
                },
                [StsRole.Suppressor] = new List<string>
                {
                    "GunLogicer", "Adrenaline", "Medkit", "ArmorHeavy", "Radio", "GrenadeHE", "KeycardO5"
                },
                [StsRole.Specialist] = new List<string>
                {
                    "GunAK", "Medkit", "Adrenaline", "ArmorHeavy", "Radio", "GrenadeHE", "GrenadeFlash", "Jailbird", "KeycardO5"
                },
                [StsRole.Elite] = new List<string>
                {
                    "GunE11SR", "Medkit", "Medkit", "Adrenaline", "ArmorLight", "Radio", "KeycardO5", "GrenadeHE", "GrenadeFlash"
                },
                [StsRole.Soldier] = new List<string>
                {
                    "GunCrossvec", "Medkit", "Adrenaline", "ArmorLight", "GrenadeFlash", "Radio", "KeycardO5"
                }
            };
        }

        internal static Dictionary<StsRole, Dictionary<string, int>> CreateAmmo()
        {
            return new Dictionary<StsRole, Dictionary<string, int>>
            {
                // COM-45 的弹种存在争议：官方 Wiki 记为 9x19mm，旧配置按 .44 给。
                // 两种都发，避免任一为真时队长副武器打不出子弹（服务器另有无限弹药插件，多给一种只是保底）。
                [StsRole.Commander] = new Dictionary<string, int> { ["Ammo556x45"] = 500, ["Ammo9x19"] = 500, ["Ammo44cal"] = 500 },
                [StsRole.Suppressor] = new Dictionary<string, int> { ["Ammo762x39"] = 500 },
                [StsRole.Specialist] = new Dictionary<string, int> { ["Ammo762x39"] = 500 },
                [StsRole.Elite] = new Dictionary<string, int> { ["Ammo556x45"] = 500 },
                [StsRole.Soldier] = new Dictionary<string, int> { ["Ammo9x19"] = 500 }
            };
        }
    }
}
