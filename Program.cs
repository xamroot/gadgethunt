using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

public class LambdaHolder
{
    public Func<int, int> MyLambda = x => x * x;
}

public class TestLol
{
    public string xxx = "";
    public int yyy;
    public TestLol() { xxx = "hello!"; }
}

public class Program
{

    public static bool HasParseMethodReturningBool(Type type)
    {
        var parseMethod = type.GetMethod("Parse", new[] { typeof(string) });
        return parseMethod != null &&
               parseMethod.ReturnType == type &&
               parseMethod.GetParameters().Length == 1 &&
               parseMethod.GetParameters()[0].ParameterType == typeof(string);
    }

    public static bool HasMethodWithTypeConverterAttribute(Type type)
    {
        return type.GetCustomAttributes(typeof(TypeConverterAttribute), false).Length > 0;
    }


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
        catch (Exception e) {
        
        }

        if (wasAssemblyLoaded)
        {
            if (assembly != null) {
                try
                {

                    foreach (Type type in assembly.GetTypes())
                    {
                        types.Add(type);
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

    public static List<string> GetDllFilePaths(string path)
    {
        List<string> dllFiles = new List<string>();
        dllFiles.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));
        return dllFiles;
    }

    public static string GetNetVersion(string path, string version)
    {
        List<string> dllFiles = new List<string>();
        List<string> netVersions = Directory.GetDirectories(path).ToList<string>().Where(d => new DirectoryInfo(d).Name.StartsWith(version)).ToList();
        if (netVersions.Count > 0)
        {
            return netVersions.OrderByDescending(s => int.Parse(s.Split("\\").Last().Replace(".",""))).First();

        }
        else
        {
            throw new Exception($"No .NET versions found for the specified version\"{version}\"");
        }
    }

    public static bool HasParameterlessConstructor(ConstructorInfo[] constructors)
    {
        foreach (var constructor in constructors)
        {
            if (constructor.GetParameters().Length == 0)
                return true;
        }
        return false;
    }

    public static string BuildSerializedPayload(Type type)
    {
        Dictionary<string, object> kvp = new Dictionary<string, object>();

        string serial = "ERR";

        // build ctors
        // REQUIRED BY NEWTONSOFT DESERIALIZATION
        // we need a type which has a parameterless constructor
        // is there a 
        if (HasParameterlessConstructor(type.GetConstructors()))
        {


            // build props

            kvp["$type"] = type.AssemblyQualifiedName;
            try
            {
                object o1 = Activator.CreateInstance(type);

                foreach (var prop in type.GetProperties())
                {
                    // check that the property has a setter
                    if (prop?.GetSetMethod() != null)
                    {
                        object value = new object();
                        bool wasValueSet = false;
                        switch (prop.PropertyType.ToString())
                        {
                            case "System.String":
                                value = "test string";
                                wasValueSet = true;
                                break;
                            case "System.Boolean":
                                value = true;
                                wasValueSet = true;
                                break;
                            default:
                                break;
                        }
                        if (wasValueSet)
                        {
                           prop.SetValue(o1, value);
                        }
                    }
                }

                string s1 = JsonConvert.SerializeObject(o1, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });

                Console.WriteLine($"\nWorking on the type: {type.FullName}");
                Console.WriteLine(s1);

                object obj = JsonConvert.DeserializeObject(s1, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });

                Console.WriteLine(obj.GetType().AssemblyQualifiedName);
                serial = s1;


            }
            catch (Exception e)
                {
                Console.WriteLine("ERR");
                Console.WriteLine(e.ToString());

            }
            //serial = @"{""Site"":{},""Container"":{},""DesignMode"":true,""$type"":""System.ComponentModel.MarshalByValueComponent, System.ComponentModel.TypeConverter""}";
            //serial = @"{""$type"":""System.ComponentModel.MarshalByValueComponent, System.ComponentModel.TypeConverter"",""Site"":null,""Container"":null,""DesignMode"":false}";
            //serial = @"{""$type"":""TestLol, gadgethunt"",""xxx"":""hello!"",""yyy"":0}";
            //Console.WriteLine(JsonConvert.SerializeObject(new TestLol(), new JsonSerializerSettings() { TypeNameHandling=TypeNameHandling.All}));
        }

        return $"{serial}";
    }

