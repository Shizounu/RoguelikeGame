using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Core.Inventory_System
{
    public interface IItem {
        string Name { get; }
        float Weight { get; }
        ItemType Type { get; }  
    }
}
