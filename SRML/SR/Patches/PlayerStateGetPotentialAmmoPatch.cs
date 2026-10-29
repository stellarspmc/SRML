using HarmonyLib;
using System.Collections.Generic;

namespace SRML.SR.Patches
{
    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("GetPotentialAmmo")]
    internal static class PlayerStateGetPotentialAmmoPatch
    {
        public static void Postfix(HashSet<Identifiable.Id> __result)
        {
            if (!AmmoRegistry.inventoryPrefabsToPatch.TryGetValue(PlayerState.AmmoMode.DEFAULT, out var value)) return;
            foreach (var v in value) __result.Add(v);
        }
    }
}
