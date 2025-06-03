using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
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

        public TypeTreeNode(string type, string name, int level, bool align = false)
        {
            m_Type = type;
            m_Name = name;
            m_Level = level;
            m_MetaFlag = align ? 0x4000 : 0;
        }
    }
}