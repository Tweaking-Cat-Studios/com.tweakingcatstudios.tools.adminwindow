#if HAS_REFLEX
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reflex;        // SceneExtensions.GetSceneContainer()
using Reflex.Core;
using Reflex.Extensions;
using TCS.AdminWindow.Core.Integration;

namespace TCS.Admin
{
    [InitializeOnLoad]
    public static class ReflexBridge
    {
        // Cache MethodInfos for generic resolution paths once
        static readonly MethodInfo MiSingle =
            typeof(Container).GetMethods().FirstOrDefault(m => m.Name == "Single" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);

        static readonly MethodInfo MiResolve =
            typeof(Container).GetMethods().FirstOrDefault(m => m.Name == "Resolve" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);

        static readonly MethodInfo MiAll =
            typeof(Container).GetMethods().FirstOrDefault(m => m.Name == "All" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);

        static ReflexBridge()
        {
            // Plug into the adapter right away (Editor domain reload)
            ReflexAdapter.ResolveOrNull = ResolveViaReflex;

            // Keep things tidy around play mode transitions
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    ReflexAdapter.ResolveOrNull = ResolveViaReflex;
                else if (state == PlayModeStateChange.ExitingPlayMode)
                    ReflexAdapter.ResolveOrNull = null;
            };
        }

        // Called by Admin whenever it needs a DI instance of 'contract'
        static object ResolveViaReflex(Type contract)
        {
            if (!Application.isPlaying) return null; // containers exist only in play mode

            // Search all loaded scenes; Scene containers inherit ProjectScope bindings
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                var container = scene.GetSceneContainer();
                if (container == null) continue;

                if (TryFromContainer(container, contract, out var inst))
                    return inst;
            }

            return null;
        }

        static bool TryFromContainer(Container c, Type contract, out object instance)
        {
            instance = null;

            // Prefer Single<T>() when unique
            if (MiSingle != null)
            {
                try
                {
                    instance = MiSingle.MakeGenericMethod(contract).Invoke(c, null);
                    if (instance != null) return true;
                }
                catch { /* not bound or not unique */ }
            }

            // Fallback: Resolve<T>() (last registration)
            if (MiResolve != null)
            {
                try
                {
                    instance = MiResolve.MakeGenericMethod(contract).Invoke(c, null);
                    if (instance != null) return true;
                }
                catch { /* not bound */ }
            }

            // Last: All<T>() → accept only if exactly one to avoid ambiguity
            if (MiAll != null)
            {
                try
                {
                    var seq = MiAll.MakeGenericMethod(contract).Invoke(c, null) as System.Collections.IEnumerable;
                    object only = null; int count = 0;
                    if (seq != null)
                    {
                        foreach (var it in seq) { only = it; count++; if (count > 1) break; }
                        if (count == 1 && only != null) { instance = only; return true; }
                    }
                }
                catch { }
            }

            return false;
        }
    }
}
#endif