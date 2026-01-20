#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.tcs.tools.adminwindow.Core.Binding
{
    public sealed class EntitySetBinder : IArgBinder
    {
        public bool CanBind(Type t) =>
            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(EntitySet<>);

        public object Bind(string source, Type setType)
        {
            var elemType = setType.GetGenericArguments()[0]; // e.g., Enemy
            if (!typeof(Component).IsAssignableFrom(elemType))
                throw new Exception($"EntitySet<T>: T must derive from Component. Got {elemType.Name}");

            var via = (source ?? "").Trim();
            if (via.Length == 0) via = "selection";

            if (via.Equals("selection", StringComparison.OrdinalIgnoreCase) ||
                via.Equals("selected", StringComparison.OrdinalIgnoreCase))
                return BuildFromSelection(elemType, "selection");

            if (via.StartsWith("scene:", StringComparison.OrdinalIgnoreCase))
                return BuildFromScene(elemType, via.Substring("scene:".Length).Trim());

            if (via.StartsWith("project:", StringComparison.OrdinalIgnoreCase))
                return BuildFromProject(elemType, via.Substring("project:".Length).Trim());

            // Shorthand: just a type name => scene:<TypeName>
            if (!via.Contains(':'))
                return BuildFromScene(elemType, via);

            throw new Exception($"Unknown target set '{source}'. Use 'selection', 'scene:Type', or 'project:Type'.");
        }

        object BuildFromSelection(Type elemType, string via)
        {
            var items = new List<EntitySetEntry>();
            foreach (var obj in Selection.objects)
            {
                if (!obj) continue;

                if (obj is GameObject go) CollectFromGameObject(elemType, go, false, "", items);
                else if (obj is Component c) CollectComponent(elemType, c, false, "", items);
                else // asset (e.g., prefab)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path)) continue;
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab) CollectFromGameObject(elemType, prefab, true, path, items);
                }
            }
            return MakeSet(elemType, items, via);
        }

        object BuildFromScene(Type elemType, string typeOverride)
        {
            var targetType = ResolveComponentType(elemType, typeOverride);
            var comps = UnityEngine.Object.FindObjectsByType(
                targetType, FindObjectsInactive.Include, FindObjectsSortMode.None);

            var items = comps
                .OfType<Component>()
                .Where(c => elemType.IsAssignableFrom(c.GetType()))
                .Select(c => new EntitySetEntry(c, false, "", null))
                .ToList();

            var via = string.IsNullOrEmpty(typeOverride) ? "scene" : $"scene:{targetType.Name}";
            return MakeSet(elemType, items, via);
        }

        object BuildFromProject(Type elemType, string typeName)
        {
            var targetType = ResolveComponentType(elemType, typeName);
            var guids = AssetDatabase.FindAssets("t:Prefab");
            var items = new List<EntitySetEntry>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) continue;

                foreach (var c in prefab.GetComponentsInChildren(targetType, true).OfType<Component>())
                {
                    if (!elemType.IsAssignableFrom(c.GetType())) continue;
                    var so = new SerializedObject(c);
                    items.Add(new EntitySetEntry(c, true, path, so));
                }
            }
            return MakeSet(elemType, items, $"project:{targetType.Name}");
        }

        public static bool LooksLikeTargetSpec(string token, Type elemType)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            // Explicit keywords
            if (token.Equals("selection", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("selected", StringComparison.OrdinalIgnoreCase))
                return true;

            // Explicit scopes
            if (token.StartsWith("scene:", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("project:", StringComparison.OrdinalIgnoreCase))
                return true;

            // Shorthand type name (no colon): matches a Component name assignable to elemType
            if (!token.Contains(':'))
            {
                var any = TypeCache.GetTypesDerivedFrom<Component>()
                    .Any(t => elemType.IsAssignableFrom(t) &&
                              t.Name.Equals(token, StringComparison.OrdinalIgnoreCase));
                return any;
            }

            return false;
        }

        // --- helpers ---

        readonly struct EntitySetEntry
        {
            public readonly Component Comp; public readonly bool IsAsset; public readonly string Path; public readonly SerializedObject SO;
            public EntitySetEntry(Component comp, bool isAsset, string path, SerializedObject so) { Comp = comp; IsAsset = isAsset; Path = path; SO = so; }
        }

        static void CollectFromGameObject(Type elemType, GameObject go, bool isAsset, string path, List<EntitySetEntry> outList)
        {
            var comps = go.GetComponentsInChildren(elemType, true);
            foreach (var c in comps)
            {
                SerializedObject so = null;
                if (isAsset) so = new SerializedObject(c);
                outList.Add(new EntitySetEntry((Component)c, isAsset, path, so));
            }
        }

        static void CollectComponent(Type elemType, Component c, bool isAsset, string path, List<EntitySetEntry> outList)
        {
            if (elemType.IsAssignableFrom(c.GetType()))
            {
                SerializedObject so = null;
                if (isAsset) so = new SerializedObject(c);
                outList.Add(new EntitySetEntry(c, isAsset, path, so));
            }
        }

        static Type ResolveComponentType(Type elemType, string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return elemType;
            var matches = TypeCache.GetTypesDerivedFrom<Component>()
                .Where(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) && elemType.IsAssignableFrom(t))
                .ToList();
            if (matches.Count == 0) throw new Exception($"No Component '{typeName}' assignable to {elemType.Name}");
            return matches[0];
        }

        object MakeSet(Type elemType, List<EntitySetEntry> entries, string via)
        {
            // Deduplicate components so we don’t add duplicates
            var seen = new HashSet<Component>();

            // 1) Close the outer generic: EntitySet<elemType>
            var setType = typeof(EntitySet<>).MakeGenericType(elemType);

            // 2) Get the "Items" field type (this is List<EntryClosed>) from the CLOSED constructed type
            var itemsField = setType.GetField(
                "Items",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (itemsField == null)
                throw new Exception($"Failed to reflect field 'Items' on {setType}");

            var listType = itemsField.FieldType; // this is List<EntryClosed>
            var entryType = listType.GetGenericArguments()[0]; // this is the CLOSED nested Entry type

            // 3) Create List<EntryClosed>
            var listGeneric = (System.Collections.IList)Activator.CreateInstance(listType);

            // 4) Find the Entry constructor: (T comp, bool isAsset, string path, SerializedObject so)
            var ctor = entryType.GetConstructor(new[] { elemType, typeof(bool), typeof(string), typeof(SerializedObject) });
            if (ctor == null)
                throw new Exception(
                    $"Constructor not found on {entryType} – expected (" +
                    $"{elemType.Name}, bool, string, SerializedObject)");

            // 5) Populate the list
            foreach (var e in entries)
            {
                if (!seen.Add(e.Comp)) continue; // skip duplicates
                // No need for Convert.ChangeType for reference types when assignable
                var compArg = e.Comp; // assignable to elemType by construction
                listGeneric.Add(ctor.Invoke(new object[] { compArg, e.IsAsset, e.Path, e.SO }));
            }

            // 6) Construct EntitySet<T>(List<Entry> items, string via)
            return Activator.CreateInstance(setType, new object[] { listGeneric, via });
        }
    }
}
#endif
