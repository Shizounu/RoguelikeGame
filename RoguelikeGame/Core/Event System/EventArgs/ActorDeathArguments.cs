using RoguelikeGame.Map.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Event.EventArguments
{
    public class ActorDeathArguments : EventArgs {
        public Actor Attacker;
        public Actor Defender;

        public int KillDamage; 
    }
}
