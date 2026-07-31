using System;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;

namespace STSFifth
{
    public sealed class StsFifthPlugin : Plugin<StsConfig>
    {
        private const string LogPrefix = "[STSFifth]";
        private const string TranslationFileName = "translation.yml";

        private StsEventsHandler eventsHandler;
        private StsService stsService;
        private StsNukeService nukeService;
        private StsCommandRegistrar commandRegistrar;

        public override string Name => "STSFifth";

        public override string Description => "SCP: Secret Laboratory 第五特别行动组 LabAPI 插件。";

        public override string Author => "Crystal";

        public override Version Version => new Version(1, 1, 2);

        public override Version RequiredApiVersion => new Version(1, 1, 7);

        public override string ConfigFileName { get; set; } = "config.yml";

        public StsTranslation Translation { get; private set; } = StsTranslation.CreateDefault();

        public override void LoadConfigs()
        {
            try
            {
                base.LoadConfigs();
            }
            catch (Exception exception)
            {
                Logger.Error($"{LogPrefix} 主配置加载失败，将使用默认配置。错误：{exception}");
                Config = StsConfig.CreateDefault();
            }

            if (Config == null)
            {
                Logger.Warn($"{LogPrefix} 主配置为空，已使用默认配置。");
                Config = StsConfig.CreateDefault();
            }

            Config.Validate(LogConfigWarning);
            LoadTranslation();
        }

        public override void Enable()
        {
            if (Config != null && !Config.IsEnabled)
            {
                Logger.Info($"{LogPrefix} 插件配置已关闭，未注册事件处理器。");
                return;
            }

            StsAudioService audioService = new StsAudioService(Config, Translation);
            audioService.RegisterAudioResources();

            StsHudService hudService = new StsHudService(Config, Translation);
            StsPresentationService presentationService = new StsPresentationService(Config, Translation, hudService);
            StsSpawnService spawnService = new StsSpawnService(Config);

            stsService = new StsService(Config, Translation, audioService, presentationService, spawnService);
            nukeService = new StsNukeService(Config, Translation, audioService, hudService, spawnService, stsService);

            eventsHandler = new StsEventsHandler(stsService, nukeService);
            commandRegistrar = new StsCommandRegistrar(Config, Translation, stsService, nukeService);

            CustomHandlersManager.RegisterEventsHandler(eventsHandler);
            commandRegistrar.Register();

            Logger.Info($"{LogPrefix} 插件已启用，配置和翻译已加载，事件处理器、生成状态机、Omega 核弹系统和管理员命令已注册。");
        }

        public override void Disable()
        {
            if (commandRegistrar != null)
            {
                commandRegistrar.Unregister();
                commandRegistrar = null;
            }

            if (eventsHandler != null)
            {
                CustomHandlersManager.UnregisterEventsHandler(eventsHandler);
                eventsHandler = null;
            }

            if (nukeService != null)
            {
                nukeService.StopAll("插件禁用");
                nukeService = null;
            }

            if (stsService != null)
            {
                stsService.ClearRoundState("插件禁用");
                stsService = null;
            }

            Logger.Info($"{LogPrefix} 插件已禁用，事件处理器已注销，生成状态和核弹状态已清理。");
        }

        private void LoadTranslation()
        {
            try
            {
                if (!ConfigurationLoader.TryLoadConfig(this, TranslationFileName, out StsTranslation translation, false) || translation == null)
                {
                    Logger.Warn($"{LogPrefix} 翻译文件加载失败，将使用默认翻译。");
                    Translation = StsTranslation.CreateDefault();
                    return;
                }

                Translation = translation;
            }
            catch (Exception exception)
            {
                Logger.Error($"{LogPrefix} 翻译文件加载失败，将使用默认翻译。错误：{exception}");
                Translation = StsTranslation.CreateDefault();
            }

            Translation.Validate(LogConfigWarning);
        }

        private static void LogConfigWarning(string message)
        {
            Logger.Warn($"{LogPrefix} {message}");
        }
    }
}
