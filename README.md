# STSFifth - 第五特别行动组插件

![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7+-blue)
![Version](https://img.shields.io/badge/Version-1.2.0-green)
![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.x-orange)
![License](https://img.shields.io/badge/License-GPL--3.0-red)

SCP: Secret Laboratory 服务器插件，为游戏添加第五特别行动组（STS-5）特殊阵营和 Omega 核弹系统。

## 功能特性

### 第五特别行动组
- **定时生成**：回合开始后可配置延迟时间（默认 15 分钟）从旁观者中随机选择 3-6 名玩家生成
- **五种职位**：队长、压制者、特种干员、精英、士兵，各有独特装备和血量
- **自定义展示**：头顶显示职位名称，屏幕下方显示任务提示
- **专属音效**：入场时播放全服 CASSIE 公告和成员专属背景音乐
- **灵活配置**：生成位置、延迟时间、装备、血量等均可通过配置文件自定义
- **纯净物品栏**：生成时清空原版物品，仅发放配置的装备

### Omega 核弹系统 ⭐新增
- **硬币启动**：队长生成时获得特殊硬币（放大尺寸），在核弹室投掷启动
- **所有成员可用**：任何 STS-5 成员都可以拾取并使用硬币启动核弹
- **Alpha 核弹锁定**：启动 Omega 核弹时自动停止并锁定 Alpha 核弹本局
- **130 秒倒计时**：可配置的倒计时，实时显示在所有玩家屏幕上
- **设施灯光变色**：启动时全设施灯光变为可配置的颜色（默认蓝色）
- **CASSIE 公告**：启动、停止、重启时播放带钟声的 CASSIE 公告
- **核弹音频**：播放专属核弹音频（支持循环）
- **可停止重启**：任何人按核弹面板按钮可停止，再次投掷硬币可重启（时间重置）
- **爆炸效果**：
  - 杀死所有非基金会阵营（SCP、混沌、D级人员等）
  - 传送基金会阵营（科学家、九尾狐、设施警卫）到地表撤离点
  - 强制结束回合，基金会胜利
- **独立运行**：与 Alpha 核弹系统完全独立，互不干扰

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
- AudioManagerAPI 2.4.2 或更高版本

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

### Omega 核弹配置 ⭐新增

```yaml
Nuke:
  IsEnabled: true                           # 是否启用 Omega 核弹功能
  DetonationSeconds: 130.0                  # 核弹倒计时（秒）
  GiveCoinToCommander: true                 # 是否给队长发放启动硬币
  CoinPickupScale: 1.8                      # 硬币掉落物缩放倍数
  NukeAudioVolume: 0.8                      # 核弹音频音量
  LoopNukeAudio: false                      # 核弹音频是否循环播放
  EnableLightEffect: true                   # 是否启用灯光变色效果
  LightColorR: 0                            # 灯光颜色 - 红色分量（0-255）
  LightColorG: 128                          # 灯光颜色 - 绿色分量（0-255）
  LightColorB: 255                          # 灯光颜色 - 蓝色分量（0-255）
  CountdownHudX: 0                          # 倒计时 HUD X 坐标
  CountdownHudY: 200                        # 倒计时 HUD Y 坐标
  CountdownHudFontSize: 30                  # 倒计时 HUD 字体大小
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

### stsrole - 强制指定玩家职位
- `stsrole <玩家ID> <职位>` - 强制指定玩家为某个 STS-5 职位

职位代码：`Commander`, `Suppressor`, `Specialist`, `Elite`, `Soldier`

### omeganuke - 核弹控制 ⭐新增
- `omeganuke start` - 强制启动 Omega 核弹
- `omeganuke stop` - 强制停止 Omega 核弹

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

### Q: Omega 核弹如何启动？ ⭐新增
A: 
1. 等待 STS-5 生成（默认回合开始后 15 分钟）
2. 队长会自动获得一个放大的硬币
3. 前往地下核弹室（Heavy Containment Zone 核弹房间）
4. 在核弹室内投掷硬币即可启动
5. 任何 STS-5 成员都可以捡起硬币使用

### Q: Omega 核弹可以停止吗？ ⭐新增
A: 可以。任何人都可以按核弹室的红色按钮停止 Omega 核弹。停止后可以再次投掷硬币重启，时间会重置为 130 秒。

### Q: Omega 核弹和 Alpha 核弹有什么区别？ ⭐新增
A: 
- Omega 核弹启动时会自动停止并锁定 Alpha 核弹本局
- Omega 核弹爆炸只杀死非基金会阵营，基金会阵营会被传送到地表
- Omega 核弹爆炸后强制基金会胜利
- Alpha 核弹爆炸杀死所有人

### Q: 硬币掉落物为什么这么大？ ⭐新增
A: 为了方便识别 Omega 核弹启动硬币，插件会自动放大 STS-5 成员掉落的硬币尺寸（默认 1.8 倍）。可以在配置文件中调整 `Nuke.CoinPickupScale`。

### Q: 如何更改灯光颜色？ ⭐新增
A: 在配置文件中修改 `Nuke.LightColorR`、`Nuke.LightColorG`、`Nuke.LightColorB` 三个值（0-255），可以设置任意 RGB 颜色。默认为蓝色 (0, 128, 255)。

## 已知限制

- 生成需要足够数量的旁观者候选人（默认至少 3 人）
- 承载角色使用九尾狐阵营，可能与某些阵营统计插件产生冲突
- Omega 核弹音频长度需足够覆盖 130 秒倒计时（或启用循环播放）

## 技术支持

- **问题反馈**：[GitHub Issues](https://github.com/whystars/STSFifth/issues)
- **版本历史**：[Releases](https://github.com/whystars/STSFifth/releases)

## 许可证

本项目使用 GPL v3 许可证。详见 [LICENSE](LICENSE) 文件。

## 更新日志

### v1.2.0 (当前)
- 🎉 **完整实现 Omega 核弹系统**
- ✨ 队长生成时获得启动硬币（可配置放大尺寸）
- ✨ 在核弹室投掷硬币启动 Omega 核弹
- ✨ 启动时自动停止并锁定 Alpha 核弹本局
- ✨ 130 秒倒计时（可配置）
- ✨ 实时 HUD 倒计时显示（可配置位置和字体）
- ✨ 设施灯光变色效果（RGB 可配置）
- ✨ CASSIE 公告系统（启动/停止/重启）
- ✨ 核弹音频播放（支持循环）
- ✨ 可停止和重启机制
- ✨ 爆炸逻辑：杀死非基金会，传送基金会到地表
- ✨ 强制基金会胜利结束回合
- ✨ 管理员命令 `omeganuke start/stop`
- 🔧 修复物品发放逻辑：清空原版物品，仅发放配置的装备
- 🔧 生成时使用 `RoleSpawnFlags.None` 阻止原版装备

### v1.1.2-pre
- ⬆️ 升级 AudioManagerAPI 2.3.6 → 2.4.2
- 🔧 适配 AudioManagerAPI 2.4.x 新的泛型状态参数 API
- 📝 更新依赖文档

### v1.1.1-pre
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
