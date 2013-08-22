using System;
using Microsoft.Win32;

namespace Fuzzman.Agent.Actions
{
    [Serializable]
    public abstract class RegistryActionBase : ActionBase
    {
        protected static RegistryKey GetBaseKey(string path)
        {
            int firstSlash = path.IndexOf('\\');
            if (firstSlash == -1)
            {
                throw new ArgumentException("Key path does not contain base key.");
            }

            string baseKeyName = path.Substring(0, firstSlash);
            switch (baseKeyName)
            {
                case "HKCU":
                    return Registry.CurrentUser;
                case "HKCR":
                    return Registry.ClassesRoot;
                case "HCU":
                    return Registry.CurrentUser;
                case "HKLM":
                    return Registry.LocalMachine;
                default:
                    throw new ArgumentException("Invalid base key in path.");
            }
        }

        protected static string GetRelativePath(string path)
        {
            int firstSlash = path.IndexOf('\\');
            if (firstSlash == -1)
            {
                throw new ArgumentException("Key path does not contain base key.");
            }

            return path.Substring(firstSlash + 1);
        }

        protected static string GetKeyPath(string path)
        {
            path = GetRelativePath(path);
            int lastSlash = path.LastIndexOf('\\');
            return path.Substring(0, lastSlash);
        }

        protected static string GetValueName(string path)
        {
            int lastSlash = path.LastIndexOf('\\');
            return path.Substring(lastSlash + 1);
        }
    }
}
