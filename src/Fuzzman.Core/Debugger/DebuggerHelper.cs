using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.Debugger.Simple
{
    public static class DebuggerHelper
    {
        public static string ReadNullTerminatedStringAscii(IntPtr processHandle, IntPtr addr)
        {
            StringBuilder builder = new StringBuilder(128);
            byte[] data = new byte[1];
            for (; ; )
            {
                uint bytesRead;
                Kernel32.ReadProcessMemory(processHandle, addr, data, 1, out bytesRead);
                addr += 1;
                char c = (char)data[0];
                if (c == 0)
                    break;
                builder.Append(c);
            }
            return builder.ToString();
        }

        public static string ReadNullTerminatedStringUnicode(IntPtr processHandle, IntPtr addr)
        {
            StringBuilder builder = new StringBuilder(128);
            byte[] data = new byte[2];
            for (; ; )
            {
                uint bytesRead;
                Kernel32.ReadProcessMemory(processHandle, addr, data, 2, out bytesRead);
                addr += 2;
                char c = (char)(data[0] + (data[1] << 8));
                if (c == 0)
                    break;
                builder.Append(c);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Read an arbitrary data type from the target's memory space.
        /// </summary>
        /// <param name="processHandle"></param>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ReadTargetMemory(IntPtr processHandle, IntPtr address, Type type)
        {
            int size = Marshal.SizeOf(type);
            byte[] data = new byte[size];
            uint bytesRead = 0;
            Kernel32.ReadProcessMemory(processHandle, address, data, (uint)size, out bytesRead);

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            object result = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), type);
            handle.Free();

            return result;
        }

        /// <summary>
        /// Reads the value of the UNICODE_STRING object from the target's memory space.
        /// </summary>
        /// <param name="processHandle"></param>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static string ReadUnicodeString(IntPtr processHandle, IntPtr addr)
        {
            UNICODE_STRING str = (UNICODE_STRING)ReadTargetMemory(processHandle, addr, typeof(UNICODE_STRING));
            return ReadUnicodeString(processHandle, str);
        }

        /// <summary>
        /// Reads the value of the UNICODE_STRING object from the target's memory space.
        /// </summary>
        /// <param name="processHandle"></param>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static string ReadUnicodeString(IntPtr processHandle, UNICODE_STRING str)
        {
            byte[] buffer = new byte[str.Length];
            uint bytesRead;
            Kernel32.ReadProcessMemory(processHandle, str.Buffer, buffer, str.Length, out bytesRead);
            return Encoding.Unicode.GetString(buffer);
        }

        public static List<ModuleInfo> BuildModuleList(IntPtr processHandle, IntPtr processPebAddress)
        {
            List<ModuleInfo> modules = new List<ModuleInfo>();

            PEB peb = (PEB)DebuggerHelper.ReadTargetMemory(
                processHandle,
                processPebAddress,
                typeof(PEB));
            PEB_LDR_DATA loaderData = (PEB_LDR_DATA)DebuggerHelper.ReadTargetMemory(
                processHandle,
                peb.LoaderData,
                typeof(PEB_LDR_DATA));

            IntPtr loaderDataAnchor = peb.LoaderData + (int)Marshal.OffsetOf(typeof(PEB_LDR_DATA), "InLoadOrderModuleList");
            IntPtr currentModulePtr = loaderData.InLoadOrderModuleList.Flink;
            while (currentModulePtr != loaderDataAnchor)
            {
                LDR_MODULE module = (LDR_MODULE)DebuggerHelper.ReadTargetMemory(
                    processHandle,
                    currentModulePtr,
                    typeof(LDR_MODULE));

                ModuleInfo moduleInfo = new ModuleInfo();
                moduleInfo.Name = ReadUnicodeString(processHandle, module.BaseDllName);
                moduleInfo.FullPath = ReadUnicodeString(processHandle, module.FullDllName);
                moduleInfo.BaseAddress = module.BaseAddress;
                moduleInfo.MappedSize = module.SizeOfImage;
                modules.Add(moduleInfo);

                currentModulePtr = module.InLoadOrderModuleList.Flink;
            }

            return modules;
        }

        public static Location LocateModuleOffset(IntPtr processHandle, IntPtr processPebAddress, IntPtr targetAddress)
        {
            List<ModuleInfo> modules = BuildModuleList(processHandle, processPebAddress);
            foreach (ModuleInfo module in modules)
            {
                long diff = (long)targetAddress - (long)module.BaseAddress;
                if (diff < 0)
                    continue;
                if (diff > module.MappedSize)
                    continue;
                return new Location() { ModuleName = module.Name, Offset = (uint)diff };
            }
            return null;
        }
    }
}
