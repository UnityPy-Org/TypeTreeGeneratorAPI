#if ENABLE_IL2CPP
using AssetRipper.Primitives;
using Cpp2IL.Core;
using Cpp2IL.Core.Api;
using Cpp2IL.Core.OutputFormats;
using Cpp2IL.Core.ProcessingLayers;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator
{
    public class Il2CppHandler
    {
        static private UnityVersion unityVersion;

        static Il2CppHandler()
        {
            Cpp2IlApi.Init();
            Cpp2IlApi.ConfigureLib(false);
        }

        public static void Initialize(byte[] assemblyData, byte[] metadataData, string unityVersionString)
        {
            unityVersion = UnityVersion.Parse(unityVersionString);
            Cpp2IlApi.InitializeLibCpp2Il(assemblyData, metadataData, unityVersion, false);
        }

        public static Dictionary<string, AsmResolver.DotNet.AssemblyDefinition> GenerateAssemblyDefinitions()
        {
            List<Cpp2IlProcessingLayer> processingLayers = [
                new AttributeAnalysisProcessingLayer(),
            ];

            foreach (Cpp2IlProcessingLayer cpp2IlProcessingLayer in processingLayers)
            {
                cpp2IlProcessingLayer.PreProcess(Cpp2IlApi.CurrentAppContext!, processingLayers);
            }

            foreach (Cpp2IlProcessingLayer cpp2IlProcessingLayer in processingLayers)
            {
                cpp2IlProcessingLayer.Process(Cpp2IlApi.CurrentAppContext!);
            }

            AsmResolverDllOutputFormat outputFormat = new AsmResolverDllOutputFormatEmpty();

            var assemblyResolver = new AssemblyResolver();
            foreach (var assembly in outputFormat.BuildAssemblies(Cpp2IlApi.CurrentAppContext!))
            {
                assemblyResolver.AddToCache(assembly.ManifestModule.Name, assembly);
                foreach (var module in assembly.Modules)
                {
                    module.MetadataResolver = new AsmResolver.DotNet.DefaultMetadataResolver(assemblyResolver);
                }
            }
            return assemblyResolver.assemblyDefinitions;
        }

      
    }
}
#endif
