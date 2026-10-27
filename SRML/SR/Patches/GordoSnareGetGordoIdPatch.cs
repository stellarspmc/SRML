using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using MonomiPark.SlimeRancher.DataModel;
using System;

namespace SRML.SR.Patches;

[HarmonyPatch(typeof(GordoSnare), "GetGordoIdForBait")]
[HarmonyPriority(Priority.First)]
internal static class GordoSnareGetGordoIdPatch
{
    private static readonly Func<ZoneDirector.Zone, bool> HasAccessToZone = ZoneDirector.HasAccessToZone;

    public static bool Prefix(GordoSnare __instance, ref Identifiable.Id __result)
    {
        var dictionary = new Dictionary<Identifiable.Id, float>(Identifiable.idComparer);
        var normalIds = new List<Identifiable.Id>();
        var favIds = new List<Identifiable.Id>(); // To handle cases where multiple slimes favour the same id
        var baitId = __instance.GetPrivateField<SnareModel>("model").baitTypeId;

        foreach (var gordoEntry in GameContext.Instance.LookupDirector.GordoEntries)
        {
            var gordo = gordoEntry.GetComponent<GordoIdentifiable>();

            if (SnareRegistry.pinkLike.Contains(gordo.id) || !gordo.nativeZones.Any(HasAccessToZone))
                continue;

            var diet = gordoEntry.GetComponent<GordoEat>().slimeDefinition.Diet;
            var list2 = new List<SlimeDiet.EatMapEntry>();
            diet.AddEatMapEntries(baitId, list2);
            var eatMapEntry = list2.FirstOrDefault();

            if (eatMapEntry == null)
                continue;

            (var message, var ids) = eatMapEntry.isFavorite ? ("Found favorite", favIds) : ("Adding potential", normalIds);
            Log.Debug(message, "gordo", gordo.id, "hasAccess", true); // Not using flag because the base game never even logs false here
            ids.Add(gordo.id);
        }

        if (normalIds.Count > 0)
        {
            float value = __instance.foodTypeSnareWeight / normalIds.Count;

            for (var j = 0; j < normalIds.Count; j++)
                dictionary.Add(normalIds[j], value);
        }

        if (favIds.Count > 0)
        {
            float value = __instance.favoredFoodSnareWeight / favIds.Count;

            for (var j = 0; j < favIds.Count; j++)
                dictionary.Add(favIds[j], value);
        }

        float value2 = __instance.pinkSnareWeight / SnareRegistry.pinkLike.Count;

        foreach (var id in SnareRegistry.pinkLike)
            dictionary.Add(id, value2);

        var pink = Randoms.SHARED.Pick(SnareRegistry.pinkLike);
        __result = Randoms.SHARED.Pick(dictionary, pink);
        return false;
    }

    private static T Pick<T>(this Randoms random, HashSet<T> vals)
    {
        return vals.ElementAt(random.GetInt(vals.Count));
    }
}