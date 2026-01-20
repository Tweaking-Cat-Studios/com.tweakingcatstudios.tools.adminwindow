using System;

namespace com.tcs.tools.adminwindow.Core.Binding
{
    public interface IArgBinder
    {
        bool CanBind(Type t);
        object Bind(string source, Type t);
    }
}