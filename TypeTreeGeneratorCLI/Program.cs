using System.CommandLine;
using TypeTreeGeneratorAPI;
using TypeTreeGeneratorAPI.TypeTreeGenerator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var unityVersionOption = new Option<string>(
            aliases: ["--unity-version", "-uv"],
            description: "The Unity Version of the game"
        )
        { IsRequired = true };

        var backendOption = new Option<string>(
            aliases: ["--backend", "-b"],
            description: "The backend to use (AssetStudio, AssetsTools, AssetRipper)",
            getDefaultValue: () => "AssetsTools"
        );


        var monoDirectoryOption = new Option<string?>(
            aliases: ["--mono-directory", "-md"],
            description: "The path to a directory containing .dll files"
        );

        var il2cppAssemblyPathOption = new Option<string?>(
            aliases: ["--il2cpp-assembly", "-ia"],
            description: "The path to an il2cpp assembly (GameAssembly.dll, libil2cpp.so)"
        );

        var il2cppMetadataPathOption = new Option<string?>(
            aliases: ["--il2cpp-metadata", "-im"],
            description: "The path to an il2cpp metadata file (global-metadata.dat)"
        );

        var rootCommand = new RootCommand("TypeTreeGeneratorAPI");
        rootCommand.AddOption(unityVersionOption);
        rootCommand.AddOption(backendOption);
        rootCommand.AddOption(monoDirectoryOption);
        rootCommand.AddOption(il2cppAssemblyPathOption);
        rootCommand.AddOption(il2cppMetadataPathOption);

        rootCommand.SetHandler((unityVersion, backend, monoDirectory, il2cppAssembly, il2CppMetadata) =>
        {
            var handle = new TypeTreeGeneratorHandle(backend, unityVersion);
            if (monoDirectory is not null)
            {
                foreach (var dll_fp in Directory.GetFiles(monoDirectory, "*.dll"))
                {
                    var dll = File.ReadAllBytes(dll_fp);
                    handle.Instance.LoadDll(dll);
                }
            }
            if (il2cppAssembly is not null && il2CppMetadata is not null)
            {
                var assembly = File.ReadAllBytes(il2cppAssembly);
                var metadata = File.ReadAllBytes(il2CppMetadata);
                handle.Instance.LoadIl2Cpp(assembly, metadata);
            }

            foreach (var (assemblyName, fullName) in handle.Instance.GetMonoBehaviourDefinitions())
            {
                var nodes = handle.Instance.GenerateTreeNodes(assemblyName, fullName)!;
                if (nodes == null || nodes.Count == 0)
                    continue;
                Console.WriteLine($"{assemblyName} -  {fullName}");
                Console.WriteLine(nodes.Count);
                Console.WriteLine(TypeTreeNodeSerializer.ToJson(nodes));
            }
        },
            unityVersionOption, backendOption, monoDirectoryOption, il2cppAssemblyPathOption, il2cppMetadataPathOption);

        return await rootCommand.InvokeAsync(args);
    }
}
