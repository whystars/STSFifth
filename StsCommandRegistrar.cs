using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using Logger = LabApi.Features.Console.Logger;
using LabServer = LabApi.Features.Wrappers.Server;

namespace STSFifth
{
    public sealed class StsCommandRegistrar
    {
        private const string LogPrefix = "[STSFifth]";

        private readonly List<ICommand> commands = new List<ICommand>();
        private bool registered;

        public StsCommandRegistrar(StsConfig config, StsTranslation translation, StsService stsService, StsNukeService nukeService)
        {
            commands.Add(new StsRoleCommand(config, translation, stsService));
            commands.Add(new StsNukeCommand(config, translation, nukeService));
        }

        public void Register()
        {
            if (registered)
            {
                return;
            }

            foreach (ICommand command in commands)
            {
                RegisterCommand(LabServer.RemoteAdminCommandHandler, command);
            }

            registered = true;
            Logger.Info($"{LogPrefix} 管理员命令 stsrole / stsnuke 已注册到 RemoteAdmin。");
        }

        public void Unregister()
        {
            if (!registered)
            {
                return;
            }

            foreach (ICommand command in commands)
            {
                UnregisterCommand(LabServer.RemoteAdminCommandHandler, command);
            }

            registered = false;
            Logger.Info($"{LogPrefix} 管理员命令 stsrole / stsnuke 已注销。");
        }

        private static void RegisterCommand(CommandHandler handler, ICommand command)
        {
            try
            {
                handler.RegisterCommand(command);
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 注册管理员命令失败：Command={command.Command}，错误：{exception.Message}");
            }
        }

        private static void UnregisterCommand(CommandHandler handler, ICommand command)
        {
            try
            {
                handler.UnregisterCommand(command);
            }
            catch (Exception exception)
            {
                Logger.Warn($"{LogPrefix} 注销管理员命令失败：Command={command.Command}，错误：{exception.Message}");
            }
        }
    }

    internal sealed class StsRoleCommand : ICommand
    {
        private static readonly StsRole[] AssignableRoles =
        {
            StsRole.Commander,
            StsRole.Suppressor,
            StsRole.Specialist,
            StsRole.Elite,
            StsRole.Soldier
        };

        private readonly StsConfig config;
        private readonly StsTranslation translation;
        private readonly StsService stsService;

        public StsRoleCommand(StsConfig config, StsTranslation translation, StsService stsService)
        {
            this.config = config ?? StsConfig.CreateDefault();
            this.translation = translation ?? StsTranslation.CreateDefault();
            this.stsService = stsService;
        }

        public string Command => "stsrole";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "STSFifth 管理员命令：设置本插件自定义职位。";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!CanExecute(sender, out response))
            {
                return false;
            }

            if (arguments.Count == 0)
            {
                response = BuildUsage();
                return true;
            }

            if (arguments.Count != 2 || !int.TryParse(GetArgument(arguments, 0), out int playerId))
            {
                response = $"{translation.CommandResponses.InvalidUsage}\n{BuildUsage()}";
                return false;
            }

            string rawRole = GetArgument(arguments, 1);
            if (!Enum.TryParse(rawRole, true, out StsRole role) || !AssignableRoles.Contains(role))
            {
                response = translation.CommandResponses.InvalidRole.Replace("{Role}", rawRole);
                return false;
            }

            Player target = FindPlayerById(playerId);
            if (target == null)
            {
                response = translation.CommandResponses.PlayerNotFound.Replace("{PlayerId}", playerId.ToString());
                return false;
            }

            string executorName = GetSenderName(sender);
            bool success = stsService.TryAssignStsRoleForCommand(target, role, executorName, out string reason);

            if (!success)
            {
                response = reason;
                return false;
            }

