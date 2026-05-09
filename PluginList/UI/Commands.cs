using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace PluginList.UI {
    internal sealed partial class Menu {
        private string _newCmdName = string.Empty;
        private string _newCmdAction = string.Empty;
        private int _editingCmdIndex = -1;

        private void UICommands() {
            SectionHeader("Custom Commands");

            bool isEditing = _editingCmdIndex != -1;
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), isEditing ? "EDIT COMMAND" : "ADD NEW COMMAND");
            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##cmd_name", "Button Name (e.g. Enter GPose)", ref _newCmdName, 50);

            ImGui.Spacing();
            ImGui.Spacing();
            
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Command Text:");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextMultiline("##cmd_action", ref _newCmdAction, 1000, new System.Numerics.Vector2(0, 75f * ImGuiHelpers.GlobalScale));

            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Multiple lines are fully supported!\n\n" + "Use '//m 0' for Individual Macro #0\n" + "Use '//m 100' for Shared Macro #0");
            }

            ImGui.Spacing();
            ImGui.Spacing();
            
            float actionButtonWidth = isEditing ? (ImGui.GetContentRegionAvail().X / 2f) - 4f : ImGui.GetContentRegionAvail().X;
            
            if (ImGui.Button(isEditing ? "Save Changes" : "Add Command", new System.Numerics.Vector2(actionButtonWidth, 0))) {
                if (!string.IsNullOrWhiteSpace(_newCmdName) && !string.IsNullOrWhiteSpace(_newCmdAction)) {
                    if (isEditing) {
                        var editedCommand = _config.CustomCommands[_editingCmdIndex];
                        
                        string oldTag = "[Command] " + editedCommand.Name;
                        string newTag = "[Command] " + _newCmdName;
                        
                        editedCommand.Name = _newCmdName;
                        editedCommand.Command = _newCmdAction;
                        int pinnedCommandIndex = _config.EnabledPlugins.IndexOf(oldTag);
                        
                        if (pinnedCommandIndex != -1) {
                            _config.EnabledPlugins[pinnedCommandIndex] = newTag;
                        }
                        
                        _editingCmdIndex = -1;
                    } else {
                        _config.CustomCommands.Add(new CustomCommand { Name = _newCmdName, Command = _newCmdAction });
                    }
                    
                    _saveConfig();
                    _newCmdName = string.Empty;
                    _newCmdAction = string.Empty;
                }
            }

            if (isEditing) {
                ImGui.SameLine();
                
                if (ImGui.Button("Cancel", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 0))) {
                    _editingCmdIndex = -1;
                    _newCmdName = string.Empty;
                    _newCmdAction = string.Empty;
                }
            }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Spacing();
            
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "YOUR COMMANDS");
            ImGui.Spacing();
            ImGui.Spacing();

            bool changed = false;
            
            for (int commandIndex = _config.CustomCommands.Count - 1; commandIndex >= 0; commandIndex--) {
                var command = _config.CustomCommands[commandIndex];
                bool deleteClicked = DrawActionIcon($"delete_{commandIndex}", FontAwesomeIcon.TrashAlt, new Vector4(0.85f, 0.35f, 0.35f, 1f), "Delete command");

                if (deleteClicked) {
                    _config.EnabledPlugins.Remove("[Command] " + command.Name);
                    _config.CustomCommands.RemoveAt(commandIndex);

                    if (_editingCmdIndex == commandIndex) {
                        _editingCmdIndex = -1;
                        _newCmdName = string.Empty;
                        _newCmdAction = string.Empty;
                    } else if (_editingCmdIndex > commandIndex) {
                        _editingCmdIndex--;
                    }
                    
                    changed = true;
                }
                
                ImGui.SameLine();
                bool editClicked = DrawActionIcon($"edit_{commandIndex}", FontAwesomeIcon.Edit, new Vector4(0.55f, 0.80f, 1.00f, 1f), "Edit command");
                
                if (editClicked) {
                    _editingCmdIndex = commandIndex;
                    _newCmdName = command.Name;
                    _newCmdAction = command.Command;
                }
                
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(command.Name);
                ImGui.SameLine();
                
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), $"({command.Command.Replace("\n", " ")})");
                ImGui.Spacing();
            }

            if (changed) {
                _saveConfig();
            }
        }

        private bool DrawActionIcon(string id, FontAwesomeIcon icon, Vector4 iconColor, string tooltip) {
            ImGui.PushID(id);
            var iconSize = new Vector2(20f * ImGuiHelpers.GlobalScale, 20f * ImGuiHelpers.GlobalScale);
            var startPos = ImGui.GetCursorPos();
            ImGui.InvisibleButton("##action", iconSize);

            bool isHovered = ImGui.IsItemHovered();
            bool isClicked = ImGui.IsItemClicked();

            ImGui.SetCursorPos(startPos);
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(isHovered ? Vector4.One : iconColor, icon.ToIconString());
            ImGui.PopFont();

            if (isHovered) {
                ImGui.SetTooltip(tooltip);
            }

            ImGui.PopID();
            return isClicked;
        }
    }
}
