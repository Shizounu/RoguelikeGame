using RoguelikeGame.Color;
using RoguelikeGame.Map.Actors;
using RoguelikeGame.Map.Actors.Drops;
using RoguelikeGame.Systems.Event;
using RoguelikeGame.Systems.Inventory.ItemDefinition;
using RogueSharp.DiceNotation;
using System.Collections.Generic;

namespace RoguelikeGame.Core.Map.Actors
{
    public static class GenericMonsters
    {
        public static Monster CreateKobold(int level)
        {
            int health = Dice.Roll("2D5");
            Monster kobold = new Monster()
            {
                Attack = Dice.Roll("1D3") + level / 3,
                AttackChance = Dice.Roll("25D3"),
                Awareness = 10,
                Color = Colors.KoboldColor,
                Defense = Dice.Roll("1D3") + level / 3,
                DefenseChance = Dice.Roll("10D4"),
                Gold = Dice.Roll("5D5"),
                Health = health,
                MaxHealth = health,
                Name = "Kobold",
                Speed = 14,
                Symbol = 'k',

                DropFunctions = new List<ItemDrop> { new GoldDrop(), new InteractableDrop<HealingPotion>() },
                Level = level
            };
            EventSystem.Instance.OnActorDeath += (invoker, args) => kobold.DoDropsIfKilled(args);

            return kobold;
        }
    }
}
