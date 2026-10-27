using System;
using System.IO;
using Microsoft.Win32;

namespace SRMLInstaller {
    public static class GameFinder
    {
        public static string FindGame() { // only steam support for now
            var gameRelativePath = Path.Combine("steamapps", "common", "Slime Rancher", "SlimeRancher_Data", "Managed", "Assembly-CSharp.dll");
            var steamRoot = string.Empty;

            if (OperatingSystem.IsWindows()) {
                try {
                    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                    if (key?.GetValue("SteamPath") is string path) steamRoot = path.Replace('/', Path.DirectorySeparatorChar);
                } catch { 
                    // ignored
                }
            
                if (string.IsNullOrEmpty(steamRoot) || !Directory.Exists(steamRoot)) steamRoot = @"C:\Program Files (x86)\Steam";
            }
            else if (OperatingSystem.IsLinux()) {
                // Standard Linux Steam paths
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var paths = new[] {
                    Path.Combine(home, ".steam", "steam"),
                    Path.Combine(home, ".local", "share", "Steam")
                };

                foreach (var p in paths) {
                    if (!Directory.Exists(p)) continue;
                    steamRoot = p;
                    break;
                }
            }
            else if (OperatingSystem.IsMacOS()) steamRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "Steam");
            if (!string.IsNullOrEmpty(steamRoot)) {
                var fullPath = Path.Combine(steamRoot, gameRelativePath);
                if (File.Exists(fullPath)) return fullPath;
            }

            Console.WriteLine("Could not automatically find Slime Rancher installation.");
            Console.Write("Please enter the path to your Slime Rancher folder: ");
            var manualPath = Console.ReadLine()?.Trim('"');

            if (string.IsNullOrEmpty(manualPath)) throw new FileNotFoundException("Could not locate Slime Rancher game files.");
            var target = manualPath.EndsWith("Assembly-CSharp.dll") ? manualPath : Path.Combine(manualPath, "SlimeRancher_Data", "Managed", "Assembly-CSharp.dll");
            return File.Exists(target) ? target : throw new FileNotFoundException("Could not locate Slime Rancher game files.");
        }
    }
}