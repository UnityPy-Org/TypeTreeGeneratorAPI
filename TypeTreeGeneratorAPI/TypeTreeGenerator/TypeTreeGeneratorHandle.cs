namespace TypeTreeGeneratorAPI.TypeTreeGenerator
{
    public class TypeTreeGeneratorHandle
    {
        public TypeTreeGenerator Instance { get; }

        public TypeTreeGeneratorHandle(string type, string unityVersionString)
        {
            switch (type)
            {
                case "AssetStudio":
                    Instance = new AssetStudio.AssetStudioGenerator(unityVersionString);
                    break;
                case "AssetsTools":
                    Instance = new AssetsTools.AssetsToolsGenerator(unityVersionString);
                    break;
                case "AssetRipper":
                    Instance = new AssetRipper.AssetRipperGenerator(unityVersionString);
                    break;
                default:
                    throw new ArgumentException($"Unknown TypeTreeGenerator type: {type}");
            }
        }
    }
}
