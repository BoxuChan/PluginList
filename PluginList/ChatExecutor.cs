using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;
using System.Collections.Generic;

namespace PluginList {
    public unsafe class ChatExecutor {
        private static UIModule* _uiModule;

        public delegate void ProcessChatBoxDelegate(UIModule* uiModule, nint message, nint unused, byte a4);

        [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9")]
        public static ProcessChatBoxDelegate? ProcessChatBox = null;

        private static readonly Queue<string> _commandQueue = new();
        private static float _delayTimer = 0f;
        private const float DelayBetweenCommands = 0.1f;

        public static void Initialize(IGameInteropProvider interopProvider) {
            _uiModule = Framework.Instance()->GetUIModule();
            interopProvider.InitializeFromAttributes(new ChatExecutor());
        }

        public static void ExecuteCommand(string commandText) {
            if (string.IsNullOrWhiteSpace(commandText)) {
                return;
            }

            foreach (var commandLine in commandText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                var trimmedCommand = commandLine.Trim();
                
                if (string.IsNullOrWhiteSpace(trimmedCommand)) {
                    continue;
                }

                if (trimmedCommand.StartsWith("//m", StringComparison.OrdinalIgnoreCase)) {
                    if (int.TryParse(trimmedCommand[3..].Trim(), out int macroIndex)) {
                        ExecuteGameMacro(macroIndex);
                        continue;
                    }
                }

                _commandQueue.Enqueue(trimmedCommand);
            }
        }

        public static void ProcessQueue(float deltaTime) {
            if (_commandQueue.Count == 0 || ProcessChatBox == null) {
                return;
            }

            _delayTimer -= deltaTime;
            if (_delayTimer > 0f) {
                return;
            }

            var queuedCommand = _commandQueue.Dequeue();
            var utf8Command = Utf8String.FromString(queuedCommand);
            
            try {
                ProcessChatBox(_uiModule, (nint)utf8Command, nint.Zero, 0);
            } finally {
                utf8Command->Dtor(true);
            }

            _delayTimer = DelayBetweenCommands;
        }

        private static void ExecuteGameMacro(int macroIndex) {
            if (macroIndex < 0 || macroIndex >= 200) {
                return;
            }

            var raptureShellModule = _uiModule->GetRaptureShellModule();
            var raptureMacroModule = _uiModule->GetRaptureMacroModule();
            
            if (raptureShellModule == null || raptureMacroModule == null) {
                return;
            }

            ref var selectedMacro = ref (macroIndex < 100 ? ref raptureMacroModule->Individual[macroIndex] : ref raptureMacroModule->Shared[macroIndex - 100]);

            fixed (RaptureMacroModule.Macro* selectedMacroPointer = &selectedMacro) {
                raptureShellModule->ExecuteMacro(selectedMacroPointer);
            }
        }

        public static List<(int Index, string Name)> GetAvailableMacros() {
            var result = new List<(int Index, string Name)>();
            
            if (_uiModule == null) {
                return result;
            }

            var raptureMacroModule = _uiModule->GetRaptureMacroModule();
            
            if (raptureMacroModule == null) {
                return result;
            }

            for (int macroIndex = 0; macroIndex < 100; macroIndex++) {
                ref var macroData = ref raptureMacroModule->Individual[macroIndex];
                var macroName = macroData.Name.ToString();
                
                if (!string.IsNullOrWhiteSpace(macroName) || macroData.IconId != 0) {
                    result.Add((macroIndex, $"[Individual] {(string.IsNullOrWhiteSpace(macroName) ? $"Unnamed #{macroIndex}" : macroName)}"));
                }
            }

            for (int macroIndex = 0; macroIndex < 100; macroIndex++) {
                ref var macroData = ref raptureMacroModule->Shared[macroIndex];
                var macroName = macroData.Name.ToString();
                
                if (!string.IsNullOrWhiteSpace(macroName) || macroData.IconId != 0) {
                    result.Add((macroIndex + 100, $"[Shared] {(string.IsNullOrWhiteSpace(macroName) ? $"Unnamed #{macroIndex}" : macroName)}"));
                }
            }

            return result;
        }

        public static string GetMacroName(int macroIndex) {
            if (_uiModule == null) {
                return $"Macro #{macroIndex}";
            }
            
            var raptureMacroModule = _uiModule->GetRaptureMacroModule();
            
            if (raptureMacroModule == null) {
                return $"Macro #{macroIndex}";
            }

            ref var macroData = ref (macroIndex < 100 ? ref raptureMacroModule->Individual[macroIndex] : ref raptureMacroModule->Shared[macroIndex - 100]);

            var macroName = macroData.Name.ToString();
            
            return string.IsNullOrWhiteSpace(macroName) ? $"Macro #{macroIndex}" : macroName;
        }
    }
}
