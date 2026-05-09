using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PluginList.UI;

namespace PluginList {
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public class PluginList : IDalamudPlugin {
        internal static PluginList Instance { get; private set; } = null!;

        private readonly Config _config;
        internal readonly IDalamudPluginInterface _pluginInterface;
        private readonly ICommandManager _commandManager;
        private readonly IPluginLog _pluginLogger;

        internal readonly Menu _mainUI;
        private readonly HoverDock _hoverDock;
        private readonly WindowSystem _windowSystem = new("PluginList");

        public PluginList(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IPluginLog pluginLogger,
            IGameInteropProvider gameInteropProvider) {
            Instance = this;

            _pluginInterface = pluginInterface;
            _commandManager = commandManager;
            _pluginLogger = pluginLogger;

            _config = _pluginInterface.GetPluginConfig() as Config ?? new Config();

            ChatExecutor.Initialize(gameInteropProvider);

            _hoverDock = new HoverDock(_config);
            _mainUI = new Menu(_config, SaveConfig, _pluginInterface, _hoverDock);

            _windowSystem.AddWindow(_hoverDock);
            _windowSystem.AddWindow(_mainUI);

            _pluginInterface.UiBuilder.Draw += _windowSystem.Draw;
            _pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            _pluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;

            _commandManager.AddHandler("/pluginlist", new CommandInfo(OnCommand) {
                HelpMessage = "/pluginlist: Opens the PluginList settings window."
            });
            
            _commandManager.AddHandler("/pl", new CommandInfo(OnCommand) {
                HelpMessage = "/pl: Opens the PluginList settings window."
            });
        }

        public void Dispose() {
            _pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
            _pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
            _pluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;

            _windowSystem.RemoveAllWindows();
            _mainUI.Dispose();

            _commandManager.RemoveHandler("/pluginlist");
            _commandManager.RemoveHandler("/pl");
        }

        internal void SaveConfig() => _pluginInterface.SavePluginConfig(_config);

        private void OpenConfigUi() => _mainUI.IsOpen = true;

        private void OnCommand(string commandName, string commandArguments) => _mainUI.IsOpen = true;
    }
}
