using AssetStudio;
using System.Runtime.InteropServices;

namespace TypeTreeGeneratorAPI
{
    public static class NativeAPI
    {
        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_init")]
        public static IntPtr TypeTreeGenerator_create(IntPtr unityVersionPtr)
        {
            string? unityVersion = Marshal.PtrToStringUTF8(unityVersionPtr);
            if (unityVersion == null)
            {
                return IntPtr.Zero;
            }
            try
            {
                var typeTreeGenerator = new TypeTreeGenerator_AssetStudio(unityVersion);
                return GCHandle.ToIntPtr(GCHandle.Alloc(typeTreeGenerator));
            }
            catch
            {
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
                var typeTreeGenerator = (TypeTreeGenerator_AssetStudio)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                using (var stream = new UnmanagedMemoryStream(dllPtr, dllLength, dllLength, FileAccess.ReadWrite))
                {
                    if (stream == null)
                    {
                        return -1;
                    }
                    typeTreeGenerator.LoadDLL(stream);
                }
                return 0;
            }
            catch
            {
                return -1;
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_loadIL2CPP")]
        unsafe public static int TypeTreeGenerator_loadIL2CPP(IntPtr typeTreeGeneratorPtr, byte* assemblyDataPtr, int assemblyDataLength, byte* metadataDataPtr, int metadataDataLength)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero || assemblyDataPtr == null || metadataDataPtr == null)
            {
                return -1;
            }

            try
            {
                var typeTreeGenerator = (TypeTreeGenerator_AssetStudio)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
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

                typeTreeGenerator.LoadIL2CPP(assemblyData, metadataData);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_del")]
        public static int TypeTreeGenerator_delete(IntPtr typeTreeGeneratorPtr)
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
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_generateTreeNodesJson")]
        public static int TypeTreeGenerator_generateTypeTreeNodesJson(IntPtr typeTreeGeneratorPtr, IntPtr assemblyNamePtr, IntPtr fullNamePtr, IntPtr jsonAddr)
        {
            string? assemblyName = Marshal.PtrToStringUTF8(assemblyNamePtr);
            string? fullName = Marshal.PtrToStringUTF8(fullNamePtr);

            if (typeTreeGeneratorPtr == IntPtr.Zero || assemblyName == null || fullName == null)
            {
                return -1;
            }
            try
            {
                var typeTreeGenerator = (TypeTreeGenerator_AssetStudio)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;

                var typeTreeNodes = typeTreeGenerator.GenerateTreeNodes(assemblyName, fullName);
                if (typeTreeNodes == null)
                {
                    return -1;
                }
                var json = TypeTreeNodeSerializer.ToJson(typeTreeNodes!);
                Marshal.WriteIntPtr(jsonAddr, Marshal.StringToCoTaskMemUTF8(json));
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_generateTreeNodesRaw")]
        public static int TypeTreeGenerator_generateTypeTreeNodesRaw(IntPtr typeTreeGeneratorPtr, IntPtr assemblyNamePtr, IntPtr fullNamePtr, IntPtr arrAddrPtr, IntPtr arrLengthPtr)
        {
            string? assemblyName = Marshal.PtrToStringUTF8(assemblyNamePtr);
            string? fullName = Marshal.PtrToStringUTF8(fullNamePtr);

            if (typeTreeGeneratorPtr == IntPtr.Zero || assemblyName == null || fullName == null)
            {
                return -1;
            }
            try
            {
                var typeTreeGenerator = (TypeTreeGenerator_AssetStudio)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                var typeTreeNodes = typeTreeGenerator.GenerateTreeNodes(assemblyName, fullName);
                if (typeTreeNodes == null)
                {
                    return -1;
                }
                var (arrayPtr, arrayLength) = TypeTreeNodeSerializer.ToRaw(typeTreeNodes!);

                Marshal.WriteIntPtr(arrAddrPtr, arrayPtr);
                Marshal.WriteInt32(arrLengthPtr, arrayLength);

                return 0;
            }
            catch
            {
                return -1;
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

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_getMonoBehaviorDefinitions")]
        public static int TypeTreeGenerator_getMonoBehaviorDefinitions(IntPtr typeTreeGeneratorPtr, IntPtr arrAddrPtr, IntPtr arrLengthPtr)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero)
            {
                return -1;
            }
            try
            {
                var typeTreeGenerator = (TypeTreeGenerator_AssetStudio)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;

                var typeNames = typeTreeGenerator.GetMonoBehaviourDefinitions();

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
            catch
            {
                return -1;
            }
        }


        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_freeMonoBehaviorDefinitions")]
        public static int TypeTreeGenerator_freeMonoBehaviorDefinitions(IntPtr arrAddr, int arrLength)
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

        [UnmanagedCallersOnly(EntryPoint = "FreeCoTaskMem")]
        public static void FreeCoTaskMem(IntPtr ptr)
        {
            Marshal.FreeCoTaskMem(ptr);
        }
    }
}