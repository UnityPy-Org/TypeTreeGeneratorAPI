using AssetRipper.Primitives;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator
{
    abstract public class TypeTreeGenerator
    {
        protected readonly string unityVersionString;
        protected readonly UnityVersion unityVersion;
        protected virtual bool supportsIl2Cpp => false;

        public TypeTreeGenerator(string unityVersionString)
        {
            this.unityVersionString = unityVersionString;
            unityVersion = UnityVersion.Parse(unityVersionString);
        }

        public List<TypeTreeNode> GetMonoBehaviourRootNodes()
        {
            var pathIdType = (unityVersion.Major >= 5) ? "SInt64" : "int";

            return [
                new TypeTreeNode("MonoBehaviour", "Base", 0, false),
                new TypeTreeNode("PPtr<GameObject>", "m_GameObject", 1, true),
                new TypeTreeNode("int", "m_FileID", 2, false),
                new TypeTreeNode(pathIdType, "m_PathID", 2, false),
                new TypeTreeNode("UInt8", "m_Enabled", 1, false),
                new TypeTreeNode("PPtr<MonoScript>", "m_Script", 1, true), // PPtr
                new TypeTreeNode("int", "m_FileID", 2, false),
                new TypeTreeNode(pathIdType, "m_PathID", 2, false),
                new TypeTreeNode("String", "m_Name", 1, true)
            ];
        }

        abstract public List<(string, string)> GetMonoBehaviourDefinitions();
        abstract public List<TypeTreeNode>? GenerateTreeNodes(string assemblyName, string fullName);

        abstract public List<string> GetAssemblyNames();

        abstract public void LoadDll(Stream dllStream);

        virtual public void LoadDll(byte[] dll)
        {
            using (var dllStream = new MemoryStream(dll))
            {
                LoadDll(dllStream);
            }
        }

#if ENABLE_IL2CPP
        public virtual void LoadIl2Cpp(byte[] assemblyData, byte[] metadataData)
        {
            Il2CppHandler.Initialize(assemblyData, metadataData, unityVersionString);
            if (!supportsIl2Cpp)
            {
                LoadIl2CppAssemblyDefinitionsAsDll();
            }
        }

        public virtual void LoadIl2CppAssemblyDefinitionsAsDll()
        {
            foreach (var asmDef in Il2CppHandler.GenerateAssemblyDefinitions().Values)
            {
                var image = asmDef.ManifestModule!.ToPEImage(new AsmResolver.DotNet.Builder.ManagedPEImageBuilder());
                if (image is null)
                    continue;
                var fileBuilder = new AsmResolver.PE.Builder.ManagedPEFileBuilder();
                using (var dllStream = new MemoryStream())
                {
                    fileBuilder.CreateFile(image).Write(dllStream);
                    dllStream.Position = 0;
                    LoadDll(dllStream);
                }
            }
        }
#endif
    }
}
