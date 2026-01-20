using System;

namespace com.tcs.tools.adminwindow.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AdminCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Category { get; }
        public string Help { get; }
        public bool Dangerous { get; }
        public string[] Aliases { get; }

        public AdminCommandAttribute(
            string name,
            string category = "General",
            string help = "",
            bool dangerous = false,
            params string[] aliases)
        {
            Name = name; Category = category; Help = help; Dangerous = dangerous; Aliases = aliases ?? Array.Empty<string>();
        }
    }
}