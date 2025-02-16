using AssetStudio;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace TypeTreeGeneratorAPI
{
    public static class NativeAPI
    {
        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_init")]
        unsafe public static IntPtr TypeTreeGenerator_create(IntPtr unityVersionPtr)
        {
            string? unityVersion = Marshal.PtrToStringUTF8(unityVersionPtr);
            if (unityVersion == null)
            {
                return 0;
            }
            try
            {
                var typeTreeGenerator = new TypeTreeGenerator(unityVersion);
                return (IntPtr)GCHandle.Alloc(typeTreeGenerator);
            }
            catch
            {
                return 0;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_loadDLL")]
        unsafe public static int TypeTreeGenerator_loadDLL(IntPtr typeTreeGeneratorPtr, IntPtr dllPtr, int dllLength)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero)
            {
                return -1;
            }
            try
            {
                var typeTreeGenerator = (TypeTreeGenerator)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                using (var stream = new UnmanagedMemoryStream((byte*)dllPtr, dllLength, dllLength, FileAccess.ReadWrite))
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
        unsafe public static int TypeTreeGenerator_loadIL2CPP(IntPtr typeTreeGeneratorPtr, IntPtr assemblyDataPtr, int assemblyDataLength, IntPtr metadataDataPtr, int metadataDataLength)
        {
            if (typeTreeGeneratorPtr == IntPtr.Zero)
            {
                return -1;
            }
            try
            {
                var typeTreeGenerator = (TypeTreeGenerator)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                byte[] assemblyData = new byte[assemblyDataLength];
                byte[] metadataData = new byte[metadataDataLength];

                fixed (byte* managedPtr = assemblyData)
                {
                    Buffer.MemoryCopy((void*)assemblyDataPtr, managedPtr, assemblyDataLength, assemblyDataLength);
                }
                fixed (byte* managedPtr = metadataData)
                {
                    Buffer.MemoryCopy((void*)metadataDataPtr, managedPtr, metadataDataLength, metadataDataLength);
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
                var typeTreeGenerator = (TypeTreeGenerator)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;
                GCHandle.FromIntPtr(typeTreeGeneratorPtr).Free();
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_generateTreeNodesJson")]
        unsafe public static int TypeTreeGenerator_generateTypeTreeNodesJson(IntPtr typeTreeGeneratorPtr, IntPtr assemblyNamePtr, IntPtr fullNamePtr, IntPtr jsonAddr, IntPtr jsonLength)
        {
            string? assemblyName = Marshal.PtrToStringUTF8(assemblyNamePtr);
            string? fullName = Marshal.PtrToStringUTF8(fullNamePtr);

            if (typeTreeGeneratorPtr == IntPtr.Zero || assemblyName == null || fullName == null)
            {
                return -1;
            }
            try
            {
                var typeTreeGenerator = (TypeTreeGenerator)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;

                var typeTreeNodes = typeTreeGenerator.GenerateTreeNodes(assemblyName, fullName);
                if (typeTreeNodes == null)
                {
                    return -1;
                }
                var json = TypeTreeNodeSerializer.ToJson(typeTreeNodes!);
                var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

                // Allocate memory for the JSON string
                IntPtr jsonPtr = Marshal.AllocCoTaskMem(jsonBytes.Length);
                Marshal.Copy(jsonBytes, 0, jsonPtr, jsonBytes.Length);

                // Write the JSON pointer to the address specified by `jsonAddr`
                Marshal.WriteIntPtr(jsonAddr, jsonPtr);

                // Write the JSON length to the address specified by `jsonLength`
                Marshal.WriteInt32(jsonLength, jsonBytes.Length);

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "TypeTreeGenerator_generateTreeNodesRaw")]
        unsafe public static int TypeTreeGenerator_generateTypeTreeNodesRaw(IntPtr typeTreeGeneratorPtr, IntPtr assemblyNamePtr, IntPtr fullNamePtr, IntPtr arrAddrPtr, IntPtr arrLengthPtr)
        {
            string? assemblyName = Marshal.PtrToStringUTF8(assemblyNamePtr);
            string? fullName = Marshal.PtrToStringUTF8(fullNamePtr);

            if (typeTreeGeneratorPtr == IntPtr.Zero || assemblyName == null || fullName == null)
            {
                return -1;
            }
            try
            {
                var typeTreeGenerator = (TypeTreeGenerator)GCHandle.FromIntPtr(typeTreeGeneratorPtr).Target!;

                var typeTreeNodes = typeTreeGenerator.GenerateTreeNodes(assemblyName, fullName);
                if (typeTreeNodes == null)
                {
                    return -1;
                }
                var (arrayPtr, arrayLength) = TypeTreeNodeSerializer.ToRaw(typeTreeNodes!);

                // Write the JSON pointer to the address specified by `jsonAddr`
                Marshal.WriteIntPtr(arrAddrPtr, arrayPtr);

                // Write the JSON length to the address specified by `jsonLength`
                Marshal.WriteInt32(arrLengthPtr, arrayLength);

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "FreeCoTaskMem")]
        public static void FreeCoTaskMem(IntPtr ptr)
        {
            Marshal.FreeCoTaskMem(ptr);
        }
    }
}