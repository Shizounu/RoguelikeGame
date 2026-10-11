using RoguelikeGame.Map;
using RoguelikeGame.Map.Actors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.MapManagment 
{
    public class MapManager : Singleton<MapManager> {

        public Dictionary<int, DungeonMap> GeneratedMaps = new Dictionary<int, DungeonMap>();
        private readonly Dictionary<string, DungeonMap> _namedMaps =
            new Dictionary<string, DungeonMap>(StringComparer.Ordinal);
        private string _staticMapDirectory;
        private int CurrentLayer => Game.Player.CurrentLayer;
        public string StaticMapDirectory
        {
            get { return _staticMapDirectory ?? GetDefaultStaticMapDirectory(); }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("A static map directory is required.", nameof(value));

                _staticMapDirectory = Path.GetFullPath(value);
            }
        }

        public DungeonMap GetMap(int Layer, bool Descending)
        {
            if (GeneratedMaps.ContainsKey(Layer))
            {
                RegisterMap(GetDungeonMapId(Layer), GeneratedMaps[Layer]);
                return GeneratedMaps[Layer];
            }

            DungeonMap map = MapGenerator.CreateMap(ConsoleDefinitions.MapConsoleWidth, ConsoleDefinitions.MapConsoleHeight, 20, 7, 13, Layer, Descending);
            GeneratedMaps.Add(Layer, map);
            RegisterMap(GetDungeonMapId(Layer), map);
            return map;
        }

        public static string GetDungeonMapId(int layer)
        {
            return $"dungeon:{layer}";
        }

        public void RegisterMap(string mapId, DungeonMap map)
        {
            if (string.IsNullOrWhiteSpace(mapId))
                throw new ArgumentException("A map ID is required.", nameof(mapId));
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            DungeonMap existingMap;
            if (_namedMaps.TryGetValue(mapId, out existingMap))
            {
                if (existingMap != map)
                    throw new InvalidOperationException($"A different map is already registered as '{mapId}'.");
                return;
            }

            map.MapId = mapId;
            _namedMaps.Add(mapId, map);
        }

        public bool TryGetMap(string mapId, out DungeonMap map)
        {
            return _namedMaps.TryGetValue(mapId, out map);
        }

        public void InitializeWorld()
        {
            DungeonMap dungeon = GetMap(1, true);
            Player player = Game.Player;
            int dungeonArrivalX = player.X;
            int dungeonArrivalY = player.Y;

            string mapDirectory = StaticMapDirectory;
            DungeonMap overworld = StaticMapFileService.LoadOrCreate(
                StaticMapFileService.GetMapFilePath(mapDirectory, WorldGenerator.OverworldMapId),
                WorldGenerator.OverworldMapId,
                () => WorldGenerator.CreateOverworld(
                    ConsoleDefinitions.MapConsoleWidth, ConsoleDefinitions.MapConsoleHeight));
            DungeonMap city = StaticMapFileService.LoadOrCreate(
                StaticMapFileService.GetMapFilePath(mapDirectory, WorldGenerator.CityMapId),
                WorldGenerator.CityMapId,
                () => WorldGenerator.CreateCity(
                    ConsoleDefinitions.MapConsoleWidth, ConsoleDefinitions.MapConsoleHeight));

            RegisterMap(WorldGenerator.OverworldMapId, overworld);
            RegisterMap(WorldGenerator.CityMapId, city);

            dungeon.interactables.RemoveAll(interactable =>
            {
                RoguelikeGame.Map.Object.Stairs stairs = interactable as RoguelikeGame.Map.Object.Stairs;
                return stairs != null && stairs.TargetLayer == 0;
            });
            dungeon.AddInteractable(new RoguelikeGame.Map.Object.Stairs
            {
                X = dungeonArrivalX,
                Y = dungeonArrivalY,
                Symbol = '<',
                TargetMapId = WorldGenerator.OverworldMapId,
                TargetX = WorldGenerator.DungeonEntranceX,
                TargetY = WorldGenerator.DungeonEntranceY
            });

            ValidateNamedConnections();
            TransitionPlayer(player, WorldGenerator.OverworldMapId,
                WorldGenerator.OverworldArrivalX, WorldGenerator.OverworldArrivalY);
        }

        private void ValidateNamedConnections()
        {
            foreach (DungeonMap sourceMap in _namedMaps.Values)
            {
                foreach (RoguelikeGame.Map.Object.Stairs exit in sourceMap.interactables
                    .OfType<RoguelikeGame.Map.Object.Stairs>()
                    .Where(stairs => !string.IsNullOrWhiteSpace(stairs.TargetMapId)))
                {
                    DungeonMap destinationMap;
                    if (!_namedMaps.TryGetValue(exit.TargetMapId, out destinationMap))
                        throw new InvalidDataException(
                            $"Map '{sourceMap.MapId}' has an exit to unregistered map '{exit.TargetMapId}'.");

                    RogueSharp.Point arrival;
                    if (exit.TargetX.HasValue)
                    {
                        int x = exit.TargetX.Value;
                        int y = exit.TargetY.Value;
                        if (x < 0 || y < 0 || x >= destinationMap.Width || y >= destinationMap.Height ||
                            !destinationMap.GetCell(x, y).IsWalkable)
                            throw new InvalidDataException(
                                $"Map '{sourceMap.MapId}' has an exit to an invalid arrival cell on '{exit.TargetMapId}'.");
                    }
                    else
                    {
                        arrival = GetDefaultArrivalPoint(destinationMap);
                        if (!destinationMap.GetCell(arrival.X, arrival.Y).IsWalkable)
                            throw new InvalidDataException(
                                $"Map '{sourceMap.MapId}' has no valid default arrival on '{exit.TargetMapId}'.");
                    }
                }
            }
        }

        private static string GetDefaultStaticMapDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StaticMaps");
        }

        public DungeonMap GetActiveMap()
        {
            DungeonMap map;
            if (Game.Player != null &&
                !string.IsNullOrEmpty(Game.Player.CurrentMapId) &&
                _namedMaps.TryGetValue(Game.Player.CurrentMapId, out map))
            {
                return map;
            }

            return GeneratedMaps[CurrentLayer];
        }

        public void TransitionPlayer(Player player, string destinationMapId, int? targetX, int? targetY)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (targetX.HasValue != targetY.HasValue)
                throw new ArgumentException("Both arrival coordinates must be provided together.");

            DungeonMap destinationMap;
            if (!_namedMaps.TryGetValue(destinationMapId, out destinationMap))
                throw new KeyNotFoundException($"No map is registered as '{destinationMapId}'.");

            int x;
            int y;
            if (targetX.HasValue)
            {
                x = targetX.Value;
                y = targetY.Value;
            }
            else
            {
                RogueSharp.Point arrival = GetDefaultArrivalPoint(destinationMap);
                x = arrival.X;
                y = arrival.Y;
            }

            if (!destinationMap.GetCell(x, y).IsWalkable)
                throw new InvalidOperationException($"The arrival cell ({x}, {y}) on map '{destinationMapId}' is not walkable.");

            DungeonMap currentMap = GetActiveMap();
            currentMap.RemovePlayer(player);
            destinationMap.AddPlayerAt(player, x, y);
            player.CurrentMapId = destinationMapId;
        }

        private static RogueSharp.Point GetDefaultArrivalPoint(DungeonMap map)
        {
            if (map.Rooms.Count > 0)
            {
                RogueSharp.Point entrance = map.Rooms.First().Center;
                if (map.GetCell(entrance.X, entrance.Y).IsWalkable)
                    return entrance;
            }

            int centerX = ConsoleDefinitions.MapConsoleWidth / 2;
            int centerY = ConsoleDefinitions.MapConsoleHeight / 2;
            for (int distance = 0; distance < ConsoleDefinitions.MapConsoleWidth + ConsoleDefinitions.MapConsoleHeight; distance++)
            {
                for (int y = Math.Max(0, centerY - distance); y <= Math.Min(ConsoleDefinitions.MapConsoleHeight - 1, centerY + distance); y++)
                {
                    for (int x = Math.Max(0, centerX - distance); x <= Math.Min(ConsoleDefinitions.MapConsoleWidth - 1, centerX + distance); x++)
                    {
                        if (Math.Abs(centerX - x) + Math.Abs(centerY - y) == distance &&
                            x < map.Width && y < map.Height && map.GetCell(x, y).IsWalkable)
                            return new RogueSharp.Point(x, y);
                    }
                }
            }

            throw new InvalidOperationException($"Map '{map.MapId}' has no walkable arrival cell.");
        }
    }
}
