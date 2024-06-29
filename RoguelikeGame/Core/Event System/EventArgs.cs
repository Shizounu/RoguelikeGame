using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Core.Event_System
{
    public delegate void EventHandler<T>(T e) where T : EventArgs;
    public abstract class EventArgs
    {

    }
}
