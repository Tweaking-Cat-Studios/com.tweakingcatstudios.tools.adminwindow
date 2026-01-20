#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using TCS.Admin;

namespace com.tcs.tools.adminwindow.Core.Registry
{
    // Editor-only augmentation that provides asset-based caching and rebuild controls.
    public static partial class AdminRegistry
    {
        static bool _attemptedHydrateFromAsset;

        [InitializeOnLoadMethod]
        private static void PrewarmFromAsset()
        {
            if (_built) return;
            if (TryHydrateFromAsset())
            {
                _built = true;
                return;
            }
            // Do NOT auto-rebuild here to avoid hitches near Play Mode.
            // Developers can use the Refresh action to rebuild explicitly.
        }

        [InitializeOnLoadMethod]
        private static void HookPlayPrewarm()
        {
            // Ensure single subscription across reloads
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // Prewarm just before entering Play (still in Edit Mode context)
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                if (_built) return;
                if (TryHydrateFromAsset())
                {
                    _built = true;
                }
                // Intentionally do not trigger reflection rebuild here
            }
        }

        [MenuItem("TCS/Admin/Refresh Admin Registry")]
        public static void Rebuild()
        {
            BuildByReflection();
            SaveToAsset();
        }

        [MenuItem("TCS/Admin/Clear Admin Registry Cache")]        
        public static void ClearCache()
        {
            var asset = AdminRegistryCacheIO.LoadOrCreate();
            asset.entries = Array.Empty<CommandEntryDTO>();
            asset.assembliesHash = null;
            AdminRegistryCacheIO.Save(asset);
            _byKey.Clear();
            _built = false;
        }

        private static void SaveToAsset()
        {
            var asset = AdminRegistryCacheIO.LoadOrCreate();
            asset.entries = _byKey.Values.Distinct().Select(ToDTO).ToArray();
            asset.assembliesHash = AdminRegistryCacheIO.ComputeAssembliesHash();
            AdminRegistryCacheIO.Save(asset);
        }

        private static bool TryHydrateFromAsset()
        {
            if (_attemptedHydrateFromAsset) return _byKey.Count > 0;
            _attemptedHydrateFromAsset = true;

            var asset = AdminRegistryCacheIO.LoadOrCreate();
            if (asset.entries == null || asset.entries.Length == 0)
                return false;

            // Note: Do not strictly fail on assemblies hash mismatch to avoid unnecessary rebuilds.
            // We will attempt to hydrate and gracefully skip entries that no longer resolve.

            _byKey.Clear();
            int success = 0;
            foreach (var e in asset.entries)
            {
                if (!TryHydrate(e, out var desc)) continue;
                _byKey[e.name] = desc;
                foreach (var a in desc.Aliases ?? Array.Empty<string>()) _byKey[a] = desc;
                success++;
            }
            return success > 0;
        }

        private static CommandEntryDTO ToDTO(CommandDescriptor d)
        {
            return new CommandEntryDTO
            {
                name = d.Name,
                aliases = d.Aliases ?? Array.Empty<string>(),
                category = d.Category,
                help = d.Help,
                dangerous = d.Dangerous,
                declaringTypeAQN = d.Method.DeclaringType.AssemblyQualifiedName,
                methodName = d.Method.Name,
                isStatic = d.Method.IsStatic,
                parameters = d.Parameters?.Select(p => new ParameterDTO
                {
                    name = p.Name,
                    typeAQN = p.Type.AssemblyQualifiedName,
                    optional = p.Optional,
                    defaultValueJson = null,
                }).ToArray() ?? Array.Empty<ParameterDTO>()
            };
        }

        private static bool TryHydrate(CommandEntryDTO e, out CommandDescriptor desc)
        {
            desc = null;
            var type = Type.GetType(e.declaringTypeAQN);
            if (type == null) return false;

            var paramTypes = (e.parameters ?? Array.Empty<ParameterDTO>())
                .Select(p => Type.GetType(p.typeAQN))
                .ToArray();
            if (paramTypes.Any(t => t == null)) return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var method = type.GetMethod(e.methodName, flags, null, paramTypes, null);
            if (method == null) return false;

            var resolver = method.IsStatic ? TargetResolvers.Static() : TargetResolvers.ForType(type);
            var pds = (e.parameters ?? Array.Empty<ParameterDTO>())
                .Select(p => new ParameterDescriptor(p.name, Type.GetType(p.typeAQN), p.optional, null, ""))
                .ToArray();

            desc = new CommandDescriptor(
                e.name,
                e.aliases ?? Array.Empty<string>(),
                e.category,
                e.help,
                e.dangerous,
                method,
                resolver,
                pds);
            return true;
        }
    }
}
#endif
