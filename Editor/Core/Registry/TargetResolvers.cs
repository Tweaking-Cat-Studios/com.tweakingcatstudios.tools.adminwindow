// Assets/TCSTools/Admin/Editor/Core/Registry/TargetResolvers.cs
using System;
using TCS.AdminWindow.Core.Integration;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TCS.Admin
{
    public static class TargetResolvers
    {
        public static Func<object> Static(object instance = null) => () => instance;

        public static Func<object> ForType(Type t)
        {
            return () =>
            {
                // 1) DI (ReflexBridge sets this)
                var fromDi = ReflexAdapter.ResolveOrNull?.Invoke(t);
                if (fromDi != null) return fromDi;

                // 2) Scene object (MonoBehaviours/ScriptableObjects)
                var sceneObj = Object.FindFirstObjectByType(t);
                if (sceneObj != null) return sceneObj;

#if UNITY_EDITOR
                // 3) Asset lookup (SO-based singletons etc.)
                var guids = AssetDatabase.FindAssets($"t:{t.Name}");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var asset = AssetDatabase.LoadAssetAtPath(path, t);
                    if (asset != null) return asset;
                }
#endif
                return null;
            };
        }
    }
}