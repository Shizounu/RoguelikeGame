using RoguelikeGame.Map.Actors;
using RoguelikeGame.Systems.Event;
using RoguelikeGame.Systems.Message;

namespace RoguelikeGame.Systems.Command.Commands
{
    public class ResolveDamage : ICommand {
        public ResolveDamage(Actor defender, int damage, Actor attacker)
        {
            this.defender = defender;
            this.attacker = attacker;
            this.damage = damage;
        }

        public Actor defender;
        public Actor attacker;
        public int damage;

        public void Execute(CommandSystem commandSystem, int executionPriority = 0)
        {
            if (damage > 0)
            {
                defender.Health -= damage;

                EventSystem.Instance.OnActorDamage?.Invoke(null, new Event.EventArguments.ActorDamageArguments()
                {
                    Attacker = attacker,
                    Defender = defender,
                    Damage = damage,
                });

                MessageLog.Instance.Add($"  {defender.Name} was hit for {damage} damage");

                if (defender.Health <= 0)
                {
                    commandSystem.EnqueueCommand(new ResolveDeath(defender, attacker, damage), executionPriority + 1);
                }
            }
            else
            {
                MessageLog.Instance.Add($"  {defender.Name} blocked all damage");
            }
        }
    }
}
