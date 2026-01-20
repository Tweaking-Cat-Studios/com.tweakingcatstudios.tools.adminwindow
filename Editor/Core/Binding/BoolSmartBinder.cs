using System;
using System.Linq;

namespace com.tcs.tools.adminwindow.Core.Binding
{
    public sealed class BoolSmartBinder : IArgBinder
    {
        static readonly string[] True = { "1","true","on","yes","y" };
        static readonly string[] False = { "0","false","off","no","n" };
        public bool CanBind(Type t) => t == typeof(bool);
        public object Bind(string s, Type t)
        {
            if (True.Contains(s, StringComparer.OrdinalIgnoreCase)) return true;
            if (False.Contains(s, StringComparer.OrdinalIgnoreCase)) return false;
            return bool.Parse(s);
        }
    }
}