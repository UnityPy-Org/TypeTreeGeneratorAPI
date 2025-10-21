using System.Runtime.InteropServices;
using TypeTreeGeneratorAPI.TypeTreeGenerator;

namespace TypeTreeGeneratorAPI
{
    public static class NativeAPI
    {
        private static bool addMonoBehaviourRootNodes = true;

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_setAddMonoBehaviourRootNodes")]
        public static int TypeTreeGenerator_Config_setAddMonoBehaviourRootNodes(byte value)
        {
            switch (value)
            {
                case 0:
                    addMonoBehaviourRootNodes = false;
                    break;
                case 1:
                    addMonoBehaviourRootNodes = true;
                    break;
                default:
                    Console.WriteLine($"Expected True(1)/False(0), but got {value} for TypeTreeGenerator_setAddMonoBehaviourRootNodes");
                    return -1;
            }
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_init")]
        public static IntPtr TypeTreeGenerator_init(IntPtr unityVersionPtr, IntPtr generatorName)
        {
            string? unityVersion = Marshal.PtrToStringUTF8(unityVersionPtr);
            string? generatorNameStr = Marshal.PtrToStringUTF8(generatorName);
            if (unityVersion == null)
            {
                return IntPtr.Zero;
            }
            try
            {
                var handle = new TypeTreeGeneratorHandle((generatorNameStr != null) ? generatorNameStr : "AssetStudio", unityVersion);
                return GCHandle.ToIntPtr(GCHandle.Alloc(handle));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating TypeTreeGenerator:\n{ex.Message}");
                // If the generator type is not recognized, we return IntPtr.Zero to indicate failure.
                // This should be handled by the caller to avoid further issues.
                Console.WriteLine("Failed to create TypeTreeGenerator instance. Ensure the generator name is correct and supported.");
                return IntPtr.Zero;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_loadDLL")]
        unsafe public static int TypeTreeGenerator_loadDLL(IntPtr typeTreeGeneratorPtr, byte* dllPtr, int dllLength)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero)
            {
                return -1;
            }
            try
            {
                var handle = (TypeTreeGeneratorHandle)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                using (var stream = new UnmanagedMemoryStream(dllPtr, dllLength, dllLength, FileAccess.ReadWrite))
                {
                    if (stream == null)
                    {
                        return -1;
                    }
                    handle.Instance.LoadDll(stream);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dll:\n{ex.Message}");
                return -1;
            }
        }


#if ENABLE_IL2CPP
        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_loadIL2CPP")]
        unsafe public static int TypeTreeGenerator_loadIL2CPP(IntPtr typeTreeGeneratorPtr, byte* assemblyDataPtr, int assemblyDataLength, byte* metadataDataPtr, int metadataDataLength)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero || assemblyDataPtr == null || metadataDataPtr == null)
            {
                return -1;
            }

            try
            {
                var handle = (TypeTreeGeneratorHandle)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                byte[] assemblyData = new byte[assemblyDataLength];
                byte[] metadataData = new byte[metadataDataLength];

                fixed (byte* managedPtr = assemblyData)
                {
                    Buffer.MemoryCopy(assemblyDataPtr, managedPtr, assemblyDataLength, assemblyDataLength);
                }
                fixed (byte* managedPtr = metadataData)
                {
                    Buffer.MemoryCopy(metadataDataPtr, managedPtr, metadataDataLength, metadataDataLength);
                }

                handle.Instance.LoadIl2Cpp(assemblyData, metadataData);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading il2cpp:\n{ex.Message}");
                return -1;
            }
        }
#endif

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_del")]
        public static int TypeTreeGenerator_del(IntPtr typeTreeGeneratorPtr)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero)
            {
                return -1;
            }
            try
            {
                var gch = GCHandle.FromIntPtr(typeTreeGeneratorPtr);
                //var typeTreeGenerator = (TypeTreeGenerator)gch.Target!;
                gch.Free();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting TypeTreeGenerator:\n{ex.Message}");
                return -1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_getLoadedDLLNames")]
        public static IntPtr TypeTreeGenerator_getLoadedDLLNames(IntPtr typeTreeGeneratorPtr)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero)
            {
                return 0;
            }
            var handle = (TypeTreeGeneratorHandle)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
            var names = handle.Instance.GetAssemblyNames();
            if (names == null || names.Count == 0)
            {
                return 0;
            }
            var json = string.Join(",", names.Select(name => $"\"{name}\""));
            return Marshal.StringToCoTaskMemUTF8($"[{json}]");
        }

        public enum GenerateTreeNodesResult
        {
            Ok = 0,
            Error = -1,
            NotFound = -2,
        }

        private static List<TypeTreeNode>? GenerateTypeTreeNodes(TypeTreeGeneratorHandle handle, string assemblyName, string fullName)
        {
            var typeTreeNodes = handle.Instance.GenerateTreeNodes(assemblyName, fullName);
            if (addMonoBehaviourRootNodes && typeTreeNodes != null && fullName != "UnityEngine.MonoBehaviour")
            {
                var monoBehaviourRootNodes = handle.Instance.GetMonoBehaviourRootNodes();
                if (typeTreeNodes.Count == 0)
                {
                    return monoBehaviourRootNodes;
                }
                if (typeTreeNodes[0].m_Level == 0)
                {
                    // move head to MonoBehaviour head
                    monoBehaviourRootNodes[0] = typeTreeNodes[0];
                    typeTreeNodes = typeTreeNodes.Slice(0, typeTreeNodes.Count - 1);
                }
                return monoBehaviourRootNodes.Concat(typeTreeNodes).ToList();
            }
            return typeTreeNodes;
        }


        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_generateTreeNodesJson")]
        public static GenerateTreeNodesResult TypeTreeGenerator_generateTreeNodesJson(IntPtr typeTreeGeneratorPtr, IntPtr assemblyNamePtr, IntPtr fullNamePtr, IntPtr jsonAddr)
        {
            string? assemblyName = Marshal.PtrToStringUTF8(assemblyNamePtr);
            string? fullName = Marshal.PtrToStringUTF8(fullNamePtr);

            if (typeTreeGeneratorPtr == IntPtr.Zero || assemblyName == null || fullName == null)
            {
                return GenerateTreeNodesResult.Error;
            }
            try
            {
                var handle = (TypeTreeGeneratorHandle)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                var typeTreeNodes = GenerateTypeTreeNodes(handle, assemblyName, fullName);
                if (typeTreeNodes == null)
                {
                    Marshal.WriteIntPtr(jsonAddr, IntPtr.Zero);
                    return GenerateTreeNodesResult.NotFound;
                }
                var json = TypeTreeNodeSerializer.ToJson(typeTreeNodes!);
                Marshal.WriteIntPtr(jsonAddr, Marshal.StringToCoTaskMemUTF8(json));
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating tree nodes:\n{ex.Message}");
                return GenerateTreeNodesResult.Error;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_generateTreeNodesRaw")]
        public static GenerateTreeNodesResult TypeTreeGenerator_generateTreeNodesRaw(IntPtr typeTreeGeneratorPtr, IntPtr assemblyNamePtr, IntPtr fullNamePtr, IntPtr arrAddrPtr, IntPtr arrLengthPtr)
        {
            string? assemblyName = Marshal.PtrToStringUTF8(assemblyNamePtr);
            string? fullName = Marshal.PtrToStringUTF8(fullNamePtr);

            if (typeTreeGeneratorPtr == IntPtr.Zero || assemblyName == null || fullName == null)
            {
                return GenerateTreeNodesResult.Error;
            }
            try
            {
                var handle = (TypeTreeGeneratorHandle)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                var typeTreeNodes = GenerateTypeTreeNodes(handle, assemblyName, fullName);
                if (typeTreeNodes == null)
                {
                    Marshal.WriteIntPtr(arrAddrPtr, IntPtr.Zero);
                    Marshal.WriteInt32(arrLengthPtr, 0);
                    return GenerateTreeNodesResult.NotFound;
                }
                var (arrayPtr, arrayLength) = TypeTreeNodeSerializer.ToRaw(typeTreeNodes!);

                Marshal.WriteIntPtr(arrAddrPtr, arrayPtr);
                Marshal.WriteInt32(arrLengthPtr, arrayLength);

                return GenerateTreeNodesResult.Ok;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating tree nodes:\n{ex.Message}");
                return GenerateTreeNodesResult.Error;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_freeTreeNodesRaw")]
        public static int TypeTreeGenerator_freeTreeNodesRaw(IntPtr arrAddr, int arrLength)
        {
            if (arrAddr == IntPtr.Zero)
            {
                return -1;
            }
            for (int i = 0; i < arrLength; i++)
            {
                Marshal.DestroyStructure<TypeTreeNode>(arrAddr + i * Marshal.SizeOf<TypeTreeNode>());
            }
            Marshal.FreeCoTaskMem(arrAddr);
            return 0;
        }

        public static int _TypeTreeGenerator_getMonoBehaviourDefinitions(IntPtr typeTreeGeneratorPtr, IntPtr arrAddrPtr, IntPtr arrLengthPtr)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero)
            {
                return -1;
            }
            try
            {
                var handle = (TypeTreeGeneratorHandle)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;

                var typeNames = handle.Instance.GetMonoBehaviourDefinitions();

                var arrayLength = typeNames.Count;
                var arrayPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf<IntPtr>() * arrayLength * 2);
                for (int i = 0; i < arrayLength; i++)
                {
                    string module = typeNames[i].Item1;
                    string fullName = typeNames[i].Item2;
                    Marshal.WriteIntPtr(arrayPtr, (i * 2) * Marshal.SizeOf<IntPtr>(), Marshal.StringToCoTaskMemUTF8(module));
                    Marshal.WriteIntPtr(arrayPtr, (i * 2 + 1) * Marshal.SizeOf<IntPtr>(), Marshal.StringToCoTaskMemUTF8(fullName));
                }

                Marshal.WriteIntPtr(arrAddrPtr, arrayPtr);
                Marshal.WriteInt32(arrLengthPtr, arrayLength);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting MonoBehaviour definitions:\n{ex.Message}");
                return -1;
            }
        }

        public static int _TypeTreeGenerator_freeMonoBehaviourDefinitions(IntPtr arrAddr, int arrLength)
        {
            if (arrAddr == IntPtr.Zero)
            {
                return -1;
            }
            for (int i = 0; i < arrLength; i++)
            {
                var modulePtr = Marshal.ReadIntPtr(arrAddr, i * IntPtr.Size * 2);
                var fullNamePtr = Marshal.ReadIntPtr(arrAddr, i * IntPtr.Size * 2 + IntPtr.Size);
                Marshal.FreeCoTaskMem(modulePtr);
                Marshal.FreeCoTaskMem(fullNamePtr);
            }
            Marshal.FreeCoTaskMem(arrAddr);
            return 0;
        }

        // alias for backward compatibility
        // alias for backward compatibility
        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_getMonoBehaviourDefinitions")]
        public static int TypeTreeGenerator_getMonoBehaviourDefinitions(IntPtr typeTreeGeneratorPtr, IntPtr arrAddrPtr, IntPtr arrLengthPtr)
        {
            return _TypeTreeGenerator_getMonoBehaviourDefinitions(typeTreeGeneratorPtr, arrAddrPtr, arrLengthPtr);
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_freeMonoBehaviourDefinitions")]
        public static int TypeTreeGenerator_freeMonoBehaviourDefinitions(IntPtr arrAddr, int arrLength)
        {
            return _TypeTreeGenerator_freeMonoBehaviourDefinitions(arrAddr, arrLength);
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_getMonoBehaviorDefinitions")]
        public static int TypeTreeGenerator_getMonoBehaviorDefinitions(IntPtr typeTreeGeneratorPtr, IntPtr arrAddrPtr, IntPtr arrLengthPtr)
        {
            return _TypeTreeGenerator_getMonoBehaviourDefinitions(typeTreeGeneratorPtr, arrAddrPtr, arrLengthPtr);
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_freeMonoBehaviorDefinitions")]
        public static int TypeTreeGenerator_freeMonoBehaviorDefinitions(IntPtr arrAddr, int arrLength)
        {
            return _TypeTreeGenerator_freeMonoBehaviourDefinitions(arrAddr, arrLength);
        }



        [UnmanagedCallersOnly(EntryPoint = "FreeCoTaskMem")]
        public static void FreeCoTaskMem(IntPtr ptr)
        {
            Marshal.FreeCoTaskMem(ptr);
        }
    }
}