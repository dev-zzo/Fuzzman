using System;

namespace Fuzzman.Core.Debugger
{
    /// <summary>
    /// Represents the known module data.
    /// </summary>
    public struct ModuleInfo
    {
        public string Name;

        public IntPtr BaseAddress;

        public uint MappedSize;

        public string FullPath;

        public override string ToString()
        {
            return String.Format("{0:X8} {1:X8} {2}", (uint)this.BaseAddress, this.MappedSize, this.Name);
        }
    }
}
