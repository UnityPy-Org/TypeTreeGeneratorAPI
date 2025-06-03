using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using AssetsTools.NET.Extra;
using System.Reflection;

namespace TypeTreeGeneratorAPI.TypeTreeGenerator.AssetsTools
{
    public interface IMonoBehaviourTemplateGeneratorPatch : IMonoBehaviourTemplateGenerator
    {
        virtual AssetTypeTemplateField GetTemplateFieldPatch(AssetTypeTemplateField templateField, string assemblyName, string nameSpace, string className, UnityVersion unityVersionExtra)
        {
            return GetTemplateField(templateField, assemblyName, nameSpace, className, unityVersionExtra);
        }
    }

    public class Cpp2IlTempGeneratorPatch : Cpp2IlTempGenerator, IMonoBehaviourTemplateGeneratorPatch
    {
        public Cpp2IlTempGeneratorPatch() : base("", "")
        {
            // As we initialized LibCpp2Il, we can set the _initialized field to true
            // otherwise Cpp2IlTempGenerator will try to initialize again from the dummy paths
            typeof(Cpp2IlTempGenerator)
                .GetField("_initialized", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(this, true);
        }
    }

    public class MonoCecilTempGeneratorPatch : MonoCecilTempGenerator, IMonoBehaviourTemplateGeneratorPatch
    {
        public MonoCecilTempGeneratorPatch() : base("")
        {
        }

        public AssetTypeTemplateField GetTemplateFieldPatch(AssetTypeTemplateField baseField, string assemblyName, string nameSpace, string className, UnityVersion unityVersion)
        {
            // 1:1 copy of the original method, but without filepath check and using the loadedAssemblies dictionary
            if (!assemblyName.EndsWith(".dll"))
            {
                assemblyName += ".dll";
            }
            var asm = loadedAssemblies[assemblyName];

            List<AssetTypeTemplateField> newFields = Read(asm, nameSpace, className, unityVersion);

            AssetTypeTemplateField newBaseField = baseField.Clone();
            newBaseField.Children.AddRange(newFields);

            return newBaseField;
        }
    }
}
