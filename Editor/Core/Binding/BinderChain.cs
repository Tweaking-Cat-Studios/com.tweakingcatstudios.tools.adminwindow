using System;

namespace com.tcs.tools.adminwindow.Core.Binding
{
    public static class BinderChain
    {
        // Make sure this is the order (EntitySet BEFORE primitives)
        static readonly IArgBinder[] _binders = {
            new EntitySetBinder(),
            // (your other special binders e.g., SquareBinder if you kept chess)
            new BoolSmartBinder(),
            new EnumBinder(),
            new PrimitiveBinder()
        };

        public static object Bind(string source, Type t)
        {
            foreach (var b in _binders) if (b.CanBind(t)) return b.Bind(source, t);
            throw new Exception($"No binder for type {t.Name}");
        }
    }
}