    public static List<string> GetAllDllFiles(string targetVersion)
    {
        List<string> dotnetRefPaths = new List<string>()
        {
            @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App",
            @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App",
            @"C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App"
        };

        List<string> dllPaths = new List<string>();
        foreach (string dotnetRefPath in dotnetRefPaths)
        {
            string dllContainerPath = GetNetVersion(dotnetRefPath, targetVersion);
            dllPaths.AddRange(GetDllFilePaths(dllContainerPath));
        }

        return dllPaths;
    }

    public static void Main(string[] args)
    {

        LambdaHolder instance = new LambdaHolder();
        //Console.WriteLine(instance.MyLambda(5)); // Output: 25

        string serial = JsonConvert.SerializeObject(instance, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        Console.WriteLine(serial);
        
        Console.ReadLine();
        return;
        List<string> dllFiles = GetAllDllFiles("8");

        Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        ConcurrentBag<Type> results = new ConcurrentBag<Type>();
        ConcurrentBag<Type> discoveredTypes = new ConcurrentBag<Type>();

        BlockingCollection<string> stage1 = new BlockingCollection<string>(boundedCapacity: 5);
        BlockingCollection<Type> stage2 = new BlockingCollection<Type>(boundedCapacity: 5);
        BlockingCollection<Type> stage3 = new BlockingCollection<Type>(boundedCapacity: 5);



        /*

        // ConcurrentBag to hold the results from each thread
        ConcurrentBag<Type> results = new ConcurrentBag<Type>();
        ConcurrentBag<Type> discoveredTypes = new ConcurrentBag<Type>();

        Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();




        // Execute GetTypesFromDll in parallel
        Parallel.ForEach(dllPaths, dllPath =>
        {
            var types = GetTypesFromDll(dllPath);
            foreach(var type in types) { discoveredTypes.Add(type); }
        });

        // Execute GetTypesFromDll in parallel
        Parallel.ForEach(discoveredTypes, discoType =>
        {



            if (HasMethodWithTypeConverterAttribute(discoType))
            {
                results.Add(discoType);
            }
        });

        // Optional: Process the results
        foreach (var type in results)
        {
            Console.WriteLine(type.FullName);
        }
        */

        // Create the collections for passing data between stages


        // Start the first stage 
        Task.Factory.StartNew(() =>
        {
            foreach (var dllFilePath in stage1.GetConsumingEnumerable())
            {
                List<Type> loadedTypes = GetTypesFromDll(dllFilePath);
                foreach (var type in loadedTypes) { stage2.Add(type); }
            }
            stage2.CompleteAdding();
        });

        // Start the second stage
        Task.Factory.StartNew(() =>
        {
            foreach (var item in stage2.GetConsumingEnumerable())
            {
                // add each discovered type to stage2
                if (HasMethodWithTypeConverterAttribute(item))
                {
                    stage3.Add(item);
                }
            }

            stage3.CompleteAdding();
            Console.WriteLine("COMPLETED STAGE3");
        });

        // Start the results stage
        Task.Factory.StartNew(() =>
            {
            foreach (var type in stage3.GetConsumingEnumerable())
            {
                string output_text = $"{type.FullName}\n";
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach(var prop in props)
                    {
                        output_text += $"|\t{prop.PropertyType}, {prop.Name}\n";
                    }

                string serial = BuildSerializedPayload(type);
                if (serial != "ERR")
                {
                    Console.WriteLine(serial);

                }

                }
        });

        // Add types from the currently loaded assemblies
        Parallel.ForEach(loadedAssemblies, asm =>
        {
            Type[] loadedTypes = asm.GetTypes();
            foreach (var type in loadedTypes) { stage2.Add(type); }
        });

        foreach (string x in dllFiles)
        {
            stage1.Add(x);
        }
        stage1.CompleteAdding();

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();

    }
}
