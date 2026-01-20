using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using com.tcs.tools.adminwindow.Core.Attributes;
using TCS.Admin;

namespace com.tcs.tools.adminwindow.Core.Registry
{
    public record ParameterDescriptor(string Name, Type Type, bool Optional, object DefaultValue, string Help);
    public record CommandDescriptor(
        string Name, string[] Aliases, string Category, string Help, bool Dangerous,
        MethodInfo Method, System.Func<object> TargetResolver, ParameterDescriptor[] Parameters);

    public static partial class AdminRegistry
    {
        internal static readonly Dictionary<string, CommandDescriptor> _byKey = new(StringComparer.OrdinalIgnoreCase);
        internal static bool _built;

        public static IReadOnlyDictionary<string, CommandDescriptor> Commands
        {
            get
            {
                EnsureBuilt();
                return _byKey;
            }
        }

        static void EnsureBuilt()
        {
            if (_built) return;
#if UNITY_EDITOR
            // Avoid heavy reflection scans during Play Mode to prevent hitches.
            if (Application.isPlaying)
                return; // rely on editor hydration; otherwise commands remain empty until Refresh

            // Safe to build by reflection while in Edit Mode (e.g., when opening the Admin Window)
            BuildByReflection();
#endif
        }

        internal static void BuildByReflection()
        {
            _byKey.Clear();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                {
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        var attr = m.GetCustomAttribute<AdminCommandAttribute>();
                        if (attr == null) continue;

                        var resolver = m.IsStatic
                            ? TargetResolvers.Static()
                            : TargetResolvers.ForType(t);

                        var pds = m.GetParameters().Select(p =>
                            new ParameterDescriptor(p.Name, p.ParameterType, p.HasDefaultValue, p.HasDefaultValue ? p.DefaultValue : null, "")).ToArray();

                        var desc = new CommandDescriptor(
                            attr.Name, attr.Aliases ?? Array.Empty<string>(), attr.Category, attr.Help, attr.Dangerous, m, resolver, pds);

                        _byKey[attr.Name] = desc;
                        foreach (var a in desc.Aliases) _byKey[a] = desc;
                    }
                }
            }
            _built = true;
        }

        public static bool TryGet(string name, out CommandDescriptor desc)
        {
            EnsureBuilt();
            return _byKey.TryGetValue(name, out desc);
        }

        public static IEnumerable<CommandDescriptor> All()
        {
            EnsureBuilt();
            return _byKey.Values.Distinct();
        }
    }
}
