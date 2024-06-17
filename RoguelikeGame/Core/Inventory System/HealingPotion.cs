using RLNET;
using RogueSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoguelikeGame.Core.Inventory_System;
using RoguelikeGame.Core;
using RogueSharp.DiceNotation;
using RoguelikeGame.Systems;

namespace RoguelikeGame
{
    public class HealingPotion : IItem, IInteractable, IConsumable
    {
        public string Name => "Healing Potion";

        public float Weight => 0.1f;

        public ItemType Type => ItemType.Consumable;

        public RLColor Color => RLColor.Red;

        public char Symbol => 'p';

        public int X { get; set; }
        public int Y { get; set; }


        public void Draw(RLConsole console, IMap map)
        {
            if (!map.GetCell(X, Y).IsExplored)
                return;

            if (map.IsInFov(X, Y))
                console.Set(X, Y, Color, Colors.FloorBackgroundFov, Symbol);
            else // When not in field-of-view just draw a normal floor
                console.Set(X, Y, Colors.Floor, Colors.FloorBackground, '.');
        }

        public void Interact()
        {
            Game.Player.AddItem(this);
            Game.GetActiveMap().interactables.Remove(this);
        }

        public void Use()
        {
            int amount = Dice.Roll("2d12");
            MessageLog.Instance.Add($"{Game.Player.Name} drank {Name} and healed {amount}");
            Game.Player.Heal(amount);
            Game.Player.RemoveItem(this);
        }
    }
}
