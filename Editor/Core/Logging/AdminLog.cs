using System;
using System.Collections.Generic;

namespace com.tcs.tools.adminwindow.Core.Logging
{
    public enum AdminLogLevel { Info, Warning, Error }

    public static class AdminLog
    {
        public struct Line { public DateTime Time; public AdminLogLevel Level; public string Text; }
        static readonly List<Line> _lines = new();

        public static IReadOnlyList<Line> Lines => _lines;

        public static void Info(string t) => Append(AdminLogLevel.Info, t);
        public static void Warn(string t) => Append(AdminLogLevel.Warning, t);
        public static void Error(string t) => Append(AdminLogLevel.Error, t);

        static void Append(AdminLogLevel lvl, string t) => _lines.Add(new Line{ Time = DateTime.Now, Level = lvl, Text = t });
        public static void Clear() => _lines.Clear();
    }
}