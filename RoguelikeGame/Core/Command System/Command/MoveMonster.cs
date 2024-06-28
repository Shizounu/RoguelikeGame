using RoguelikeGame.Map.Actors;
using RoguelikeGame.Systems.Command;
using RogueSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Command.Commands
{
    public class MoveMonster : ICommand
    {
        public MoveMonster(Monster monster, Cell cell)
        {
            this.monster = monster;
            this.cell = cell;
        }

        Monster monster;
        Cell cell; 

        public void Execute(CommandSystem commandSystem, int executionPriority = 0)
        {
            if (!Game.GetActiveMap().SetActorPosition(monster, cell.X, cell.Y))
            {
                if (Game.Player.X == cell.X && Game.Player.Y == cell.Y)
                {
                    commandSystem.EnqueueCommand(new Attack(monster, Game.Player));
                }
            }
        }
    }
}
