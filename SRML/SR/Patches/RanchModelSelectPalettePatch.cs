using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;

namespace SRML.SR.Patches
{
    [HarmonyPatch(typeof(RanchModel), "SelectPalette")]
    internal static class RanchModelSelectPalettePatch
    {
        public static void Prefix(RanchModel __instance, RanchDirector.PaletteType type, ref RanchDirector.Palette pal)
        {
            if (!((RanchDirector)__instance.participant).paletteDict.ContainsKey(pal))
                pal = ChromaRegistry.GetDefaultPaletteForType(type);
        }
    }
}
