using AsmResolver.DotNet.Builder;
using AsmResolver.PE.Builder;
using AssetRipper.Primitives;
using AssetStudio;
using Cpp2IL.Core.OutputFormats;
using Cpp2IL.Core;
using Mono.Cecil;
using System.Diagnostics.CodeAnalysis;

namespace TypeTreeGeneratorAPI
{
    public class TypeTreeGenerator
    {
        private Dictionary<string, ModuleDefinition> moduleDic;
        private MyAssemblyResolver resolver;
        private ReaderParameters readerParameters;
        private string unityVersion;
        private SerializedTypeHelper serializedTypeHelper;

        public TypeTreeGenerator(string unityVersion)
        {
            this.unityVersion = unityVersion;
            moduleDic = new Dictionary<string, ModuleDefinition>();
            resolver = new MyAssemblyResolver();
            readerParameters = new ReaderParameters();
            readerParameters.InMemory = true;
            readerParameters.ReadWrite = false;
            readerParameters.AssemblyResolver = resolver;

            var arVersion = UnityVersion.Parse(unityVersion);
            serializedTypeHelper = new SerializedTypeHelper([arVersion.Major, arVersion.Minor, arVersion.Build, arVersion.TypeNumber]);
        }

        ~TypeTreeGenerator()
        {
            foreach (var pair in moduleDic)
            {
                pair.Value.Dispose();
            }
            moduleDic.Clear();
        }

        public void LoadDLL(byte[] dll)
        {
            using (var dllStream = new MemoryStream(dll))
            {
                LoadDLL(dllStream);
            }
        }

        public void LoadDLL(Stream dllStream)
        {
            var assembly = AssemblyDefinition.ReadAssembly(dllStream, readerParameters);
            resolver.Register(assembly);
            moduleDic.Add(assembly.MainModule.Name, assembly.MainModule);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public void LoadIL2CPP(byte[] assemblyData, byte[] metadataData)
        {
            Cpp2IlApi.Init();
            Cpp2IlApi.ConfigureLib(false);
            Cpp2IlApi.InitializeLibCpp2Il(assemblyData, metadataData, UnityVersion.Parse(unityVersion), false);

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

        public List<TypeDefinition> GetMonoBehaviourDefinitions()
        {
            var monoBehaviourDefs = new List<TypeDefinition>();
            foreach (var (name, module) in moduleDic)
            {
                foreach (var type in module.Types)
                {
                    if (type.BaseType?.FullName == "UnityEngine.MonoBehaviour")
                    {
                        monoBehaviourDefs.Add(type);
                    }
                }
            }
            return monoBehaviourDefs;
        }

        public TypeDefinition? GetTypeDefinition(string assemblyName, string fullName)
        {
            if (moduleDic.TryGetValue(assemblyName, out var module))
            {
                var typeDef = module.GetType(fullName);
                if (typeDef == null && assemblyName == "UnityEngine.dll")
                {
                    foreach (var pair in moduleDic)
                    {
                        typeDef = pair.Value.GetType(fullName);
                        if (typeDef != null)
                        {
                            break;
                        }
                    }
                }
                return typeDef;
            }
            return null;
        }

        public List<TypeTreeNode>? GenerateTreeNodes(string assemblyName, string fullName)
        {
            var typeDef = GetTypeDefinition(assemblyName, fullName);
            if (typeDef != null)
            {
                return GenerateTreeNodes(typeDef);
            }
            return null;
        }

        public List<TypeTreeNode> GenerateTreeNodes(TypeDefinition typeDef)
        {
            //  from AssetStudioUtility.MonoBehaviourConverter
            var m_Nodes = new List<TypeTreeNode>();
            serializedTypeHelper.AddMonoBehaviour(m_Nodes, 0);
            var converter = new TypeDefinitionConverter(typeDef, serializedTypeHelper, 1);
            m_Nodes.AddRange(converter.ConvertToTypeTreeNodes());
            return m_Nodes;
        }

    }
}
