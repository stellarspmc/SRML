using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRML.SR.Patches
{
    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("CheckAllUpgradeLockers")]
    internal static class PlayerStateNullCheckPatches
    {
        public static bool Prefix(PlayerState __instance) => __instance.model != null;
    }
}
