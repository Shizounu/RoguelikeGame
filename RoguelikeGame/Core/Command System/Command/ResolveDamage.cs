using RoguelikeGame.Map.Actors;
using RoguelikeGame.Systems.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace RoguelikeGame.Systems.Command.Commands
{
    public class ResolveDamage : ICommand {
        public ResolveDamage(Actor defender, int damage)
        {
            this.defender = defender;
            this.damage = damage;
        }

        public Actor defender;
        public int damage;

        public void Execute(CommandSystem commandSystem, int executionPriority = 0)
        {
            if (damage > 0)
            {
                defender.Health -= damage;

                MessageLog.Instance.Add($"  {defender.Name} was hit for {damage} damage");

                if (defender.Health <= 0)
                    commandSystem.EnqueueCommand(new ResolveDeath(defender), executionPriority + 1);
            }
            else
            {
                MessageLog.Instance.Add($"  {defender.Name} blocked all damage");
            }
        }
    }
}
