using System;

namespace com.tcs.tools.adminwindow.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class CVarAttribute : Attribute
    {
        public string Key { get; }
        public string Category { get; }
        public string Help { get; }

        public CVarAttribute(string key, string category = "General", string help = "")
        { Key = key; Category = category; Help = help; }
    }
}