// credit: https://github.com/AhmedAhmedEG/Unity-Type-Tree-Generator/blob/main/UnityTypeTreeGeneratorCLI/CLI.cs
// changes:
//   - assemblyMap -> monoTemplateFieldsGenerator.loadedAssemblies
//   - collpase GenerateTypeTreeNode into TypeTemplateToTypeTreeNodes
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Mono.Cecil;
using TypeTreeNode = AssetStudio.TypeTreeNode;

namespace TypeTreeGeneratorAPI
{
    internal class TypeTreeGenerator_AssetsTools : TypeTreeGenerator
    {
        private readonly UnityVersion unityVersionExtra;
        private readonly MonoCecilTempGenerator monoTemplateFieldsGenerator = new("");
        private ClassDatabaseFile classDatabase = new();

        public TypeTreeGenerator_AssetsTools(string unityVersion) : base(unityVersion)
        {
            unityVersionExtra = new UnityVersion(unityVersionString);
        }

        ~TypeTreeGenerator_AssetsTools()
        {
            monoTemplateFieldsGenerator.Dispose();
        }

        public void InitClassDatabase(Stream stream)
        {
            var classPackage = new ClassPackageFile();
            classPackage.Read(new AssetsFileReader(stream));

            classDatabase = classPackage.GetClassDatabase(this.unityVersionString);
        }

        public override List<TypeTreeNode>? GenerateTreeNodes(string assemblyName, string fullName)
        {
            var asm = monoTemplateFieldsGenerator.loadedAssemblies[assemblyName];
            var type = asm.MainModule.Types.First(t => t.Name == fullName);

            var typeTemplates = GenerateTypeTemplates(asm, type);
            if (typeTemplates.Any())
            {
                return GenerateTypeTreeNodes(typeTemplates);
            }
            return null;
        }

        public override List<Tuple<string, string>> GetMonoBehaviourDefinitions()
        {
            var monoBehaviourDefs = new List<Tuple<string, string>>();
            foreach (var (asmName, asmDef) in monoTemplateFieldsGenerator.loadedAssemblies)
            {
                foreach (var type in asmDef.MainModule.Types)
                {
                    if (IsMonoBehaviour(type))
                    {
                        monoBehaviourDefs.Add(new Tuple<string, string>(asmName, type.FullName));
                    }
                }
            }
            return monoBehaviourDefs;
        }

        public override void LoadDLL(Stream dllStream)
        {
            var assembly = AssemblyDefinition.ReadAssembly(dllStream, readerParameters);
            resolver.Register(assembly);
            monoTemplateFieldsGenerator.loadedAssemblies.Add(assembly.MainModule.Name, assembly);
        }



        /* Returns a template field for a given type using the class database, It's essential for unity base types
         like MonoBehavior and GameObject, MonoCecilTempGenerator will never return template fields for unity base types. */
        private AssetTypeTemplateField? QueryClassDatabase(TypeDefinition type)
        {
            var typeTemplate = new AssetTypeTemplateField();
            var classDatabaseType = classDatabase.FindAssetClassByName(type.Name);

            if (classDatabaseType is null || (classDatabaseType.EditorRootNode == null && classDatabaseType.ReleaseRootNode == null)) return null;
            typeTemplate.FromClassDatabase(classDatabase, classDatabaseType);

            return typeTemplate;
        }


        /* Returns a list of type templates for a given type utilizing the class database for unity base types, and
         MonoCecilTempGenerator for the type it self, it returns an empty list if there was no type templates returned
         from any of the unity base types. */
        private List<AssetTypeTemplateField> GenerateTypeTemplates(AssemblyDefinition asm, TypeDefinition type)
        {
            var typeTemplates = new List<AssetTypeTemplateField>();
            var generatorTypeTemplates = monoTemplateFieldsGenerator.Read(asm,
                                                                                                  type.Namespace,
                                                                                                  type.Name,
                                                                                                  this.unityVersionExtra);

            // If the generator resulted in nothing, this means this type is not serializable in the first place, so ignore it.
            if (generatorTypeTemplates.Count == 0) return typeTemplates;

            var baseTypes = GetBaseTypes(type);
            foreach (var typeTemplate in baseTypes.Select(QueryClassDatabase))
            {
                // template fields of type component are not needed for deserialization.
                if (typeTemplate is null || typeTemplate.Type == "Component") continue;
                typeTemplates.Add(typeTemplate);

                /* If the current template field type was one of those, then break as the generator recursively gets the
                 rest of the base types internally, thus we prevent possible duplicates */
                if (typeTemplate.Type is "MonoBehaviour" or "ScriptableObject")
                    break;
            }

            /* If there's not a single type template for any of the base types, it means this type is not supported, thus,
            do not generate type templates for the type it self and return an empty list. */
            if (!typeTemplates.Any()) return typeTemplates;

            typeTemplates.AddRange(generatorTypeTemplates);
            return typeTemplates;
        }

        // Returns a list of type tree nodes given a list of type templates.
        private static List<TypeTreeNode> GenerateTypeTreeNodes(List<AssetTypeTemplateField> typeTemplates)
        {
            var typeTreeNodes = new List<TypeTreeNode>();
            if (!typeTemplates.Any()) return typeTreeNodes;

            typeTreeNodes.AddRange(TypeTemplateToTypeTreeNodes(typeTemplates[0], level: 0));

            for (int i = 1; i < typeTemplates.Count; i++)
                typeTreeNodes.AddRange(TypeTemplateToTypeTreeNodes(typeTemplates[i], level: 1));

            return typeTreeNodes;
        }

        // Recursive function that returns a list of type tree nodes for a type template including it's child type templates too.
        private static List<TypeTreeNode> TypeTemplateToTypeTreeNodes(AssetTypeTemplateField typeTemplate, int level = 0)
        {
            var typeTreeNodes = new List<TypeTreeNode> {
                new TypeTreeNode(typeTemplate.Type, typeTemplate.Name, level, typeTemplate.IsAligned)
            };

            foreach (var child in typeTemplate.Children)
                typeTreeNodes.AddRange(TypeTemplateToTypeTreeNodes(child, level + 1));

            return typeTreeNodes;
        }

        // Returns a list of the base types for a given type, ordered from top type to bottom type.
        private static List<TypeDefinition> GetBaseTypes(TypeDefinition type)
        {
            var baseTypes = new List<TypeDefinition>();
            if (type.BaseType is null) return baseTypes;

            var baseType = type.BaseType.Resolve();

            baseTypes.Insert(0, baseType);
            baseTypes.InsertRange(0, GetBaseTypes(baseType));

            return baseTypes;
        }
    }
}
