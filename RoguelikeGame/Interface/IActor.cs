using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Interface
{
    internal interface IActor
    {
        string Name { get; set;  }
        int Awareness { get;set;  }
    }
}
