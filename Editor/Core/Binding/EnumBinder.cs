using System;

namespace com.tcs.tools.adminwindow.Core.Binding
{
    public sealed class EnumBinder : IArgBinder
    {
        public bool CanBind(Type t) => t.IsEnum;
        public object Bind(string s, Type t) => Enum.Parse(t, s, true);
    }
}