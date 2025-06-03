using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator
{
    public class MonoCecilResolver : DefaultAssemblyResolver
    {
        public ReaderParameters readerParameters;
        public MonoCecilResolver()
        {
            readerParameters = new ReaderParameters
            {
                InMemory = true,
                ReadWrite = false,
                AssemblyResolver = this
            };
        }

        public void Register(AssemblyDefinition assembly)
        {
            RegisterAssembly(assembly);
        }
    }
}