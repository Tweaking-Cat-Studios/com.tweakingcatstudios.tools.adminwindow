using System;
using System.Linq;
using System.Text;
using com.tcs.tools.adminwindow.Core.Attributes;
using com.tcs.tools.adminwindow.Core.Registry;
using UnityEngine;

namespace com.tcs.tools.adminwindow.Core.Commands
{
    /// <summary>
    /// Built-in meta-commands for the Admin Window.
    /// </summary>
    public static class CoreAdminCommands
    {
        [AdminCommand("help", category:"Core", help:"Show help for a specific command. Usage: help <command>")]
        public static string Help(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return "Usage: help <commandName>";

            if (!AdminRegistry.TryGet(command, out var desc))
            {
                Debug.LogError($"Unknown command '{command}'. Try 'list' to see available commands.");
                return $"Unknown command '{command}'.";
            }

            var ps = desc.Parameters;
            var signature = ps.Length == 0
                ? "(no parameters)"
                : string.Join(", ", ps.Select(p => p.Optional ? $"[{p.Name}:{PrettyType(p.Type)}]" : $"{p.Name}:{PrettyType(p.Type)}"));

            var aliases = (desc.Aliases != null && desc.Aliases.Length > 0)
                ? $"\nAliases: {string.Join(", ", desc.Aliases)}"
                : string.Empty;

            return $"{desc.Name} — {desc.Help}\nCategory: {desc.Category}{aliases}\nParams: {signature}";
        }

        [AdminCommand("list", category:"Core", help:"List available commands. Usage: list [prefix]")]
        public static string List(string prefix = null)
        {
            var all = AdminRegistry.All();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                all = all.Where(c => c.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }

            var ordered = all.OrderBy(c => c.Category).ThenBy(c => c.Name).ToArray();
            if (ordered.Length == 0)
                return string.IsNullOrWhiteSpace(prefix)
                    ? "No commands found."
                    : $"No commands found with prefix '{prefix}'.";

            var sb = new StringBuilder();
            foreach (var c in ordered)
            {
                sb.AppendLine($"{c.Category} | {c.Name} - {c.Help}");
            }
            return sb.ToString();
        }

        static string PrettyType(Type t)
        {
            if (!t.IsGenericType) return t.Name;
            var def = t.GetGenericTypeDefinition();
            var args = t.GetGenericArguments();
            var name = def.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0) name = name.Substring(0, tick);
            return $"{name}<{string.Join(", ", args.Select(PrettyType))}>";
        }
    }
}
