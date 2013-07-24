using System;
using System.Text;
using Fuzzman.Core.Interop;
using System.Runtime.InteropServices;

namespace Fuzzman.Core.Debugger.Simple
{
    internal static class DebuggerHelper
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
        public static object ReadTargetStructure(IntPtr processHandle, IntPtr address, Type type)
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
            UNICODE_STRING str = (UNICODE_STRING)ReadTargetStructure(processHandle, addr, typeof(UNICODE_STRING));
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

    }
}
