#if ENABLE_IL2CPP || ENABLE_ASSET_RIPPER
namespace TypeTreeGeneratorAPI.TypeTreeGenerator;

public class AssemblyResolver : AsmResolver.DotNet.IAssemblyResolver
{
    public Dictionary<string, AsmResolver.DotNet.AssemblyDefinition> assemblyDefinitions = new();

    public void AddToCache(string name, AsmResolver.DotNet.AssemblyDefinition definition)
    {
        assemblyDefinitions[name] = definition;
    }

    public void AddToCache(AsmResolver.DotNet.AssemblyDescriptor descriptor, AsmResolver.DotNet.AssemblyDefinition definition)
    {
        assemblyDefinitions[descriptor.Name] = definition;
    }

    public void ClearCache()
    {
        assemblyDefinitions.Clear();
    }

    public bool HasCached(AsmResolver.DotNet.AssemblyDescriptor descriptor)
    {
        return assemblyDefinitions.ContainsKey(descriptor.Name);
    }

    public bool RemoveFromCache(AsmResolver.DotNet.AssemblyDescriptor descriptor)
    {
        return assemblyDefinitions.Remove(descriptor.Name);
    }

    public AsmResolver.DotNet.AssemblyDefinition? Resolve(AsmResolver.DotNet.AssemblyDescriptor assembly)
    {
        if (assemblyDefinitions.TryGetValue(assembly.Name, out var assemblyDef))
        {
            return assemblyDef;
        }
        return null;
    }
}
#endif
