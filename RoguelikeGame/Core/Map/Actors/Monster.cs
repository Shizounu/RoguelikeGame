using RLNET;
using RoguelikeGame.Core.Behaviours;
using RoguelikeGame.Core.Map;
using RoguelikeGame.Interfaces_and_Abstracts;
using RoguelikeGame.Systems;
using RogueSharp.DiceNotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Core
{
    public class Monster : Actor
    {
        public int? TurnsAlerted { get; set; }

        public virtual void PerformAction(CommandSystem commandSystem)
        {
            var behavior = new StandardMoveAndAttack();
            behavior.Act(this, commandSystem);
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

        public virtual void DoDrops(DungeonMap map) {
            if(Gold > 0)
            {
                GoldPile goldPile = new GoldPile
                {
                    X = this.X,
                    Y = this.Y,
                    Amount = Gold
                };

                
                map.AddInteractable(goldPile);
            }

            if(Dice.Roll("1d10") > 0)
            {
                HealingPotion potion = new HealingPotion()
                {
                    X = this.X,
                    Y = this.Y
                };

                map.AddInteractable(potion);
            }
        }
    }
}
