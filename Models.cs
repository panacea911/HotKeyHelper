using System.Collections.Generic;

namespace HotKeyHelper
{
    public class HotkeyAction
    {
        public string Name { get; set; } = "Новое действие";
        
        // Example: "Control+Shift+A"
        public string KeyCombination { get; set; } = "";
        
        // "RunProgram", "CloseWindow", "OpenFolder"
        public string ActionType { get; set; } = "RunProgram"; 
        
        public string TargetPath { get; set; } = "";
        
        public string Arguments { get; set; } = "";
    }

    public class AppConfig
    {
        public bool RunOnStartup { get; set; } = false;
        public List<HotkeyAction> Hotkeys { get; set; } = new List<HotkeyAction>();
    }
}