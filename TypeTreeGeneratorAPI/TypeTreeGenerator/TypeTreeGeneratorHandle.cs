namespace TypeTreeGeneratorAPI.TypeTreeGenerator
{
    public class TypeTreeGeneratorHandle
    {
        public TypeTreeGenerator Instance { get; }

        public TypeTreeGeneratorHandle(string type, string unityVersionString)
        {
            switch (type)
            {
#if ENABLE_ASSET_STUDIO
                case "AssetStudio":
                    Instance = new AssetStudio.AssetStudioGenerator(unityVersionString);
                    break;
#endif
#if ENABLE_ASSETS_TOOLS
                case "AssetsTools":
                    Instance = new AssetsTools.AssetsToolsGenerator(unityVersionString);
                    break;
#endif
#if ENABLE_ASSET_RIPPER
                case "AssetRipper":
                    Instance = new AssetRipper.AssetRipperGenerator(unityVersionString);
                    break;
#endif
                default:
                    throw new ArgumentException($"Unknown TypeTreeGenerator type: {type}");
            }
        }
    }
}
