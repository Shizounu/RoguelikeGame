using RoguelikeGame.Map.Actors;
using RoguelikeGame.Systems.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Command.Commands
{
    public class ResolveDeath : ICommand {
        public ResolveDeath(Actor defender)
        {
            this.defender = defender;
        }
        public Actor defender;

        public void Execute(CommandSystem commandSystem, int executionPriority = 0)
        {
            if (defender is Player)
            {
                MessageLog.Instance.Add($"  {defender.Name} was killed, GAME OVER MAN!");
            }
            else if (defender is Monster monster)
            {
                monster.DoDrops(Game.GetActiveMap(), monster);
                Game.GetActiveMap().RemoveMonster(monster);
                MessageLog.Instance.Add($"{defender.Name} died and dropped {defender.Gold} gold");
            }
        }
    }
}
