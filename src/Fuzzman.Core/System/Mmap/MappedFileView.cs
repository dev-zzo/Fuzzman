using System;
using System.Runtime.InteropServices;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.System.Mmap
{
    public class MappedFileView : IDisposable
    {
        internal MappedFileView(IntPtr hMapping, FileMapProtection protection, ulong offset, ulong length)
        {
            this.mappingLength = length;

            FileMapAccess access = FileMapAccess.Read;
            if (protection == FileMapProtection.PageReadWrite)
                access |= FileMapAccess.Write;

            this.viewBase = Kernel32.MapViewOfFileEx(
                hMapping,
                access,
                (uint)(offset >> 32),
                (uint)offset,
                (IntPtr)length,
                IntPtr.Zero);
            if (this.viewBase == IntPtr.Zero)
            {
                throw new Exception("Failed to create a view.");
            }
        }

        public void Dispose()
        {
            if (this.viewBase != IntPtr.Zero)
            {
                Kernel32.UnmapViewOfFile(this.viewBase);
                this.viewBase = IntPtr.Zero;
            }
        }

        public byte this[ulong position]
        {
            get
            {
                IntPtr ptr = (IntPtr)((ulong)this.viewBase + position);
                return Marshal.ReadByte(ptr);
            }
            set
            {
                IntPtr ptr = (IntPtr)((ulong)this.viewBase + position);
                Marshal.WriteByte(ptr, value);
            }
        }

        public void Read<T>(ulong position, out T data) where T : struct
        {
            Type type = typeof(T);
            if (position + (ulong)Marshal.SizeOf(type) > this.mappingLength)
            {
                throw new ArgumentException("Reading out of mapped area.");
            }

            IntPtr ptr = (IntPtr)((ulong)this.viewBase + position);
            object obj = Marshal.PtrToStructure(ptr, type);
            data = (T)obj;
        }

        public void Write<T>(ulong position, T data)
        {
            if (position + (ulong)Marshal.SizeOf(data) > this.mappingLength)
            {
                throw new ArgumentException("Writing out of mapped area.");
            }

            IntPtr ptr = (IntPtr)((ulong)this.viewBase + position);
            Marshal.StructureToPtr(data, ptr, false);
        }

        private IntPtr viewBase = IntPtr.Zero;
        private ulong mappingLength;
    }
}
