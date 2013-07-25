using System;
using System.Runtime.InteropServices;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.System.Mmap
{
    /// <summary>
    /// Implements a view of the memory-mapped file.
    /// </summary>
    public class MappedFileView : IDisposable
    {
        internal MappedFileView(IntPtr hMapping, FileMapProtection protection, uint offset, uint length)
        {
            this.mappingLength = length;

            FileMapAccess access = FileMapAccess.Read;
            if (protection == FileMapProtection.PageReadWrite)
                access |= FileMapAccess.Write;

            this.viewBase = Kernel32.MapViewOfFileEx(
                hMapping,
                access,
                0,
                offset,
                (IntPtr)length,
                IntPtr.Zero);
            if (this.viewBase == IntPtr.Zero)
            {
                throw new Exception("Failed to create a view.");
            }
        }

        /// <summary>
        /// Access the raw pointer for e.g. bulk copy operations.
        /// </summary>
        public IntPtr BasePointer { get { return this.viewBase; } }

        /// <summary>
        /// Get the mapped length.
        /// </summary>
        public uint Length { get { return this.mappingLength; } }

        /// <summary>
        /// Simple byte-wise access. Slow.
        /// </summary>
        /// <param name="offset">Access offset.</param>
        /// <returns></returns>
        public byte this[uint offset]
        {
            get
            {
                if (offset >= this.mappingLength)
                {
                    throw new ArgumentException("Reading out of mapped area.");
                }

                IntPtr ptr = (IntPtr)((ulong)this.viewBase + offset);
                return Marshal.ReadByte(ptr);
            }
            set
            {
                if (offset >= this.mappingLength)
                {
                    throw new ArgumentException("Reading out of mapped area.");
                }

                IntPtr ptr = (IntPtr)((ulong)this.viewBase + offset);
                Marshal.WriteByte(ptr, value);
            }
        }

        /// <summary>
        /// Read arbitrary value-types.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Read<T>(uint offset, out T data) where T : struct
        {
            Type type = typeof(T);
            if (offset + Marshal.SizeOf(type) > this.mappingLength)
            {
                throw new ArgumentException("Reading out of mapped area.");
            }

            IntPtr ptr = (IntPtr)((ulong)this.viewBase + offset);
            object obj = Marshal.PtrToStructure(ptr, type);
            data = (T)obj;
        }

        /// <summary>
        /// Write arbitrary value-types.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="offset"></param>
        /// <param name="data"></param>
        public void Write<T>(uint offset, T data)
        {
            if (offset + Marshal.SizeOf(data) > this.mappingLength)
            {
                throw new ArgumentException("Writing out of mapped area.");
            }

            IntPtr ptr = (IntPtr)((ulong)this.viewBase + offset);
            Marshal.StructureToPtr(data, ptr, false);
        }

        public void Dispose()
        {
            if (this.viewBase != IntPtr.Zero)
            {
                Kernel32.UnmapViewOfFile(this.viewBase);
                this.viewBase = IntPtr.Zero;
            }
        }

        private IntPtr viewBase = IntPtr.Zero;
        private uint mappingLength;
    }
}
