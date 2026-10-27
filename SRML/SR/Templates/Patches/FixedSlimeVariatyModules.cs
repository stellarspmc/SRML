using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SRML.SR.Templates.Patches;

[HarmonyPatch(typeof(SlimeVarietyModules))]
[HarmonyPatch("MergeGeneralComponents")]
public static class FixedSlimeVarietyModules
{
    public static bool Prefix(SlimeVarietyModules __instance, List<Component> ___addedComponents)
    {
        foreach (var gameObject in __instance.slimeModules)
        {
            if (gameObject != null)
            {
                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component is Collider || __instance.GetComponent(component.GetType()) == null)
                    {
                        ___addedComponents.Add(__instance.gameObject.AddComponent(component.GetType()).GetCopyOf(component));
                    }
                }
            }
            var childCount = gameObject.transform.childCount;
            for (var k = 0; k < childCount; k++)
            {
                if (__instance.gameObject.transform.Find(gameObject.transform.GetChild(k).name) != null)
                    continue;

                var gameObject2 = UnityEngine.Object.Instantiate(gameObject.transform.GetChild(k).gameObject, __instance.transform);
                gameObject2.transform.localPosition = gameObject.transform.GetChild(k).gameObject.transform.localPosition;
                gameObject2.transform.localRotation = gameObject.transform.GetChild(k).gameObject.transform.localRotation;
            }
        }

        if (__instance.baseModule != null)
        {
            var flag = __instance.GetComponent<RejectBaseNontriggerColliders>() != null;
            foreach (var component in __instance.baseModule.GetComponents<Component>())
            {
                if (component is Collider && (((Collider)component).isTrigger || !flag) || __instance.GetComponent(component.GetType()) == null)
                {
                    ___addedComponents.Add(__instance.gameObject.AddComponent(component.GetType()).GetCopyOf(component));
                }
            }
            var childCount = __instance.baseModule.transform.childCount;
            for (var k = 0; k < childCount; k++)
            {
                if (__instance.gameObject.transform.Find(__instance.baseModule.transform.GetChild(k).name) != null)
                    continue;

                var gameObject = UnityEngine.Object.Instantiate(__instance.baseModule.transform.GetChild(k).gameObject, __instance.transform);
                gameObject.transform.localPosition = __instance.baseModule.transform.GetChild(k).gameObject.transform.localPosition;
                gameObject.transform.localRotation = __instance.baseModule.transform.GetChild(k).gameObject.transform.localRotation;
            }
        }

        __instance.GetComponent<SlimeSubbehaviourPlexer>().CollectSubbehaviours();

        return false;
    }
}