using RLNET;

using RogueSharp;

using System.Collections.Generic;
using System.Linq;
using System;

using RoguelikeGame.Color;
using RoguelikeGame.Map.Actors;
using RoguelikeGame.Map.Object;
using RoguelikeGame.Systems;
using RoguelikeGame.Systems.Content;

namespace RoguelikeGame.Map
{
    // Our custom DungeonMap class extends the base RogueSharp Map class
    public class DungeonMap : RogueSharp.Map
    {
        public List<Rectangle> Rooms = new List<Rectangle>();
        public List<Monster> Monsters = new List<Monster>();
        public List<NonPlayerCharacter> NonPlayerCharacters = new List<NonPlayerCharacter>();
        public List<Door> Doors = new List<Door>();
        public List<IInteractable> interactables = new List<IInteractable>();
        private readonly Dictionary<Tuple<int, int>, TerrainTile> _terrainTiles =
            new Dictionary<Tuple<int, int>, TerrainTile>();
        public Systems.Scheduling.SchedulingSystem SchedulingSystem; 
        public string MapId { get; internal set; }
        public string DisplayName { get; set; } = "Dungeon";
        public bool LightingEnabled { get; set; } = true;
        public string PaletteName { get; set; } = "Dawnbringer";

        public DungeonMap()
        {
            SchedulingSystem = new Systems.Scheduling.SchedulingSystem();
            Rooms = new List<Rectangle>();
            Monsters = new List<Monster>();
            Doors = new List<Door>();  
        }

        #region Drawing
        // The Draw method will be called each time the map is updated
        // It will render all of the symbols/colors for each cell to the map sub console
        public void Draw(RLConsole mapConsole)
        {
            mapConsole.Clear();
            foreach (Cell cell in GetAllCells())
                SetConsoleSymbolForCell(mapConsole, cell);
            foreach (Monster monster in Monsters)
                monster.Draw(mapConsole, this);
            foreach (NonPlayerCharacter nonPlayerCharacter in NonPlayerCharacters)
                nonPlayerCharacter.Draw(mapConsole, this);
            foreach (Door door in Doors)
                door.Draw(mapConsole, this);
            foreach (IInteractable interactable in interactables) 
                interactable.Draw(mapConsole, this);
        }

        private void SetConsoleSymbolForCell(RLConsole console, Cell cell)
        {
            // When we haven't explored a cell yet, we don't want to draw anything
            if (!cell.IsExplored)
            {
                return;
            }

            TerrainTile terrainTile;
            if (_terrainTiles.TryGetValue(Tuple.Create(cell.X, cell.Y), out terrainTile))
            {
                console.Set(cell.X, cell.Y, ApplyLighting(terrainTile.Color, cell.X, cell.Y),
                    ApplyLighting(terrainTile.Background, cell.X, cell.Y), terrainTile.Symbol);
                return;
            }

            // When a cell is currently in the field-of-view it should be drawn with ligher colors
            if (IsVisible(cell.X, cell.Y))
            {
                // Choose the symbol to draw based on if the cell is walkable or not
                // '.' for floor and '#' for walls
                if (cell.IsWalkable)
                {
                    console.Set(cell.X, cell.Y, ApplyPalette(Colors.FloorFov), ApplyPalette(Colors.FloorBackgroundFov), '.');
                }
                else
                {
                    console.Set(cell.X, cell.Y, ApplyPalette(Colors.WallFov), ApplyPalette(Colors.WallBackgroundFov), '#');
                }
            }
            // When a cell is outside of the field of view draw it with darker colors
            else
            {
                if (cell.IsWalkable)
                {
                    console.Set(cell.X, cell.Y, ApplyPalette(Colors.Floor), ApplyPalette(Colors.FloorBackground), '.');
                }
                else
                {
                    console.Set(cell.X, cell.Y, ApplyPalette(Colors.Wall), ApplyPalette(Colors.WallBackground), '#');
                }
            }
        }


        #endregion

        #region Logic
        // This method will be called any time we move the player to update field-of-view
        public void UpdatePlayerFieldOfView()
        {
            Player player = Game.Player;
            if (!LightingEnabled)
            {
                foreach (Cell cell in GetAllCells())
                    SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);
                return;
            }

