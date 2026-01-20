using System;
using System.Collections.Generic;
using System.Text;

namespace com.tcs.tools.adminwindow.Core.Parsing
{
    public record CommandInvocation(string Name, List<string> Positional, Dictionary<string,string> Named, HashSet<string> Flags);

    public static class AdminParser
    {
        public static CommandInvocation Parse(string input)
        {
            var tokens = Tokenize(input);
            if (tokens.Count == 0) throw new Exception("Empty command.");
            var name = tokens[0];
            var positional = new List<string>();
            var named = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.StartsWith("--"))
                {
                    var eq = t.IndexOf('=');
                    if (eq > 2) named[t.Substring(2, eq-2)] = t[(eq+1)..];
                    else flags.Add(t.Substring(2));
                }
                else positional.Add(t);
            }
            return new(name, positional, named, flags);
        }

        static List<string> Tokenize(string s)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool q = false;
            for (int i=0;i<s.Length;i++)
            {
                var c = s[i];
                if (c=='"') { q = !q; continue; }
                if (char.IsWhiteSpace(c) && !q) { if (sb.Length>0){list.Add(sb.ToString()); sb.Clear();} }
                else sb.Append(c);
            }
            if (sb.Length>0) list.Add(sb.ToString());
            return list;
        }
    }
}