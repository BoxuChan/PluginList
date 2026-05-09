using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace PluginList.UI {
    internal sealed partial class Menu {
        private int _dragIndex = -1;
        private string _dragType = string.Empty;

        private void UIOrganization() {
            SectionHeader("Organization");

            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "SORTING MODE");
            ImGui.Spacing();
            ImGui.Spacing();

            int selectedSortMode = (int)_config.ShortcutSortMode;

            if (ImGui.RadioButton("Alphabetical", ref selectedSortMode, 0)) {
                _config.ShortcutSortMode = (SortMode)selectedSortMode;
                _saveConfig();
            }

            ImGui.Spacing();
            ImGui.Spacing();
            
            if (ImGui.RadioButton("Manual (Drag & Drop)", ref selectedSortMode, 1))
            {
                _config.ShortcutSortMode = (SortMode)selectedSortMode;
                _saveConfig();
            }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (_config.ShortcutSortMode != SortMode.Manual) {
                return;
            }

            PushScrollbarStyle();
            if (!ImGui.BeginChild("##OrganizationManualScroll", new Vector2(0, 0), false)) {
                ImGui.EndChild();
                PopScrollbarStyle();
                return;
            }

            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "MANUAL SORTING");
            ImGui.TextWrapped("Drag items to reorder. Drag from Unpinned to Main to pin. Drag from Main to Unpinned to unpin.");
            
            ImGui.Spacing();
            ImGui.Spacing();

            bool changed = false;
            var unpinnedItems = GetUnpinnedList();

            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Main Shortcuts");
            DrawDropZone("main_top", 0, unpinnedItems, ref changed, false);

            for (int i = 0; i < _config.EnabledPlugins.Count; i++) {
                var item = _config.EnabledPlugins[i];
                var (displayName, color) = GetItemDisplay(item);

                DrawColorPicker(item, ref color, ref changed);
                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.Selectable($"  ->   {displayName}##main_{i}");
                ImGui.PopStyleColor();

                if (ImGui.BeginDragDropSource()) {
                    _dragIndex = i; _dragType = "DND_MAIN";
                    
                    ImGui.SetDragDropPayload("DND_MAIN", Array.Empty<byte>(), ImGuiCond.None);
                    ImGui.TextUnformatted($"Moving: {displayName}");
                    ImGui.EndDragDropSource();
                }

                if (ImGui.BeginDragDropTarget()) {
                    AcceptPayload();
                    
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
                        if (_dragType == "DND_MAIN" && _dragIndex != -1 && _dragIndex != i) {
                            var moved = _config.EnabledPlugins[_dragIndex];
                            _config.EnabledPlugins.RemoveAt(_dragIndex);
                            _config.EnabledPlugins.Insert(i, moved);
                            
                            changed = true;
                        } else if (_dragType == "DND_UNPINNED" && _dragIndex != -1) {
                            _config.EnabledPlugins.Insert(i, unpinnedItems[_dragIndex]);
                            
                            changed = true;
                        }
                        
                        _dragIndex = -1;
                    }
                    
                    ImGui.EndDragDropTarget();
                }

                ImGui.Spacing();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Unpinned Shortcuts");
            DrawDropZone("unpinned_top", 0, unpinnedItems, ref changed, true);

            for (int i = 0; i < unpinnedItems.Count; i++) {
                var item = unpinnedItems[i];
                var (displayName, color) = GetItemDisplay(item);

                DrawColorPicker(item, ref color, ref changed);
                ImGui.SameLine();

                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.Selectable($"  ->   {displayName}##unpinned_{i}");
                ImGui.PopStyleColor();

                if (ImGui.BeginDragDropSource()) {
                    _dragIndex = i; _dragType = "DND_UNPINNED";
                    
                    ImGui.SetDragDropPayload("DND_UNPINNED", Array.Empty<byte>(), ImGuiCond.None);
                    ImGui.TextUnformatted($"Pinning: {displayName}");
                    ImGui.EndDragDropSource();
                }

                if (ImGui.BeginDragDropTarget()) {
                    AcceptPayload();
                    
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
                        if (_dragType == "DND_UNPINNED" && _dragIndex != -1 && _dragIndex != i) {
                            ReorderUnpinned(unpinnedItems, _dragIndex, i);
                            
                            changed = true;
                        } else if (_dragType == "DND_MAIN" && _dragIndex != -1) {
                            var moved = _config.EnabledPlugins[_dragIndex];
                            
                            if (moved.StartsWith("[Command] ") || moved.StartsWith("[Macro] ")) {
                                _config.EnabledPlugins.RemoveAt(_dragIndex);
                                
                                changed = true;
                            }
                        }
                        
                        _dragIndex = -1;
                    }
                    
                    ImGui.EndDragDropTarget();
                }

                ImGui.Spacing();
            }

            if (changed) {
                _saveConfig();
            }

            ImGui.EndChild();
            PopScrollbarStyle();
        }

        private void DrawDropZone(string id, int targetIndex, List<string> unpinnedItems, ref bool changed, bool isUnpinnedDropZone) {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.InvisibleButton(id, new Vector2(ImGui.GetContentRegionAvail().X, 4f * ImGuiHelpers.GlobalScale));
            ImGui.PopStyleVar();

            if (!ImGui.BeginDragDropTarget()) {
                return;
            }
            
            AcceptPayload();
            
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
                if (!isUnpinnedDropZone) {
                    if (_dragType == "DND_MAIN" && _dragIndex != -1) {
                        var moved = _config.EnabledPlugins[_dragIndex];
                        _config.EnabledPlugins.RemoveAt(_dragIndex);
                        _config.EnabledPlugins.Insert(Math.Min(targetIndex, _config.EnabledPlugins.Count), moved);
                        
                        changed = true;
                    } else if (_dragType == "DND_UNPINNED" && _dragIndex != -1) {
                        _config.EnabledPlugins.Insert(Math.Min(targetIndex, _config.EnabledPlugins.Count), unpinnedItems[_dragIndex]);
                        
                        changed = true;
                    }
                } else if (_dragType == "DND_MAIN" && _dragIndex != -1) {
                    var moved = _config.EnabledPlugins[_dragIndex];
                    
                    if (moved.StartsWith("[Command] ") || moved.StartsWith("[Macro] ")) {
                        _config.EnabledPlugins.RemoveAt(_dragIndex);
                        
                        changed = true;
                    }
                }
                
                _dragIndex = -1;
            }
            
            ImGui.EndDragDropTarget();
        }

        private void DrawColorPicker(string item, ref Vector4 color, ref bool changed) {
            ImGui.PushID($"color_{item}");
            
            if (ImGui.ColorEdit4("##picker", ref color, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreview)) {
                _config.ItemColors[item] = color;
                changed = true;
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                _config.ItemColors.Remove(item);
                changed = true;
            }
            
            ImGui.PopID();
        }

        private (string displayName, Vector4 color) GetItemDisplay(string item) {
            bool isCommandItem = item.StartsWith("[Command] ");
            bool isMacroItem = item.StartsWith("[Macro] ");
            string displayName = item;

            if (isCommandItem) {
                var customCommand = _config.CustomCommands.FirstOrDefault(command => command.Name == item[10..]);
                displayName = customCommand != null ? $"{customCommand.Name} ({customCommand.Command.Replace("\n", " ")})" : item;
            } else if (isMacroItem && int.TryParse(item[8..], out int macroIndex)) {
                displayName = $"[Macro] {ChatExecutor.GetMacroName(macroIndex)}";
            } else {
                var plugin = _pluginInterface.InstalledPlugins.FirstOrDefault(p => p.InternalName == item);
                
                if (plugin != null) {
                    displayName = plugin.Name;
                }
            }

            Vector4 defaultColor = isCommandItem ? new Vector4(0.85f, 0.9f, 1f, 1f) : isMacroItem ? new Vector4(0.85f, 1f, 0.85f, 1f) : Vector4.One;
            Vector4 color = _config.ItemColors.TryGetValue(item, out var savedColor) ? savedColor : defaultColor;
            return (displayName, color);
        }

        private List<string> GetUnpinnedList() {
            var list = new List<string>();

            foreach (var customCommand in _config.CustomCommands) {
                if (!_config.EnabledPlugins.Contains("[Command] " + customCommand.Name)) {
                    list.Add("[Command] " + customCommand.Name);
                }
            }

            foreach (var macroIndex in _config.SavedMacros) {
                if (!_config.EnabledPlugins.Contains("[Macro] " + macroIndex)) {
                    list.Add("[Macro] " + macroIndex);
                }
            }

            return list;
        }

        private void ReorderUnpinned(List<string> unpinnedItems, int sourceIndex, int destinationIndex) {
            var sourceItem = unpinnedItems[sourceIndex];
            var destinationItem = unpinnedItems[destinationIndex];
            
            if (sourceItem.StartsWith("[Command] ") && destinationItem.StartsWith("[Command] ")) {
                var sourceCommand = _config.CustomCommands.First(command => command.Name == sourceItem[10..]);
                var destinationCommand = _config.CustomCommands.First(command => command.Name == destinationItem[10..]);
                int sourceCommandIndex = _config.CustomCommands.IndexOf(sourceCommand), destinationCommandIndex = _config.CustomCommands.IndexOf(destinationCommand);
                
                (_config.CustomCommands[sourceCommandIndex], _config.CustomCommands[destinationCommandIndex]) = (_config.CustomCommands[destinationCommandIndex], _config.CustomCommands[sourceCommandIndex]);
            } else if (sourceItem.StartsWith("[Macro] ") && destinationItem.StartsWith("[Macro] ")) {
                int sourceMacroIndex = int.Parse(sourceItem[8..]), destinationMacroIndex = int.Parse(destinationItem[8..]);
                int sourceMacroListIndex = _config.SavedMacros.IndexOf(sourceMacroIndex), destinationMacroListIndex = _config.SavedMacros.IndexOf(destinationMacroIndex);
                
                (_config.SavedMacros[sourceMacroListIndex], _config.SavedMacros[destinationMacroListIndex]) = (_config.SavedMacros[destinationMacroListIndex], _config.SavedMacros[sourceMacroListIndex]);
            }
        }

        private static void AcceptPayload() {
            ImGui.AcceptDragDropPayload("DND_MAIN");
            ImGui.AcceptDragDropPayload("DND_UNPINNED");
        }
    }
}
