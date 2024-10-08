using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace UnityEditor.Networking
{
    internal class WeaverRunner
    {
        [InitializeOnLoadMethod]
        static void OnInitializeOnLoad()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        internal static void OnCompilationFinished(string targetAssembly, CompilerMessage[] messages)
        {
            const string k_HlapiRuntimeAssemblyName = "com.unity.multiplayer-hlapi.Runtime";

            // Do nothing if there were compile errors on the target
            if (messages.Length > 0)
            {
                foreach (var msg in messages)
                {
                    if (msg.type == CompilerMessageType.Error)
                    {
                        return;
                    }
                }
            }

            // Should not run on the editor only assemblies
            if (targetAssembly.Contains("-Editor") || targetAssembly.Contains(".Editor"))
            {
                return;
            }

            // Should not run on own assembly or Unity assemblies
            if (targetAssembly.Contains("com.unity") || Path.GetFileName(targetAssembly).StartsWith("Unity"))
            {
                return;
            }

            var scriptAssembliesPath = Application.dataPath + "/../" + Path.GetDirectoryName(targetAssembly);

            string unityEngine = "";
            string unetAssemblyPath = "";
            var outputDirectory = scriptAssembliesPath;
            var assemblyPath = targetAssembly;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool usesUnet = false;
            bool foundThisAssembly = false;
            HashSet<string> depenencyPaths = new HashSet<string>();
            foreach (var assembly in assemblies)
            {
                // Find the assembly currently being compiled from domain assembly list and check if it's using unet
                if (assembly.GetName().Name == Path.GetFileNameWithoutExtension(targetAssembly))
                {
                    foundThisAssembly = true;
                    foreach (var dependency in assembly.GetReferencedAssemblies())
                    {
                        // Since this assembly is already loaded in the domain this is a no-op and retuns the
                        // already loaded assembly
                        var location = Assembly.Load(dependency).Location;
                        depenencyPaths.Add(Path.GetDirectoryName(location));
                        if (dependency.Name.Contains(k_HlapiRuntimeAssemblyName))
                        {
                            usesUnet = true;
                        }
                    }
                }
                try
                {
                    if (assembly.Location.Contains("UnityEngine.CoreModule"))
                    {
                        unityEngine = assembly.Location;
                    }
                    if (assembly.Location.Contains(k_HlapiRuntimeAssemblyName))
                    {
                        unetAssemblyPath = assembly.Location;
                    }
                }
                catch (NotSupportedException)
                {
                    // in memory assembly, can't get location
                }
            }

            if (!foundThisAssembly)
            {
                // Target assembly not found in current domain, trying to load it to check references
                // will lead to trouble in the build pipeline, so lets assume it should go to weaver.
                // Add all assemblies in current domain to dependency list since there could be a
                // dependency lurking there (there might be generated assemblies so ignore file not found exceptions).
                // (can happen in runtime test framework on editor platform and when doing full library reimport)
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        if (!(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder))
                        {
                            var location = Assembly.Load(assembly.GetName().Name).Location;
                            if (!string.IsNullOrEmpty(location))
                                depenencyPaths.Add(Path.GetDirectoryName(location));
                        }
                    }
                    catch (FileNotFoundException) { }
                }
                usesUnet = true;
            }

            if (!usesUnet)
            {
                return;
            }

            if (string.IsNullOrEmpty(unityEngine))
            {
                Debug.LogError("Failed to find UnityEngine assembly");
                return;
            }

            if (string.IsNullOrEmpty(unetAssemblyPath))
            {
                Debug.LogError("Failed to find hlapi runtime assembly");
                return;
            }

            Unity.UNetWeaver.Program.Process(unityEngine, unetAssemblyPath, outputDirectory, new[] { assemblyPath }, depenencyPaths.ToArray(), (value) => { Debug.LogWarning(value); }, (value) => { Debug.LogError(value); });
        }
    }
}
