using RLNET;
using RoguelikeGame.Interface;
using RoguelikeGame.Interfaces_and_Abstracts;
using RoguelikeGame.Systems;
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
        public RLColor Color { get; set; }
        public char Symbol { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public int TargetLayer;

        public void Draw(RLConsole console, IMap map) {
            if (!map.GetCell(X, Y).IsExplored)
                return;

            if (map.IsInFov(X, Y)) {
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
            bool descending = (TargetLayer > Game.Player.CurrentLayer);

            //Remove from old map
            Game.GetActiveMap().RemovePlayer(Game.Player);

            //add to new map
            DungeonMap map = Game.GetMap(TargetLayer, descending);
            Game.Player.CurrentLayer = TargetLayer;
            map.AddPlayer(Game.Player, descending);


            string dir = descending ? "Descends" : "Ascends";
            MessageLog.Instance.Add($"The Rogue {dir} to Layer {TargetLayer}");
        }
    }
}
