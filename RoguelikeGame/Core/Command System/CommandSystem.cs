using System.Collections.Generic;
using System.Linq;

namespace RoguelikeGame.Systems.Command
{
    public class CommandSystem : Singleton<CommandSystem>
    {
        public CommandSystem() : base()
        {
            PriorityCommandQueue = new SortedList<int, Queue<ICommand>>();
        }

        public SortedList<int, Queue<ICommand>> PriorityCommandQueue { get; private set; }

        public void EnqueueCommand(ICommand command, int priority = 0) {
            if(!PriorityCommandQueue.ContainsKey(priority))
                PriorityCommandQueue.Add(priority, new Queue<ICommand>());
            PriorityCommandQueue[priority].Enqueue(command);
        }
        public ICommand DequeueCommand() {
            var KVP = PriorityCommandQueue.Last();
            ICommand command = KVP.Value.Dequeue();
            if(KVP.Value.Count == 0)
                PriorityCommandQueue.Remove(KVP.Key);

            return command; 
        }

        public void DoAllCommands() {
            while(PriorityCommandQueue.Count > 0)
                DequeueCommand().Execute(this);
        }
        

    }


}
