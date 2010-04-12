using System.Collections.Generic;
using System.Linq;
using IronPython.Runtime.Types;
using Microsoft.Scripting.Hosting;

namespace Editor.Extensions
{
    internal static class ScriptScopeExtensionMethods
    {
        public static IEnumerable<PythonType> GetImplementationsOf<T>(this ScriptScope scope)
        {
            return scope.GetItems()
                .Select(kvp => kvp.Value)
                .OfType<PythonType>()
                .Where(pythonType => typeof (T).IsAssignableFrom(pythonType.__clrtype__()))
                .Where(pythonType => !pythonType.__clrtype__().IsAbstract);
        }
    }
}