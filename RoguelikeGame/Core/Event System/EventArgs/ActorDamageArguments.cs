using RoguelikeGame.Map.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Event.EventArguments
{
    public class ActorDamageArguments : EventArgs
    {
        public Actor Attacker;
        public Actor Defender; 
        public int Damage;
    }
}
