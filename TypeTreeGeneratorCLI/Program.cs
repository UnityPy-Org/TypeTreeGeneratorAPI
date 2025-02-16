using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using TypeTreeGeneratorAPI;

Console.WriteLine("Hello, World!");
var c = new TypeTreeGenerator("2020.3.41f1");

var asm_fp = "D:\\Program Files\\SwordofConvallaria\\SoCLauncher\\SwordOfConvallaria\\GameAssembly.dll";
var metadata_fp = "D:\\Program Files\\SwordofConvallaria\\SoCLauncher\\SwordOfConvallaria\\SoC_Data\\il2cpp_data\\Metadata\\global-metadata.dat";
var asm_fs = new FileStream(asm_fp, FileMode.Open, FileAccess.Read);
var metadata_fs = new FileStream(metadata_fp, FileMode.Open, FileAccess.Read);
var asm = new byte[asm_fs.Length];
var metadata = new byte[metadata_fs.Length];
asm_fs.ReadExactly(asm);
metadata_fs.ReadExactly(metadata);
asm_fs.Close();
metadata_fs.Close();
c.LoadIL2CPP(asm, metadata);
foreach (var def in c.GetMonoBehaviourDefinitions())
{
    var nodes = c.GenerateTreeNodes(def.Module.Name, def.FullName);
    if (nodes.Count == 0)
        continue;
    Console.WriteLine($"{def.Module.Name} -  {def.FullName}");
    Console.WriteLine(nodes.Count);
    Console.WriteLine(TypeTreeNodeSerializer.ToJson(nodes));
}