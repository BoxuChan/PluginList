using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;

namespace PluginList.UI {
    internal sealed partial class Menu {
        private string[]? _readmeContent;
        private bool _readmeLoaded;

        private void UIAbout() {
            if (!_readmeLoaded) {
                _readmeLoaded = true;
                
                try {
                    var readmePath = Path.Combine(_pluginInterface.AssemblyLocation.DirectoryName!, "README.md");
                    if (File.Exists(readmePath)) _readmeContent = File.ReadAllLines(readmePath);
                } catch { }
            }

            if (_readmeContent == null) { 
                ImGui.TextWrapped("The contents of README.md couldn't be read.");
                return;
            }

            ImGui.Spacing();
            int lineIndex = 0;
            
            while (lineIndex < _readmeContent.Length) {
                var line = _readmeContent[lineIndex];

                if (line.StartsWith("```")) {
                    var code = new List<string>();
                    lineIndex++;
                    
                    while (lineIndex < _readmeContent.Length && !_readmeContent[lineIndex].StartsWith("```")) {
                        code.Add(_readmeContent[lineIndex]);
                        lineIndex++;
                    }
                    
                    lineIndex++;
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.60f, 0.85f, 1.00f, 1f));

                    foreach (var codeLine in code) {
                        ImGui.TextWrapped(codeLine);
                    }
                    
                    ImGui.PopStyleColor();
                    continue;
                }

                if (line.StartsWith("[![")) {
                    lineIndex++;
                    continue;
                }

                if (line.StartsWith("# ")) {
                    ImGui.Spacing();
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.80f, 0.80f, 1.00f, 1f));
                    
                    var availableWidth = ImGui.GetContentRegionAvail().X;
                    var headerText = line[2..];
                    
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (availableWidth - ImGui.CalcTextSize(headerText).X) * 0.5f);
                    ImGui.TextUnformatted(headerText);
                    ImGui.PopStyleColor();
                    
                    ImGui.Separator();
                    ImGui.Spacing();
                    
                    lineIndex++;
                    continue;
                }

                if (line.StartsWith("## ")) {
                    ImGui.Spacing();
                    
                    SectionHeader(line[3..].Trim('_').Trim('"'));
                    lineIndex++;
                    continue;
                }

                if (line.StartsWith("### ")) {
                    ImGui.Spacing();
                    
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.70f, 0.70f, 0.95f, 1f));
                    ImGui.TextUnformatted(StripInlineMarkdown(line[4..]));
                    ImGui.PopStyleColor();
                    
                    ImGui.Spacing();
                    lineIndex++;
                    continue;
                }

                if (line.StartsWith("|")) {
                    var tableContent = new List<string>();
                    
                    while (lineIndex < _readmeContent.Length && _readmeContent[lineIndex].StartsWith("|")) {
                        tableContent.Add(_readmeContent[lineIndex]);
                        lineIndex++;
                    }
                    
                    RenderMarkdownTable(tableContent);
                    continue;
                }

                if (line.StartsWith("- ") || line.StartsWith("* ")) {
                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGui.TextWrapped(StripInlineMarkdown(line[2..]));
                    
                    lineIndex++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line)) {
                    ImGui.Spacing();
                    
                    lineIndex++;
                    continue;
                }

                ImGui.TextWrapped(StripInlineMarkdown(line));
                lineIndex++;
            }
        }

        private static void RenderMarkdownTable(List<string> rows) {
            if (rows.Count < 2) {
                return;
            }
            
            var columnCount = ParseTableRow(rows[0]).Count;
            
            if (columnCount == 0) {
                return;
            }

            ImGui.Spacing();
            if (!ImGui.BeginTable("##table", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) {
                return;
            }

            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++) {
                ImGui.TableSetupColumn(ParseTableRow(rows[0])[columnIndex]);
            }
            
            ImGui.TableHeadersRow();

            for (int rowIndex = 2; rowIndex < rows.Count; rowIndex++) {
                var cells = ParseTableRow(rows[rowIndex]);
                ImGui.TableNextRow();
                
                for (int columnIndex = 0; columnIndex < columnCount && columnIndex < cells.Count; columnIndex++) {
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(StripInlineMarkdown(cells[columnIndex]));
                }
            }

            ImGui.EndTable();
            ImGui.Spacing();
        }

        private static List<string> ParseTableRow(string row) {
            var cells = new List<string>();

            foreach (var cell in row.Split('|', System.StringSplitOptions.RemoveEmptyEntries)) {
                cells.Add(cell.Trim());
            }
            
            return cells;
        }

        private static string StripInlineMarkdown(string text) {
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
            text = Regex.Replace(text, @"__(.+?)__", "$1");
            text = Regex.Replace(text, @"_(.+?)_", "$1");
            text = Regex.Replace(text, @"`(.+?)`", "$1");
            text = Regex.Replace(text, @"\[(.+?)\]\(.+?\)", "$1");
            
            return text;
        }
    }
}
