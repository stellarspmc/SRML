using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SRML.SR.Patches
{
    [HarmonyPatch(typeof(GamepadPanel))]
    [HarmonyPatch("SetupBindings")]
    internal static class GamepadPanelSetupBindingsPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var codeInstructions = instr.ToList();
            var allInstructions = new List<CodeInstruction>(codeInstructions);
            var latestBinding = codeInstructions.Last(x => x.opcode == OpCodes.Call && ((MethodInfo)x.operand).Name == "CreateGamepadBindingLine");
            var index = codeInstructions.IndexOfItem(latestBinding) + 1;

            var newInstructions = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GamepadPanelSetupBindingsPatch), "Alternate"))
            };
            allInstructions.InsertRange(index, newInstructions);
            return allInstructions;
        }

        public static void Alternate(GamepadPanel ui)
        {
            foreach (var v in SRInput.Actions.Actions.Where(x => BindingRegistry.moddedBindings.Contains(x)))
                ui.CreateGamepadBindingLine("key." + v.Name.ToLowerInvariant(), v);
        }
    }
}
