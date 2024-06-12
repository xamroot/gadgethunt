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
using System.Runtime.InteropServices;
using gadgethunt.lib;

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
        string netVersion = "8";
        int pipelineCapacity = 10;
        List<string> dllFiles = GetAllDllFiles(netVersion);
        string[] dummy = new string[5];
        dummy[1] = "hello!";

        Console.WriteLine(JsonConvert.SerializeObject(dummy, new JsonSerializerSettings { TypeNameHandling=TypeNameHandling.All}));
        return;

        BlockingCollection<string> stage1 = new BlockingCollection<string>(boundedCapacity: pipelineCapacity);
        BlockingCollection<Type> stage2 = new BlockingCollection<Type>(boundedCapacity: pipelineCapacity);
        BlockingCollection<Type> stage3 = new BlockingCollection<Type>(boundedCapacity: pipelineCapacity);
        BlockingCollection<string> outputStage = new BlockingCollection<string>(boundedCapacity: pipelineCapacity);
        Queue<string> outputQueue = new Queue<string>();
        bool finishedFlag = false;

        // Start the first stage 
        Task.Factory.StartNew(() =>
        {
            Processors.DllLoaderFromDiscoveredPath(stage1, stage2);
        });

        // Start the second stage
        Task.Factory.StartNew(() =>
        {
            Processors.FilterTypes(stage2, stage3);
        });

        // Start the serializer stage
        Task.Factory.StartNew(() =>
        {
            Processors.FuzzDeserialization(stage3, outputStage);
        });

        // Start the thread safe message handling queue for proper output stuffs
        Task.Factory.StartNew(() =>
        {
            Processors.OutputReader(outputStage, outputQueue);
            finishedFlag = true;
        });

        // Add types from the currently loaded assemblies
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        IEnumerable<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(predicate: a => !a.FullName.StartsWith("gadgethunt"));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
