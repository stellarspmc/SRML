using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SRML.SR.SaveSystem.Data;
using UnityEngine;

namespace SRML.SR.SaveSystem.Patches
{
    [HarmonyPatch(typeof(SlimeEat), "EatAndTransform")]
    internal static class EatAndTransformCopyPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var insertionIndex = -1;
            for (var i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldloc_1 && code[i - 1].opcode == OpCodes.Pop)
                {
                    insertionIndex = i;
                    break;
                }
            }

            var getGet = AccessTools.Property(typeof(Component), nameof(Component.gameObject)).GetGetMethod();
            var newInstructions = new List<CodeInstruction>();
            newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
            newInstructions.Add(new CodeInstruction(OpCodes.Call, getGet));
            newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_1));
            newInstructions.Add(new CodeInstruction(OpCodes.Call, typeof(EatAndTransformCopyPatch).GetMethod("CopyComponentsToTransformed", BindingFlags.Public | BindingFlags.Static)));
            if (insertionIndex != -1) code.InsertRange(insertionIndex, newInstructions);
            return code;
        }

        public static void CopyComponentsToTransformed(GameObject original, GameObject transformed)
        {
            if (original.GetComponent<ExtendedData.TransformableParticipant>() == null) return;
            foreach (var part in original.GetComponents<ExtendedData.TransformableParticipant>())
            {
                if (part.CopyCondition())
                {
                    if (!transformed.HasComponent(part.GetType())) transformed.AddComponent(part.GetType());
                    var data = new CompoundDataPiece("");
                    part.WriteData(data);
                    ((ExtendedData.TransformableParticipant)transformed.GetComponent(part.GetType())).ReadData(data);
                }
            }
        }
    }
}
