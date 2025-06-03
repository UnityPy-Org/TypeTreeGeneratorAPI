using AsmResolver.DotNet;
using AsmResolver.PE.File;
using AssetRipper.Primitives;
using AssetRipper.SerializationLogic;
using Cpp2IL.Core.Extensions;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator.AssetRipper
{
    public class AssetRipperGenerator : TypeTreeGenerator
    {
        protected Dictionary<string, AssemblyDefinition> assemblyDefinitions => assemblyResolver.assemblyDefinitions;
        private Dictionary<ITypeDefOrRef, SerializableType> monoTypeCache = new();
        private Il2CppHandler.AssemblyResolver assemblyResolver = new();
        private SerializableTypeConverter typeConverter;

        protected override bool supportsIl2Cpp => true;

        public AssetRipperGenerator(string unityVersionString) : base(unityVersionString)
        {
            typeConverter = new SerializableTypeConverter(this.unityVersion);
        }

        public override List<TypeTreeNode>? GenerateTreeNodes(string assemblyName, string fullName)
        {
            int lastDot = fullName.LastIndexOf('.');
            var nameSpace = lastDot == -1 ? "" : fullName.Substring(0, lastDot);
            var className = lastDot == -1 ? fullName : fullName.Substring(lastDot + 1);


            var type = FindType(assemblyName, nameSpace, className);
            var monoType = default(SerializableType?);
            string? failureReason = null;
            if (!new FieldSerializer(new UnityVersion(6000)).TryCreateSerializableType(type, monoTypeCache, out monoType, out failureReason))
            {
                throw new InvalidOperationException($"Failed to create SerializableType: {failureReason}");
            }

            return typeConverter.FromSerializableType(monoType);
        }

        public override List<(string, string)> GetMonoBehaviourDefinitions()
        {
            var monoBehaviourDefinitions = new List<(string, string)>();
            foreach (var assembly in assemblyDefinitions.Values)
            {
                foreach (var module in assembly.Modules)
                {
                    foreach (var type in module.TopLevelTypes)
                    {
                        if (type.IsClass && type.BaseType?.FullName == "UnityEngine.MonoBehaviour")
                        {
                            monoBehaviourDefinitions.Add((assembly.Name, type.FullName));
                        }
                    }
                }
            }
            return monoBehaviourDefinitions;
        }

        public override void LoadDll(Stream dllStream)
        {
            LoadDll(dllStream.ReadBytes());
        }

        public override void LoadDll(byte[] dllData)
        {
            var pefile = PEFile.FromBytes(dllData);
            var assembly = AssemblyDefinition.FromFile(pefile);
            loadAssembly(assembly);
        }

        public override void LoadIl2Cpp(byte[] assemblyData, byte[] metadataData)
        {
            base.LoadIl2Cpp(assemblyData, metadataData);
            foreach (var assembly in Il2CppHandler.GenerateAssemblyDefinitions().Values)
            {
                loadAssembly(assembly);
            }
        }

        private void loadAssembly(AssemblyDefinition assembly)
        {
            assemblyResolver.assemblyDefinitions[assembly.Name] = assembly;
            foreach (ModuleDefinition module in assembly.Modules)
            {
                module.MetadataResolver = new DefaultMetadataResolver(assemblyResolver);
            }
        }

        private TypeDefinition? FindType(string assembly, string @namespace, string name)
        {
            AssemblyDefinition? definition = assemblyDefinitions[assembly];
            if (definition == null)
            {
                return null;
            }

            foreach (ModuleDefinition module in definition.Modules)
            {
                TypeDefinition? type = GetType(module, @namespace, name);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private static TypeDefinition? GetType(ModuleDefinition module, string @namespace, string name)
        {
            IList<TypeDefinition> types = module.TopLevelTypes;
            foreach (TypeDefinition type in types)
            {
                if ((type.Namespace ?? "") == @namespace && type.Name == name)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
