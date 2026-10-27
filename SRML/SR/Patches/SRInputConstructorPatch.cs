namespace SRML.SR.Patches
{
    //[HarmonyPatch(typeof(SRInput), MethodType.Constructor)]
    internal static class SRInputConstructorPatch
    {
        public static void Postfix(SRInput __instance)
        {
            //foreach (var v in BindingRegistry.precreators) v(__instance.actions);
        }
    }
}
