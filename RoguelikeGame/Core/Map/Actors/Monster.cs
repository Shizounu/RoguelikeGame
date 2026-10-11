using RLNET;

using System;
using System.Collections.Generic;

using RogueSharp.DiceNotation;

using RoguelikeGame.Color;
using RoguelikeGame.Systems.Command;
using RoguelikeGame.Systems.Command.Behaviour;
using RoguelikeGame.Systems.Scheduling;
using RoguelikeGame.Systems.Event.EventArguments;
using RoguelikeGame.Systems.MapManagment;
using RoguelikeGame.Map.Actors.Drops;
using RoguelikeGame.Systems.Content;

namespace RoguelikeGame.Map.Actors
{
    public class Monster : Actor {

        public int? TurnsAlerted { get; set; }
        public string ContentId { get; set; }
        public string BehaviorId { get; set; }
        public List<ContentCatalog.DropDefinition> DropDefinitions { get; set; }

        public virtual void PerformAction()
        {
            if (BehaviorId != null && BehaviorId != "AI.StandardMoveAndAttack")
                throw new InvalidOperationException($"Monster '{ContentId}' has unsupported behavior '{BehaviorId}'.");
            var behavior = new StandardMoveAndAttack();
            behavior.Act(this);
        }

        public void DrawStats(RLConsole statConsole, int position)
        {
            // Leave row 0 for the panel heading and space entries by one row.
            int yPosition = (position * 2) + 1;

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

        
        public List<ItemDrop> DropFunctions;

        public void DoDropsIfKilled(ActorDeathArguments args)
        {
            if (args.Defender != this)
                return;
            DoDrops(MapManager.Instance.GetActiveMap(), this);
        }
        public void DoDrops(DungeonMap map, Monster monster) {
            if (DropDefinitions != null)
            {
                foreach (ContentCatalog.DropDefinition drop in DropDefinitions)
                    ContentCatalog.Current.ExecuteDrop(drop, map, monster);
                return;
            }

            foreach (var item in DropFunctions) {
                item.DoDrop(map, monster);
            }
        }

        public override void OnSchedule()
        {
            PerformAction();
            MapManager.Instance.GetActiveMap().SchedulingSystem.Add(this);
            MapManager.Instance.GetActiveMap().SchedulingSystem.Get().OnSchedule();
        }
    }
}
