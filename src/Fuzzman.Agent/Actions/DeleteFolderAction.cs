using System;
using System.IO;

namespace Fuzzman.Agent.Actions
{
    [Serializable]
    public class DeleteFolderAction : ActionBase
    {
        public string Path { get; set; }

        public override void Execute()
        {
            try
            {
                Directory.Delete(this.Path, true);
            }
            catch (Exception e)
            {
            }
        }
    }
}
