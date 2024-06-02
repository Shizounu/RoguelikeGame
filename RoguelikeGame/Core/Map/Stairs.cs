using RLNET;
using RoguelikeGame.Interface;
using RoguelikeGame.Interfaces_and_Abstracts;
using RogueSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Core
{
    public class Stairs : IDrawable, IInteractable
    {
        public RLColor Color
        {
            get; set;
        }
        public char Symbol
        {
            get; set;
        }
        public int X
        {
            get; set;
        }
        public int Y
        {
            get; set;
        }
        public bool IsUp
        {
            get; set;
        }

        public void Draw(RLConsole console, IMap map)
        {
            if (!map.GetCell(X, Y).IsExplored)
            {
                return;
            }

            Symbol = IsUp ? '<' : '>';

            if (map.IsInFov(X, Y))
            {
                Color = Colors.Player;
            }
            else
            {
                Color = Colors.Floor;
            }

            console.Set(X, Y, Color, null, Symbol);
        }

        public void Interact()
        {
            MapGenerator mapGenerator = new MapGenerator(Game._mapConsole.Width, Game._mapConsole.Height, 20, 13, 7, ++Game._mapLevel);
            Game.DungeonMap = mapGenerator.CreateMap();
        }
    }
}
