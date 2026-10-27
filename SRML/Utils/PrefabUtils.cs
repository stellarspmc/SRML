using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SRML.Utils {
    public static class PrefabUtils {
        public static void ReplaceFieldsWith<T>(GameObject prefab, T original, T newValue) {
            var components = prefab.GetComponentsInChildren<Component>(true);

            foreach (var comp in components) {
                if (!comp) continue;
                foreach (var field in comp.GetType().GetFields()) {
                    if (field.FieldType != typeof(T)) continue;
                    var t = (T)field.GetValue(comp);
                    if (!t.Equals(original)) continue;
                    field.SetValue(comp,newValue);
                }
            }
        }

        public static GameObject CopyPrefab(GameObject prefab) => Object.Instantiate(prefab, Main.prefabParent, false);

        public static Object DeepCopyObject(Object ob) {
            var refToRef = new Dictionary<Object, Object>();
            Object DeepCopyObject_Internal(Object obj) {
                if (!obj) return obj;
                if (refToRef.TryGetValue(obj, out var old)) return old;
                if (refToRef.Values.Contains(obj)) return obj;
                var newObj = Object.Instantiate(obj);
                if (!typeof(ScriptableObject).IsAssignableFrom(newObj.GetType())) return newObj;
                refToRef[obj] = newObj;

                foreach (var v in newObj.GetType().GetFields()) v.SetValue(newObj,ProcessObject(v.GetValue(newObj)));
                return newObj;

                object ProcessObject(System.Object theObj) {
                    if (theObj == null) return null;

                    if (typeof(Component).IsAssignableFrom(theObj.GetType()) || typeof(GameObject).IsAssignableFrom(theObj.GetType())) return theObj;
                    if (typeof(Object).IsAssignableFrom(theObj.GetType())) return DeepCopyObject_Internal((Object)theObj);
                    if (theObj.GetType().IsArray) {
                        var g = theObj as Array;
                        for (var i = 0; i < g.Length; i++) g.SetValue(ProcessObject(g.GetValue(i)), i);
                    }
                    else if (!theObj.GetType().IsPrimitive&&!typeof(String).IsAssignableFrom(theObj.GetType())&&!theObj.GetType().IsEnum)
                        foreach(var v in theObj.GetType().GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public))
                            v.SetValue(theObj, ProcessObject(v.GetValue(theObj)));

                    return theObj;
                }
            }
            return DeepCopyObject_Internal(ob);
        }
    }
}
