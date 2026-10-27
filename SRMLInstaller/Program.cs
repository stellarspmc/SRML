using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace SRMLInstaller {
    internal class Program {
        // ReSharper disable once InconsistentNaming
        private const string SRML = "SRML.dll";
        private const string EmbeddedResourcePath = "SRMLInstaller.Libs.";
        private const string EmbeddedResourceProject = "SRMLInstaller.";

        [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "<Pending>")]
        private static void Main(string[] args) {
            var vortexMode = args.Length > 0 && args[0].StartsWith("-v");
            var vortexUninstall = vortexMode && args[0] == "-vu";

            try {
                var filename = !vortexMode && args.Length == 0 ? GameFinder.FindGame() : (vortexMode ? args[1] : args[0]) + "/SlimeRancher_Data/Managed/Assembly-CSharp.dll";
                var root = Path.GetDirectoryName(filename);
                if (root != null) {
                    var srmlPath = Path.Combine(root, SRML);

                    Console.WriteLine();

                    var uninstalling = false;
                    var patched = false;
                    var didSrmlExist = File.Exists(srmlPath);

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
                        SendFilesOver(didSrmlExist);
                    }
                    else {
                        SendFilesOver();
                        goto try_to_patch;
                    }

                    string GetAlternateRoot() {
                        var directoryInfo = Directory.GetParent(root)?.Parent;
                        if (directoryInfo == null) return "";
                        var alternateRoot = Path.Combine(directoryInfo.FullName, "SRML", "Libs");
                        if (!Directory.Exists(alternateRoot)) Directory.CreateDirectory(alternateRoot);
                        return alternateRoot;
                    }

                    void SendFilesOver(bool canLog = true) {
                        foreach(var file in Directory.GetFiles(GetAlternateRoot())) File.Delete(file);
                        foreach (var v in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x =>
                                     x.Length > EmbeddedResourceProject.Length &&
                                     x[..EmbeddedResourceProject.Length] == EmbeddedResourceProject)) {
                            var file = v[EmbeddedResourcePath.Length..];
                            var combine = Path.Combine(file.Contains("SRML") ? root:GetAlternateRoot(), file);
                            //var libPath = Path.Combine(libFolder, file);
                            if (File.Exists(combine)) {
                                if (canLog) Console.WriteLine(!uninstalling ? $"Found old {file}! Replacing..." : $"Deleting {file}...");
                                File.Delete(combine);
                            }
                            if (uninstalling) continue;
                            var str = File.Create(combine);
                            var otherStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Program), v[EmbeddedResourceProject.Length..]);
                            if (otherStream != null) {
                                otherStream.CopyTo(str);
                                otherStream.Close();
                            }
                            str.Close();
                        }

                        if (uninstalling) return;
                        var directoryInfo = Directory.GetParent(root)?.Parent;
                        if (directoryInfo == null) return;
                        var umfPath = Path.Combine(directoryInfo.FullName, "uModFramework", "Lib", "net462");
                        if (!File.Exists(Path.Combine(umfPath, "0Harmony.dll"))) return;
                        Console.WriteLine("Found existing UMF installation! Patching...");
                        File.Copy(Path.Combine(GetAlternateRoot(), "0Harmony.dll"), Path.Combine(umfPath, "0Harmony.dll"), true);
                    }

                    Console.WriteLine();

                    var type = patched ? "Update" : "Installation";
                    var ending = patched? "" : $"(old assembly stored as { Path.GetFileNameWithoutExtension(filename)}_old.dll)";

                    if (!uninstalling) {
                        Console.WriteLine($"{type} complete! "+ending);
                        var directoryInfo = Directory.GetParent(root)?.Parent;
                        if (directoryInfo != null) {
                            var path = Path.Combine(directoryInfo.FullName, "SRML", "Mods");
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
}