using AssetRipper.Primitives;
using AssetStudio;
using Mono.Cecil;

namespace TypeTreeGeneratorAPI
{
    public class TypeTreeGenerator_AssetStudio : TypeTreeGenerator
    {
        private Dictionary<string, ModuleDefinition> moduleDic;
        private SerializedTypeHelper serializedTypeHelper;

        public TypeTreeGenerator_AssetStudio(string unityVersion) : base(unityVersion)
        {
            moduleDic = new Dictionary<string, ModuleDefinition>();
            var arVersion = UnityVersion.Parse(unityVersion);
            serializedTypeHelper = new SerializedTypeHelper([arVersion.Major, arVersion.Minor, arVersion.Build, arVersion.TypeNumber]);
        }

        ~TypeTreeGenerator_AssetStudio()
        {
            foreach (var pair in moduleDic)
            {
                pair.Value.Dispose();
            }
            moduleDic.Clear();
        }

        public override void LoadDLL(Stream dllStream)
        {
            var assembly = AssemblyDefinition.ReadAssembly(dllStream, readerParameters);
            resolver.Register(assembly);
            moduleDic.Add(assembly.MainModule.Name, assembly.MainModule);
        }

        public override List<Tuple<string, string>> GetMonoBehaviourDefinitions()
        {
            var monoBehaviourDefs = new List<Tuple<string, string>>();
            foreach (var (moduleName, module) in moduleDic)
            {
                foreach (var type in module.Types)
                {
                    if (IsMonoBehaviour(type))
                    {
                        monoBehaviourDefs.Add(new Tuple<string, string>(moduleName, type.FullName));
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

        public List<TypeTreeNode> GenerateTreeNodes(TypeDefinition typeDef)
        {
            //  from AssetStudioUtility.MonoBehaviourConverter
            var m_Nodes = new List<TypeTreeNode>();
            serializedTypeHelper.AddMonoBehaviour(m_Nodes, 0);
            var converter = new TypeDefinitionConverter(typeDef, serializedTypeHelper, 1);
            m_Nodes.AddRange(converter.ConvertToTypeTreeNodes());
            return m_Nodes;
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
    }
}
