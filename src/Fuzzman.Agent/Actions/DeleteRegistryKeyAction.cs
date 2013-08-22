using System;
using Microsoft.Win32;

namespace Fuzzman.Agent.Actions
{
    [Serializable]
    public class DeleteRegistryKeyAction : RegistryActionBase
    {
        public string Key { get; set; }

        public override void Execute()
        {
            try
            {
                RegistryKey baseKey = GetBaseKey(this.Key);
                baseKey.DeleteSubKeyTree(GetRelativePath(this.Key));
            }
            catch (Exception e)
            {
            }
        }
    }
}
