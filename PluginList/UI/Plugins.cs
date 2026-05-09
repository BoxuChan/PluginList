using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace PluginList.UI {
    internal sealed partial class Menu {
        private string _pluginSearch = string.Empty;

        private void UIPlugins() {
            SectionHeader("Plugins");

            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "ENABLED PLUGINS");
            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##plugin_search", "Search plugins...", ref _pluginSearch, 100);
            ImGui.Spacing();
            ImGui.Spacing();

            var filteredPlugins = _pluginInterface.InstalledPlugins.Where(plugin => plugin.IsLoaded).Where(plugin => string.IsNullOrWhiteSpace(_pluginSearch) || plugin.Name.Contains(_pluginSearch, StringComparison.OrdinalIgnoreCase)).OrderBy(plugin => plugin.Name).ToList();

            bool changed = false;
            PushScrollbarStyle();
            
            if (ImGui.BeginChild("##PluginScroll", new Vector2(0, 0), true)) {
                foreach (var plugin in filteredPlugins) {
                    bool isPluginPinned = _config.EnabledPlugins.Contains(plugin.InternalName);
                    
                    if (ImGui.Checkbox($"##chk_{plugin.InternalName}", ref isPluginPinned)) {
                        if (isPluginPinned) {
                            _config.EnabledPlugins.Add(plugin.InternalName);
                        } else {
                            _config.EnabledPlugins.Remove(plugin.InternalName);
                        }
                        
                        changed = true;
                    }
                    
                    ImGui.SameLine();
                    ImGui.TextUnformatted(plugin.Name);
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
