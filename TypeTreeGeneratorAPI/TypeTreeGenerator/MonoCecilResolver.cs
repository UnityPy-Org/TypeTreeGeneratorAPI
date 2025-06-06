using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator
{
    public class MonoCecilResolver : BaseAssemblyResolver
    {
        readonly Dictionary<string, AssemblyDefinition> cache;
        public ReaderParameters readerParameters;
        public MonoCecilResolver()
        {
            cache = new();
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

        // DefaultAssemblyResolver - fullname -> name
        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition? assembly; // Use nullable type to handle potential null values
            if (cache.TryGetValue(name.Name, out assembly) && assembly != null) // Ensure assembly is not null
                return assembly;

            assembly = base.Resolve(name);
            cache[name.FullName] = assembly;

            return assembly;
        }

        protected void RegisterAssembly(AssemblyDefinition assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly)); // Use nameof for better readability

            var name = assembly.Name.Name;
            if (cache.ContainsKey(name))
                return;

            cache[name] = assembly;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var assembly in cache.Values)
                assembly.Dispose();

            cache.Clear();

            base.Dispose(disposing);
        }
    }
}