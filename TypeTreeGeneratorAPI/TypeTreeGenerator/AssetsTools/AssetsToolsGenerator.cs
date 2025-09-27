using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Mono.Cecil;
#if ENABLE_IL2CPP
using LibCpp2IL;
#endif

namespace TypeTreeGeneratorAPI.TypeTreeGenerator.AssetsTools
{
    public class AssetsToolsGenerator : TypeTreeGenerator
    {
        protected UnityVersion unityVersionExtra;
        protected MonoCecilTempGeneratorPatch monoCecilGenerator = new();
#if ENABLE_IL2CPP
        protected Cpp2IlTempGeneratorPatch cpp2IlGenerator = new();
#endif
        protected readonly MonoCecilResolver resolver = new();
        protected override bool supportsIl2Cpp => true;
        private bool monoLoaded = false;

        public AssetsToolsGenerator(string unityVersionString) : base(unityVersionString)
        {
            unityVersionExtra = new UnityVersion(unityVersionString);
#if ENABLE_IL2CPP
            cpp2IlGenerator.SetUnityVersion(unityVersionExtra);
#endif
        }

        ~AssetsToolsGenerator()
        {
            monoCecilGenerator.Dispose();
#if ENABLE_IL2CPP
            cpp2IlGenerator.Dispose();
#endif
        }

        public override List<TypeTreeNode>? GenerateTreeNodes(string assemblyName, string fullName)
        {
            string nameSpace = string.Empty;
            string className = string.Empty;
            var lastDot = fullName.LastIndexOf('.');
            if (lastDot == -1)
            {
                className = fullName;
            }
            else
            {
                nameSpace = fullName.Substring(0, lastDot);
                className = fullName.Substring(lastDot + 1);
            }


            var templateField = new AssetTypeTemplateField
            {
                Name = "Base",
                Type = className,
                ValueType = AssetValueType.None,
                IsArray = false,
                IsAligned = false,
                HasValue = false,
                Children = new List<AssetTypeTemplateField>(0)
            };


            IMonoBehaviourTemplateGeneratorPatch monoTemplateGenerator;
            if (monoLoaded) {
                monoTemplateGenerator = monoCecilGenerator;
            } else {
#if ENABLE_IL2CPP
                monoTemplateGenerator = cpp2IlGenerator;
#else
                monoTemplateGenerator = monoCecilGenerator;
#endif
            }
            
            var field = monoTemplateGenerator.GetTemplateFieldPatch(templateField, assemblyName, nameSpace, className, unityVersionExtra);

            return field == null ? null : ConvertAssetTypeTemplateFieldIntoTypeTreeNode(field);
        }

        private List<TypeTreeNode> ConvertAssetTypeTemplateFieldIntoTypeTreeNode(AssetTypeTemplateField rootField)
        {
            var nodes = new List<TypeTreeNode>();
            var stack = new Stack<(AssetTypeTemplateField field, int level)>();
            stack.Push((rootField, 0));

            while (stack.Count > 0)
            {
                var (field, level) = stack.Pop();

                nodes.Add(new TypeTreeNode(field.Type, field.Name, level, field.IsAligned));

                // Push children in reverse to maintain left-to-right DFS order
                for (int i = field.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push((field.Children[i], level + 1));
                }
            }

            return nodes;
        }


        public override void LoadDll(Stream dllStream)
        {
            monoLoaded = true;
            var assembly = AssemblyDefinition.ReadAssembly(dllStream, resolver.readerParameters);
            resolver.Register(assembly);
            monoCecilGenerator.loadedAssemblies.Add(assembly.MainModule.Name, assembly);
        }

#if ENABLE_IL2CPP
        public override void LoadIl2Cpp(byte[] assemblyData, byte[] metadataData)
        {
            base.LoadIl2Cpp(assemblyData, metadataData);
            cpp2IlGenerator.SetInitialized(true);
        }
#endif

        public override List<(string, string)> GetClassDefinitions()
        {
            if (monoLoaded)
            {
                return GetClassDefinitions_Mono();
            }
#if ENABLE_IL2CPP
            else if (LibCpp2IlMain.TheMetadata != null)
            {
                return GetClassDefinitions_Il2Cpp();
            }
#endif
            else
            {
                // TODO - err
                return new List<(string, string)>();
            }
        }

        public List<(string, string)> GetClassDefinitions_Mono()
        {
            var typedefs = new List<(string, string)>();
            foreach (var (asmName, asmDef) in monoCecilGenerator.loadedAssemblies)
            {
                foreach (var type in asmDef.MainModule.Types)
                {
                    typedefs.Add((asmName, type.FullName));
                }
            }
            return typedefs;
        }

#if ENABLE_IL2CPP
        public List<(string, string)> GetMonoBehaviourDefinitions_Il2Cpp()
        {
            var typedefs = new List<(string, string)>();
            if (LibCpp2IlMain.TheMetadata == null)
            {
                // TODO - err
                return typedefs;
            }
            foreach (var asmDef in LibCpp2IlMain.TheMetadata.AssemblyDefinitions)
            {
                if (asmDef.Image.Types == null || asmDef.AssemblyName.Name is null)
                    continue;
                foreach (var asmType in asmDef.Image.Types)
                {
                    if (asmType.FullName is null)
                        continue;
                     typedefs.Add((asmDef.AssemblyName.Name, asmType.FullName));
                }
            }
            return monoBehaviourDefs;
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

        public override List<string> GetAssemblyNames()
        {
            if (monoLoaded && monoCecilGenerator != null)
            {
                return monoCecilGenerator.loadedAssemblies.Keys.ToList();
            }
#if ENABLE_IL2CPP
            else if (LibCpp2IlMain.TheMetadata != null)
            {
                return LibCpp2IlMain.TheMetadata.AssemblyDefinitions
                    .Select(asmDef => asmDef.AssemblyName.Name)
                    .ToList();
            }
#endif
            else
            {
                return new List<string>();
            }
        }
    }
}
