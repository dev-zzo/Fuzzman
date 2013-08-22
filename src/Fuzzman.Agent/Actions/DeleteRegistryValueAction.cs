using System;
using Microsoft.Win32;

namespace Fuzzman.Agent.Actions
{
    [Serializable]
    public class DeleteRegistryValueAction : RegistryActionBase
    {
        public string Value { get; set; }

        public override void Execute()
        {
            try
            {
                RegistryKey baseKey = GetBaseKey(this.Value);
                using (RegistryKey key = baseKey.CreateSubKey(GetKeyPath(this.Value)))
                {
                    key.DeleteValue(GetValueName(this.Value));
                }
            }
            catch (Exception e)
            {
            }
        }
    }
}
