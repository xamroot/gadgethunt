using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace gadgethunt.lib
{
    internal class DllUtilities
    {
        public static List<Type> GetTypesFromDll(string dllPath)
        {
            var types = new List<Type>();
            Assembly? assembly = null;
            bool wasAssemblyLoaded = false;
            try
            {
                assembly = Assembly.LoadFrom(dllPath);
                wasAssemblyLoaded = true;
            }
            catch (Exception e)
            {

            }

            if (wasAssemblyLoaded)
            {
                if (assembly != null)
                {
                    try
                    {
                        Span<Type> typeSpan = CollectionsMarshal.AsSpan(assembly.GetTypes().ToList());
                        for (var i = 0; i < typeSpan.Length; ++i)
                        {
                            types.Add(typeSpan[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }

            return types;
        }
    }
}
