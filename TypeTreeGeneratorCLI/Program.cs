using AssetsTools.NET;
using System.CommandLine;
using TypeTreeGeneratorAPI;
using TypeTreeGeneratorAPI.TypeTreeGenerator;
using System.Text.Json.Serialization;
using System.Text.Json;
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
            getDefaultValue: () => "AssetStudio"
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

        var outputJsonFile = new Option<string?>(
            aliases: ["--output", "-o"],
            description: "The path to output the JSON file (if not specified, outputs to console)",
            getDefaultValue: () => null
        );

        var rootCommand = new RootCommand("TypeTreeGeneratorAPI");
        rootCommand.AddOption(unityVersionOption);
        rootCommand.AddOption(backendOption);
        rootCommand.AddOption(monoDirectoryOption);
        rootCommand.AddOption(il2cppAssemblyPathOption);
        rootCommand.AddOption(il2cppMetadataPathOption);
        rootCommand.AddOption(outputJsonFile);

        rootCommand.SetHandler((unityVersion, backend, monoDirectory, il2cppAssembly, il2CppMetadata, outputJson) =>
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
            if (outputJson == null)
            {
                foreach (var (assemblyName, fullName) in handle.Instance.GetClassDefinitions())
                {
                    var nodes = handle.Instance.GenerateTreeNodes(assemblyName, fullName)!;
                    if (nodes == null || nodes.Count == 0)
                        continue;                    
                    Console.WriteLine($"{assemblyName} -  {fullName}");
                    Console.WriteLine(nodes.Count);
                    Console.WriteLine(TypeTreeNodeSerializer.ToJson(nodes));                    
                }
            } else
            {
                Dictionary<string, List<TypeTreeGeneratorAPI.TypeTreeGenerator.TypeTreeNode>> nodes = new();
                foreach (var (assemblyName, fullName) in handle.Instance.GetClassDefinitions())
                {
                    var treeNodes = handle.Instance.GenerateTreeNodes(assemblyName, fullName);
                    if (treeNodes == null || treeNodes.Count == 0)
                        continue;
                    nodes[fullName] = treeNodes;
                }
                File.WriteAllText(outputJson, TypeTreeNodeSerializer.ToJson(nodes));
                Console.WriteLine($"Saved to {outputJson}");
            }
        },
            unityVersionOption, backendOption, monoDirectoryOption, il2cppAssemblyPathOption, il2cppMetadataPathOption, outputJsonFile);

        return await rootCommand.InvokeAsync(args);
    }
}
