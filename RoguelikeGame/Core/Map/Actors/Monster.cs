using RLNET;

using System;
using System.Collections.Generic;

using RogueSharp.DiceNotation;

using RoguelikeGame.Color;
using RoguelikeGame.Systems.Command;
using RoguelikeGame.Systems.Command.Behaviour;
using RoguelikeGame.Systems.Scheduling;
using RoguelikeGame.Systems.Event.EventArguments;

namespace RoguelikeGame.Map.Actors
{
    public class Monster : Actor {

        public int? TurnsAlerted { get; set; }

        public virtual void PerformAction()
        {
            var behavior = new StandardMoveAndAttack();
            behavior.Act(this);
        }

        public void DrawStats(RLConsole statConsole, int position)
        {
            // Start at Y=13 which is below the player stats.
            // Multiply the position by 2 to leave a space between each stat
            int yPosition = (position * 2);

            // Begin the line by printing the symbol of the monster in the appropriate color
            statConsole.Print(1, yPosition, Symbol.ToString(), Color);

            // Figure out the width of the health bar by dividing current health by max health
            int width = Convert.ToInt32(((double)Health / (double)MaxHealth) * 16.0);
            int remainingWidth = 16 - width;

            // Set the background colors of the health bar to show how damaged the monster is
            statConsole.SetBackColor(3, yPosition, width, 1, Palette.Primary);
            statConsole.SetBackColor(3 + width, yPosition, remainingWidth, 1, Palette.PrimaryDarkest);

            // Print the monsters name over top of the health bar
            statConsole.Print(2, yPosition, $": {Name}", Palette.DbLight);
        }

        public delegate void DropFunction(DungeonMap map, Monster monster);
        public List<DropFunction> DropFunctions;


        public static void GoldDrop(DungeonMap map, Monster monster)
        {
            if (monster.Gold > 0)
            {
                RoguelikeGame.Map.Object.GoldPile goldPile = new RoguelikeGame.Map.Object.GoldPile
                {
                    X = monster.X,
                    Y = monster.Y,
                    Amount = monster.Gold
                };


                map.AddInteractable(goldPile);
            }
        }
        public static void HealingPotionDrop(DungeonMap map, Monster monster)
        {
            if (Dice.Roll("1d10") == 10)
            {
                RoguelikeGame.Systems.Inventory.ItemDefinition.HealingPotion potion = new RoguelikeGame.Systems.Inventory.ItemDefinition.HealingPotion()
                {
                    X = monster.X,
                    Y = monster.Y
                };

                map.AddInteractable(potion);
            }
        }

        public void DoDropsIfKilled(ActorDeathArguments args)
        {
            if (args.Defender != this)
                return;
            DoDrops(Game.GetActiveMap(), this);
        }
        public void DoDrops(DungeonMap map, Monster monster) {
            foreach (var item in DropFunctions) {
                item.Invoke(map, monster);
            }
        }

        public override void OnSchedule()
        {
            PerformAction();
            Game.GetActiveMap().SchedulingSystem.Add(this);

            Game.GetActiveMap().SchedulingSystem.Get().OnSchedule();
        }
    }
}
