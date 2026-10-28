using System.Reflection;
using HarmonyLib;

namespace SRML.Patches {
    [HarmonyPatch]
    [HarmonyPatch(typeof(AutoSaveDirector))]
    internal static class SaveGameLoadPatch {
        static MethodBase TargetMethod() => typeof(AutoSaveDirector).GetMethod("LoadSave", BindingFlags.NonPublic | BindingFlags.Instance); // weird...
        static void PostFix(AutoSaveDirector __instance) {
            // do something on save game loads
        }
    }
}
