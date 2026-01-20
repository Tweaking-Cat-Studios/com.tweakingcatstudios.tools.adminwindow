#if UNITY_EDITOR
using System;
using UnityEngine;

namespace com.tcs.tools.adminwindow.Core.Registry
{
    [Serializable]
    internal class CommandEntryDTO
    {
        public string name;
        public string[] aliases;
        public string category;
        public string help;
        public bool dangerous;

        public string declaringTypeAQN; // AssemblyQualifiedName
        public string methodName;
        public bool isStatic;

        public ParameterDTO[] parameters;
    }

    [Serializable]
    internal class ParameterDTO
    {
        public string name;
        public string typeAQN;
        public bool optional;
        public string defaultValueJson; // optional; not used currently
    }

    internal class AdminRegistryCacheAsset : ScriptableObject
    {
        public int version = 1;
        public CommandEntryDTO[] entries = Array.Empty<CommandEntryDTO>();
        public string assembliesHash;
    }
}
#endif
