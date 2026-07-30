using System;
using System.IO;
using System.Reflection;
using System.Text;
using AudioManagerAPI.Defaults;
using AudioManagerAPI.Features.Enums;
using LabApi.Features.Wrappers;
using UnityEngine;
using LabAnnouncer = LabApi.Features.Wrappers.Announcer;
using Logger = LabApi.Features.Console.Logger;

namespace STSFifth
{
    public sealed class StsAudioService
    {
        private const string LogPrefix = "[STSFifth]";
        private const string CassieResourceName = "STSFifth.Audio.entry_cassie.wav";
        private const string EntryResourceName = "STSFifth.Audio.entry_member.wav";
        private const string NukeStartResourceName = "STSFifth.Audio.nuke.wav";

        private readonly StsConfig config;
        private readonly StsTranslation translation;

        private bool audioRegistered;
        private bool cassieAudioAvailable;
        private bool entryAudioAvailable;
        private bool nukeStartAudioAvailable;
        private int cassieSessionId;
        private int entrySessionId;
        private int nukeStartSessionId;

        public StsAudioService(StsConfig config, StsTranslation translation)
        {
            this.config = config ?? StsConfig.CreateDefault();
            this.translation = translation ?? StsTranslation.CreateDefault();
        }

        public void RegisterAudioResources()
        {
            if (audioRegistered)
            {
                return;
            }

            audioRegistered = true;
            cassieAudioAvailable = TryRegisterAudioResource(config.Audio.CassieAudioKey, CassieResourceName, "入场 CASSIE 公告音频");
            entryAudioAvailable = TryRegisterAudioResource(config.Audio.EntryAudioKey, EntryResourceName, "第五特别行动组入场音频");
            nukeStartAudioAvailable = TryRegisterAudioResource(config.Audio.NukeStartAudioKey, NukeStartResourceName, "Omega 核弹启动音频");
        }

        public void PlaySummonAnnouncement(Func<Player, bool> stsMemberFilter)
        {
            PlayCassieSubtitle();
            PlayCassieAudio();
            PlayEntryAudio(stsMemberFilter);
        }

        public void PlayNukeStartAnnouncement()
        {
            PlayNukeStartCassieSubtitle();
            PlayNukeStartAudio();
        }

        public void StopAll(string reason)
        {
            StopSession(ref cassieSessionId, $"入场 CASSIE 公告音频，原因：{reason}");
            StopSession(ref entrySessionId, $"第五特别行动组入场音频，原因：{reason}");
            StopSession(ref nukeStartSessionId, $"Omega 核弹启动音频，原因：{reason}");

            // 重置可用性标志，避免外部停止后残留无效引用
            cassieSessionId = 0;
            entrySessionId = 0;
            nukeStartSessionId = 0;
        }

        private bool TryRegisterAudioResource(string rawKey, string resourceName, string purpose)
        {
            string key = NormalizeKey(rawKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                Logger.Warn($"{LogPrefix} {purpose} key 为空，已跳过嵌入资源注册。");
                return false;
            }

            using (Stream stream = OpenEmbeddedResource(resourceName))
            {
                if (stream == null)
                {
                    Logger.Warn($"{LogPrefix} {purpose}嵌入资源缺失：{resourceName}，播放时会跳过。");
                    return false;
                }
            }

            try
            {
                DefaultAudioManager.RegisterAudio(key, () => OpenEmbeddedResource(resourceName));
                Logger.Info($"{LogPrefix} 已注册{purpose}：key={key}，resource={resourceName}。");
                return true;
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 注册{purpose}失败：key={key}，resource={resourceName}。播放时仍会尝试使用该 key，错误：{exception.Message}");
                return true;
            }
        }

        private void PlayCassieSubtitle()
        {
            if (string.IsNullOrWhiteSpace(translation.EntryCassieText))
            {
                Logger.Info($"{LogPrefix} EntryCassieText 为空，已跳过 CASSIE 文本和字幕。");
                return;
            }

            try
            {
                // 入场 CASSIE 使用音频文件，不需要原生 CASSIE 语音
                LabAnnouncer.Message(
                    string.Empty,
                    translation.EntryCassieText.Trim(),
                    false,
                    0f,
                    0f);
                Logger.Info($"{LogPrefix} 已发送第五特别行动组入场 CASSIE 字幕。");
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 发送第五特别行动组入场 CASSIE 字幕失败，召唤流程不回滚。错误：{exception.Message}");
            }
        }

