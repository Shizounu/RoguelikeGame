using RLNET;
using RogueSharp;
using RoguelikeGame.Color;
using RoguelikeGame.Systems.Message;
using RoguelikeGame.Systems.MapManagment;

namespace RoguelikeGame.Map.Object
{
    public class Stairs : IDrawable, IInteractable
    {
        public RLColor Color { get; set; }
        public char Symbol { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public int TargetLayer;
        public string TargetMapId;
        public int? TargetX;
        public int? TargetY;

        public void Draw(RLConsole console, IMap map) {
            if (!map.GetCell(X, Y).IsExplored)
                return;

            DungeonMap dungeonMap = map as DungeonMap;
            bool isVisible = dungeonMap == null ? map.IsInFov(X, Y) : dungeonMap.IsVisible(X, Y);
            if (isVisible) {
                Color = Colors.Player;
            }
            else
            {
                Color = Colors.Floor;
            }

            console.Set(X, Y, dungeonMap == null ? Color : dungeonMap.ApplyPalette(Color), null, Symbol);
        }

        public void Interact()
        {
            if (!string.IsNullOrEmpty(TargetMapId))
            {
                if (TargetX.HasValue != TargetY.HasValue)
                    throw new System.InvalidOperationException(
                        $"Stairs leading to '{TargetMapId}' must define both arrival coordinates or neither.");

                MapManager.Instance.TransitionPlayer(Game.Player, TargetMapId, TargetX, TargetY);
                MessageLog.Instance.Add($"The Rogue travels to {TargetMapId}");
                return;
            }

            bool descending = (TargetLayer > Game.Player.CurrentLayer);

            //Remove from old map
            MapManager.Instance.GetActiveMap().RemovePlayer(Game.Player);

            //add to new map
            DungeonMap map = MapManager.Instance.GetMap(TargetLayer, descending);
            Game.Player.CurrentLayer = TargetLayer;
            Game.Player.CurrentMapId = MapManager.GetDungeonMapId(TargetLayer);
            map.AddPlayer(Game.Player, descending);


            string dir = descending ? "Descends" : "Ascends";
            MessageLog.Instance.Add($"The Rogue {dir} to Layer {TargetLayer}");
        }
    }
}
