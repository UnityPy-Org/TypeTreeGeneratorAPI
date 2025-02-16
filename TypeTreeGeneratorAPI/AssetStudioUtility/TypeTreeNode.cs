// Modification:
// 1. Add JsonInclude attribute to serialize the field.
// 2. Class to Struct
// 3. Remove unused fields (for this application)
// 2. Add StructLayout attribute to specify the layout of the struct.


using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace AssetStudio
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct TypeTreeNode
    {
        [MarshalAs(UnmanagedType.LPStr)]
        [JsonInclude]
        public string m_Type;

        [MarshalAs(UnmanagedType.LPStr)]
        [JsonInclude]
        public string m_Name;

        [JsonInclude]
        public int m_Level;

        [JsonInclude]
        public int m_MetaFlag;

        public TypeTreeNode(string type, string name, int level, bool align)
        {
            m_Type = type;
            m_Name = name;
            m_Level = level;
            m_MetaFlag = align ? 0x4000 : 0;
        }
    }
}