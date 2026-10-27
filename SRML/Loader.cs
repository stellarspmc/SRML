using HarmonyLib;
using SRML;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Doorstop;

internal static class Entrypoint {
    private static readonly Assembly[] Resolve = [.. Directory.GetFiles(Path.GetFullPath(@"SRML\Libs"), "*.dll").Select(Assembly.LoadFrom)];

    public static void Start() => new Task(() =>
    {
        // I really hope this number is large enough to load successfully on all PCs
        // CompanyLogoScene spends 1.5s fading in, 1s hold, 1.5s fading out
        // so this will theoretically load Doorstop as soon as the logo fades out
        Task.Delay(2750);
        Debug.Log("Doorstop has successfully hooked.");

        AppDomain.CurrentDomain.AssemblyResolve += (_, y) => Resolve.FirstOrDefault(z => z.GetName() == new AssemblyName(y.Name));

        new Harmony("SRMLInitializer").Patch(AccessTools.Method(typeof(StandaloneStartScreen), "Update"),
            new HarmonyMethod(typeof(Main), "InitializeSRMLThenBeginLoad"));
    }).Start();
}