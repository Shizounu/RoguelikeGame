using RoguelikeGame.Map;
using RoguelikeGame.Map.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Command.Commands
{
    public class MovePlayer : ICommand
    {
        public MovePlayer(Direction Direction)
        {
            this.PlayerMoveDirection = Direction;            
        }

        public Direction PlayerMoveDirection; 

        public void Execute(CommandSystem commandSystem, int executionPriority = 0)
        {
            int x = Game.Player.X;
            int y = Game.Player.Y;

            switch (PlayerMoveDirection)
            {
                case Direction.Up:
                    {
                        y = Game.Player.Y - 1;
                        break;
                    }
                case Direction.Down:
                    {
                        y = Game.Player.Y + 1;
                        break;
                    }
                case Direction.Left:
                    {
                        x = Game.Player.X - 1;
                        break;
                    }
                case Direction.Right:
                    {
                        x = Game.Player.X + 1;
                        break;
                    }
            }

            Monster monster = Game.GetActiveMap().GetMonsterAt(x, y);

            if (monster != null)
            {
                
                commandSystem.EnqueueCommand(new Attack(Game.Player, monster), executionPriority + 1);
            }


            Game.GetActiveMap().SetActorPosition(Game.Player, x, y);
        }
    }
}
