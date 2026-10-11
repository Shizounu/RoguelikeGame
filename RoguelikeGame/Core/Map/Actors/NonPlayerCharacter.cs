using System;
using RLNET;
using RoguelikeGame.Color;
using RoguelikeGame.Map;
using RoguelikeGame.Systems.Message;
using RoguelikeGame.Systems.MapManagment;
using RoguelikeGame.Systems.RandomProvider;

namespace RoguelikeGame.Map.Actors
{
    public class NonPlayerCharacter : Actor, IInteractable
    {
        public NonPlayerCharacter()
        {
            Color = Colors.Player;
            Symbol = 'n';
            Speed = 10;
        }

        public Action<NonPlayerCharacter> Interaction { get; set; }
        public bool Wanders { get; set; } = true;

        public void Interact()
        {
            if (Interaction != null)
            {
                Interaction(this);
                return;
            }

            MessageLog.Instance.Add($"{Name} has nothing to say.");
        }

        public override void OnSchedule()
        {
            DungeonMap map = MapManager.Instance.GetActiveMap();

            if (Wanders)
            {
                int direction = RandomProvider.Instance.Provider.Next(0, 4);
                int targetX = X;
                int targetY = Y;

                switch (direction)
                {
                    case 0:
                        targetY--;
                        break;
                    case 1:
                        targetY++;
                        break;
                    case 2:
                        targetX--;
                        break;
                    default:
                        targetX++;
                        break;
                }

                if (map.GetCell(targetX, targetY).IsWalkable && !map.IsInteractableAt(targetX, targetY))
                    map.SetActorPosition(this, targetX, targetY);
            }

            map.SchedulingSystem.Add(this);
            map.SchedulingSystem.Get().OnSchedule();
        }

        public override void Draw(RLConsole console, RogueSharp.IMap map)
        {
            DungeonMap dungeonMap = map as DungeonMap;
            if (dungeonMap == null ? !map.IsInFov(X, Y) : !dungeonMap.IsVisible(X, Y))
                return;

            console.Set(X, Y,
                dungeonMap == null ? Color : dungeonMap.ApplyPalette(Color),
                dungeonMap == null ? Colors.FloorBackgroundFov : dungeonMap.ApplyPalette(Colors.FloorBackgroundFov),
                Symbol);
        }
    }
}
