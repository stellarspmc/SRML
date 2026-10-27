using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SRML.Console.Commands {
    class KillAllCommand : ConsoleCommand {
        public override string ID => "killall";
        public override string Usage => "killall [radius/type]";
        public override string Description => "Kills all of a type in a radius, or just all";
        public override bool Execute(string[] args) {
            var radius = -1;
            var toKill = new List<Identifiable.Id>();
            foreach(var v in args ?? Array.Empty<string>()) {
                if(uint.TryParse(v,out var rad)) {
                    radius = (int)rad;
                    continue;
                } try {
                        toKill.Add((Identifiable.Id)Enum.Parse(typeof(Identifiable.Id), v, true));
                } catch {
                    // ignored
                }
            }

            var toDestroy = (from v in SceneContext.Instance.GameModel.AllActors().Where(x => radius == -1 || Vector3.Distance(x.Value?.transform?.position ?? SceneContext.Instance.PlayerState.model.position, SceneContext.Instance.PlayerState.model.position) < radius) where toKill.Count == 0 || toKill.Contains(v.Value.ident) select v.Value?.transform?.gameObject).ToList();
            var deletedCount = 0;
            foreach (var v in toDestroy.Where(v => v)) {
                deletedCount++;
                DeathHandler.Kill(v, DeathHandler.Source.UNDEFINED, SceneContext.Instance.Player, "KillAllCommand.Execute");
            }
            Console.Instance.LogSuccess($"Destroyed {deletedCount} actors!");
            return true;
        }

        public override List<string> GetAutoComplete(int argIndex, string argText) => Enum.GetNames(typeof(Identifiable.Id)).ToList();
    }
}
