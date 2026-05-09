using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace PluginList.UI {
    internal sealed partial class Menu {
        private void UIMacros() {
            SectionHeader("In-Game Macros");

            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "IN-GAME MACROS");
            
            ImGui.Spacing();
            
            ImGui.TextWrapped("Check the box to pin a macro to your Hover Dock.");
            
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();

            var availableMacros = ChatExecutor.GetAvailableMacros();
            
            if (availableMacros.Count == 0) {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No named or icon-assigned macros found.");
                return;
            }

            bool changed = false;
            PushScrollbarStyle();
            
            if (ImGui.BeginChild("##MacroScroll", new Vector2(0, 0), true)) {
                foreach (var macro in availableMacros) {
                    string macroTag = $"[Macro] {macro.Index}";
                    bool isMacroEnabled = _config.SavedMacros.Contains(macro.Index);
                    ImGui.PushID($"macro_{macro.Index}");
                    
                    if (ImGui.Checkbox("", ref isMacroEnabled)) {
                        if (isMacroEnabled) {
                            _config.SavedMacros.Add(macro.Index);
                            _config.EnabledPlugins.Add(macroTag);
                        } else {
                            _config.SavedMacros.Remove(macro.Index);
                            _config.EnabledPlugins.Remove(macroTag);
                        }
                        
                        changed = true;
                    }
                    
                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.TextUnformatted(macro.Name);
                }
                
                ImGui.EndChild();
            }
            
            PopScrollbarStyle();

            if (changed) {
                _saveConfig();
            }
        }
    }
}