            // Compute the field-of-view based on the player's location and awareness
            ComputeFov(player.X, player.Y, player.Awareness, true);
            // Mark all cells in field-of-view as having been explored
            foreach (Cell cell in GetAllCells())
            {
                if (IsInFov(cell.X, cell.Y))
                {
                    SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);
                }
            }
        }

        public bool IsVisible(int x, int y)
        {
            return !LightingEnabled || IsInFov(x, y);
        }

        public RLColor ApplyPalette(RLColor color)
        {
            return ContentCatalog.Current.GetPalette(PaletteName).Apply(color);
        }

        private RLColor ApplyLighting(RLColor color, int x, int y)
        {
            RLColor paletteColor = ApplyPalette(color);
            if (!LightingEnabled || IsInFov(x, y))
                return paletteColor;

            return new RLColor(paletteColor.r * 0.3f, paletteColor.g * 0.3f, paletteColor.b * 0.3f);
        }

        // Returns true when able to place the Actor on the cell or false otherwise
        public bool SetActorPosition(Actor actor, int x, int y)
        {
            // Only allow actor placement if the cell is walkable
            if (GetCell(x, y).IsWalkable)
            {
                // The cell the actor was previously on is now walkable
                SetIsWalkable(actor.X, actor.Y, true);

                OpenDoor(actor, x, y);
                // Update the actor's position
                actor.X = x;
                actor.Y = y;
                // The new cell the actor is on is now not walkable
                SetIsWalkable(actor.X, actor.Y, false);
                // Don't forget to update the field of view if we just repositioned the player
                if (actor is Player)
                {
                    UpdatePlayerFieldOfView();
                }
                return true;
            }
            return false;
        }

        // A helper method for setting the IsWalkable property on a Cell
        public void SetIsWalkable(int x, int y, bool isWalkable)
        {
            ICell cell = GetCell(x, y);
            SetCellProperties(cell.X, cell.Y, cell.IsTransparent, isWalkable, cell.IsExplored);
        }

        public void SetTerrainTile(int x, int y, char symbol, RLColor color, RLColor background,
            bool isWalkable, bool isTransparent = true, bool isExplored = true)
        {
            SetCellProperties(x, y, isTransparent, isWalkable, isExplored);
            _terrainTiles[Tuple.Create(x, y)] = new TerrainTile(symbol, color, background);
        }

        public bool TryGetTerrainTile(int x, int y, out char symbol, out RLColor color, out RLColor background)
        {
            TerrainTile terrainTile;
            if (_terrainTiles.TryGetValue(Tuple.Create(x, y), out terrainTile))
            {
                symbol = terrainTile.Symbol;
                color = terrainTile.Color;
                background = terrainTile.Background;
                return true;
            }

            ICell cell = GetCell(x, y);
            symbol = cell.IsWalkable ? '.' : '#';
            color = cell.IsWalkable ? Colors.Floor : Colors.Wall;
            background = cell.IsWalkable ? Colors.FloorBackground : Colors.WallBackground;
            return false;
        }

        // Look for a random location in the room that is walkable.
        public Point GetRandomWalkableLocationInRoom(Rectangle room)
        {
            if (DoesRoomHaveWalkableSpace(room))
            {
                for (int i = 0; i < 100; i++)
                {
                    int x = Systems.RandomProvider.RandomProvider.Instance.Provider.Next(1, room.Width - 2) + room.X;
                    int y = Systems.RandomProvider.RandomProvider.Instance.Provider.Next(1, room.Height - 2) + room.Y;
                    if (IsWalkable(x, y))
                    {
                        return new Point(x, y);
                    }
                }
            }

            // If we didn't find a walkable location in the room return null
            return default;
        }

        public void RemoveMonster(Monster monster)
        {
            Monsters.Remove(monster);
            SchedulingSystem.Remove(monster);
            // After removing the monster from the map, make sure the cell is walkable again
            SetIsWalkable(monster.X, monster.Y, true);
        }

        // The actor opens the door located at the x,y position
        private void OpenDoor(Actor actor, int x, int y)
        {
            Door door = GetDoor(x, y);
            if (door != null && !door.IsOpen)
            {
                door.IsOpen = true;
                var cell = GetCell(x, y);
                // Once the door is opened it should be marked as transparent and no longer block field-of-view
                SetCellProperties(x, y, true, cell.IsWalkable, cell.IsExplored);

                Systems.Message.MessageLog.Instance.Add($"{actor.Name} opened a door");
            }
        }
        #endregion

        #region Instatiators
        // Called by MapGenerator after we generate a new map to add the player to the map
        public void AddPlayer(Player player, bool Descending)
        {
            if (Descending) //is the player descending
            {
                player.X = Rooms.First().Center.X;
                player.Y = Rooms.First().Center.Y;
            }
            else // Ascending
            {
                player.X = Rooms.Last().Center.X;
                player.Y = Rooms.Last().Center.Y;
            }


            Game.Player = player;
            SchedulingSystem.Add(player);
            SetIsWalkable(player.X, player.Y, false);
            UpdatePlayerFieldOfView();
        }

        public void AddPlayerAt(Player player, int x, int y)
        {
            if (player == null)
                throw new System.ArgumentNullException(nameof(player));
            if (!GetCell(x, y).IsWalkable)
                throw new System.InvalidOperationException($"The arrival cell ({x}, {y}) is not walkable.");

            player.X = x;
            player.Y = y;
            Game.Player = player;
            SchedulingSystem.Add(player);
            SetIsWalkable(x, y, false);
            UpdatePlayerFieldOfView();
        }

        public void RemovePlayer(Player player)
        {
            SchedulingSystem.Remove(player);
            SetIsWalkable(player.X, player.Y, true);
        }

        public void AddMonster(Monster monster)
        {
            Monsters.Add(monster);
            SchedulingSystem.Add(monster);
            // After adding the monster to the map make sure to make the cell not walkable
            SetIsWalkable(monster.X, monster.Y, false);
        }

        public void AddNonPlayerCharacter(NonPlayerCharacter nonPlayerCharacter)
        {
            if (nonPlayerCharacter == null)
                throw new System.ArgumentNullException(nameof(nonPlayerCharacter));
            if (!GetCell(nonPlayerCharacter.X, nonPlayerCharacter.Y).IsWalkable)
                throw new System.InvalidOperationException(
                    $"The NPC placement cell ({nonPlayerCharacter.X}, {nonPlayerCharacter.Y}) is not walkable.");

            NonPlayerCharacters.Add(nonPlayerCharacter);
            SetIsWalkable(nonPlayerCharacter.X, nonPlayerCharacter.Y, false);
            SchedulingSystem.Add(nonPlayerCharacter);
        }

        public void RemoveNonPlayerCharacter(NonPlayerCharacter nonPlayerCharacter)
        {
            if (NonPlayerCharacters.Remove(nonPlayerCharacter))
                SetIsWalkable(nonPlayerCharacter.X, nonPlayerCharacter.Y, true);
        }

        public void AddInteractable(IInteractable interactable)
        {
            interactables.Add(interactable);
        }
        #endregion

        #region Helpers
        // Iterate through each Cell in the room and return true if any are walkable
        public bool DoesRoomHaveWalkableSpace(Rectangle room)
        {
            for (int x = 1; x <= room.Width - 2; x++)
            {
                for (int y = 1; y <= room.Height - 2; y++)
                {
                    if (IsWalkable(x + room.X, y + room.Y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Monster GetMonsterAt(int x, int y)
        {
            return Monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }

        // Return the door at the x,y position or null if one is not found.
        public Door GetDoor(int x, int y)
        {
            return Doors.SingleOrDefault(d => d.X == x && d.Y == y);
        }

        public List<IInteractable> GetInteractablesAt(int X, int Y)
        {
            return interactables
                .Cast<IInteractable>()
                .Concat(NonPlayerCharacters)
                .Where(interactable => interactable.X == X && interactable.Y == Y)
                .ToList();
        }

        public List<IInteractable> GetInteractablesInReach(int x, int y)
        {
            List<IInteractable> atPosition = GetInteractablesAt(x, y);
            if (atPosition.Count > 0)
                return atPosition;

            return interactables
                .Cast<IInteractable>()
                .Concat(NonPlayerCharacters)
                .Where(interactable =>
                    System.Math.Abs(interactable.X - x) + System.Math.Abs(interactable.Y - y) == 1)
                .ToList();
        }

        public bool IsInteractableAt(int x, int y)
        {
            return interactables.Any(interactable => interactable.X == x && interactable.Y == y);
        }
        #endregion
    
    
    }

    internal sealed class TerrainTile
    {
        public TerrainTile(char symbol, RLColor color, RLColor background)
        {
            Symbol = symbol;
            Color = color;
            Background = background;
        }

        public char Symbol { get; private set; }
        public RLColor Color { get; private set; }
        public RLColor Background { get; private set; }
    }
}
