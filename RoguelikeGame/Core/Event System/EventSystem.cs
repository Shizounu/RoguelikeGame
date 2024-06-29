using RoguelikeGame.Systems.Event.EventArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Event
{
    public class EventSystem : Singleton<EventSystem>
    {
        public EventHandler<ActorDeathArguments> OnActorDeath;
        public EventHandler<ActorDamageArguments> OnActorDamage;
    }
}
