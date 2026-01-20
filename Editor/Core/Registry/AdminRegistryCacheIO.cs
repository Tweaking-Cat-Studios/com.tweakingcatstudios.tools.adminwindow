#if UNITY_EDITOR
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace com.tcs.tools.adminwindow.Core.Registry
{
    internal static class AdminRegistryCacheIO
    {
        private const string AssetPath = "Assets/TCSTools/AdminWindow/Editor/Core/Registry/AdminRegistryCache.asset";

        public static AdminRegistryCacheAsset LoadOrCreate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<AdminRegistryCacheAsset>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AdminRegistryCacheAsset>();
                EnsureFolderExists(System.IO.Path.GetDirectoryName(AssetPath));
                AssetDatabase.CreateAsset(asset, AssetPath);
                AssetDatabase.SaveAssets();
            }
            return asset;
        }

        public static void Save(AdminRegistryCacheAsset asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        public static string ComputeAssembliesHash()
        {
            var parts = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.FullName)
                .OrderBy(s => s)
                .ToArray();
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(string.Join("|", parts));
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private static void EnsureFolderExists(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return;
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            var path = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = path + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(path, parts[i]);
                path = next;
            }
        }
    }
}
#endif
