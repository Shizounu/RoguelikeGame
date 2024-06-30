using RoguelikeGame.Map.Actors;
using RoguelikeGame.Systems.Event;
using RoguelikeGame.Systems.MapManagment;
using RoguelikeGame.Systems.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace RoguelikeGame.Systems.Command.Commands
{
    public class ResolveDeath : ICommand {
        public ResolveDeath(Actor defender, Actor attacker, int damage)
        {
            this.defender = defender;
            this.attacker = attacker;
            this.damage = damage;

        }
        public Actor attacker; 
        public Actor defender;
        public int damage;
        public void Execute(CommandSystem commandSystem, int executionPriority = 0)
        {
            EventSystem.Instance.OnActorDeath?.Invoke(null, new Event.EventArguments.ActorDeathArguments()
            {
                Attacker = attacker,
                Defender = defender,
                KillDamage = damage
            });

            if (defender is Player)
            {
                MessageLog.Instance.Add($"  {defender.Name} was killed, GAME OVER MAN!");
            }
            else if (defender is Monster monster)
            {
                MapManager.Instance.GetActiveMap().RemoveMonster(monster);
                MessageLog.Instance.Add($"{defender.Name} died and dropped {defender.Gold} gold");
            }
        }
    }
}
