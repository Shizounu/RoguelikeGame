using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Command
{
    public interface ICommand
    {
        void Execute(CommandSystem commandSystem, int executionPriority = 0);
    }
}
