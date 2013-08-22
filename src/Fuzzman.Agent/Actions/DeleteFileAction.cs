using System;
using System.IO;

namespace Fuzzman.Agent.Actions
{
    [Serializable]
    public class DeleteFileAction : ActionBase
    {
        public string Path { get; set; }

        public override void Execute()
        {
            try
            {
                File.Delete(this.Path);
            }
            catch (Exception e)
            {
            }
        }
    }
}
