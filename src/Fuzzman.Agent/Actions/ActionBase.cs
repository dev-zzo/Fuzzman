using System;

namespace Fuzzman.Agent.Actions
{
    [Serializable]
    public abstract class ActionBase
    {
        public abstract void Execute();

        public static void Execute(ActionBase[] actions)
        {
            foreach (ActionBase action in actions)
            {
                action.Execute();
            }
        }
    }
}
