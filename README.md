# TypeTreeGenerator API

A simple project for offering a native API to extract typetrees from Unity assemblies.
For this .net9 with AoT gets used in combination with a dedicated translation layer.

## TODO:

- python bindings
- c++ bindings
- documentation
- tests
- automated release workflow
- credits

## Planned:

- Il2CPPInspector-Redux as additional backend for il2cpp
- using TPK dumps instead of Unity's reference (especially due to this usage of the reference most likely violating the license)

## Test Locally
```bash
dotnet restore
# Change your runtime identifier if needed (e.g. linux-x64, osx-x64, win-x64, etc.)
dotnet publish -c Release -r win-x64
# Build Python bindings
cd bindings\python
# Install
pip install .
```