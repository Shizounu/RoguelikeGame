using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Map.Actors.Drops
{
    public class GoldDrop : ItemDrop
    {
        public override void DoDrop(DungeonMap map, Monster monster)
        {
            if (monster.Gold > 0)
            {
                Object.GoldPile goldPile = new Object.GoldPile
                {
                    X = monster.X,
                    Y = monster.Y,
                    Amount = monster.Gold
                };

                map.AddInteractable(goldPile);
            }
        }
    }
}
