using System;
using System.Globalization;

namespace com.tcs.tools.adminwindow.Core.Binding
{
    public sealed class PrimitiveBinder : IArgBinder
    {
        public bool CanBind(Type t) =>
            t.IsPrimitive || t == typeof(string) || t == typeof(decimal);
        public object Bind(string s, Type t) =>
            Convert.ChangeType(s, t, CultureInfo.InvariantCulture);
    }
}