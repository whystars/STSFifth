using System;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Extension;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities;
using LabApi.Features.Wrappers;
using Logger = LabApi.Features.Console.Logger;

namespace STSFifth
{
    public sealed class StsHudService
    {
        private const string LogPrefix = "[STSFifth]";
        private const string RoleHintPrefix = "STSFifth.Role.";
        private const string NukeCountdownHintPrefix = "STSFifth.NukeCountdown.";
        private const string NotificationHintPrefix = "STSFifth.Notification.";

        private readonly StsConfig config;
        private readonly StsTranslation translation;

        public StsHudService(StsConfig config, StsTranslation translation)
        {
            this.config = config ?? StsConfig.CreateDefault();
            this.translation = translation ?? StsTranslation.CreateDefault();

            Logger.Info(
                $"{LogPrefix} HUD 布局已加载：角色提示=({this.config.Hud.RoleHintX:0.###}, {this.config.Hud.RoleHintY:0.###})，" +
                $"临时通知=({this.config.Hud.NotificationHintX:0.###}, {this.config.Hud.NotificationHintY:0.###})，字号={this.config.Hud.FontSize}。");
        }

        public void ShowRoleHud(Player player, StsRole role, string roleName)
        {
            if (player == null)
            {
                return;
            }

            if (!translation.RoleHudTexts.TryGetValue(role, out string template) || string.IsNullOrWhiteSpace(template))
            {
                template = $"你是 {roleName}";
            }

            string text = template.Replace("{RoleName}", roleName ?? role.ToString());

            UpsertHint(
                player,
                GetRoleHintId(player),
                text,
                config.Hud.RoleHintX,
                config.Hud.RoleHintY,
                false);
        }

        // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
        /*
        public void ShowNukeCountdown(Player player, float remainingSeconds)
        {
            if (player == null)
            {
                return;
            }

            string text = (translation.NukeCountdownHudText ?? "Omega核弹倒计时:{Seconds}秒")
                .Replace("{Seconds}", ((int)Math.Ceiling(remainingSeconds)).ToString());

            UpsertHint(
                player,
                GetNukeCountdownHintId(player),
                text,
                config.Hud.NukeCountdownX,
                config.Hud.NukeCountdownY,
                false);
        }

        public void HideNukeCountdown(Player player)
        {
            if (player == null)
            {
                return;
            }

            PlayerDisplay display = TryGetDisplay(player);
            if (display == null)
            {
                return;
            }

            try
            {
                RemoveHintIfExists(display, GetNukeCountdownHintId(player));
                display.ForceUpdate(true);
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 隐藏核弹倒计时 HUD 失败：{FormatPlayer(player)}，错误：{exception.Message}");
            }
        }
        */

        public void ShowNotification(Player player, string text, float durationSeconds)
        {
            if (player == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Hint hint = UpsertHint(
                player,
                GetNotificationHintId(player),
                text,
                config.Hud.NotificationHintX,
                config.Hud.NotificationHintY,
                false);

            if (hint == null || durationSeconds <= 0f)
            {
                return;
            }

            try
            {
                hint.HideAfter(durationSeconds);
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 临时 HUD 自动隐藏失败：{FormatPlayer(player)}，错误：{exception.Message}");
            }
        }

        public void ClearPlayer(Player player)
        {
            if (player == null)
            {
                return;
            }

            PlayerDisplay display = TryGetDisplay(player);
            if (display == null)
            {
                return;
            }

            try
            {
                RemoveHintIfExists(display, GetRoleHintId(player));
                // TODO: 待后续设计文档完善后重新实现 Omega 核弹功能
                //RemoveHintIfExists(display, GetNukeCountdownHintId(player));
                RemoveHintIfExists(display, GetNotificationHintId(player));
                display.ForceUpdate(true);
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 清理 HUD 失败：{FormatPlayer(player)}，错误：{exception.Message}");
            }
        }

        private Hint UpsertHint(Player player, string id, string text, float x, float y, bool hide)
        {
            PlayerDisplay display = TryGetDisplay(player);
            if (display == null)
            {
                return null;
            }

            try
            {
                Hint hint = null;
                if (display.HasHint(id))
                {
                    if (display.GetHint(id) is Hint existingHint)
                    {
                        if (HasExpectedLayout(existingHint, x, y))
                        {
                            hint = existingHint;
                        }
                        else
                        {
                            display.RemoveHint(id);
                        }
                    }
                    else
                    {
                        display.RemoveHint(id);
                    }
                }

                if (hint == null)
                {
                    hint = new Hint
                    {
                        Id = id,
                        Text = text ?? string.Empty,
                        FontSize = config.Hud.FontSize,
                        XCoordinate = x,
                        YCoordinate = y,
                        Alignment = HintAlignment.Center,
                        YCoordinateAlign = HintVerticalAlign.Bottom,
                        Hide = hide
                    };
                    display.AddHint(hint);
                }
                else
                {
                    hint.Text = text ?? string.Empty;
                    hint.Hide = hide;
                }

                display.ForceUpdate(true);
                return hint;
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 更新 HUD 失败：{FormatPlayer(player)}，HintId={id}，错误：{exception.Message}");
                return null;
            }
        }

        private static void RemoveHintIfExists(PlayerDisplay display, string id)
        {
            if (display.HasHint(id))
            {
                display.RemoveHint(id);
            }
        }

        private bool HasExpectedLayout(Hint hint, float x, float y)
        {
            return hint != null &&
                   Math.Abs(hint.XCoordinate - x) < 0.01f &&
                   Math.Abs(hint.YCoordinate - y) < 0.01f &&
                   hint.FontSize == config.Hud.FontSize &&
                   hint.Alignment == HintAlignment.Center &&
                   hint.YCoordinateAlign == HintVerticalAlign.Bottom;
        }

        private static PlayerDisplay TryGetDisplay(Player player)
        {
            try
            {
                PlayerDisplay display = PlayerDisplay.Get(player);
                if (display == null)
                {
                    Logger.Warn($"{LogPrefix} HintServiceMeow 未返回显示对象：{FormatPlayer(player)}。");
                }

                return display;
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 获取 HintServiceMeow 显示对象失败：{FormatPlayer(player)}，错误：{exception.Message}");
                return null;
            }
        }

        private static string GetRoleHintId(Player player)
        {
            return $"{RoleHintPrefix}{player.PlayerId}";
        }

        private static string GetNukeCountdownHintId(Player player)
        {
            return $"{NukeCountdownHintPrefix}{player.PlayerId}";
        }

        private static string GetNotificationHintId(Player player)
        {
            return $"{NotificationHintPrefix}{player.PlayerId}";
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