            response = translation.CommandResponses.RoleAssigned
                .Replace("{PlayerName}", FormatPlayer(target))
                .Replace("{RoleUid}", role.ToString())
                .Replace("{RoleName}", GetRoleDisplayName(role));
            return true;
        }

        private bool CanExecute(ICommandSender sender, out string response)
        {
            if (!config.EnableTestCommands)
            {
                response = translation.CommandResponses.CommandsDisabled;
                return false;
            }

            if (HasRemoteAdminPermission(sender))
            {
                response = string.Empty;
                return true;
            }

            response = translation.CommandResponses.NoPermission;
            return false;
        }

        private bool HasRemoteAdminPermission(ICommandSender sender)
        {
            try
            {
                if (sender is CommandSender commandSender && commandSender.FullPermissions)
                {
                    return true;
                }

                Player player = Player.Get(sender);
                return player != null && player.RemoteAdminAccess;
            }
            catch (Exception exception)
            {
                Logger.Warn($"[STSFifth] 检查 stsrole 权限失败：Sender={GetSenderName(sender)}，错误：{exception.Message}");
                return false;
            }
        }

        private string BuildUsage()
        {
            IEnumerable<string> roleLines = AssignableRoles.Select(role => $"{role} = {GetRoleDisplayName(role)}");
            return string.Join(
                "\n",
                "用法：",
                "stsrole",
                "stsrole <PlayerId> <RoleUid>",
                "示例：stsrole 3 Soldier",
                "角色 UID 对照：",
                string.Join("\n", roleLines));
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

        private static string GetSenderName(ICommandSender sender)
        {
            return string.IsNullOrWhiteSpace(sender?.LogName) ? "<unknown>" : sender.LogName;
        }

        private static string GetArgument(ArraySegment<string> arguments, int index)
        {
            if (arguments.Array == null || index < 0 || index >= arguments.Count)
            {
                return string.Empty;
            }

            return arguments.Array[arguments.Offset + index];
        }

        private static Player FindPlayerById(int playerId)
        {
            return Player.List.FirstOrDefault(player => player != null && player.PlayerId == playerId) ??
                   Player.DummyList.FirstOrDefault(player => player != null && player.PlayerId == playerId);
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

    internal sealed class StsNukeCommand : ICommand
    {
        private readonly StsConfig config;
        private readonly StsTranslation translation;
        private readonly StsNukeService nukeService;

        public StsNukeCommand(StsConfig config, StsTranslation translation, StsNukeService nukeService)
        {
            this.config = config ?? StsConfig.CreateDefault();
            this.translation = translation ?? StsTranslation.CreateDefault();
            this.nukeService = nukeService;
        }

        public string Command => "stsnuke";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "STSFifth 管理员命令：强制启动或关闭 Omega 核弹。";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!CanExecute(sender, out response))
            {
                return false;
            }

            if (arguments.Count == 0)
            {
                response = "用法：stsnuke start|stop";
                return true;
            }

            string action = GetArgument(arguments, 0).ToLower();

            if (action == "start")
            {
                nukeService.ForceStartNuke();
                response = translation.CommandResponses.NukeStarted;
                return true;
            }

            if (action == "stop")
            {
                nukeService.ForceStopNuke();
                response = translation.CommandResponses.NukeStopped;
                return true;
            }

            response = translation.CommandResponses.NukeInvalidAction.Replace("{Action}", action);
            return false;
        }

        private bool CanExecute(ICommandSender sender, out string response)
        {
            if (!config.EnableTestCommands)
            {
                response = translation.CommandResponses.CommandsDisabled;
                return false;
            }

            if (HasRemoteAdminPermission(sender))
            {
                response = string.Empty;
                return true;
            }

            response = translation.CommandResponses.NoPermission;
            return false;
        }

        private bool HasRemoteAdminPermission(ICommandSender sender)
        {
            try
            {
                if (sender is CommandSender commandSender && commandSender.FullPermissions)
                {
                    return true;
                }

                Player player = Player.Get(sender);
                return player != null && player.RemoteAdminAccess;
            }
            catch (Exception exception)
            {
                Logger.Warn($"[STSFifth] 检查 stsnuke 权限失败：Sender={sender?.LogName ?? "<null>"}，错误：{exception.Message}");
                return false;
            }
        }

        private static string GetArgument(ArraySegment<string> arguments, int index)
        {
            if (arguments.Array == null || index < 0 || index >= arguments.Count)
            {
                return string.Empty;
            }

            return arguments.Array[arguments.Offset + index];
        }
    }
}
