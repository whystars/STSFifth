# STSFifth - 第五特别行动组插件

![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7+-blue)
![Version](https://img.shields.io/badge/Version-1.1.1--pre-green)
![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.x-orange)
![License](https://img.shields.io/badge/License-GPL--3.0-red)

SCP: Secret Laboratory 服务器插件，为游戏添加第五特别行动组（STS-5）特殊阵营。

## 功能特性

### 第五特别行动组
- **定时生成**：回合开始后可配置延迟时间（默认 15 分钟）从旁观者中随机选择 3-6 名玩家生成
- **五种职位**：队长、压制者、特种干员、精英、士兵，各有独特装备和血量
- **自定义展示**：头顶显示职位名称，屏幕下方显示任务提示
- **专属音效**：入场时播放全服 CASSIE 公告和成员专属背景音乐
- **灵活配置**：生成位置、延迟时间、装备、血量等均可通过配置文件自定义

### 职位装备

| 职位 | 承载角色 | 血量 | 护甲 | 主武器 | 副武器 | 其他装备 |
|------|---------|------|------|--------|--------|----------|
| 队长 | 九尾指挥官 | 150 | 重甲 | E11 | COM-45 | 医疗包、肾上腺素、手榴弹、对讲机、O5钥匙卡 |
| 压制者 | 九尾中士 | 120 | 重甲 | Logicer | - | 医疗包、肾上腺素、手榴弹、对讲机、O5钥匙卡 |
| 特种干员 | 九尾中士 | 120 | 重甲 | AK | 囚鸟 | 医疗包、肾上腺素、手榴弹、闪光弹、对讲机、O5钥匙卡 |
| 精英 | 九尾中士 | 120 | 轻甲 | E11 | - | 医疗包×2、肾上腺素、手榴弹、闪光弹、对讲机、O5钥匙卡 |
| 士兵 | 九尾列兵×2 | 120 | 轻甲 | 维克托 | - | 医疗包、肾上腺素、闪光弹、对讲机、O5钥匙卡 |

## 安装

### 前置要求
- SCP: Secret Laboratory 服务器
- LabAPI 1.1.7 或更高版本
- HintServiceMeow 5.5.0 或更高版本
- AudioManagerAPI 2.3.6 或更高版本

### 安装步骤
1. 下载 `STSFifth.dll` 到服务器的 `LabMods` 目录
2. 启动服务器生成配置文件
3. 编辑 `LabMods/STSFifth/config.yml` 进行配置
4. 重启服务器

## 配置

### 主要配置项

```yaml
# 是否启用插件
IsEnabled: true

# 回合开始后延迟多少分钟触发生成
SpawnDelayMinutes: 15.0

# 生成所需的最低候选人数
MinimumSummonCount: 3

# 单次最多生成的人数
MaximumSummonCount: 6

# 是否允许旁观者 Dummy 进入候选池
AllowSpectatorDummies: true
```

### 职位配置

每个职位可单独配置血量、承载角色、装备和弹药：

```yaml
RoleSettings:
  Commander:
    MaxCount: 1
    Priority: 0
    CarrierRole: NtfCaptain
    MaxHealth: 150
  Suppressor:
    MaxCount: 1
    Priority: 1
    CarrierRole: NtfSergeant
    MaxHealth: 120
  # ... 其他职位配置
```

### 生成位置配置

支持三种生成策略：
- **预设点位**：在配置的固定坐标生成
- **九尾生成点**：使用游戏原生的九尾生成点
- **逃生区散布**：在地表逃生区随机分散生成

```yaml
Spawn:
  Strategy: EscapeZoneSpread  # 推荐使用
  SpreadRadius: 10.0
  PresetSpawnPoints: []
```

### 音频配置

插件使用嵌入资源和游戏原生 CASSIE 语音系统：

```yaml
Audio:
  CassieAudioKey: STS5_EntryCassie          # 入场 CASSIE 音频文件
  CassieSubtitleDurationSeconds: 20.0       # CASSIE 字幕显示时长
  EntryAudioKey: STS5_EntryMember           # 成员专属入场音乐
  CassieVolume: 1.0                         # CASSIE 音频音量
  EntryVolume: 1.0                          # 入场音乐音量
```

### HUD 配置

自定义玩家界面显示：

```yaml
Hud:
  RoleHintX: 0
  RoleHintY: 850          # 角色提示 Y 坐标
  FontSize: 25
```

### 翻译配置

所有文本均可在 `translation.yml` 中自定义：

```yaml
RoleDisplayNames:
  Commander: 队长
  Suppressor: 压制者
  Specialist: 特种干员
  Elite: 精英
  Soldier: 士兵

EntryCassieText: "所有单位注意，经O5议会指令第五特别行动组已进入设施..."
```

## 管理员命令

- `stsrole <玩家ID> <职位>` - 强制指定玩家为某个 STS-5 职位

职位代码：`Commander`, `Suppressor`, `Specialist`, `Elite`, `Soldier`

## 常见问题

### Q: 如何调整生成延迟？
A: 修改 `config.yml` 中的 `SpawnDelayMinutes`，单位为分钟（支持小数）

### Q: 音频没有播放？
A: 检查以下内容：
- 服务器日志中是否有音频注册错误
- AudioManagerAPI 是否正确安装
- 配置中的音频 Key 是否正确
- 音频音量是否设置为 0

### Q: CASSIE 播报听不到或内容不对？
A: CASSIE 入场公告使用预录音频 + CASSIE 句号延时字幕：
- **音频**：播放 `entry_cassie.wav` 音频文件（预录制）
- **字幕**：在 `translation.yml` 的 `EntryCassieText` 字段配置
- **字幕时长**：在 `config.yml` 的 `Audio.CassieSubtitleDurationSeconds` 配置（默认 20 秒）

### Q: 如何修改生成位置？
A: 推荐使用 `EscapeZoneSpread` 策略在地表随机分散生成。如需固定点位，可使用 `stsrole` 命令生成后记录坐标，然后配置到 `PresetSpawnPoints`。

### Q: 玩家头顶信息显示不正确？
A: 检查 HUD 坐标配置，确保 `RoleHintY` 值合适（默认 850）。如果文本重叠，可适当减小此值。

## 已知限制

- 本插件暂未实现 Omega 核弹系统，该功能正在重新设计中
- 生成需要足够数量的旁观者候选人（默认至少 3 人）
- 承载角色使用九尾狐阵营，可能与某些阵营统计插件产生冲突

## 技术支持

- **问题反馈**：[GitHub Issues](https://github.com/whystars/STSFifth/issues)
- **版本历史**：[Releases](https://github.com/whystars/STSFifth/releases)

## 许可证

本项目使用 GPL v3 许可证。详见 [LICENSE](LICENSE) 文件。

## 更新日志

### v1.1.1-pre (当前)
- 🐛 修复 CustomInfoArea 残留 UnitID 的问题
- 🐛 修复角色介绍 HUD 坐标过低导致文本重叠
- ✨ 增强 CASSIE 入场字幕延时功能（支持配置显示时长）
- 🔧 暂时移除 Omega 核弹功能（等待重新设计）

### v1.1.0
- 🎉 首次正式发布
- ✨ 实现第五特别行动组完整功能
- ✨ 五种职位与装备系统
- ✨ 自定义音频与 HUD 显示
- ✨ 灵活的生成位置策略
