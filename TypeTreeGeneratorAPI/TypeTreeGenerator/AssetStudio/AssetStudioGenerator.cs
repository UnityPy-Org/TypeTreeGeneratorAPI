using Mono.Cecil;
using TypeTreeGeneratorAPI.TypeTreeGenerator.AssetStudio.AssetStudioUtility;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator.AssetStudio
{
    public class AssetStudioGenerator : TypeTreeGenerator
    {
        private Dictionary<string, ModuleDefinition> moduleDic = new();
        private SerializedTypeHelper serializedTypeHelper;
        private readonly MonoCecilResolver resolver = new();

        public AssetStudioGenerator(string unityVersionString) : base(unityVersionString)
        {
            serializedTypeHelper = new SerializedTypeHelper([unityVersion.Major, unityVersion.Minor, unityVersion.Build, unityVersion.TypeNumber]);
        }

        ~AssetStudioGenerator()
        {
            foreach (var pair in moduleDic)
            {
                pair.Value.Dispose();
            }
            moduleDic.Clear();
        }

        public override void LoadDll(Stream dllStream)
        {
            var assembly = AssemblyDefinition.ReadAssembly(dllStream, resolver.readerParameters);
            resolver.Register(assembly);
            moduleDic.Add(assembly.MainModule.Name, assembly.MainModule);
        }

        public override List<(string, string)> GetClassDefinitions()
        {
            var typedefs = new List<(string, string)>();
            foreach (var (moduleName, module) in moduleDic)
            {
                foreach (var type in module.Types)
                {
                    typedefs.Add((moduleName, type.FullName));
                }
            }
            return typedefs;
        }

        public override List<TypeTreeNode>? GenerateTreeNodes(string assemblyName, string fullName)
        {
            var assemblyNameNormalized = assemblyName.EndsWith(".dll") ? assemblyName : $"{assemblyName}.dll";
            
            var typeDef = GetTypeDefinition(assemblyNameNormalized, fullName);
            if (typeDef != null)
            {
                return GenerateTreeNodes(typeDef);
            }
            return null;
        }

        private List<TypeTreeNode> GenerateTreeNodes(TypeDefinition typeDef)
        {
            var converter = new TypeDefinitionConverter(typeDef, serializedTypeHelper, 1);
            return converter.ConvertToTypeTreeNodes();
        }

        private TypeDefinition? GetTypeDefinition(string assemblyName, string fullName)
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

        public override List<string> GetAssemblyNames()
        {
            return [.. moduleDic.Keys];
        }
    }
}
