using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace PluginList {
    public class HoverDock : Window {
        private readonly Config _config;

        private bool _isDragging;
        private float _expandProgress;
        private const float AnimSpeed = 8f;

        private float ExpandedLength => 250f * ImGuiHelpers.GlobalScale;
        private float Thickness => 300f * ImGuiHelpers.GlobalScale;
        private float TabRadius => 25f * ImGuiHelpers.GlobalScale;
        private bool IsVertical => _config.CurrentEdge == DockEdge.Left || _config.CurrentEdge == DockEdge.Right;

        public HoverDock(Config config) : base("##PluginListDock", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings) {
            _config = config;
            IsOpen = config.IsEnabled;
            RespectCloseHotkey = false;
        }

        public override void PreDraw() {
            var mainViewport = ImGui.GetMainViewport();
            
            float eased = Ease(_expandProgress);
            float length = eased * ExpandedLength;
            float total = Math.Max(TabRadius, length + TabRadius);

            float centerX = mainViewport.Pos.X + mainViewport.Size.X * 0.5f + (!IsVertical ? _config.EdgeOffset : 0f);
            float centerY = mainViewport.Pos.Y + mainViewport.Size.Y * 0.5f + (IsVertical ? _config.EdgeOffset : 0f);

            Vector2 pos, pivot;
            
            switch (_config.CurrentEdge) {
                case DockEdge.Right:
                    pos = new(mainViewport.Pos.X + mainViewport.Size.X, centerY);
                    pivot = new(1f, 0.5f);
                    break;
                
                case DockEdge.Left:
                    pos = new(mainViewport.Pos.X, centerY);
                    pivot = new(0f, 0.5f);
                    break;
                
                case DockEdge.Top:
                    pos = new(centerX, mainViewport.Pos.Y);
                    pivot = new(0.5f, 0f);
                    break;
                
                default:
                    pos = new(centerX, mainViewport.Pos.Y + mainViewport.Size.Y);
                    pivot = new(0.5f, 1f);
                    break;
            }

            var size = IsVertical ? new Vector2(total, Thickness) : new Vector2(Thickness, total);
            
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, pivot);
            ImGui.SetNextWindowSize(size, ImGuiCond.Always);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 10f * ImGuiHelpers.GlobalScale));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.2f, 0.2f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
        }

        public override void PostDraw() {
            ImGui.PopStyleVar(4);
            ImGui.PopStyleColor(3);
        }

        public override void Draw() {
            var io = ImGui.GetIO();
            ChatExecutor.ProcessQueue(io.DeltaTime);

            var mainViewport = ImGui.GetMainViewport();
            var windowPosition = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            var drawList = ImGui.GetWindowDrawList();

            float eased = Ease(_expandProgress);
            float length = eased * ExpandedLength;

            Vector2 panelMin, panelMax, tabCenter;
            string chevron;
            ComputeGeometry(windowPosition, windowSize, length, out panelMin, out panelMax, out tabCenter, out chevron);

            bool windowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            bool tabHovered = Vector2.Distance(io.MousePos, tabCenter) <= TabRadius + 2f * ImGuiHelpers.GlobalScale;
            bool edgeHovered = IsNearEdge(io.MousePos, mainViewport, windowPosition, windowSize);

            if (windowHovered || tabHovered || edgeHovered || _isDragging) {
                _expandProgress += AnimSpeed * io.DeltaTime;
            } else {
                _expandProgress -= AnimSpeed * io.DeltaTime;
            }

            _expandProgress = Math.Clamp(_expandProgress, 0f, 1f);
            eased = Ease(_expandProgress);
            length = eased * ExpandedLength;
            
            ComputeGeometry(windowPosition, windowSize, length, out panelMin, out panelMax, out tabCenter, out chevron);

            if (length > 0.5f) {
                drawList.AddRectFilled(panelMin, panelMax, ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.09f, 0.95f)));
            }

            float tabAlpha = 1f - eased;
            
            if (tabAlpha > 0.02f) {
                drawList.AddCircleFilled(tabCenter, TabRadius, ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.09f, 0.95f * tabAlpha)), 40);
                DrawTabChevron(drawList, tabCenter, chevron, tabAlpha);
            }

            if (eased > 0.05f && length > 40f * ImGuiHelpers.GlobalScale) {
                DrawContent(windowPosition, panelMin, panelMax, eased, drawList);
            }
        }

        private void DrawContent(Vector2 windowPosition, Vector2 panelMin, Vector2 panelMax, float alpha, ImDrawListPtr drawList) {
            float contentPadding = 16f * ImGuiHelpers.GlobalScale;
            float panelWidth = IsVertical ? (panelMax.X - panelMin.X) : (panelMax.X - panelMin.X);
            float contentWidth = Math.Max(1f, panelWidth - contentPadding * 2f);

            var contentStart = new Vector2(panelMin.X - windowPosition.X + contentPadding, panelMin.Y - windowPosition.Y + contentPadding);

            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);
            ImGui.PushClipRect(panelMin, panelMax, true);
            ImGui.SetCursorPos(contentStart);
            
            float gearSize = 20f * ImGuiHelpers.GlobalScale;
            float dragZoneWidth = contentWidth - gearSize - 8f * ImGuiHelpers.GlobalScale;

            ImGui.PushID("pl_drag");
            ImGui.InvisibleButton("##drag", new Vector2(dragZoneWidth, gearSize));
            
            if (ImGui.IsItemActive()) {
                _isDragging = true;
                
                if (IsVertical) {
                    _config.EdgeOffset += ImGui.GetIO().MouseDelta.Y;
                } else {
                    _config.EdgeOffset += ImGui.GetIO().MouseDelta.X;
                }
            } else if (_isDragging) {
                PluginList.Instance.SaveConfig();
                _isDragging = false;
            }

            if (ImGui.IsItemHovered()) {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            }
            
            ImGui.PopID();

            ImGui.SetCursorPos(contentStart);
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "SHORTCUTS");

            ImGui.SameLine(contentStart.X + contentWidth - gearSize);
            ImGui.PushID("pl_gear");
            
            var gearCursor = ImGui.GetCursorPos();
            ImGui.InvisibleButton("##gear", new Vector2(gearSize, gearSize));
            
            bool gearHovered = ImGui.IsItemHovered();
            bool gearClicked = ImGui.IsItemClicked();
            
            ImGui.SetCursorPos(gearCursor);
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(gearHovered ? Vector4.One : new Vector4(0.5f, 0.5f, 0.5f, 1f), FontAwesomeIcon.Cog.ToIconString());
            ImGui.PopFont();

            if (gearClicked) {
                PluginList.Instance._mainUI.IsOpen = true;
            }

            if (gearHovered) {
                ImGui.SetTooltip("PluginList Settings");
            }
            
            ImGui.PopID();
            
            ImGui.SetCursorPosX(contentStart.X);
            float separatorY = ImGui.GetCursorScreenPos().Y;
            drawList.AddLine(new Vector2(windowPosition.X + contentStart.X, separatorY), new Vector2(windowPosition.X + contentStart.X + contentWidth, separatorY), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.12f)));
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10f * ImGuiHelpers.GlobalScale);

            DrawShortcuts(contentStart, contentWidth, windowPosition, drawList);

            ImGui.PopClipRect();
            ImGui.PopStyleVar();
        }

        private void DrawShortcuts(Vector2 contentStart, float contentWidth, Vector2 windowPosition, ImDrawListPtr drawList) {
            var installedPlugins = PluginList.Instance._pluginInterface.InstalledPlugins.ToList();

            IEnumerable<string> mainList = _config.EnabledPlugins;

            if (_config.ShortcutSortMode == SortMode.Alphabetical) {
                mainList = mainList.OrderBy(GetSortKey);
            }

            bool hasSeparator = false;
            foreach (var item in mainList) {
                if (!hasSeparator && item.StartsWith("[Macro] ")) {
                    ImGui.SetCursorPosX(contentStart.X);
                    ImGui.Spacing();
                    float separatorY = ImGui.GetCursorScreenPos().Y;
                    drawList.AddLine(new Vector2(windowPosition.X + contentStart.X, separatorY), new Vector2(windowPosition.X + contentStart.X + contentWidth, separatorY), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.08f)));
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10f * ImGuiHelpers.GlobalScale);
                    hasSeparator = true;
                }

                ImGui.SetCursorPosX(contentStart.X);
                DrawItem(item, contentWidth, installedPlugins, "_main");
            }

            var unpinned = GetUnpinnedItems();
            
            if (_config.EnabledPlugins.Count > 0 && unpinned.Count > 0) {
                ImGui.SetCursorPosX(contentStart.X);
                ImGui.Spacing();
                
                float separatorY = ImGui.GetCursorScreenPos().Y;
                drawList.AddLine(new Vector2(windowPosition.X + contentStart.X, separatorY), new Vector2(windowPosition.X + contentStart.X + contentWidth, separatorY), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.08f)));
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10f * ImGuiHelpers.GlobalScale);
            }

            IEnumerable<string> unpinnedSorted = unpinned;

            if (_config.ShortcutSortMode == SortMode.Alphabetical) {
                unpinnedSorted = unpinned.OrderBy(GetSortKey);
            }

            hasSeparator = false;
            foreach (var item in unpinnedSorted) {
                if (!hasSeparator && item.StartsWith("[Macro] ")) {
                    ImGui.SetCursorPosX(contentStart.X);
                    ImGui.Spacing();
                    float separatorY = ImGui.GetCursorScreenPos().Y;
                    drawList.AddLine(new Vector2(windowPosition.X + contentStart.X, separatorY), new Vector2(windowPosition.X + contentStart.X + contentWidth, separatorY), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.08f)));
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10f * ImGuiHelpers.GlobalScale);
                    hasSeparator = true;
                }

                ImGui.SetCursorPosX(contentStart.X);
                DrawItem(item, contentWidth, installedPlugins, "_unpinned");
            }

            ImGui.Spacing();
        }

        private void DrawItem(string item, float contentWidth, System.Collections.Generic.List<Dalamud.Plugin.IExposedPlugin> installedPlugins, string suffix) {
            Vector4 defaultColor = item.StartsWith("[Command] ") ? new Vector4(0.85f, 0.9f, 1f, 1f) : item.StartsWith("[Macro] ") ? new Vector4(0.85f, 1f, 0.85f, 1f) : Vector4.One;
            Vector4 color = _config.ItemColors.TryGetValue(item, out var itemColor) ? itemColor : defaultColor;

            if (item.StartsWith("[Command] ")) {
                var command = _config.CustomCommands.FirstOrDefault(customCommand => customCommand.Name == item[10..]);
                
                if (command == null) {
                    return;
                }
                
                ImGui.PushStyleColor(ImGuiCol.Text, color);

                if (ImGui.Selectable($"{command.Name}##{command.Command}{suffix}", false, ImGuiSelectableFlags.None, new Vector2(contentWidth, 0))) {
                    ChatExecutor.ExecuteCommand(command.Command);
                }
                
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip($"Custom Command\n\nName: {command.Name}\nAction: {command.Command.Replace("\n", "  |  ")}");
                }
            } else if (item.StartsWith("[Macro] ")) {
                if (!int.TryParse(item[8..], out int macroIndex)) {
                    return;
                }
                
                ImGui.PushStyleColor(ImGuiCol.Text, color);

                if (ImGui.Selectable($"{ChatExecutor.GetMacroName(macroIndex)}##macro_{macroIndex}{suffix}", false, ImGuiSelectableFlags.None, new Vector2(contentWidth, 0))) {
                    ChatExecutor.ExecuteCommand($"//m {macroIndex}");
                }
                
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip($"FFXIV Macro\n\nName: {ChatExecutor.GetMacroName(macroIndex)}\nIndex: {macroIndex}\nLeft-Click: Run macro");
                }
            } else {
                var plugin = installedPlugins.FirstOrDefault(installedPlugin => installedPlugin.InternalName == item);

                if (plugin == null || !plugin.IsLoaded) {
                    return;
                }
                
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                
                if (ImGui.Selectable($"{plugin.Name}##{plugin.InternalName}{suffix}", false, ImGuiSelectableFlags.None, new Vector2(contentWidth, 0))) {
                    if (plugin.HasMainUi) {
                        plugin.OpenMainUi();
                    } else if (plugin.HasConfigUi) {
                        plugin.OpenConfigUi();
                    }
                }
                
                ImGui.PopStyleColor();

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && plugin.HasConfigUi) {
                    plugin.OpenConfigUi();
                }
                
                if (ImGui.IsItemHovered()) {
                    var tooltipText = (plugin.HasMainUi ? "Left-Click: Open menu\n" : "") + (plugin.HasConfigUi ? "Right-Click: Open settings" : "");
                    
                    if (!string.IsNullOrEmpty(tooltipText)) {
                        ImGui.SetTooltip($"Plugin: {plugin.Name}\n\n{tooltipText.TrimEnd('\n')}");
                    }
                }
            }
        }

        private List<string> GetUnpinnedItems() {
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

        private string GetSortKey(string item) {
            if (item.StartsWith("[Command] ")) {
                return item[10..];
            }

            if (item.StartsWith("[Macro] ") && int.TryParse(item[8..], out int macroIndex)) {
                return ChatExecutor.GetMacroName(macroIndex);
            }
            
            return PluginList.Instance._pluginInterface.InstalledPlugins.FirstOrDefault(p => p.InternalName == item)?.Name ?? item;
        }

        private void ComputeGeometry(Vector2 winPos, Vector2 winSize, float length, out Vector2 panelMin, out Vector2 panelMax, out Vector2 tabCenter, out string chevron) {
            float windowX = winPos.X, windowY = winPos.Y, windowWidth = winSize.X, windowHeight = winSize.Y;
            
            switch (_config.CurrentEdge) {
                case DockEdge.Right:
                    panelMin = new(windowX + windowWidth - length, windowY);
                    panelMax = new(windowX + windowWidth, windowY + windowHeight);
                    tabCenter = new(panelMin.X, windowY + windowHeight * 0.5f);
                    
                    chevron = FontAwesomeIcon.ChevronLeft.ToIconString();
                    break;
                
                case DockEdge.Left:
                    panelMin = new(windowX, windowY);
                    panelMax = new(windowX + length, windowY + windowHeight);
                    tabCenter = new(panelMax.X, windowY + windowHeight * 0.5f);
                    
                    chevron = FontAwesomeIcon.ChevronRight.ToIconString();
                    break;
                
                case DockEdge.Top:
                    panelMin = new(windowX, windowY);
                    panelMax = new(windowX + windowWidth, windowY + length);
                    tabCenter = new(windowX + windowWidth * 0.5f, panelMax.Y);
                    
                    chevron = FontAwesomeIcon.ChevronDown.ToIconString();
                    break;
                
                default:
                    panelMin = new(windowX, windowY + windowHeight - length);
                    panelMax = new(windowX + windowWidth, windowY + windowHeight);
                    tabCenter = new(windowX + windowWidth * 0.5f, panelMin.Y);
                    
                    chevron = FontAwesomeIcon.ChevronUp.ToIconString();
                    break;
            }
        }

        private bool IsNearEdge(Vector2 mousePosition, ImGuiViewportPtr viewport, Vector2 windowPosition, Vector2 windowSize) {
            float edgeTolerance = TabRadius + 8f * ImGuiHelpers.GlobalScale;
            
            switch (_config.CurrentEdge) {
                case DockEdge.Right:
                    return mousePosition.X >= viewport.Pos.X + viewport.Size.X - edgeTolerance && mousePosition.Y >= windowPosition.Y && mousePosition.Y <= windowPosition.Y + windowSize.Y;
        
                case DockEdge.Left:
                    return mousePosition.X <= viewport.Pos.X + edgeTolerance && mousePosition.Y >= windowPosition.Y && mousePosition.Y <= windowPosition.Y + windowSize.Y;
        
                case DockEdge.Top:
                    return mousePosition.Y <= viewport.Pos.Y + edgeTolerance && mousePosition.X >= windowPosition.X && mousePosition.X <= windowPosition.X + windowSize.X;
        
                default:
                    return mousePosition.Y >= viewport.Pos.Y + viewport.Size.Y - edgeTolerance && mousePosition.X >= windowPosition.X && mousePosition.X <= windowPosition.X + windowSize.X;
            }
        }

        private void DrawTabChevron(ImDrawListPtr drawList, Vector2 tabCenter, string chevronIcon, float alpha) {
            ImGui.PushFont(UiBuilder.IconFont);
            var iconSize = ImGui.CalcTextSize(chevronIcon);
            ImGui.PopFont();

            float offset = TabRadius * 0.45f;
            var iconPosition = tabCenter;
            
            switch (_config.CurrentEdge) {
                case DockEdge.Right: 
                    iconPosition.X -= offset;
                    break;
                
                case DockEdge.Left:
                    iconPosition.X += offset;
                    break;
                
                case DockEdge.Top:
                    iconPosition.Y += offset;
                    break;
                
                default:
                    iconPosition.Y -= offset;
                    break;
            }
            
            iconPosition -= iconSize * 0.5f;
            drawList.AddText(UiBuilder.IconFont, ImGui.GetFontSize(), iconPosition, ImGui.GetColorU32(new Vector4(0.75f, 0.75f, 0.75f, alpha)), chevronIcon);
        }

        private static float Ease(float t) => 1f - (1f - t) * (1f - t);
    }
}
