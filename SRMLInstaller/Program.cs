using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace SRMLInstaller;

internal class Program {
    // ReSharper disable once InconsistentNaming
    private const string SRML = "SRML.dll";

    [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "<Pending>")]
    private static void Main(string[] args) {
        var vortexMode = args.Length > 0 && args[0].StartsWith("-v");
        var vortexUninstall = vortexMode && args[0] == "-vu";

        try {
            var filename = !vortexMode && args.Length == 0 ? GameFinder.FindGame() : (vortexMode ? args[1] : args[0]) + "/SlimeRancher_Data/Managed/Assembly-CSharp.dll";
            var root = Path.GetDirectoryName(filename);
            if (root != null) {
                var srmlPath = Path.Combine(root, "SRML", SRML);
                var uninstalling = false;
                var patched = false;

                try_to_patch:
                if (File.Exists(srmlPath)) {
                    var patcher = new Patcher(filename, GetOnLoad(srmlPath));
                    if (patcher.IsPatched()) {
                        patched = true;
                        if (!vortexMode) {
                            Console.Write("Game is already patched! Would you like to uninstall? (selecting n will instead trigger an update) (y/n): ");

                            poll_user:
                            var response = Console.ReadLine();
                            switch (response) {
                                case "yes":
                                case "y":
                                    uninstalling = true;
                                    patcher.Uninstall();
                                    break;
                                case "n":
                                case "no":
                                    break;
                                default:
                                    Console.Write("Please enter a valid option! (y/n): ");
                                    goto poll_user;
                            }
                        }

                        if (vortexUninstall) {
                            uninstalling = true;
                            patcher.Uninstall();
                        }
                    }
                    else {
                        Console.WriteLine("Patching...");
                        patcher.Patch();
                        Console.WriteLine("Patching Successful!");
                        patcher.Save();
                    }

                    patcher.Dispose();
                    SendFilesOver(false);
                }
                else {
                    SendFilesOver();
                    goto try_to_patch;
                }

                void SendFilesOver(bool log = true) {
                    var localLibsDir = Path.Combine(AppContext.BaseDirectory, "Libs");
                    var targetSrmlDir = Path.Combine(root, "SRML");

                    if (!Directory.Exists(targetSrmlDir)) Directory.CreateDirectory(targetSrmlDir);
                    foreach (var file in Directory.GetFiles(targetSrmlDir)) File.Delete(file);

                    if (uninstalling) return;

                    if (!Directory.Exists(localLibsDir)) throw new Exception($"Local 'Libs' folder not found at {localLibsDir}! Please ensure the Libs folder is packaged with the installer.");
                    foreach (var filePath in Directory.GetFiles(localLibsDir)) {
                        var fileName = Path.GetFileName(filePath);
                        var destPath = Path.Combine(targetSrmlDir, fileName);
        
                        if (log) Console.WriteLine($"Copying {fileName}...");
                        File.Copy(filePath, destPath, true);
                    }

                    var directoryInfo = Directory.GetParent(root)?.Parent;
                    if (directoryInfo == null) return;
                    var umfPath = Path.Combine(directoryInfo.FullName, "uModFramework", "Lib", "net462");
                    if (!File.Exists(Path.Combine(umfPath, "0Harmony.dll"))) return;
    
                    Console.WriteLine("Found existing UMF installation! Patching...");
                    File.Copy(Path.Combine(targetSrmlDir, "0Harmony.dll"), Path.Combine(umfPath, "0Harmony.dll"), true);
                }

                Console.WriteLine();

                var type = patched ? "Update" : "Installation";
                var ending = patched? "" : $"(old assembly stored as { Path.GetFileNameWithoutExtension(filename)}_old.dll)";

                if (!uninstalling) {
                    Console.WriteLine($"{type} complete! "+ending);
                    var dir = Directory.GetParent(root)?.Parent;
                    if (dir != null) {
                        var path = Path.Combine(dir.FullName, "SRML", "Mods");
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        Console.WriteLine($"Mods can be installed at {path}");
                    }
                }
                else Console.WriteLine("Uninstallation complete!");
            }
        } catch (UnauthorizedAccessException e) {
            Console.WriteLine(e.Message);
            Console.WriteLine($"Please run {Path.GetFileName(Assembly.GetExecutingAssembly().Location)} as an administrator!");
        } catch (Exception e) {
            Console.WriteLine(e);
        }
        if (vortexMode) return;

        Console.Write("Press any key to continue...");
        Console.ReadKey();
    }

    private static MethodReference GetOnLoad(string path) {
        AssemblyDefinition def;
        try {
            def = AssemblyDefinition.ReadAssembly(path);
        } catch {
            throw new Exception($"Couldn't find {SRML}!");
        }

        return def.MainModule.GetType("SRML.Main").Methods.First(x => x.Name == "PreLoad");
    }
}