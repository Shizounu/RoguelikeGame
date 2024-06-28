using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Scheduling
{
    public interface IScheduleable
    {
        int Time { get; }
        void OnSchedule();
    }
}
