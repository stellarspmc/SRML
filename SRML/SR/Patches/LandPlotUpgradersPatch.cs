using System.Linq;
using HarmonyLib;

namespace SRML.SR.Patches
{
    [HarmonyPatch(typeof(LandPlot), "Awake")]
    internal static class LandPlotUpgradersPatch
    {
        static void Postfix(LandPlot __instance) {
            var g = __instance.gameObject;
            foreach (var upgrade in LandPlotUpgradeRegistry.moddedUpgraders.Where(upgrade => upgrade.Item2 == __instance.typeId && !g.GetComponent(upgrade.Item1)))
                g.AddComponent(upgrade.Item1);
        }
    }
}
