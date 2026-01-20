#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.tcs.tools.adminwindow.Core.Binding
{
    // A typed bag of targets with uniform mutation helpers.
    public sealed class EntitySet<T> where T : Component
    {
        public readonly List<Entry> Items;
        public readonly string Via; // e.g., "selection", "scene:Enemy", "project:Enemy"

        public EntitySet(List<Entry> items, string via)
        {
            Items = items ?? new(); Via = via ?? "selection";
        }

        public bool IsEmpty => Items.Count == 0;

        public readonly struct Entry
        {
            public readonly T Component;
            public readonly bool IsAsset;
            public readonly string AssetPath;         // non-empty if IsAsset
            public readonly SerializedObject SO;      // non-null if IsAsset

            public Entry(T comp, bool isAsset, string path, SerializedObject so)
            {
                Component = comp; IsAsset = isAsset; AssetPath = path ?? ""; SO = so;
            }
        }

        /// <summary>
        /// Applies actions to all entries. Handles Undo/dirty/apply for assets automatically.
        /// Returns number of entries modified.
        /// </summary>
        public int Apply(string undoName, Action<T> sceneInstance, Action<SerializedObject> assetSO = null)
        {
            int n = 0;
            foreach (var e in Items)
            {
                if (e.IsAsset)
                {
                    if (assetSO == null) continue; // no asset handler provided
                    Undo.RegisterCompleteObjectUndo(e.SO.targetObject, undoName);
                    assetSO(e.SO);
                    e.SO.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(e.SO.targetObject);
                    n++;
                }
                else
                {
                    // Scene instance (playmode or edit-time)
                    sceneInstance?.Invoke(e.Component);
                    n++;
                }
            }
            return n;
        }
    }
}
#endif