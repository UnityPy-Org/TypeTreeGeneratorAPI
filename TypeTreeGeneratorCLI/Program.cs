using TypeTreeGeneratorAPI;

if (args.Length < 2)
{
    Console.WriteLine("Usage: TypeTreeGeneratorCLI <unity_version> <dll directory>");
    Console.WriteLine("Usage: TypeTreeGeneratorCLI <unity_version> <il2cpp assembly> <il2cpp metadata>");
    return;
}

var generator = new TypeTreeGenerator(args[0]);

if (args.Length == 2)
{
    var dll_dir = args[1];
    foreach (var dll_fp in Directory.GetFiles(dll_dir, "*.dll"))
    {
        var dll = File.ReadAllBytes(dll_fp);
        generator.LoadDLL(dll);
    }
}
else if (args.Length == 3)
{
    var assembly_fp = args[1];
    var metadata_fp = args[2];
    var assembly = File.ReadAllBytes(assembly_fp);
    var metadata = File.ReadAllBytes(metadata_fp);
    generator.LoadIL2CPP(assembly, metadata);
}

foreach (var def in generator.GetMonoBehaviourDefinitions())
{
    var nodes = generator.GenerateTreeNodes(def.Module.Name, def.FullName)!;
    if (nodes.Count == 0)
        continue;
    Console.WriteLine($"{def.Module.Name} -  {def.FullName}");
    Console.WriteLine(nodes.Count);
    Console.WriteLine(TypeTreeNodeSerializer.ToJson(nodes));
}