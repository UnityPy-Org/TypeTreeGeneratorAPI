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

        public override List<(string, string)> GetMonoBehaviourDefinitions()
        {
            var monoBehaviourDefs = new List<(string, string)>();
            foreach (var (moduleName, module) in moduleDic)
            {
                foreach (var type in module.Types)
                {
                    if (IsMonoBehaviour(type))
                    {
                        monoBehaviourDefs.Add((moduleName, type.FullName));
                    }
                }
            }
            return monoBehaviourDefs;
        }

        public override List<TypeTreeNode>? GenerateTreeNodes(string assemblyName, string fullName)
        {
            var typeDef = GetTypeDefinition(assemblyName, fullName);
            if (typeDef != null)
            {
                return GenerateTreeNodes(typeDef);
            }
            return null;
        }

        private static bool IsMonoBehaviour(TypeDefinition type)
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

        private List<TypeTreeNode> GenerateTreeNodes(TypeDefinition typeDef)
        {
            //  from AssetStudioUtility.MonoBehaviourConverter
            var m_Nodes = new List<TypeTreeNode>();
            serializedTypeHelper.AddMonoBehaviour(m_Nodes, 0);
            var converter = new TypeDefinitionConverter(typeDef, serializedTypeHelper, 1);
            m_Nodes.AddRange(converter.ConvertToTypeTreeNodes());
            return m_Nodes;
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
    }
}
