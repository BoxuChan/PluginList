using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;

namespace PluginList {
    public enum SortMode { Alphabetical, Manual }
    public enum DockEdge { Top, Bottom, Left, Right }

    public class CustomCommand {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
    }

    [Serializable]
    public class Config : IPluginConfiguration {
        public int Version { get; set; } = 1;

        public bool IsEnabled { get; set; } = true;
        public DockEdge CurrentEdge { get; set; } = DockEdge.Right;
        public float EdgeOffset { get; set; } = 0f;
        public SortMode ShortcutSortMode { get; set; } = SortMode.Alphabetical;

        public List<string> EnabledPlugins { get; set; } = new();
        public List<CustomCommand> CustomCommands { get; set; } = new();
        public List<int> SavedMacros { get; set; } = new();
        public Dictionary<string, Vector4> ItemColors { get; set; } = new();
    }
}
