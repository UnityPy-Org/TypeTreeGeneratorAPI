# TypeTreeGenerator API

A simple project for offering a native API to extract typetrees from Unity assemblies.
For this .net9 with AoT gets used in combination with a dedicated translation layer.

## TODO:

- C++ headers
- documentation
- tests
- automated release workflow

## Planned:

- Il2CPPInspector-Redux as additional backend for il2cpp
- using TPK dumps instead of Unity's reference (especially due to this usage of the reference most likely violating the license)

## Credits:

- [nesrak1/AssetsTools.NET](https://github.com/nesrak1/AssetsTools.NET) - AssetsTools backend
- [AssetRipper/AssetRipper](https://github.com/AssetRipper/AssetRipper) - AssetRipper backend
- [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio) - AssetStudio backend
- [Unity-Technologies/UnityCsReference](https://github.com/Unity-Technologies/UnityCsReference) - part of the AssetStudio backend
