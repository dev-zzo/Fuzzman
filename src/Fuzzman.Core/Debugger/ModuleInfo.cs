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
    }
}