        private void PlayNukeStartCassieSubtitle()
        {
            if (string.IsNullOrWhiteSpace(translation.NukeStartCassieText))
            {
                Logger.Info($"{LogPrefix} NukeStartCassieText 为空，已跳过核弹启动 CASSIE 字幕。");
                return;
            }

            try
            {
                string announcement = translation.NukeStartCassieAnnouncement ?? "OMEGA WARHEAD ACTIVATED";
                string announcementWithSeconds = announcement.Replace("{Seconds}", ((int)config.Nuke.CountdownSeconds).ToString());
                string textWithSeconds = translation.NukeStartCassieText.Replace("{Seconds}", ((int)config.Nuke.CountdownSeconds).ToString());

                LabAnnouncer.Message(
                    announcementWithSeconds.Trim(),
                    textWithSeconds.Trim(),
                    false,
                    0f,
                    0f);
                Logger.Info($"{LogPrefix} 已发送 Omega 核弹启动 CASSIE 公告。");
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 发送 Omega 核弹启动 CASSIE 公告失败。错误：{exception.Message}");
            }
        }

        private void PlayCassieAudio()
        {
            string key = NormalizeKey(config.Audio.CassieAudioKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                Logger.Warn($"{LogPrefix} Audio.CassieAudioKey 为空，已跳过入场 CASSIE 公告音频播放。");
                return;
            }

            if (!cassieAudioAvailable)
            {
                Logger.Warn($"{LogPrefix} 入场 CASSIE 公告音频未成功注册，已跳过播放。key={key}");
                return;
            }

            StopSession(ref cassieSessionId, "替换旧入场 CASSIE 公告音频");

            cassieSessionId = PlayGlobalAudio(
                key,
                config.Audio.CassieVolume,
                target => target != null,
                "入场 CASSIE 公告音频");
        }

        private void PlayEntryAudio(Func<Player, bool> stsMemberFilter)
        {
            string key = NormalizeKey(config.Audio.EntryAudioKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                Logger.Warn($"{LogPrefix} Audio.EntryAudioKey 为空，已跳过第五特别行动组入场音频播放。");
                return;
            }

            if (!entryAudioAvailable)
            {
                Logger.Warn($"{LogPrefix} 第五特别行动组入场音频未成功注册，已跳过播放。key={key}");
                return;
            }

            if (stsMemberFilter == null)
            {
                Logger.Warn($"{LogPrefix} 第五特别行动组入场音频缺少成员过滤器，已跳过播放。");
                return;
            }

            StopSession(ref entrySessionId, "替换旧第五特别行动组入场音频");

            entrySessionId = PlayGlobalAudio(
                key,
                config.Audio.EntryVolume,
                target => target != null && stsMemberFilter(target),
                "第五特别行动组入场音频");
        }

        private void PlayNukeStartAudio()
        {
            string key = NormalizeKey(config.Audio.NukeStartAudioKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                Logger.Warn($"{LogPrefix} Audio.NukeStartAudioKey 为空，已跳过 Omega 核弹启动音频播放。");
                return;
            }

            if (!nukeStartAudioAvailable)
            {
                Logger.Warn($"{LogPrefix} Omega 核弹启动音频未成功注册，已跳过播放。key={key}");
                return;
            }

            StopSession(ref nukeStartSessionId, "替换旧 Omega 核弹启动音频");

            nukeStartSessionId = PlayGlobalAudio(
                key,
                config.Audio.NukeStartVolume,
                target => target != null,
                "Omega 核弹启动音频（循环）",
                loop: true);
        }

        private int PlayGlobalAudio(string key, float volume, Func<Player, bool> filter, string purpose, bool loop = false)
        {
            try
            {
                int sessionId = DefaultAudioManager.Instance.PlayGlobalAudio(
                    key: key,
                    loop: loop,
                    volume: Mathf.Clamp01(volume),
                    priority: AudioPriority.High,
                    validPlayersFilter: filter,
                    queue: false,
                    fadeInDuration: 0f,
                    persistent: loop,
                    lifespan: null,
                    autoCleanup: true);

                if (sessionId == 0)
                {
                    Logger.Warn($"{LogPrefix} 播放{purpose}失败：AudioManagerAPI 返回 sessionId=0。key={key}");
                    return 0;
                }

                Logger.Info($"{LogPrefix} 已播放{purpose}：key={key}，sessionId={sessionId}，循环={loop}。");
                return sessionId;
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 播放{purpose}失败。key={key}，错误：{exception.Message}");
                return 0;
            }
        }

        private void StopSession(ref int sessionId, string purpose)
        {
            if (sessionId <= 0)
            {
                return;
            }

            int stoppedSessionId = sessionId;
            try
            {
                DefaultAudioManager.Stop(stoppedSessionId);
                Logger.Info($"{LogPrefix} 已停止{purpose}。sessionId={stoppedSessionId}");
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 停止{purpose}失败。sessionId={stoppedSessionId}，错误：{exception.Message}");
            }
            finally
            {
                sessionId = 0;
            }
        }

        private static Stream OpenEmbeddedResource(string resourceName)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}
