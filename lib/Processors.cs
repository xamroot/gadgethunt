using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace gadgethunt.lib
{
    internal static class Processors
    {
        internal static bool DllLoaderFromDiscoveredPath(
            BlockingCollection<string> inputFilePathsCollection,
            BlockingCollection<Type> outputLoadedTypesCollection
        )
        {
            foreach (var dllFilePath in inputFilePathsCollection.GetConsumingEnumerable())
            {
                List<Type> loadedTypes = DllUtilities.GetTypesFromDll(dllFilePath);
                foreach (var type in loadedTypes) { outputLoadedTypesCollection.Add(type); }
            }
            outputLoadedTypesCollection.CompleteAdding();
            return true;
        }

        internal static bool FilterTypes(
            BlockingCollection<Type> discoverdTypes,
            BlockingCollection<Type> filteredTypes
        )
        {
            foreach (var item in discoverdTypes.GetConsumingEnumerable())
            {
                // add each discovered type to stage2
                // add filter capabilities here eventually lol
                //if (Filterers.HasMethodWithTypeConverterAttribute(item))
                if (true)
                {
                    filteredTypes.Add(item);
                }
            }

            filteredTypes.CompleteAdding();
            return true;
        }

        internal static bool FuzzDeserialization(
            BlockingCollection<Type> inputTypes,
            BlockingCollection<string> outputStage
        )
        {
            foreach (var type in inputTypes.GetConsumingEnumerable())
            {
                string output_text = $"{type.FullName}\n";
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var prop in props)
                {
                    output_text += $"|\t{prop.PropertyType}, {prop.Name}\n";
                }

                string serial = SerialUtils.BuildSerializedPayload(type);
                outputStage.Add(serial);
            }

            outputStage.CompleteAdding();

            return true;
        }


        internal static bool OutputReader(
            BlockingCollection<string> outputStage,
            Queue<string> outputQueue
        )
        {
            foreach (string output in outputStage.GetConsumingEnumerable())
            {
                outputQueue.Enqueue(output);
                Console.WriteLine(output);

                outputQueue.Clear();
            }
            return true;
        }

    }
}
