// based on:
// AssetRipper.Import/Structure/Assembly/TypeTrees/TypeTreeNodeStruct.cs
using AssetRipper.Primitives;
using AssetRipper.SerializationLogic;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator.AssetRipper
{
    public class SerializableTypeConverter
    {
        bool use64BitPathId = true; // Unity < 5.0.0.0 uses SInt32 for PathID, but Unity >= SInt64
        private List<TypeTreeNode> nodes = new();

        public SerializableTypeConverter(UnityVersion unityVersion)
        {
            if (unityVersion.Major < 5)
            {
                use64BitPathId = false;
            }
        }

        public SerializableTypeConverter(bool use64BitPathId)
        {
            this.use64BitPathId = use64BitPathId;
        }


        public List<TypeTreeNode> FromSerializableType(SerializableType type)
        {
            nodes.Clear();
            FromSerializableType(type, "Base", false, 0);
            return nodes;
        }

        private void FromSerializableType(SerializableType type, string name, bool alignBytes, int level)
        {
            string typeName = type.Type switch
            {
                PrimitiveType.Bool => "bool",
                PrimitiveType.Char => "UInt16",
                PrimitiveType.Byte => "UInt8",
                PrimitiveType.SByte => "SInt8",
                PrimitiveType.Short => "SInt16",
                PrimitiveType.UShort => "UInt16",
                PrimitiveType.Int => "SInt32",
                PrimitiveType.UInt => "UInt32",
                PrimitiveType.Long => "SInt64",
                PrimitiveType.ULong => "UInt64",
                PrimitiveType.Single => "float",
                PrimitiveType.Double => "double",
                PrimitiveType.String => "string",
                PrimitiveType.Complex => type.IsEnginePointer() ? "PPtr<Object>" : type.Name,
                _ => type.Name,
            };
            nodes.Add(new(typeName, name, level, alignBytes));


            switch (type.Fields.Count)
            {
                case 0:
                    if (type.Type is PrimitiveType.String)
                    {
                        nodes.Add(new("Array", "Array", level + 1));
                        nodes.Add(new("SInt32", "size", level + 1));
                        nodes.Add(new("char", "data", level + 1));
                    }
                    else if (type.IsEnginePointer())
                    {
                        nodes.Add(new("SInt32", "m_FileID", level + 1));
                        nodes.Add(new(use64BitPathId ? "SInt64" : "SInt32", "m_PathID", level + 1));
                    }
                    break;
                default:
                    foreach (var field in type.Fields)
                    {
                        FromSerializableTypeField(field, level + 1);
                    }
                    break;
            }
        }

        private void FromSerializableTypeField(SerializableType.Field field, int level)
        {
            switch (field.ArrayDepth)
            {
                case 0:
                    FromSerializableType(field.Type, field.Name, field.Align, level);
                    return;
                case 1:
                    {
                        // Note: treatment of alignment is incorrect.
                        // In particular, this code treats field.Align as shared between the array and its elements.
                        // This is certainly not correct, but fixing it to store separate would require changing the code in a bunch of places.
                        // Also, alignment is not important for the current use case. I just included it to lay the groundwork for future use.
                        nodes.Add(new("Array", field.Name, level, field.Align));
                        nodes.Add(new("SInt32", "size", level + 1));
                        FromSerializableType(field.Type, "data", field.Align, level + 1);
                        return;
                    }
                case 2:
                    throw new NotImplementedException("Array depth 2 is not implemented.");
                default:
                    throw new NotSupportedException($"Array depth {field.ArrayDepth} is not supported.");
            }
        }
    }
}
