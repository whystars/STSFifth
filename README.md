# STSFifth - 第五特别行动组插件

![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7+-blue)
![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.x-orange)
![License](https://img.shields.io/badge/License-GPL--3.0-red)

SCP: Secret Laboratory 服务器插件，为游戏添加第五特别行动组（STS-5）特殊阵营和 Omega 核弹机制。

基于 LabAPI 框架开发。

## 功能特性

### 第五特别行动组
- **定时生成**：回合开始 15 分钟后从旁观者中随机选择 3-6 名玩家生成
- **五种职位**：队长、压制者、特种干员、精英、士兵，各有独特装备和血量
- **自定义展示**：头顶显示职位名称，屏幕下方显示任务提示
- **专属音效**：入场时播放全服 CASSIE 公告和成员专属背景音乐

### Omega 核弹系统
- **地表启动**：STS-5 成员可在地表按钮启动 Omega 核弹（130 秒倒计时）
- **地下关闭**：任何人可在地下核弹室按红色按钮关闭
- **重新启动**：关闭后可重新启动，从剩余时间继续倒计时
- **循环音乐**：启动时播放循环背景音乐，关闭时停止
- **爆炸结算**：倒计时归零后，非基金会阵营全部死亡，基金会传送到逃生区，强制基金会获胜

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

# Omega 核弹倒计时秒数
Nuke:
  CountdownSeconds: 130.0
  NotificationDurationSeconds: 8.0
  EndRoundDelaySeconds: 3.0
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
  # ... 其他职位配置
```

### 音频配置

插件使用嵌入资源和游戏原生 CASSIE 语音系统：

```yaml
Audio:
  CassieAudioKey: STS5_EntryCassie      # 入场 CASSIE 音频文件
  EntryAudioKey: STS5_EntryMember       # 成员专属入场音乐
  NukeStartAudioKey: STS5_NukeStart     # 核弹启动背景音乐
  CassieVolume: 1.0                     # CASSIE 音频音量
  EntryVolume: 1.0                      # 入场音乐音量
  NukeStartVolume: 1.0                  # 核弹音乐音量
```

**CASSIE 播报机制**：
- **入场**：播放 `entry_cassie.wav` 音频文件 + 显示中文字幕
- **核弹启动**：游戏原生 CASSIE 语音朗读英文 + 播放 `nuke.wav` 背景音乐 + 显示中文字幕（类似原版 Alpha 核弹）
- **核弹关闭**：游戏原生 CASSIE 语音朗读英文 + 显示中文字幕

**自定义 CASSIE 语音**（仅核弹启动/关闭）：
可在 `translation.yml` 中配置：
- `NukeStartCassieAnnouncement` - 核弹启动时 CASSIE 朗读的英文文本（CASSIE 语法）
- `NukeStartCassieText` - 核弹启动时显示的中文字幕
- `NukeStopCassieAnnouncement` - 核弹关闭时 CASSIE 朗读的英文文本（CASSIE 语法）
- `NukeStopCassieText` - 核弹关闭时显示的中文字幕

## 管理员命令

需要 RemoteAdmin 权限：

### stsrole
手动设置玩家为第五特别行动组成员

```
stsrole <PlayerId> <RoleUid>
```

**职位 UID**：
- `Commander` - 队长
- `Suppressor` - 压制者
- `Specialist` - 特种干员
- `Elite` - 精英
- `Soldier` - 士兵

**示例**：
```
stsrole 1 Commander
```

### stsnuke
强制控制 Omega 核弹状态

```
stsnuke start   # 强制启动
stsnuke stop    # 强制关闭
```

## 游戏机制

### 生成触发
- 回合开始 15 分钟后自动触发（可配置）
- 从旁观者中随机选择候选人
- 最少 3 人，最多 6 人
- 一局只触发 1 次
- 人数不足按优先级截断：队长 → 压制者 → 特种干员 → 精英 → 士兵

### Omega 核弹
- **启动条件**：STS-5 成员在地表按钮 + Omega 未启动 + 原版核弹未启动
- **关闭条件**：任何人在地下红色按钮 + Omega 已启动
- **重启机制**：关闭后可重新启动，从剩余时间继续倒计时（不重置为 130 秒）
- **冲突处理**：Omega 核弹与原版核弹互斥，同时只能有一个运行

### 爆炸结算
1. 基金会阵营（包括 STS-5、QRT、PSC）传送到逃生区
2. 播放震屏和开门特效
3. 非基金会阵营全部死亡（死亡原因："你在Omega核弹爆炸中消失了"）
4. 全服显示"核辐射下的秘密"提示
5. 3 秒后强制结束回合，基金会获胜

## 跨插件兼容性

### 已测试兼容
- **QRTForces**：基金会快速反应部队插件
- **PSCFaction**：PSC 阵营插件
- **HUDInfo**：HUD 信息显示插件
- **LevelUp**：等级系统插件

### 豁免机制
爆炸时自动豁免所有基金会阵营玩家（`Faction.FoundationStaff`），包括：
- STS-5 成员
- QRT 成员
- PSC 成员
- 科学家
- 设施警卫

## 技术细节

### 开发环境
- .NET Framework 4.8.1
- C# 13.0
- LabAPI 1.1.7

### 依赖库
- HintServiceMeow 5.5.0 - HUD 显示
- AudioManagerAPI 2.3.6 - 音频播放
- YamlDotNet 18.1.0 - 配置序列化

### 嵌入资源
- `entry_cassie.wav` (2.5 MB) - 入场 CASSIE 公告音频
- `entry_member.wav` (30 MB) - 成员专属入场音乐
- `nuke.wav` (26 MB) - Omega 核弹启动/倒计时背景音乐（循环播放）

**CASSIE 系统**：
- **入场**：播放预录制的 `entry_cassie.wav` 音频文件
- **核弹启动/关闭**：使用游戏原生 CASSIE 语音朗读英文，类似原版 Alpha 核弹

## 常见问题

### Q: 为什么生成不了第五特别行动组？
A: 检查以下条件：
- 回合是否已经过 15 分钟（可在配置中调整）
- 旁观者人数是否达到最低要求（默认 3 人）
- 当前回合是否已经生成过（一局只生成 1 次）
- 插件配置中 `IsEnabled` 是否为 `true`

### Q: Omega 核弹按钮按不了？
A: 检查以下条件：
- 地表按钮：按的人必须是 STS-5 成员，且 Omega 未启动，且原版核弹未启动
- 地下按钮：Omega 必须已经启动才能关闭

### Q: 核弹爆炸后为什么基金会没有传送到逃生区？
A: 可能的原因：
- 服务器地图逃生区坐标异常
- 与其他插件冲突（修改了传送逻辑）
- 检查服务器日志中是否有传送相关的错误信息

### Q: 如何调整生成时间？
A: 编辑配置文件中的 `SpawnDelayMinutes`，单位为分钟（支持小数）

### Q: 音频没有播放？
A: 检查以下内容：
- 服务器日志中是否有音频注册错误
- AudioManagerAPI 是否正确安装
- 配置中的音频 Key 是否正确
- 音频音量是否设置为 0

### Q: CASSIE 播报听不到或内容不对？
A: CASSIE 播报分为两种机制：
- **入场 CASSIE**：播放 `entry_cassie.wav` 音频文件（预录制）+ 中文字幕
- **核弹启动/关闭 CASSIE**：游戏原生 CASSIE 语音朗读英文 + 中文字幕
  - 英文朗读文本：在 `translation.yml` 的 `NukeStartCassieAnnouncement` / `NukeStopCassieAnnouncement` 字段配置
  - 中文字幕：在 `translation.yml` 的 `NukeStartCassieText` / `NukeStopCassieText` 字段配置
  - 确认 CASSIE 英文文本使用了正确的 CASSIE 语法（大写英文、使用 `.` 作为停顿）

## 许可证

本项目使用 GPL v3 许可证。详见 [LICENSE](LICENSE) 文件。

## 作者

Crystal

## 版本

0.1.0

---

**注意**：本插件为 SCP: Secret Laboratory 服务器插件，需要服务器管理员权限才能安装和配置。
