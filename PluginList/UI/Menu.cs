using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace PluginList.UI {
    internal enum Category {
        Plugins,
        Commands,
        Macros,
        Organization,
        About
    }

    internal sealed partial class Menu : Window, IDisposable {
        private readonly Config _config;
        private readonly Action _saveConfig;
        internal readonly IDalamudPluginInterface _pluginInterface;
        private readonly HoverDock _hoverDock;

        private Category _category = Category.Plugins;

        private bool _theme;
        private const int ThemeColor = 10;

        internal Menu(Config config, Action saveConfig, IDalamudPluginInterface pluginInterface, HoverDock hoverDock) : base("PluginList  |  v1.0.0.0###PluginListMain", ImGuiWindowFlags.NoScrollbar, forceMainWindow: false) {
            _config = config;
            _saveConfig = saveConfig;
            _pluginInterface = pluginInterface;
            _hoverDock = hoverDock;

            SizeConstraints = new WindowSizeConstraints {
                MinimumSize = new Vector2(550, 450),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            AllowPinning = true;

            TitleBarButtons.Add(new TitleBarButton {
                Icon = FontAwesomeIcon.Heart,
                IconOffset = new Vector2(0, 1),
                ShowTooltip = () => {
                    using var tooltip = Dalamud.Interface.Utility.Raii.ImRaii.Tooltip();
                    ImGui.TextUnformatted("Support PluginList on Ko-fi");
                },
                Click = _ => OpenUrl("https://ko-fi.com/boxu_chan")
            });
        }

        public void Dispose() { }

        public override void PreDraw() {
            if (!_theme) {
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.06f, 0.06f, 0.10f, 0.97f));
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.04f, 0.04f, 0.08f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.30f, 0.30f, 0.55f, 0.60f));
                ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.20f, 0.20f, 0.45f, 0.55f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.28f, 0.28f, 0.65f, 0.80f));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.22f, 0.22f, 0.60f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.14f, 0.14f, 0.28f, 0.54f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered,new Vector4(0.22f, 0.22f, 0.50f, 0.40f));
                ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0.55f, 0.55f, 1.00f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.10f, 0.10f, 0.30f, 1.00f));
                
                _theme = true;
            }
        }

        public override void PostDraw() {
            if (_theme) {
                ImGui.PopStyleColor(ThemeColor);
                _theme = false;
            }
        }

        public override void Draw() {
            var windowStyle = ImGui.GetStyle();
            var footerHeight = ImGui.GetTextLineHeight() + windowStyle.ItemSpacing.Y * 2f;
            var contentHeight = ImGui.GetContentRegionAvail().Y - footerHeight;

            if (!ImGui.BeginTable("##Layout", 2, ImGuiTableFlags.None)) return;

            try {
                ImGui.TableSetupColumn("##Menu", ImGuiTableColumnFlags.WidthFixed, 200f);
                ImGui.TableSetupColumn("##Content", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextColumn();

                var generalSectionStartY = ImGui.GetCursorPosY();
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.10f, 0.20f, 0.60f));
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
                float generalBoxHeight = ImGui.GetTextLineHeight() * 2f + ImGui.GetStyle().ItemSpacing.Y * 7f + ImGui.GetStyle().WindowPadding.Y * 2f;
                
                if (ImGui.BeginChild("##GeneralBox", new Vector2(0, generalBoxHeight), true, ImGuiWindowFlags.NoScrollbar)) {
                    DrawGeneralSettings();
                }
                
                ImGui.EndChild();
                ImGui.PopStyleVar();
                ImGui.PopStyleColor();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                
                var leftPanelUsedHeight = ImGui.GetCursorPosY() - generalSectionStartY;
                PushScrollbarStyle();
                
                if (ImGui.BeginChild("##Categories", new Vector2(0, contentHeight - leftPanelUsedHeight), false, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove)) {
                    var defaultIconColor = new Vector4(0.70f, 0.70f, 0.70f, 1f);
                    
                    NavItem("Plugins", Category.Plugins, FontAwesomeIcon.Plug, defaultIconColor);
                    NavItem("Commands", Category.Commands, FontAwesomeIcon.Terminal, defaultIconColor);
                    NavItem("Macros", Category.Macros, FontAwesomeIcon.Scroll, defaultIconColor);
                    
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                    
                    NavItem("Organization", Category.Organization, FontAwesomeIcon.SortAmountDown, defaultIconColor);
                    
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                    
                    NavItem("About", Category.About, FontAwesomeIcon.InfoCircle, defaultIconColor);
                }
                
                ImGui.EndChild();
                PopScrollbarStyle();
                ImGui.TableNextColumn();
                
                var contentFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize;
                contentFlags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
                
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));
                
                if (ImGui.BeginChild("##Content", new Vector2(0, contentHeight), false, contentFlags)) {
                    const float contentInset = 10f;
                    ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(contentInset, 2f));
                    float contentWidth = Math.Max(1f, ImGui.GetContentRegionAvail().X - contentInset);
                    
                    if (ImGui.BeginChild("##ContentInset", new Vector2(contentWidth, ImGui.GetContentRegionAvail().Y - 2f), false, contentFlags)) {
                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.BeginGroup();
                        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X);

                        switch (_category) {
                            case Category.Plugins:
                                UIPlugins();
                                break;
                            
                            case Category.Commands:
                                UICommands();
                                break;
                            
                            case Category.Macros:
                                UIMacros();
                                break;
                            
                            case Category.Organization:
                                UIOrganization();
                                break;
                            
                            case Category.About:
                                PushScrollbarStyle();
                                if (ImGui.BeginChild("##AboutScroll", new Vector2(0, ImGui.GetContentRegionAvail().Y), false)) {
                                    UIAbout();
                                }

                                ImGui.EndChild();
                                PopScrollbarStyle();
                                break;
                        }

                        ImGui.Spacing();
                        ImGui.Spacing();
                        
                        ImGui.PopTextWrapPos();
                        ImGui.EndGroup();
                        ImGui.PopItemWidth();
                    }
                    ImGui.EndChild();
                }
                
                ImGui.EndChild();
                ImGui.PopStyleVar();
            } catch (Exception exception) {
                ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), $"UI Error: {exception.Message}");
            }

            ImGui.EndTable();

            var footerText = "© Boxu - 2026 | Dalamud 15.0.0 (Patch 7.5)";
            var footerAvailableWidth = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + footerAvailableWidth - ImGui.CalcTextSize(footerText).X - 4f);
            
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.4f, 0.6f, 0.7f));
            ImGui.TextUnformatted(footerText);
            ImGui.PopStyleColor();
        }

        private void DrawGeneralSettings() {
            bool isHoverDockEnabled = _config.IsEnabled;
            
            if (ImGui.Checkbox("Enable Hover Dock", ref isHoverDockEnabled)) {
                _config.IsEnabled = isHoverDockEnabled;
                _hoverDock.IsOpen = isHoverDockEnabled;
                _saveConfig();
            }

            ImGui.Spacing();
            ImGui.Spacing();

            int selectedDockEdge = (int)_config.CurrentEdge;
            var dockEdgeNames = Enum.GetNames(typeof(DockEdge));
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            
            if (ImGui.Combo("##edge", ref selectedDockEdge, dockEdgeNames, dockEdgeNames.Length)) {
                _config.CurrentEdge = (DockEdge)selectedDockEdge;
                _config.EdgeOffset = 0f;
                _saveConfig();
            }

            ImGui.Spacing();
        }

        private void NavItem(string label, Category target, FontAwesomeIcon faIcon, Vector4 iconColor) {
            var height = ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y * 3f;
            var cursorPosition = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();
            var framePadding = ImGui.GetStyle().FramePadding.X;
            var iconText = faIcon.ToIconString();

            float iconWidth;
            using (_pluginInterface.UiBuilder.IconFontHandle.Push()) {
                iconWidth = ImGui.CalcTextSize(iconText).X;
            }

            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0f, 0.5f));

            if (ImGui.Selectable($"##{target}", _category == target, ImGuiSelectableFlags.None, new Vector2(0, height))) {
                _category = target;
            }
            
            ImGui.PopStyleVar();

            float iconX = cursorPosition.X + framePadding + 8f;
            float iconY = cursorPosition.Y + (height - ImGui.GetTextLineHeight()) * 0.5f;

            drawList.AddText(_pluginInterface.UiBuilder.IconFontHandle.Lock().ImFont, ImGui.GetFontSize(), new Vector2(iconX, iconY), ImGui.ColorConvertFloat4ToU32(iconColor), iconText);
            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize(), new Vector2(iconX + iconWidth + 8f, iconY), ImGui.GetColorU32(ImGuiCol.Text), label);
        }

        private static void PushScrollbarStyle() {
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 14f);
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 6f);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.06f, 0.06f, 0.12f, 0.90f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.20f, 0.20f, 0.50f, 0.90f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0.24f, 0.24f, 0.55f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0.24f, 0.24f, 0.55f, 0.95f));
        }

        private static void PopScrollbarStyle() {
            ImGui.PopStyleColor(4);
            ImGui.PopStyleVar(2);
        }

        internal static void SectionHeader(string text) {
            var availableWidth = ImGui.GetContentRegionAvail().X;
            
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (availableWidth - ImGui.CalcTextSize(text).X) * 0.5f);
            ImGui.TextColored(new Vector4(0.75f, 0.75f, 1.00f, 1f), text);
            ImGui.Separator(); ImGui.Spacing(); ImGui.Spacing();
        }

        internal static void OpenUrl(string url) {
            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) {
                    UseShellExecute = true
                });
            } catch { }
        }
    }
}
