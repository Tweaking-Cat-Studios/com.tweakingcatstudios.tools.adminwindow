using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;


// For Reflex integration, follow initial setup for Reflex setup at https://github.com/gustavopsantos/Reflex
// Then, Uncomment all block comments in this file

namespace TCS.AdminWindow.Core.Integration
{
    /// <summary>
    /// Thin indirection layer so Admin can ask for DI instances without
    /// taking a hard dependency on any specific container.
    /// A bridge (e.g., ReflexBridge) sets ResolveOrNull.
    /// </summary>
    public static class ReflexAdapter
    {
        /// <summary>
        /// Set by a DI bridge. Given a contract Type, return an instance or null.
        /// Keep this thread-main only (Editor context).
        /// </summary>
        public static Func<Type, object> ResolveOrNull;

        /// <summary>
        /// Admin uses this to obtain a late-bound resolver closure.
        /// Returns null when no bridge is installed or no instance is available.
        /// </summary>
        public static Func<object> TryResolve(Type contract)
        {
            var fn = ResolveOrNull;
            if (fn == null) return null;

            // Late-binding (resolve each invocation). Safer if lifetimes change.
            return () => fn(contract);
        }
    }
}

