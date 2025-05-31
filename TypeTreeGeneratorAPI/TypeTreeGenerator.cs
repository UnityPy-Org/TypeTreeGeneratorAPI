using AsmResolver.DotNet.Builder;
using AsmResolver.PE.Builder;
using AssetRipper.Primitives;
using AssetStudio;
using Cpp2IL.Core;
using Cpp2IL.Core.OutputFormats;
using Mono.Cecil;
using System.Diagnostics.CodeAnalysis;

namespace TypeTreeGeneratorAPI
{
    abstract public class TypeTreeGenerator
    {
        protected readonly string unityVersionString;
        protected readonly UnityVersion unityVersion;
        protected readonly MyAssemblyResolver resolver;
        protected readonly ReaderParameters readerParameters;

        public TypeTreeGenerator(string unityVersion)
        {
            this.unityVersionString = unityVersion;
            this.unityVersion = UnityVersion.Parse(unityVersionString);

            resolver = new MyAssemblyResolver();
            readerParameters = new ReaderParameters
            {
                InMemory = true,
                ReadWrite = false,
                AssemblyResolver = resolver
            };
        }


        abstract public List<Tuple<string, string>> GetMonoBehaviourDefinitions();
        abstract public List<TypeTreeNode>? GenerateTreeNodes(string assemblyName, string fullName);
        abstract public void LoadDLL(Stream dllStream);

        public void LoadDLL(byte[] dll)
        {
            using (var dllStream = new MemoryStream(dll))
            {
                LoadDLL(dllStream);
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public void LoadIL2CPP(byte[] assemblyData, byte[] metadataData)
        {
            Cpp2IlApi.Init();
            Cpp2IlApi.ConfigureLib(false);
            Cpp2IlApi.InitializeLibCpp2Il(assemblyData, metadataData, this.unityVersion, false);

            var dllGen = new AsmResolverDllOutputFormatEmpty();

            foreach (var dotAssemblyDefinition in dllGen.BuildAssemblies(Cpp2IlApi.CurrentAppContext))
            {
                var image = dotAssemblyDefinition.ManifestModule!.ToPEImage(new ManagedPEImageBuilder());
                if (image is null)
                    continue;
                var fileBuilder = new ManagedPEFileBuilder();
                using (var dllStream = new MemoryStream())
                {
                    fileBuilder.CreateFile(image).Write(dllStream);
                    dllStream.Position = 0;
                    LoadDLL(dllStream);
                }
            }
            Cpp2IlPluginManager.CallOnFinish();
        }

        protected static bool IsMonoBehaviour(TypeDefinition type)
        {
            while (type != null)
            {
                if (type.BaseType == null)
                    return false;
                if (type.BaseType.FullName == "UnityEngine.MonoBehaviour")
                    return true;
                try
                {
                    // Resolve the base type to continue up the hierarchy
                    type = type.BaseType.Resolve();
                }
                catch
                {
                    // If we can't resolve, break out
                    break;
                }
            }
            return false;
        }

    }
}
