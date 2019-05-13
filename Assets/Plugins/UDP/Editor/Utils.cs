using System;
using System.Collections.Generic;

namespace UnityEngine.UDP.Editor
{
    public static class Utils
    {
        static Dictionary<string, Type> m_TypeCache = new Dictionary<string, Type>();
        private static string[] k_WhiteListedAssemblies = {"UnityEditor"};

        public static Type FindTypeByName(string name)
        {
            if (m_TypeCache.ContainsKey(name))
            {
                return m_TypeCache[name];
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (!AllowLookupForAssembly(assembly.FullName))
                    continue;

                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.FullName == name)
                        {
                            m_TypeCache[type.FullName] = type;
                            return type;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(string.Format(
                        "Count not fetch list of types from assembly {0} due to error: {1}", assembly.FullName,
                        e.Message));
                }
            }

            return null;
        }

        private static bool AllowLookupForAssembly(string name)
        {
            return Array.Exists(k_WhiteListedAssemblies, name.StartsWith);
        }
    }
}