#if LEGACY
using System;
using System.Reflection;

namespace KorenResourcePack
{
    internal static partial class KeyViewer
    {
        // Legacy ADOFAI ships SFB.StandaloneFileBrowser in Assembly-CSharp-firstpass
        // (RDEditorUtils.ShowFileSelector wraps it). RDEditorUtils itself early-outs
        // when no scnGame level is loaded, so we can't reuse that wrapper from the
        // settings menu — call StandaloneFileBrowser directly via reflection instead.
        private static string PickPresetJsonFileImpl()
        {
            Type browserType = LegacyReflection.ResolveType(
                "SFB.StandaloneFileBrowser, Assembly-CSharp-firstpass",
                "SFB.StandaloneFileBrowser");
            if (browserType == null)
            {
                Main.mod?.Logger?.Log("[KeyViewer] SFB.StandaloneFileBrowser not found in any loaded assembly; preset import unavailable.");
                return null;
            }

            Type extensionFilterType = LegacyReflection.ResolveType(
                "SFB.ExtensionFilter, Assembly-CSharp-firstpass",
                "SFB.ExtensionFilter");

            MethodInfo[] methods = browserType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "OpenFilePanel") continue;
                if (method.ReturnType != typeof(string[])) continue;

                object[] args;
                if (!TryBuildOpenFilePanelArgs(method, extensionFilterType, out args)) continue;

                try
                {
                    object result = method.Invoke(null, args);
                    string[] paths = result as string[];
                    if (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                        return paths[0];
                    return null;
                }
                catch (Exception ex)
                {
                    Main.mod?.Logger?.Log("[KeyViewer] SFB OpenFilePanel invoke failed: " + ex.Message);
                }
            }

            Main.mod?.Logger?.Log("[KeyViewer] No usable SFB.OpenFilePanel overload found.");
            return null;
        }

        private static bool TryBuildOpenFilePanelArgs(MethodInfo method, Type extensionFilterType, out object[] args)
        {
            ParameterInfo[] parameters = method.GetParameters();
            args = new object[parameters.Length];
            int stringIndex = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                if (type == typeof(string))
                {
                    args[i] = stringIndex == 0 ? "Select DM Note preset"
                            : stringIndex == 1 ? ""
                            : stringIndex == 2 ? "json"
                            : "";
                    stringIndex++;
                }
                else if (type == typeof(bool))
                {
                    args[i] = false;
                }
                else if (type == typeof(string[]))
                {
                    args[i] = new[] { "json" };
                }
                else if (type.IsArray && extensionFilterType != null && type.GetElementType() == extensionFilterType)
                {
                    args[i] = CreateExtensionFilterArray(extensionFilterType);
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
            object json = CreateExtensionFilter(filterType, "JSON Preset", new[] { "json" });
            object all  = CreateExtensionFilter(filterType, "All Files",   new[] { "*" });
            if (json == null) return Array.CreateInstance(filterType, 0);

            int len = all != null ? 2 : 1;
            Array filters = Array.CreateInstance(filterType, len);
            filters.SetValue(json, 0);
            if (all != null) filters.SetValue(all, 1);
            return filters;
        }

        private static object CreateExtensionFilter(Type filterType, string name, string[] extensions)
        {
            ConstructorInfo[] constructors = filterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < constructors.Length; i++)
            {
                ConstructorInfo ctor = constructors[i];
                ParameterInfo[] parameters = ctor.GetParameters();
                if (parameters.Length != 2 || parameters[0].ParameterType != typeof(string)) continue;

                if (parameters[1].ParameterType == typeof(string[]))
                    return ctor.Invoke(new object[] { name, extensions });

                if (parameters[1].ParameterType.IsArray && parameters[1].ParameterType.GetElementType() == typeof(string))
                    return ctor.Invoke(new object[] { name, extensions });
            }

            return null;
        }
    }
}
#endif
