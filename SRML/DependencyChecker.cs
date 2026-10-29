using System;
using System.Collections.Generic;
using System.Linq;

namespace SRML {
    internal static class DependencyChecker {
        public static bool CheckDependencies(HashSet<SRModLoader.ProtoMod> mods) {
            foreach (var mod in mods) {
                if (mod.encounteredError != null || !mod.HasDependencies) continue;

                var unmet = mod.parsedDependencies.Where(x => !mods.Any(x.SatisfiedBy));
                var dependencies = unmet.ToList();
                if (dependencies.Count != 0)
                    mod.encounteredError = new Exception($"Unresolved dependency for '{mod.id}'! Cannot find '{dependencies[0].mod_id} {dependencies[0].version}'");
            }
            return true;
        } // no false statement?

        private static int CompareLoadingOrder(SRModLoader.ProtoMod mod1, SRModLoader.ProtoMod mod2) {
            if (mod1.id == null || mod2.id == null) return 0;

            if (mod1.load_before.Contains(mod2.id) && mod2.load_before.Contains(mod1.id))
                throw new Exception($"{mod1.id} and {mod2.id} attempting to load before one another.");
            if (mod1.load_after.Contains(mod2.id) && mod2.load_after.Contains(mod1.id))
                throw new Exception($"{mod1.id} and {mod2.id} attempting to load after one another.");

            if (mod1.load_before.Contains(mod2.id) || mod2.load_after.Contains(mod1.id)) return -1;
            if (mod1.load_after.Contains(mod2.id) || mod2.load_before.Contains(mod1.id)) return 1;
            return 0;
        }

        public static void CalculateLoadOrder(ref HashSet<SRModLoader.ProtoMod> mods, out List<string> loadOrder) {
            var modsSorted = new List<SRModLoader.ProtoMod>(mods);
            modsSorted.Sort(CompareLoadingOrder);
            mods = modsSorted.ToHashSet();
            loadOrder = modsSorted.Where(x => x.id != null).Select(x => x.id).ToList();

            UnityEngine.Debug.Log($"Loading order: [ {string.Join(", ", loadOrder)} ]");
        }

        public static Dictionary<string, SRModInfo.ModVersion> ToDependencyDictionary(this Dependency[] dependencies) => dependencies.ToDictionary(x => x.mod_id, y => y.version);

        internal class Dependency {
            public string mod_id;
            public SRModInfo.ModVersion version { get; private set; }

            public Dependency(string id, string version) {
                mod_id = id;
                this.version = SRModInfo.ModVersion.Parse(version);
            }

            [Obsolete]
            public static Dependency ParseFromString(string s) {
                var strings = s.Split(' ');
                var dep = new Dependency(strings[0], strings[1]);
                return dep;
            }

            public bool SatisfiedBy(SRModLoader.ProtoMod mod) => mod.id == mod_id && mod.encounteredError == null && SRModInfo.ModVersion.Parse(mod.version).CompareTo(version) <= 0;
        }
    }
}
