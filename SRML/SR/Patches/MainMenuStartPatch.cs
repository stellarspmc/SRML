using System.Linq;
using HarmonyLib;
using SRML.SR.UI;

namespace SRML.SR.Patches
{
    [HarmonyPatch(typeof(MainMenuUI))]
    [HarmonyPatch("Start")]
    internal static class MainMenuStartPatch
    {
        public static void Postfix(MainMenuUI __instance)
        {
            var erroring = SRModLoader.Mods.Values.Where(x => x.ModInfo.EncounteredError && x.ModInfo.LoadState != SRModInfo.State.INITIALIZATION_ERROR);
            var srMods = erroring.ToList();

            if (srMods.Count != 0)
                ErrorGUI.TryCreateExtendedError(__instance, ErrorGUI.errorUI, srMods);
            else
                SRCallbacks.OnMainMenuLoad(__instance);
        }
    }
}
