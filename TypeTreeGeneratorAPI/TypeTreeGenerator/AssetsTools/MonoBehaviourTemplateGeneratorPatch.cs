using AssetsTools.NET;
using AssetsTools.NET.Extra;
#if ENABLE_IL2CPP
using AssetsTools.NET.Cpp2IL;
using System.Reflection;
#endif

namespace TypeTreeGeneratorAPI.TypeTreeGenerator.AssetsTools
{
    public interface IMonoBehaviourTemplateGeneratorPatch : IMonoBehaviourTemplateGenerator
    {
        virtual AssetTypeTemplateField? GetTemplateFieldPatch(AssetTypeTemplateField templateField, string assemblyName, string nameSpace, string className, UnityVersion unityVersionExtra)
        {
            return GetTemplateField(templateField, assemblyName, nameSpace, className, unityVersionExtra);
        }
    }

#if ENABLE_IL2CPP
    public class Cpp2IlTempGeneratorPatch : Cpp2IlTempGenerator, IMonoBehaviourTemplateGeneratorPatch
    {
        public Cpp2IlTempGeneratorPatch() : base("", "")
        {
        }
        public void SetInitialized(bool initialized)
        {
            // This is a workaround to set the _initialized field to true
            // as we already initialized LibCpp2Il in the constructor
            var field = typeof(Cpp2IlTempGenerator).GetField("_initialized", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(this, initialized);
            }
            else
            {
                throw new InvalidOperationException("Could not find the _initialized field in Cpp2IlTempGenerator.");
            }
        }
    }
#endif

    public class MonoCecilTempGeneratorPatch : MonoCecilTempGenerator, IMonoBehaviourTemplateGeneratorPatch
    {
        public MonoCecilTempGeneratorPatch() : base("")
        {
        }

        public AssetTypeTemplateField? GetTemplateFieldPatch(AssetTypeTemplateField baseField, string assemblyName, string nameSpace, string className, UnityVersion unityVersion)
        {
            // 1:1 copy of the original method, but without filepath check and using the loadedAssemblies dictionary
            if (!assemblyName.EndsWith(".dll"))
            {
                assemblyName += ".dll";
            }
            var asm = loadedAssemblies[assemblyName];

            List<AssetTypeTemplateField> newFields = Read(asm, nameSpace, className, unityVersion);
            if (newFields == null)
            {
                return null;
            }

            AssetTypeTemplateField newBaseField = baseField.Clone();
            newBaseField.Children.AddRange(newFields);

            return newBaseField;
        }
    }
}
