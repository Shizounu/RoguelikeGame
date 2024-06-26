using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Inventory
{
    public interface IItem {
        string Name { get; }
        float Weight { get; }
        ItemType Type { get; }

        void Use();
    }
}
