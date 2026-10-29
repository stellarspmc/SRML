using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace SRML.SR
{
    public static class DroneRegistry
    {
        internal static Dictionary<Identifiable.Id, SRMod> customBasicTarget = new Dictionary<Identifiable.Id, SRMod>();

        /// <summary>
        /// Register an <see cref="Identifiable.Id"/> as a drone target.
        /// </summary>
        /// <param name="id"></param>
        public static void RegisterBasicTarget(Identifiable.Id id)
        {
            customBasicTarget.Add(id, SRMod.GetCurrentMod());

            foreach(var v in GetMetadatas())
                v.targets = v.targets.AddToArray(new DroneMetadata.Program.Target.Basic(id));
        }

        static IEnumerable<DroneMetadata> GetMetadatas()
        {
            GameContext context;
            switch (SRModLoader.CurrentLoadingStep)
            {
                case SRModLoader.LoadingStep.PRELOAD:
                    context = Object.FindObjectOfType<GameContext>();
                    break;
                default:
                    context = GameContext.Instance;
                    break;
            }

            var metadataCache = new HashSet<DroneMetadata>();
            foreach (var data in from v in Gadget.DRONE_CLASS 
                     where context.LookupDirector.gadgetDefinitionDict.ContainsKey(v) 
                     select context.LookupDirector.GetGadgetDefinition(v).prefab.GetComponentInChildren<DroneGadget>().metadata 
                     into data where metadataCache.Add(data) select data) {
                yield return data;
            }
        }
    }
}
