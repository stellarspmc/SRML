using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

var vortexMode = args.Length > 0 && args[0].StartsWith("-v");
var vortexUninstall = vortexMode && args[0] == "-vu";

var filename = !vortexMode && args.Length == 0 ? FindGame() : vortexMode ? args[1] : args[0]; // sr root
var root = Path.GetDirectoryName(filename);

try {
    if (root != null) {
        var srmlPath = Path.Combine(root, "SRML/Libs/SRML.dll");
        var patched = false;

        if (File.Exists(srmlPath) && File.Exists(Path.Combine(root, "winhttp.dll"))) { // doorstop + srml is installed
            patched = true;
            if (!vortexMode) { 
                Console.Write("Game is already patched! Would you like to uninstall? (selecting n will instead trigger an update) (y/n): ");

                poll_user:
                var response = Console.ReadLine();
                switch (response) {
                    case "yes":
                    case "y":
                        Uninstall();
                        return;
                    case "n":
                    case "no":
                        break;
                    default:
                        Console.Write("Please enter a valid option! (y/n): ");
                        goto poll_user;
                }
            }
            if (vortexUninstall) {
                Uninstall();
                return;
            }
        }
        
        // step 1: copy files
        var localDir = Path.Combine(AppContext.BaseDirectory, "Libs");
        var targetDir = Path.Combine(root, "SRML/Libs");

        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(targetDir)) File.Delete(file);

        if (!Directory.Exists(localDir)) throw new Exception($"Local 'Libs' folder not found at {localDir}! Please ensure the Libs folder is packaged with the installer.");
        foreach (var filePath in Directory.GetFiles(localDir)) { 
            var fileName = Path.GetFileName(filePath);
            var destPath = Path.Combine(targetDir, fileName);
        
            Console.WriteLine($"Copying {fileName}...");
            File.Copy(filePath, destPath, true);
        }
    
        // step 2: install doorstop
        Console.WriteLine("Downloading UnityDoorstop...");
        var zipPath = Path.Combine(Path.GetTempPath(), "doorstop_x64.zip");
        using (var webClient = new HttpClient()) {
            var s = await webClient.GetByteArrayAsync("https://github.com/NeighTools/UnityDoorstop/releases/download/v4.6.0/doorstop_win_release_4.6.0.zip"); // please fetch latest instead!!!
            await File.WriteAllBytesAsync(zipPath, s);
        }

        await using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries) {
            if (entry.FullName.Contains("x64") && (entry.FullName.EndsWith("winhttp.dll", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith("doorstop_config.ini", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".doorstop_version", StringComparison.OrdinalIgnoreCase))) {
                entry.ExtractToFile(Path.Combine(root, entry.Name), overwrite: true);
            }
        }
        
        SafeDelete(zipPath);
        // step 3: edit doorstop file
        var configPath = Path.Combine(root, "doorstop_config.ini");
        if (File.Exists(configPath)) {
            var configContent = await File.ReadAllTextAsync(configPath);
            configContent = configContent.Replace("Doorstop.dll", @"SRML\Libs\SRML.dll");
            await File.WriteAllTextAsync(configPath, configContent);
        }
        
        // finished
        Console.WriteLine();

        var type = patched ? "Update" : "Installation";
        var dir = Directory.GetParent(root)?.Parent;
        Console.WriteLine($"{type} complete! ");
        if (dir != null) {
            var path = Path.Combine(dir.FullName, "SRML", "Mods");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Console.WriteLine($"Mods can be installed at {path}");
        }
        
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()) Console.WriteLine("Please add the line \'WINEDLLOVERRIDES=\"winhttp=n,b\"\' to your launch arguments."); // TODO: make this be seen to linux users
    }
} catch (UnauthorizedAccessException e) {
    Console.WriteLine(e.Message);
    Console.WriteLine($"Please run {Path.GetFileName(Assembly.GetExecutingAssembly().Location)} as an administrator!");
} catch (Exception e) {
    Console.WriteLine(e);
} if (vortexMode) return;

Console.Write("Press any key to continue...");
Console.ReadKey();
return;

// functions

string FindGame() { // only steam support for now + steam root only
    var gameRelativePath = Path.Combine("steamapps", "common", "Slime Rancher");
    var steamRoot = string.Empty;

    if (OperatingSystem.IsWindows()) { // no checks yet
        try {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamPath") is string path) steamRoot = path.Replace('/', Path.DirectorySeparatorChar);
        } catch { /* ignored */ }
            
        if (string.IsNullOrEmpty(steamRoot) || !Directory.Exists(steamRoot)) steamRoot = @"C:\Program Files (x86)\Steam";
    }
    else if (OperatingSystem.IsLinux()) {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var paths = new[] {
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".local", "share", "Steam")
        };

        foreach (var p in paths) {
            if (!Directory.Exists(p)) continue;
            if (!File.Exists(Path.Combine(p, "SlimeRancher.exe"))) continue;
            steamRoot = p;
            break;
        }
    } else if (OperatingSystem.IsMacOS()) steamRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "Steam");
    if (!string.IsNullOrEmpty(steamRoot)) {
        var fullPath = Path.Combine(steamRoot, gameRelativePath);
        if (Directory.Exists(fullPath)) return fullPath;
    }

    Console.WriteLine("Could not automatically find Slime Rancher installation.");
    Console.Write("Please enter the path to your Slime Rancher folder: ");
    var manualPath = Console.ReadLine()?.Trim('"');

    if (string.IsNullOrEmpty(manualPath)) throw new FileNotFoundException("Could not locate Slime Rancher game files."); // only proton / .exe
    return Directory.Exists(manualPath) && File.Exists(Path.Combine(manualPath, "SlimeRancher.exe")) ? manualPath : throw new FileNotFoundException("Could not locate Slime Rancher game files.");
}

void Uninstall() { // todo: switch to bool -> debug reasons
    SafeDelete(Path.Combine(root, "SRML/Libs"), true);
    SafeDelete(Path.Combine(root, "winhttp.dll"));
    SafeDelete(Path.Combine(root, "doorstop_config.dll"));
    SafeDelete(Path.Combine(root, ".doorstop_version"));
    Console.Write("Uninstallation complete!");
}

void SafeDelete(string path, bool isDirectory = false) {
    if (isDirectory) {
        if (Directory.Exists(path)) Directory.Delete(path, true);
        return;
    }

    if (File.Exists(path)) File.Delete(path);
}