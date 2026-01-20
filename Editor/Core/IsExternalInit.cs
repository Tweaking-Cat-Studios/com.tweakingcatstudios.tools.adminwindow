// Polyfill for C# 9 init-only setters and records in older Unity/.NET targets
// This allows usage of 'init' and 'record' features in editor assemblies.
// Safe to include in Editor assembly as Unity may not define this type for .NET 4.x Equivalent.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}