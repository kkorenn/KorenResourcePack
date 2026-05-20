#if LEGACY
using System;
using System.Reflection;

namespace KorenResourcePack
{
    internal static partial class KeyViewer
    {
        private static string PickPresetJsonFileImpl()
        {
            Type browserType = LegacyReflection.ResolveType(
                "SFB.StandaloneFileBrowser",
                "StandaloneFileBrowser.StandaloneFileBrowser",
                "StandaloneFileBrowser");
            if (browserType == null) return null;

            MethodInfo[] methods = browserType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "OpenFilePanel") continue;

                object[] args;
                if (!TryBuildOpenFilePanelArgs(method, out args)) continue;

                object result = method.Invoke(null, args);
                string[] paths = result as string[];
                if (paths != null)
                    return paths.Length > 0 && !string.IsNullOrEmpty(paths[0]) ? paths[0] : null;

                string path = result as string;
                if (!string.IsNullOrEmpty(path)) return path;
            }

            return null;
        }

        private static bool TryBuildOpenFilePanelArgs(MethodInfo method, out object[] args)
        {
            ParameterInfo[] parameters = method.GetParameters();
            args = new object[parameters.Length];
            int stringIndex = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                if (type == typeof(string))
                {
                    args[i] = stringIndex++ == 0 ? "Select DM Note preset" : "";
                }
                else if (type == typeof(bool))
                {
                    args[i] = false;
                }
                else if (type == typeof(string[]))
                {
                    args[i] = new[] { "json" };
                }
                else if (type.IsArray)
                {
                    args[i] = CreateExtensionFilterArray(type.GetElementType());
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static Array CreateExtensionFilterArray(Type filterType)
        {
            if (filterType == null) return Array.CreateInstance(typeof(object), 0);

            object json = CreateExtensionFilter(filterType, "JSON Preset", new[] { "json" });
            object all = CreateExtensionFilter(filterType, "All Files", new[] { "*" });
            if (json == null || all == null) return Array.CreateInstance(filterType, 0);

            Array filters = Array.CreateInstance(filterType, 2);
            filters.SetValue(json, 0);
            filters.SetValue(all, 1);
            return filters;
        }

        private static object CreateExtensionFilter(Type filterType, string name, string[] extensions)
        {
            ConstructorInfo[] constructors = filterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < constructors.Length; i++)
            {
                ConstructorInfo constructor = constructors[i];
                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string))
                {
                    if (parameters[1].ParameterType == typeof(string[]))
                        return constructor.Invoke(new object[] { name, extensions });
                    if (parameters[1].ParameterType == typeof(string))
                        return constructor.Invoke(new object[] { name, extensions.Length > 0 ? extensions[0] : "" });
                }
            }

            return null;
        }
    }
}
#endif
