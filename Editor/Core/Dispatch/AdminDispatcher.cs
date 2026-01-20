using System;
using com.tcs.tools.adminwindow.Core.Binding;
using com.tcs.tools.adminwindow.Core.Parsing;
using com.tcs.tools.adminwindow.Core.Registry;

namespace com.tcs.tools.adminwindow.Core.Dispatch
{
    public static class AdminDispatcher
    {
        public static object Execute(CommandInvocation inv)
        {
            if (!AdminRegistry.TryGet(inv.Name, out var desc))
                throw new Exception($"Unknown command '{inv.Name}'");

            var target = desc.Method.IsStatic ? null : desc.TargetResolver?.Invoke();
            if (!desc.Method.IsStatic && target == null)
                throw new Exception($"Target instance for '{desc.Name}' not found.");

            var pis = desc.Method.GetParameters();
            var argv = new object[pis.Length];
            int pos = 0;

            for (int i = 0; i < pis.Length; i++)
            {
                var p = pis[i];
                string source = null;

                if (inv.Named.TryGetValue(p.Name, out var named))
                {
                    source = named;
                }
                else if (p.ParameterType.IsGenericType &&
                         p.ParameterType.GetGenericTypeDefinition() == typeof(EntitySet<>))
                {
                    // Peek at the next positional token; only consume it if it looks like a target spec
                    var elemType = p.ParameterType.GetGenericArguments()[0];
                    if (pos < inv.Positional.Count &&
                        EntitySetBinder.LooksLikeTargetSpec(inv.Positional[pos], elemType))
                    {
                        source = inv.Positional[pos++];
                    }
                    else
                    {
                        source = "selection"; // default without consuming a positional token
                    }
                }
                else if (pos < inv.Positional.Count)
                {
                    source = inv.Positional[pos++];
                }

                if (source == null)
                {
                    if (p.HasDefaultValue) argv[i] = p.DefaultValue;
                    else throw new Exception($"Missing required parameter '{p.Name}'.");
                }
                else
                {
                    argv[i] = BinderChain.Bind(source, p.ParameterType);
                }
            }

            return desc.Method.Invoke(target, argv);
        }
    }
}