using RLNET;

using RogueSharp;

using RoguelikeGame.Color;

namespace RoguelikeGame.Map.Object
{
    public class GoldPile : IDrawable, IInteractable
    {
        public RLColor Color { get => Colors.Gold; }
        public char Symbol { get => 'G'; }
        public int X { get; set; }
        public int Y { get; set; }

        public int Amount;
        public void Draw(RLConsole console, IMap map) {
            if (!map.GetCell(X, Y).IsExplored)
                return;

            if (map.IsInFov(X, Y))
                console.Set(X, Y, Color, Colors.FloorBackgroundFov, Symbol);
            else // When not in field-of-view just draw a normal floor
                console.Set(X, Y, Colors.Floor, Colors.FloorBackground, '.');
        }

        public void Interact()
        {
            Game.Player.AddGold(Amount);

            Game.GetActiveMap().interactables.Remove(this);
        }
    }
}
