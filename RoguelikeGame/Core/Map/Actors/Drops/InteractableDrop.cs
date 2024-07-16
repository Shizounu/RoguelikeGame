using RogueSharp.DiceNotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Map.Actors.Drops
{
    public class InteractableDrop<T> : ItemDrop where T : IInteractable, new() {
        public InteractableDrop()
        {
            DiceRoll = "1d10";
            DropThreshhold = 10; 
        }
        public InteractableDrop(string DiceRoll, int DropThreshhold)
        {
            this.DiceRoll = DiceRoll;
            this.DropThreshhold = DropThreshhold;
        }

        public string DiceRoll;
        public int DropThreshhold; 

        public override void DoDrop(DungeonMap map, Monster monster)
        {
            if(Dice.Roll(DiceRoll) >= DropThreshhold)
            {
                T item = new T()
                {
                    X = monster.X,
                    Y = monster.Y,
                };
                map.AddInteractable(item);
            }
        }
    }
}
