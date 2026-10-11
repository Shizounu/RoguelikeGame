using RLNET;
using RogueSharp;

using RoguelikeGame.Color;

namespace RoguelikeGame.Map.Object
{
    public class Door : IDrawable
    {
        public Door()
        {
            Symbol = '+';
            Color = Colors.Door;
            BackgroundColor = Colors.DoorBackground;
        }
        public bool IsOpen { get; set; }

        public RLColor Color { get; set; }
        public RLColor BackgroundColor { get; set; }
        public char Symbol { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public void Draw(RLConsole console, IMap map)
        {
            if (!map.GetCell(X, Y).IsExplored)
            {
                return;
            }

            Symbol = IsOpen ? '-' : '+';
            DungeonMap dungeonMap = map as DungeonMap;
            bool isVisible = dungeonMap == null ? map.IsInFov(X, Y) : dungeonMap.IsVisible(X, Y);
            if (isVisible)
            {
                Color = Colors.DoorFov;
                BackgroundColor = Colors.DoorBackgroundFov;
            }
            else
            {
                Color = Colors.Door;
                BackgroundColor = Colors.DoorBackground;
            }

            RLColor drawColor = dungeonMap == null ? Color : dungeonMap.ApplyPalette(Color);
            RLColor drawBackground = dungeonMap == null ? BackgroundColor : dungeonMap.ApplyPalette(BackgroundColor);
            console.Set(X, Y, drawColor, drawBackground, Symbol);
        }
    }
}
