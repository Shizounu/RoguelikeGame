using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using RogueSharp;
using ICell = RogueSharp.ICell;
using RoguelikeGame.Map;
using RoguelikeGame.Map.Actors;
using RoguelikeGame.Map.Object;

namespace RoguelikeGame.Systems.MapManagment
{
    public static class StaticMapFileService
    {
        private const int CurrentFormatVersion = 1;

        public static DungeonMap Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A map file path is required.", nameof(filePath));

            StaticMapDocument document;
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    document = (StaticMapDocument)new DataContractJsonSerializer(typeof(StaticMapDocument))
                        .ReadObject(stream);
                }
            }
            catch (SerializationException exception)
            {
                throw new InvalidDataException($"Static map file '{filePath}' contains invalid JSON.", exception);
            }

            Validate(document, filePath);
            return CreateMap(document);
        }

        public static void Save(string filePath, DungeonMap map)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A map file path is required.", nameof(filePath));
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(map.MapId))
                throw new InvalidOperationException("A map must have a registered map ID before it can be saved.");

            StaticMapDocument document = CreateDocument(map);
            Validate(document, filePath);

            string fullPath = System.IO.Path.GetFullPath(filePath);
            string directory = System.IO.Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(directory);
            string temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(StaticMapDocument));
                    serializer.WriteObject(stream, document);
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                    File.Replace(temporaryPath, fullPath, null);
                else
                    File.Move(temporaryPath, fullPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        public static DungeonMap LoadOrCreate(string filePath, string expectedMapId, Func<DungeonMap> createMap)
        {
            if (string.IsNullOrWhiteSpace(expectedMapId))
                throw new ArgumentException("A map ID is required.", nameof(expectedMapId));
            if (createMap == null)
                throw new ArgumentNullException(nameof(createMap));

            if (File.Exists(filePath))
            {
                DungeonMap loadedMap = Load(filePath);
                if (!string.Equals(loadedMap.MapId, expectedMapId, StringComparison.Ordinal))
                    throw new InvalidDataException(
                        $"Map file '{filePath}' defines ID '{loadedMap.MapId}', expected '{expectedMapId}'.");
                return loadedMap;
            }

            DungeonMap newMap = createMap();
            if (newMap == null)
                throw new InvalidOperationException($"The map factory for '{expectedMapId}' returned no map.");

            newMap.MapId = expectedMapId;
            Save(filePath, newMap);
            return newMap;
        }

        public static string GetMapFilePath(string directory, string mapId)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("A map directory is required.", nameof(directory));
            if (string.IsNullOrWhiteSpace(mapId))
                throw new ArgumentException("A map ID is required.", nameof(mapId));

            string fileName = new string(mapId.Select(character =>
                char.IsLetterOrDigit(character) || character == '-' || character == '_'
                    ? character
                    : '_').ToArray());
            return System.IO.Path.Combine(directory, fileName + ".json");
        }

        private static StaticMapDocument CreateDocument(DungeonMap map)
        {
            List<ICell> cells = map.GetAllCells().ToList();
            int width = cells.Max(cell => cell.X) + 1;
            int height = cells.Max(cell => cell.Y) + 1;
            List<StaticMapTile> tiles = new List<StaticMapTile>();
            Dictionary<string, int> tileIds = new Dictionary<string, int>(StringComparer.Ordinal);
            List<string> rows = new List<string>(height);
            List<string> exploredRows = new List<string>(height);

            for (int y = 0; y < height; y++)
            {
                StringBuilder row = new StringBuilder(width * 4);
                StringBuilder exploredRow = new StringBuilder(width);
                for (int x = 0; x < width; x++)
                {
                    ICell cell = map.GetCell(x, y);
                    char symbol;
                    RLNET.RLColor color;
                    RLNET.RLColor background;
                    map.TryGetTerrainTile(x, y, out symbol, out color, out background);

                    bool occupiedByActor = map.NonPlayerCharacters.Any(npc => npc.X == x && npc.Y == y) ||
                        map.Monsters.Any(monster => monster.X == x && monster.Y == y) ||
                        (Game.Player != null && Game.Player.CurrentMapId == map.MapId &&
                            Game.Player.X == x && Game.Player.Y == y);
                    StaticMapTile tile = new StaticMapTile
                    {
                        Symbol = symbol,
                        Red = ToByte(color.r),
                        Green = ToByte(color.g),
                        Blue = ToByte(color.b),
                        BackgroundRed = ToByte(background.r),
                        BackgroundGreen = ToByte(background.g),
                        BackgroundBlue = ToByte(background.b),
                        Walkable = cell.IsWalkable || occupiedByActor,
                        Transparent = cell.IsTransparent
                    };
                    string signature = tile.GetSignature();
                    int tileId;
                    if (!tileIds.TryGetValue(signature, out tileId))
                    {
                        tileId = tiles.Count;
                        tileIds.Add(signature, tileId);
                        tile.Id = tileId;
                        tiles.Add(tile);
                    }

                    row.Append(tileId.ToString("X4", CultureInfo.InvariantCulture));
                    exploredRow.Append(cell.IsExplored ? '1' : '0');
                }

                rows.Add(row.ToString());
                exploredRows.Add(exploredRow.ToString());
            }

            StaticMapDocument document = new StaticMapDocument
            {
                Version = CurrentFormatVersion,
                MapId = map.MapId,
                DisplayName = map.DisplayName,
                Width = width,
                Height = height,
                LightingEnabled = map.LightingEnabled,
                PaletteName = map.PaletteName,
                Tiles = tiles,
                Rows = rows,
                ExploredRows = exploredRows,
                Exits = new List<StaticMapExit>(),
                Npcs = new List<StaticMapNpc>()
            };

            foreach (IInteractable interactable in map.interactables)
            {
                Stairs stairs = interactable as Stairs;
                if (stairs == null)
                    continue;

                document.Exits.Add(new StaticMapExit
                {
                    X = stairs.X,
                    Y = stairs.Y,
                    Symbol = stairs.Symbol,
                    TargetMapId = stairs.TargetMapId,
                    TargetLayer = stairs.TargetLayer,
                    TargetX = stairs.TargetX,
                    TargetY = stairs.TargetY
                });
            }

            foreach (NonPlayerCharacter npc in map.NonPlayerCharacters)
            {
                document.Npcs.Add(new StaticMapNpc
                {
                    Name = npc.Name,
                    X = npc.X,
                    Y = npc.Y,
                    Symbol = npc.Symbol,
                    Red = ToByte(npc.Color.r),
                    Green = ToByte(npc.Color.g),
                    Blue = ToByte(npc.Color.b),
                    Wanders = npc.Wanders
                });
            }

            return document;
        }

        private static DungeonMap CreateMap(StaticMapDocument document)
        {
            Dictionary<int, StaticMapTile> tiles = document.Tiles.ToDictionary(tile => tile.Id);
            DungeonMap map = new DungeonMap
            {
                MapId = document.MapId,
                DisplayName = document.DisplayName,
                LightingEnabled = document.LightingEnabled,
                PaletteName = document.PaletteName
            };
            map.Initialize(document.Width, document.Height);

            for (int y = 0; y < document.Height; y++)
            {
                string row = document.Rows[y];
                string exploredRow = document.ExploredRows[y];
                for (int x = 0; x < document.Width; x++)
                {
                    int tileId = int.Parse(row.Substring(x * 4, 4), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture);
                    StaticMapTile tile = tiles[tileId];
                    map.SetTerrainTile(x, y, (char)tile.Symbol, ToColor(tile.Red, tile.Green, tile.Blue),
                        ToColor(tile.BackgroundRed, tile.BackgroundGreen, tile.BackgroundBlue),
                        tile.Walkable, tile.Transparent, exploredRow[x] == '1');
                }
            }

            foreach (StaticMapExit exit in document.Exits)
            {
                map.AddInteractable(new Stairs
                {
                    X = exit.X,
                    Y = exit.Y,
                    Symbol = (char)exit.Symbol,
                    TargetMapId = exit.TargetMapId,
                    TargetLayer = exit.TargetLayer,
                    TargetX = exit.TargetX,
                    TargetY = exit.TargetY
                });
            }

            foreach (StaticMapNpc npc in document.Npcs)
            {
                map.AddNonPlayerCharacter(new NonPlayerCharacter
                {
                    Name = npc.Name,
                    X = npc.X,
                    Y = npc.Y,
                    Symbol = (char)npc.Symbol,
                    Color = ToColor(npc.Red, npc.Green, npc.Blue),
                    Wanders = npc.Wanders
                });
            }

            return map;
        }

        private static void Validate(StaticMapDocument document, string filePath)
        {
            if (document == null)
                throw new InvalidDataException($"Static map file '{filePath}' is empty.");
            if (document.Version != CurrentFormatVersion)
                throw new InvalidDataException(
                    $"Static map file '{filePath}' has unsupported version {document.Version}.");
            if (string.IsNullOrWhiteSpace(document.MapId) || string.IsNullOrWhiteSpace(document.DisplayName))
                throw new InvalidDataException($"Static map file '{filePath}' must define an ID and display name.");
            if (document.Width < 1 || document.Width > ConsoleDefinitions.MapConsoleWidth ||
                document.Height < 1 || document.Height > ConsoleDefinitions.MapConsoleHeight)
                throw new InvalidDataException(
                    $"Static map file '{filePath}' dimensions must fit the map console.");
            if (!IsSupportedPalette(document.PaletteName))
                throw new InvalidDataException(
                    $"Static map file '{filePath}' specifies unknown palette '{document.PaletteName}'.");
            if (document.Tiles == null || document.Tiles.Count == 0 ||
                document.Rows == null || document.Rows.Count != document.Height ||
                document.ExploredRows == null || document.ExploredRows.Count != document.Height ||
                document.Exits == null || document.Npcs == null)
                throw new InvalidDataException($"Static map file '{filePath}' is missing required map data.");

            HashSet<int> tileIds = new HashSet<int>();
            foreach (StaticMapTile tile in document.Tiles)
            {
                if (tile == null || tile.Id < 0 || tile.Id > 65535 || !tileIds.Add(tile.Id) ||
                    tile.Symbol < char.MinValue || tile.Symbol > char.MaxValue ||
                    !IsByte(tile.Red) || !IsByte(tile.Green) || !IsByte(tile.Blue) ||
                    !IsByte(tile.BackgroundRed) || !IsByte(tile.BackgroundGreen) || !IsByte(tile.BackgroundBlue))
                    throw new InvalidDataException($"Static map file '{filePath}' contains an invalid terrain tile.");
            }

            foreach (string row in document.Rows)
            {
                if (row == null || row.Length != document.Width * 4)
                    throw new InvalidDataException($"Static map file '{filePath}' has a malformed terrain row.");

                for (int x = 0; x < document.Width; x++)
                {
                    int tileId;
                    if (!int.TryParse(row.Substring(x * 4, 4), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out tileId) || !tileIds.Contains(tileId))
                        throw new InvalidDataException($"Static map file '{filePath}' references an unknown tile.");
                }
            }

            foreach (string row in document.ExploredRows)
            {
                if (row == null || row.Length != document.Width || row.Any(value => value != '0' && value != '1'))
                    throw new InvalidDataException($"Static map file '{filePath}' has a malformed visibility row.");
            }

            HashSet<Tuple<int, int>> entityPositions = new HashSet<Tuple<int, int>>();
            foreach (StaticMapExit exit in document.Exits)
            {
                if (exit == null || !IsInBounds(document, exit.X, exit.Y) || exit.Symbol < char.MinValue ||
                    exit.Symbol > char.MaxValue || (exit.TargetX.HasValue != exit.TargetY.HasValue) ||
                    (string.IsNullOrWhiteSpace(exit.TargetMapId) && exit.TargetLayer == 0) ||
                    !IsWalkableAt(document, tileIds, exit.X, exit.Y) ||
                    !entityPositions.Add(Tuple.Create(exit.X, exit.Y)))
                    throw new InvalidDataException($"Static map file '{filePath}' contains an invalid exit.");
            }

            foreach (StaticMapNpc npc in document.Npcs)
            {
                if (npc == null || !IsInBounds(document, npc.X, npc.Y) || string.IsNullOrWhiteSpace(npc.Name) ||
                    npc.Symbol < char.MinValue || npc.Symbol > char.MaxValue ||
                    !IsByte(npc.Red) || !IsByte(npc.Green) || !IsByte(npc.Blue) ||
                    !IsWalkableAt(document, tileIds, npc.X, npc.Y) ||
                    !entityPositions.Add(Tuple.Create(npc.X, npc.Y)))
                    throw new InvalidDataException($"Static map file '{filePath}' contains an invalid NPC.");
            }
        }

        private static bool IsWalkableAt(StaticMapDocument document, HashSet<int> tileIds, int x, int y)
        {
            int tileId = int.Parse(document.Rows[y].Substring(x * 4, 4), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture);
            return document.Tiles.First(tile => tile.Id == tileId).Walkable;
        }

        private static bool IsSupportedPalette(string name)
        {
            return string.Equals(name, "Dawnbringer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Monochrome", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Ember", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInBounds(StaticMapDocument document, int x, int y)
        {
            return x >= 0 && y >= 0 && x < document.Width && y < document.Height;
        }

        private static bool IsByte(int value)
        {
            return value >= byte.MinValue && value <= byte.MaxValue;
        }

        private static int ToByte(float value)
        {
            return Math.Max(byte.MinValue, Math.Min(byte.MaxValue,
                (int)Math.Round(value * byte.MaxValue)));
        }

        private static RLNET.RLColor ToColor(int red, int green, int blue)
        {
            return new RLNET.RLColor((byte)red, (byte)green, (byte)blue);
        }

        [DataContract]
        private sealed class StaticMapDocument
        {
            [DataMember(Order = 1)] public int Version { get; set; }
            [DataMember(Order = 2)] public string MapId { get; set; }
            [DataMember(Order = 3)] public string DisplayName { get; set; }
            [DataMember(Order = 4)] public int Width { get; set; }
            [DataMember(Order = 5)] public int Height { get; set; }
            [DataMember(Order = 6)] public bool LightingEnabled { get; set; }
            [DataMember(Order = 7)] public string PaletteName { get; set; }
            [DataMember(Order = 8)] public List<StaticMapTile> Tiles { get; set; }
            [DataMember(Order = 9)] public List<string> Rows { get; set; }
            [DataMember(Order = 10)] public List<string> ExploredRows { get; set; }
            [DataMember(Order = 11)] public List<StaticMapExit> Exits { get; set; }
            [DataMember(Order = 12)] public List<StaticMapNpc> Npcs { get; set; }
        }

        [DataContract]
        private sealed class StaticMapTile
        {
            [DataMember(Order = 1)] public int Id { get; set; }
            [DataMember(Order = 2)] public int Symbol { get; set; }
            [DataMember(Order = 3)] public int Red { get; set; }
            [DataMember(Order = 4)] public int Green { get; set; }
            [DataMember(Order = 5)] public int Blue { get; set; }
            [DataMember(Order = 6)] public int BackgroundRed { get; set; }
            [DataMember(Order = 7)] public int BackgroundGreen { get; set; }
            [DataMember(Order = 8)] public int BackgroundBlue { get; set; }
            [DataMember(Order = 9)] public bool Walkable { get; set; }
            [DataMember(Order = 10)] public bool Transparent { get; set; }

            public string GetSignature()
            {
                return string.Join(",", Symbol, Red, Green, Blue, BackgroundRed, BackgroundGreen,
                    BackgroundBlue, Walkable, Transparent);
            }
        }

        [DataContract]
        private sealed class StaticMapExit
        {
            [DataMember(Order = 1)] public int X { get; set; }
            [DataMember(Order = 2)] public int Y { get; set; }
            [DataMember(Order = 3)] public int Symbol { get; set; }
            [DataMember(Order = 4)] public string TargetMapId { get; set; }
            [DataMember(Order = 5)] public int TargetLayer { get; set; }
            [DataMember(Order = 6, EmitDefaultValue = false)] public int? TargetX { get; set; }
            [DataMember(Order = 7, EmitDefaultValue = false)] public int? TargetY { get; set; }
        }

        [DataContract]
        private sealed class StaticMapNpc
        {
            [DataMember(Order = 1)] public string Name { get; set; }
            [DataMember(Order = 2)] public int X { get; set; }
            [DataMember(Order = 3)] public int Y { get; set; }
            [DataMember(Order = 4)] public int Symbol { get; set; }
            [DataMember(Order = 5)] public int Red { get; set; }
            [DataMember(Order = 6)] public int Green { get; set; }
            [DataMember(Order = 7)] public int Blue { get; set; }
            [DataMember(Order = 8)] public bool Wanders { get; set; }
        }
    }
}
