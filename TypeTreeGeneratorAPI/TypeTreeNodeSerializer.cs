using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using TypeTreeGeneratorAPI.TypeTreeGenerator;

[JsonSerializable(typeof(TypeTreeNode))]
[JsonSerializable(typeof(List<TypeTreeNode>))] // For collections
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "<Pending>")]
public partial class MyJsonContext : JsonSerializerContext
{
    // Source generator will auto-implement this partial class
}

namespace TypeTreeGeneratorAPI
{
    public class TypeTreeNodeSerializer
    {
        public static string ToJson(List<TypeTreeNode> nodes)
        {
            return JsonSerializer.Serialize(nodes, MyJsonContext.Default.ListTypeTreeNode);
        }

        public static (IntPtr, int) ToRaw(List<TypeTreeNode> nodes)
        {
            // Allocate unmanaged memory for the array
            int structSize = Marshal.SizeOf<TypeTreeNode>();
            IntPtr arrayPtr = Marshal.AllocCoTaskMem(structSize * nodes.Count);

            // Convert each managed node to an unmanaged node
            IntPtr current = arrayPtr;
            foreach (var node in nodes)
            {
                // Copy the struct to unmanaged memory
                Marshal.StructureToPtr(node, current, false);
                current += structSize;
            }

            return (arrayPtr, nodes.Count);
        }
    }
}