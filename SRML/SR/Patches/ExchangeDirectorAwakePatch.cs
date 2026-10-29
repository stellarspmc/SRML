using HarmonyLib;

namespace SRML.SR.Patches
{
    [HarmonyPatch(typeof(ExchangeDirector), "Awake")]
    internal static class ExchangeDirectorAwakePatch
    {
        public static void Prefix(ExchangeDirector __instance)
        {
            foreach (var progressOffer in ExchangeOfferRegistry.customProgressOffers.Keys)
                __instance.progressOffers = __instance.progressOffers.AddToArray(progressOffer);
            foreach (var rancher in ExchangeOfferRegistry.customRanchers.Keys)
            {
                var modified = rancher;
                modified.chatBackground = modified.chatBackground ?? __instance.ranchers[0].chatBackground;
                __instance.ranchers = __instance.ranchers.AddToArray(modified);
            }
            foreach (var cat in ExchangeOfferRegistry.customCategories.Keys)
                __instance.catDict.Add(cat.Item1, cat.Item2);
            foreach (var list in ExchangeOfferRegistry.customUnlocks.Values)
            {
                if (list.unlock == ProgressDirector.ProgressType.NONE) __instance.initUnlocked = __instance.initUnlocked.AddRangeToArray(list.ids);
                else __instance.unlockLists = __instance.unlockLists.AddToArray(list);
            }
            foreach (var d in ExchangeOfferRegistry.customUnlockValues)
                __instance.valueDict[d.Key] = d.Value;
        }
    }
    [HarmonyPatch(typeof(ExchangeDirector), "Start")]
    internal static class ExchangeDirectorStartPatch
    {
        public static void Prefix(ExchangeDirector __instance)
        {
            if (!__instance.worldModel.currOffers.ContainsKey(ExchangeDirector.OfferType.GENERAL)) return;
            if (__instance.GetRancher(__instance.worldModel.currOffers[ExchangeDirector.OfferType.GENERAL].rancherId) == null)
                __instance.ClearOffer(ExchangeDirector.OfferType.GENERAL);
        }
    }
}
