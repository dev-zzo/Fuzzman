﻿using System;
using System.IO;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.Platform.Mmap
{
    public class MappedFile : IDisposable
    {
        public MappedFile(string path, FileMode mode, FileAccess access, uint maxSize = 0)
        {
            this.stream = new FileStream(path, mode, access);
            this.fileHandle = this.stream.SafeFileHandle.DangerousGetHandle();

            this.mappingAccess = FileMapProtection.PageReadOnly;
            if (access.HasFlag(FileAccess.Write))
                this.mappingAccess = FileMapProtection.PageReadWrite;

            this.mappingHandle = Kernel32.CreateFileMapping(
                this.fileHandle,
                IntPtr.Zero,
                this.mappingAccess,
                0,
                maxSize,
                IntPtr.Zero);
            if (this.mappingHandle == IntPtr.Zero)
            {
                throw new Exception("Failed to create the mapping.");
            }
        }

        public MappedFileView CreateView(uint offset, uint length)
        {
            if (length == 0)
            {
                UInt64 longLength;
                Kernel32.GetFileSizeEx(this.fileHandle, out longLength);
                length = (uint)longLength;
            }
            return new MappedFileView(this.mappingHandle, this.mappingAccess, offset, length);
        }

        public void Dispose()
        {
            if (this.mappingHandle != IntPtr.Zero)
            {
                Kernel32.CloseHandle(this.mappingHandle);
                this.mappingHandle = IntPtr.Zero;
            }
            if (this.stream != null)
            {
                this.stream.Dispose();
                this.stream = null;
            }
        }

        private FileStream stream = null;
        private IntPtr fileHandle = IntPtr.Zero;
        private FileMapProtection mappingAccess;
        private IntPtr mappingHandle = IntPtr.Zero;
    }
}
