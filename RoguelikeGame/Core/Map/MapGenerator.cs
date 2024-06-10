﻿using RoguelikeGame.Core.Actors;
using RoguelikeGame.Systems;
using RogueSharp;
using RogueSharp.DiceNotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Core
{
    public static class MapGenerator
    {
        // Generate a new map that is a simple open floor with walls around the outside
        // Generate a new map that places rooms randomly
        public static DungeonMap CreateMap(int _width, int _height, int _maxRooms, int _roomMinSize, int _roomMaxSize, int _mapLayer, bool Descending)
        {
            DungeonMap _map = new DungeonMap();

            // Set the properties of all cells to false
            _map.Initialize(_width, _height);

            // Try to place as many rooms as the specified maxRooms
            // Note: Only using decrementing loop because of WordPress formatting
            for (int r = _maxRooms; r > 0; r--)
            {
                // Determine the size and position of the room randomly
                int roomWidth = RandomProvider.Instance.Provider.Next(_roomMinSize, _roomMaxSize);
                int roomHeight = RandomProvider.Instance.Provider.Next(_roomMinSize, _roomMaxSize);
                int roomXPosition = RandomProvider.Instance.Provider.Next(0, _width - roomWidth - 1);
                int roomYPosition = RandomProvider.Instance.Provider.Next(0, _height - roomHeight - 1);

                // All of our rooms can be represented as Rectangles
                var newRoom = new Rectangle(roomXPosition, roomYPosition,
                  roomWidth, roomHeight);

                // Check to see if the room rectangle intersects with any other rooms
                bool newRoomIntersects = _map.Rooms.Any(room => newRoom.Intersects(room));

                // As long as it doesn't intersect add it to the list of rooms
                if (!newRoomIntersects)
                {
                    _map.Rooms.Add(newRoom);
                }
            }
            // Iterate through each room that we wanted placed 
            // call CreateRoom to make it
            foreach (Rectangle room in _map.Rooms)
                CreateRoom(_map, room);
            

            // Iterate through each room that was generated
            // Don't do anything with the first room, so start at r = 1 instead of r = 0
            for (int r = 1; r < _map.Rooms.Count; r++)
            {
                // For all remaing rooms get the center of the room and the previous room
                int previousRoomCenterX = _map.Rooms[r - 1].Center.X;
                int previousRoomCenterY = _map.Rooms[r - 1].Center.Y;
                int currentRoomCenterX = _map.Rooms[r].Center.X;
                int currentRoomCenterY = _map.Rooms[r].Center.Y;

                // Give a 50/50 chance of which 'L' shaped connecting hallway to tunnel out
                if (RandomProvider.Instance.Provider.Next(1, 2) == 1)
                {
                    CreateHorizontalTunnel(_map, previousRoomCenterX, currentRoomCenterX, previousRoomCenterY);
                    CreateVerticalTunnel(_map, previousRoomCenterY, currentRoomCenterY, currentRoomCenterX);
                }
                else
                {
                    CreateVerticalTunnel(_map, previousRoomCenterY, currentRoomCenterY, previousRoomCenterX);
                    CreateHorizontalTunnel(_map, previousRoomCenterX, currentRoomCenterX, currentRoomCenterY);
                }
            }

            foreach (Rectangle room in _map.Rooms)
                CreateDoors(_map, room);

            CreateStairs(_map, _mapLayer);

            PlacePlayer(_map, Descending);

            PlaceMonsters(_map, _mapLayer);

            return _map;
        }

        // Given a rectangular area on the map
        // set the cell properties for that area to true
        private static void CreateRoom(DungeonMap _map, Rectangle room)
        {
            for (int x = room.Left + 1; x < room.Right; x++)
            {
                for (int y = room.Top + 1; y < room.Bottom; y++)
                {
                    _map.SetCellProperties(x, y, true, true, false);
                }
            }
        }

        // Carve a tunnel out of the map parallel to the x-axis
        private static void CreateHorizontalTunnel(DungeonMap _map,int xStart, int xEnd, int yPosition)
        {
            for (int x = Math.Min(xStart, xEnd); x <= Math.Max(xStart, xEnd); x++)
            {
                _map.SetCellProperties(x, yPosition, true, true);
            }
        }

        // Carve a tunnel out of the map parallel to the y-axis
        private static void CreateVerticalTunnel(DungeonMap _map, int yStart, int yEnd, int xPosition)
        {
            for (int y = Math.Min(yStart, yEnd); y <= Math.Max(yStart, yEnd); y++)
            {
                _map.SetCellProperties(xPosition, y, true, true);
            }
        }

        // Find the center of the first room that we created and place the Player there
        private static void PlacePlayer(DungeonMap _map, bool Descending)
        {
            Player player = Game.Player;
            if (player == null)
                player = new Player();

            _map.AddPlayer(player, Descending);
        }

        private static void PlaceMonsters(DungeonMap _map, int Layer)
        {
            int Threshhold = 7 - Math.Max(0, Layer - 3);
            foreach (var room in _map.Rooms)
            {
                // Each room has a 60% chance of having monsters
                if (Dice.Roll("1D10") < Threshhold)
                {
                    // Generate between 1 and 4 monsters
                    var numberOfMonsters = Dice.Roll($"1D{Math.Max(1, Layer)}");
                    for (int i = 0; i < numberOfMonsters; i++)
                    {
                        // Find a random walkable location in the room to place the monster
                        Point randomRoomLocation = _map.GetRandomWalkableLocationInRoom(room);
                        // It's possible that the room doesn't have space to place a monster
                        // In that case skip creating the monster
                        if (randomRoomLocation != null)
                        {
                            // Temporarily hard code this monster to be created at level 1
                            var monster = Kobold.Create(1);
                            monster.X = randomRoomLocation.X;
                            monster.Y = randomRoomLocation.Y;
                            _map.AddMonster(monster);
                        }
                    }
                }
            }
        }

        private static void CreateDoors(DungeonMap _map, Rectangle room)
        {
            // The the boundries of the room
            int xMin = room.Left;
            int xMax = room.Right;
            int yMin = room.Top;
            int yMax = room.Bottom;

            // Put the rooms border cells into a list
            List<ICell> borderCells = _map.GetCellsAlongLine(xMin, yMin, xMax, yMin).ToList();
            borderCells.AddRange(_map.GetCellsAlongLine(xMin, yMin, xMin, yMax));
            borderCells.AddRange(_map.GetCellsAlongLine(xMin, yMax, xMax, yMax));
            borderCells.AddRange(_map.GetCellsAlongLine(xMax, yMin, xMax, yMax));

            // Go through each of the rooms border cells and look for locations to place doors.
            foreach (ICell cell in borderCells)
            {
                if (IsPotentialDoor(_map, cell))
                {
                    // A door must block field-of-view when it is closed.
                    _map.SetCellProperties(cell.X, cell.Y, false, true);
                    _map.Doors.Add(new Door
                    {
                        X = cell.X,
                        Y = cell.Y,
                        IsOpen = false
                    });
                }
            }
        }

        // Checks to see if a cell is a good candidate for placement of a door
        private static bool IsPotentialDoor(DungeonMap _map, ICell cell)
        {
            // If the cell is not walkable
            // then it is a wall and not a good place for a door
            if (!cell.IsWalkable)
            {
                return false;
            }

            // Store references to all of the neighboring cells 
            ICell right = _map.GetCell(cell.X + 1, cell.Y);
            ICell left = _map.GetCell(cell.X - 1, cell.Y);
            ICell top = _map.GetCell(cell.X, cell.Y - 1);
            ICell bottom = _map.GetCell(cell.X, cell.Y + 1);

            // Make sure there is not already a door here
            if (_map.GetDoor(cell.X, cell.Y) != null ||
                _map.GetDoor(right.X, right.Y) != null ||
                _map.GetDoor(left.X, left.Y) != null ||
                _map.GetDoor(top.X, top.Y) != null ||
                _map.GetDoor(bottom.X, bottom.Y) != null)
            {
                return false;
            }

            // This is a good place for a door on the left or right side of the room
            if (right.IsWalkable && left.IsWalkable && !top.IsWalkable && !bottom.IsWalkable)
            {
                return true;
            }

            // This is a good place for a door on the top or bottom of the room
            if (!right.IsWalkable && !left.IsWalkable && top.IsWalkable && bottom.IsWalkable)
            {
                return true;
            }
            return false;
        }

        private static void CreateStairs(DungeonMap _map, int CurLayer)
        {
            _map.AddInteractable(new Stairs
            {
                X = _map.Rooms.First().Center.X + 1,
                Y = _map.Rooms.First().Center.Y,
                Symbol = '<',
                TargetLayer = CurLayer - 1
            });
            _map.AddInteractable( new Stairs
            {
                X = _map.Rooms.Last().Center.X,
                Y = _map.Rooms.Last().Center.Y,
                Symbol = '>',
                TargetLayer = CurLayer + 1
            });
        }
    }
}