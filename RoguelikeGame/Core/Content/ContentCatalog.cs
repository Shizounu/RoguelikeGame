using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using RoguelikeGame.Color;
using RoguelikeGame.Core.Map.Actors;
using RoguelikeGame.Map;
using RoguelikeGame.Map.Actors;
using RoguelikeGame.Map.Actors.Drops;
using RoguelikeGame.Map.Object;
using RoguelikeGame.Systems.Inventory;
using RoguelikeGame.Systems.Inventory.ItemDefinition;
using RoguelikeGame.Systems.Event;
using RogueSharp.DiceNotation;

namespace RoguelikeGame.Systems.Content
{
    public sealed class ContentCatalog
    {
        private static readonly Regex ContentIdPattern =
            new Regex(@"^[A-Za-z][A-Za-z0-9_-]*(\.[A-Za-z][A-Za-z0-9_-]*)+$", RegexOptions.Compiled);
        private static readonly Regex DiceExpressionPattern =
            new Regex(@"^\d+[dD]\d+$", RegexOptions.Compiled);

        private static ContentCatalog _current;
        private readonly HashSet<string> _registeredIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ItemDefinition> _items =
            new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EnemyDefinition> _enemies =
            new Dictionary<string, EnemyDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SpawnTableDefinition> _spawnTables =
            new Dictionary<string, SpawnTableDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PaletteDefinition> _palettes =
            new Dictionary<string, PaletteDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, WorldDefinition> _worlds =
            new Dictionary<string, WorldDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CityDefinition> _cities =
            new Dictionary<string, CityDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, NpcDefinition> _npcs =
            new Dictionary<string, NpcDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DialogueDefinition> _dialogues =
            new Dictionary<string, DialogueDefinition>(StringComparer.OrdinalIgnoreCase);

        private ContentCatalog()
        {
        }

        public static ContentCatalog Current
        {
            get
            {
                if (_current == null)
                    throw new InvalidOperationException("Content has not been loaded. Load the content pack before using game content.");
                return _current;
            }
        }

        public static ContentCatalog LoadFromDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("A content directory is required.", nameof(directory));
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Content directory '{directory}' does not exist.");

            string[] files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (files.Length == 0)
                throw new InvalidDataException($"Content directory '{directory}' contains no JSON content packs.");

            ContentCatalog catalog = new ContentCatalog();
            foreach (string file in files)
            {
                ContentPack pack;
                try
                {
                    using (FileStream stream = File.OpenRead(file))
                    {
                        pack = (ContentPack)new DataContractJsonSerializer(typeof(ContentPack))
                            .ReadObject(stream);
                    }
                }
                catch (SerializationException exception)
                {
                    throw new InvalidDataException($"Content pack '{file}' contains invalid JSON.", exception);
                }

                catalog.RegisterPack(pack, file);
            }

            catalog.ValidateReferences(files);
            _current = catalog;
            return catalog;
        }

        public ItemDefinition GetItem(string id)
        {
            return Get(_items, id, "item");
        }

        public EnemyDefinition GetEnemy(string id)
        {
            return Get(_enemies, id, "enemy");
        }

        public SpawnTableDefinition GetSpawnTable(string id)
        {
            return Get(_spawnTables, id, "spawn table");
        }

        public PaletteDefinition GetPalette(string id)
        {
            string paletteId = id != null && id.IndexOf('.') < 0 ? "Palettes." + id : id;
            return Get(_palettes, paletteId, "palette");
        }

        public WorldDefinition GetWorld(string id)
        {
            return Get(_worlds, id, "world");
        }

        public CityDefinition GetCity(string id)
        {
            return Get(_cities, id, "city");
        }

        public NpcDefinition GetNpc(string id)
        {
            return Get(_npcs, id, "NPC");
        }

        public DialogueDefinition GetDialogue(string id)
        {
            return Get(_dialogues, id, "dialogue");
        }

        public NpcDefinition GetCityNpc(string mapId, string name)
        {
            CityDefinition city = _cities.Values.SingleOrDefault(value =>
                string.Equals(value.MapId, mapId, StringComparison.OrdinalIgnoreCase));
            if (city == null)
                throw new KeyNotFoundException($"No city content definition is registered for map '{mapId}'.");

            NpcDefinition npc = city.Npcs
                .Select(placement => GetNpc(placement.NpcId))
                .SingleOrDefault(value => string.Equals(value.Name, name, StringComparison.Ordinal));
            if (npc == null)
                throw new InvalidDataException($"City '{city.Id}' has no NPC named '{name}'.");
            return npc;
        }

        public IItem CreateItem(string id)
        {
            ItemDefinition definition = GetItem(id);
            switch (definition.Behavior)
            {
                case "Consumable.Heal":
                    return new HealingPotion(definition);
                default:
                    throw new InvalidOperationException(
                        $"Item '{definition.Id}' uses unregistered behavior '{definition.Behavior}'.");
            }
        }

        public Monster CreateEnemy(string id, int level)
        {
            EnemyDefinition definition = GetEnemy(id);
            int effectiveLevel = Math.Max(1, level);
            int health = Dice.Roll(definition.HealthDice);
            Monster monster = new Monster
            {
                ContentId = definition.Id,
                Name = definition.Name,
                Level = effectiveLevel,
                Attack = RollScaled(definition.AttackDice, effectiveLevel, definition.AttackLevelDivisor),
                AttackChance = Dice.Roll(definition.AttackChanceDice),
                Awareness = definition.Awareness,
                Defense = RollScaled(definition.DefenseDice, effectiveLevel, definition.DefenseLevelDivisor),
                DefenseChance = Dice.Roll(definition.DefenseChanceDice),
                Gold = Dice.Roll(definition.GoldDice),
                Health = health,
                MaxHealth = health,
                Speed = definition.Speed,
                Symbol = definition.Symbol,
                Color = GetColor(definition.ColorRole),
                BehaviorId = definition.Behavior,
                DropDefinitions = definition.Drops
            };

            EventSystem.Instance.OnActorDeath += (invoker, args) => monster.DoDropsIfKilled(args);
            return monster;
        }

        public RLNET.RLColor GetColor(string roleId)
        {
            string[] parts = roleId.Split('.');
            if (parts.Length < 3)
                throw new InvalidDataException($"Color role '{roleId}' must include a palette ID and role name.");

            string paletteId = string.Join(".", parts.Take(parts.Length - 1));
            return GetPalette(paletteId).GetColor(parts[parts.Length - 1]);
        }

        public void ExecuteDrop(DropDefinition drop, DungeonMap map, Monster monster)
        {
            switch (drop.Behavior)
            {
                case "Drop.Gold":
                    new GoldDrop().DoDrop(map, monster);
                    return;
                case "Drop.Item":
                    if (Dice.Roll(drop.ChanceDice) < drop.ChanceThreshold)
                        return;

                    IItem item = CreateItem(drop.ItemId);
                    IInteractable interactable = item as IInteractable;
                    if (interactable == null)
                        throw new InvalidOperationException(
                            $"Drop '{drop.Id}' item '{drop.ItemId}' is not interactable.");

                    interactable.X = monster.X;
                    interactable.Y = monster.Y;
                    map.AddInteractable(interactable);
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Drop '{drop.Id}' uses unregistered behavior '{drop.Behavior}'.");
            }
        }

        private void RegisterPack(ContentPack pack, string file)
        {
            if (pack == null)
                throw new InvalidDataException($"Content pack '{file}' is empty.");

            RegisterDefinitions(pack.Items, _items, file, ValidateItem);
            RegisterDefinitions(pack.Enemies, _enemies, file, ValidateEnemy);
            RegisterDefinitions(pack.SpawnTables, _spawnTables, file, ValidateSpawnTable);
            RegisterDefinitions(pack.Palettes, _palettes, file, ValidatePalette);
            RegisterDefinitions(pack.Worlds, _worlds, file, ValidateWorld);
            RegisterDefinitions(pack.Cities, _cities, file, ValidateCity);
            RegisterDefinitions(pack.Npcs, _npcs, file, ValidateNpc);
            RegisterDefinitions(pack.Dialogues, _dialogues, file, ValidateDialogue);
        }

        private void RegisterDefinitions<T>(List<T> definitions, Dictionary<string, T> target,
            string file, Action<T, string> validate) where T : ContentDefinition
        {
            if (definitions == null)
                return;

            foreach (T definition in definitions)
            {
                if (definition == null || !ContentIdPattern.IsMatch(definition.Id ?? string.Empty))
                    throw new InvalidDataException($"Content pack '{file}' contains an invalid dotted content ID.");
                if (!_registeredIds.Add(definition.Id))
                    throw new InvalidDataException(
                        $"Content pack '{file}' duplicates content ID '{definition.Id}' (IDs are case-insensitive).");

                validate(definition, file);
                target.Add(definition.Id, definition);
            }
        }

        private void ValidateReferences(IEnumerable<string> files)
        {
            foreach (ItemDefinition item in _items.Values)
                ValidateColorRole(item.ColorRole, item.Id);

            foreach (EnemyDefinition enemy in _enemies.Values)
            {
                ValidateColorRole(enemy.ColorRole, enemy.Id);
                foreach (DropDefinition drop in enemy.Drops)
                {
                    if (drop.Behavior == "Drop.Item")
                        Require(_items, drop.ItemId, drop.Id, "item");
                }
            }

            foreach (SpawnTableDefinition table in _spawnTables.Values)
                Require(_enemies, table.EnemyId, table.Id, "enemy");

            foreach (CityDefinition city in _cities.Values)
            {
                HashSet<string> placements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string paletteId = city.PaletteName.IndexOf('.') < 0
                    ? "Palettes." + city.PaletteName
                    : city.PaletteName;
                Require(_palettes, paletteId, city.Id, "palette");
                ValidateColorRole(city.Terrain.Interior.ColorRole, city.Id);
                ValidateColorRole(city.Terrain.Interior.BackgroundColorRole, city.Id);
                ValidateColorRole(city.Terrain.Border.ColorRole, city.Id);
                ValidateColorRole(city.Terrain.Border.BackgroundColorRole, city.Id);
                foreach (CityFeatureDefinition feature in city.Features)
                {
                    ValidateColorRole(feature.ColorRole, city.Id);
                    ValidateColorRole(feature.BackgroundColorRole, city.Id);
                }

                foreach (CityNpcPlacementDefinition placement in city.Npcs)
                {
                    Require(_npcs, placement.NpcId, city.Id, "NPC");
                    if (!placements.Add(placement.NpcId))
                        throw new InvalidDataException(
                            $"City '{city.Id}' contains multiple spawn placements for NPC '{placement.NpcId}'.");
                }

                foreach (CityExitDefinition exit in city.Exits)
                {
                    if (string.IsNullOrWhiteSpace(exit.TargetMapId))
                        throw new InvalidDataException($"City '{city.Id}' contains an exit without a target map ID.");
                    bool targetExists = _worlds.Values.Any(world =>
                        string.Equals(world.MapId, exit.TargetMapId, StringComparison.OrdinalIgnoreCase)) ||
                        _cities.Values.Any(targetCity =>
                            string.Equals(targetCity.MapId, exit.TargetMapId, StringComparison.OrdinalIgnoreCase)) ||
                        exit.TargetMapId.StartsWith("dungeon:", StringComparison.OrdinalIgnoreCase);
                    if (!targetExists)
                        throw new InvalidDataException(
                            $"City '{city.Id}' exit references unknown map '{exit.TargetMapId}'.");
                }
            }

            foreach (NpcDefinition npc in _npcs.Values)
            {
                if (!string.IsNullOrWhiteSpace(npc.ColorRole))
                    ValidateColorRole(npc.ColorRole, npc.Id);
                Require(_dialogues, npc.DialogueTreeId, npc.Id, "dialogue tree");
            }

            foreach (DialogueDefinition dialogue in _dialogues.Values)
            {
                HashSet<string> nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DialogueNodeDefinition node in dialogue.Nodes)
                {
                    if (!nodeIds.Add(node.Id))
                        throw new InvalidDataException(
                            $"Dialogue '{dialogue.Id}' contains duplicate node ID '{node.Id}'.");
                }

                foreach (DialogueBranchDefinition entry in dialogue.EntryNodes)
                    RequireNode(nodeIds, entry.NodeId, dialogue.Id);

                foreach (DialogueNodeDefinition node in dialogue.Nodes)
                {
                    foreach (DialogueBranchDefinition branch in node.Branches)
                        RequireNode(nodeIds, branch.NodeId, dialogue.Id);
                }
            }

            foreach (PaletteDefinition palette in _palettes.Values)
            {
                foreach (PaletteColorDefinition color in palette.Colors)
                {
                    if (!string.IsNullOrWhiteSpace(color.Name))
                        palette.GetColor(color.Name);
                }
            }
        }

        private static string PaletteIdForRole(string roleId)
        {
            string[] parts = roleId.Split('.');
            if (parts.Length < 3)
                throw new InvalidDataException($"Color role '{roleId}' must include a palette ID and role name.");
            return string.Join(".", parts.Take(parts.Length - 1));
        }

        private void ValidateColorRole(string roleId, string owner)
        {
            string paletteId = PaletteIdForRole(roleId);
            Require(_palettes, paletteId, owner, "palette");
            string roleName = roleId.Substring(roleId.LastIndexOf('.') + 1);
            try
            {
                _palettes[paletteId].GetColor(roleName);
            }
            catch (KeyNotFoundException exception)
            {
                throw new InvalidDataException(
                    $"Definition '{owner}' references unknown color role '{roleId}'.", exception);
            }
        }

        private void ValidateItem(ItemDefinition item, string file)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.Weight < 0 ||
                !Enum.TryParse(item.Type, true, out ItemType itemType) || !Enum.IsDefined(typeof(ItemType), itemType))
                throw Invalid(file, item.Id, "invalid item name, weight, or type");
            if (item.Behavior != "Consumable.Heal" || string.IsNullOrWhiteSpace(item.EffectDice))
                throw Invalid(file, item.Id, $"unsupported item behavior '{item.Behavior}'");
            ValidateDice(item.EffectDice, file, item.Id);
        }

        private void ValidateEnemy(EnemyDefinition enemy, string file)
        {
            if (string.IsNullOrWhiteSpace(enemy.Name) || enemy.Speed <= 0 || enemy.Awareness < 0 ||
                enemy.AttackLevelDivisor < 1 || enemy.DefenseLevelDivisor < 1 ||
                enemy.Behavior != "AI.StandardMoveAndAttack" || enemy.Drops == null)
                throw Invalid(file, enemy.Id, "invalid stats, drops, or unsupported AI behavior");

            ValidateDice(enemy.HealthDice, file, enemy.Id);
            ValidateDice(enemy.AttackDice, file, enemy.Id);
            ValidateDice(enemy.AttackChanceDice, file, enemy.Id);
            ValidateDice(enemy.DefenseDice, file, enemy.Id);
            ValidateDice(enemy.DefenseChanceDice, file, enemy.Id);
            ValidateDice(enemy.GoldDice, file, enemy.Id);

            foreach (DropDefinition drop in enemy.Drops)
            {
                if (drop == null || !ContentIdPattern.IsMatch(drop.Id ?? string.Empty) ||
                    (drop.Behavior != "Drop.Gold" && drop.Behavior != "Drop.Item"))
                    throw Invalid(file, enemy.Id, "contains an invalid or unsupported drop behavior");
                if (!_registeredIds.Add(drop.Id))
                    throw Invalid(file, enemy.Id, $"duplicates content ID '{drop.Id}'");
                if (drop.Behavior == "Drop.Item")
                {
                    ValidateDice(drop.ChanceDice, file, drop.Id);
                    if (drop.ChanceThreshold < 1)
                        throw Invalid(file, drop.Id, "must have a positive drop threshold");
                }
            }
        }

        private void ValidateSpawnTable(SpawnTableDefinition table, string file)
        {
            if (table.MinimumLayer < 1 || table.MaximumLayer < table.MinimumLayer ||
                table.BaseRoomThreshold < 0 || table.ThresholdDecreaseAfterLayer < 0 ||
                table.ThresholdDecreasePerLayer < 0 || table.MinimumEnemyLevel < 1 ||
                table.EnemyLevelDivisor < 1 || string.IsNullOrWhiteSpace(table.CountDiceTemplate))
                throw Invalid(file, table.Id, "contains invalid spawn scaling values");
            ValidateDice(table.RoomChanceDice, file, table.Id);
            if (!table.CountDiceTemplate.Contains("{Layer}"))
                throw Invalid(file, table.Id, "count dice template must include {Layer}");
        }

        private void ValidatePalette(PaletteDefinition palette, string file)
        {
            if (palette.Colors == null || palette.Colors.Count == 0 ||
                (palette.Transform != "Identity" && palette.Transform != "Grayscale" && palette.Transform != "Multiply"))
                throw Invalid(file, palette.Id, "contains no colors or an unsupported palette transform");

            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PaletteColorDefinition color in palette.Colors)
            {
                if (color == null || string.IsNullOrWhiteSpace(color.Name) || !names.Add(color.Name) ||
                    !IsByte(color.Red) || !IsByte(color.Green) || !IsByte(color.Blue))
                    throw Invalid(file, palette.Id, "contains an invalid or duplicate color role");
            }
        }

        private void ValidateWorld(WorldDefinition world, string file)
        {
            ValidateMapDefinition(world, file);
        }

        private void ValidateCity(CityDefinition city, string file)
        {
            if (string.IsNullOrWhiteSpace(city.MapId) ||
                city.Width < 3 || city.Height < 3 || string.IsNullOrWhiteSpace(city.DisplayName) ||
                city.ArrivalX < 0 || city.ArrivalY < 0 ||
                city.ArrivalX >= city.Width || city.ArrivalY >= city.Height ||
                string.IsNullOrWhiteSpace(city.PaletteName) || city.Terrain == null ||
                city.Features == null || city.Npcs == null || city.Exits == null)
                throw Invalid(file, city.Id, "must define a map ID, dimensions, appearance, terrain, features, NPCs, and exits");

            ValidateCityTile(city.Terrain.Interior, city, file, "interior");
            ValidateCityTile(city.Terrain.Border, city, file, "border");

            foreach (CityFeatureDefinition feature in city.Features)
            {
                if (feature == null || feature.X < 0 || feature.Y < 0 || feature.Width < 1 || feature.Height < 1 ||
                    feature.X + feature.Width > city.Width || feature.Y + feature.Height > city.Height)
                    throw Invalid(file, city.Id, "contains a city feature outside the map bounds");
                ValidateCityTile(feature, city, file, "feature");
            }

            foreach (CityNpcPlacementDefinition placement in city.Npcs)
            {
                if (placement == null || string.IsNullOrWhiteSpace(placement.NpcId) ||
                    placement.X < 0 || placement.Y < 0 || placement.X >= city.Width || placement.Y >= city.Height)
                    throw Invalid(file, city.Id, "contains an invalid NPC spawn placement");
                if (!GetCityTileAt(city, placement.X, placement.Y).Walkable)
                    throw Invalid(file, city.Id, $"spawns NPC '{placement.NpcId}' on an unwalkable tile");
            }

            foreach (CityExitDefinition exit in city.Exits)
            {
                if (exit == null || exit.X < 0 || exit.Y < 0 || exit.X >= city.Width || exit.Y >= city.Height ||
                    string.IsNullOrWhiteSpace(exit.TargetMapId) ||
                    (exit.TargetX.HasValue != exit.TargetY.HasValue))
                    throw Invalid(file, city.Id, "contains an invalid exit");
                if (!GetCityTileAt(city, exit.X, exit.Y).Walkable)
                    throw Invalid(file, city.Id, "places an exit on an unwalkable tile");
            }

            if (!GetCityTileAt(city, city.ArrivalX, city.ArrivalY).Walkable)
                throw Invalid(file, city.Id, "arrival coordinates must be on a walkable tile");
        }

        private static CityTileDefinition GetCityTileAt(CityDefinition city, int x, int y)
        {
            CityTileDefinition tile = x == 0 || y == 0 || x == city.Width - 1 || y == city.Height - 1
                ? city.Terrain.Border
                : city.Terrain.Interior;
            foreach (CityFeatureDefinition feature in city.Features)
            {
                if (x >= feature.X && x < feature.X + feature.Width &&
                    y >= feature.Y && y < feature.Y + feature.Height)
                    tile = feature;
            }

            return tile;
        }

        private void ValidateCityTile(CityTileDefinition tile, CityDefinition city, string file, string kind)
        {
            if (tile == null || string.IsNullOrWhiteSpace(tile.ColorRole) ||
                string.IsNullOrWhiteSpace(tile.BackgroundColorRole))
                throw Invalid(file, city.Id, $"must define {kind} tile color roles");
        }

        private static void ValidateMapDefinition(MapContentDefinition map, string file)
        {
            if (string.IsNullOrWhiteSpace(map.MapId) || string.IsNullOrWhiteSpace(map.StaticMapFile) ||
                Path.IsPathRooted(map.StaticMapFile) ||
                map.StaticMapFile.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(segment => segment == ".."))
                throw Invalid(file, map.Id, "contains an invalid map ID or static map path");
        }

        private void ValidateNpc(NpcDefinition npc, string file)
        {
            if (string.IsNullOrWhiteSpace(npc.Name) || string.IsNullOrWhiteSpace(npc.ColorRole) ||
                string.IsNullOrWhiteSpace(npc.DialogueTreeId))
                throw Invalid(file, npc.Id, "must define a name, color role, and dialogue tree ID");
        }

        private void ValidateDialogue(DialogueDefinition dialogue, string file)
        {
            if (dialogue.Nodes == null || dialogue.Nodes.Count == 0 ||
                dialogue.EntryNodes == null || dialogue.EntryNodes.Count == 0)
                throw Invalid(file, dialogue.Id, "must define nodes and at least one entry node");

            HashSet<string> nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DialogueNodeDefinition node in dialogue.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) ||
                    !nodeIds.Add(node.Id) || node.Branches == null || node.Type != "Sentence" ||
                    string.IsNullOrWhiteSpace(node.Text))
                    throw Invalid(file, dialogue.Id,
                        "contains an invalid node; current runtime supports non-empty Sentence nodes");
            }

            foreach (DialogueBranchDefinition entry in dialogue.EntryNodes)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.NodeId))
                    throw Invalid(file, dialogue.Id, "contains an invalid entry node reference");
            }

            foreach (DialogueNodeDefinition node in dialogue.Nodes)
            {
                foreach (DialogueBranchDefinition branch in node.Branches)
                {
                    if (branch == null || string.IsNullOrWhiteSpace(branch.NodeId))
                        throw Invalid(file, dialogue.Id, $"node '{node.Id}' contains an invalid branch");
                }
            }
        }

        private static void RequireNode(HashSet<string> nodeIds, string id, string dialogueId)
        {
            if (!nodeIds.Contains(id))
                throw new InvalidDataException(
                    $"Dialogue '{dialogueId}' references unknown node '{id}'.");
        }

        public string GetOpeningLine(string dialogueId)
        {
            DialogueDefinition dialogue = GetDialogue(dialogueId);
            DialogueBranchDefinition entry = dialogue.EntryNodes
                .OrderByDescending(branch => branch.Priority)
                .First();
            DialogueNodeDefinition node = dialogue.Nodes.Single(value =>
                string.Equals(value.Id, entry.NodeId, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(node.Speaker)
                ? node.Text
                : $"{node.Speaker} says, '{node.Text}'";
        }

        private static void ValidateDice(string expression, string file, string id)
        {
            if (!DiceExpressionPattern.IsMatch(expression ?? string.Empty))
                throw Invalid(file, id, $"has invalid dice expression '{expression}'");
        }

        private static bool IsByte(int value)
        {
            return value >= byte.MinValue && value <= byte.MaxValue;
        }

        private static InvalidDataException Invalid(string file, string id, string message)
        {
            return new InvalidDataException($"Content pack '{file}', definition '{id}': {message}.");
        }

        private static int RollScaled(string expression, int level, int divisor)
        {
            return Dice.Roll(expression) + level / divisor;
        }

        private static void Require<T>(Dictionary<string, T> definitions, string id, string owner, string type)
        {
            if (string.IsNullOrWhiteSpace(id) || !definitions.ContainsKey(id))
                throw new InvalidDataException($"Definition '{owner}' references unknown {type} '{id}'.");
        }

        private static T Get<T>(Dictionary<string, T> definitions, string id, string type)
        {
            T value;
            if (string.IsNullOrWhiteSpace(id) || !definitions.TryGetValue(id, out value))
                throw new KeyNotFoundException($"No {type} is registered as '{id}'.");
            return value;
        }

        [DataContract]
        public sealed class ContentPack
        {
            [DataMember(Order = 1)] public List<ItemDefinition> Items { get; set; }
            [DataMember(Order = 2)] public List<EnemyDefinition> Enemies { get; set; }
            [DataMember(Order = 3)] public List<SpawnTableDefinition> SpawnTables { get; set; }
            [DataMember(Order = 4)] public List<PaletteDefinition> Palettes { get; set; }
            [DataMember(Order = 5)] public List<WorldDefinition> Worlds { get; set; }
            [DataMember(Order = 6)] public List<CityDefinition> Cities { get; set; }
            [DataMember(Order = 7)] public List<NpcDefinition> Npcs { get; set; }
            [DataMember(Order = 8)] public List<DialogueDefinition> Dialogues { get; set; }
        }

        [DataContract]
        public abstract class ContentDefinition
        {
            [DataMember(Order = 1)] public string Id { get; set; }
        }

        [DataContract]
        public sealed class ItemDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public string Name { get; set; }
            [DataMember(Order = 3)] public float Weight { get; set; }
            [DataMember(Order = 4)] public string Type { get; set; }
            [DataMember(Order = 5)] public char Symbol { get; set; }
            [DataMember(Order = 6)] public string ColorRole { get; set; }
            [DataMember(Order = 7)] public string Behavior { get; set; }
            [DataMember(Order = 8)] public string EffectDice { get; set; }
        }

        [DataContract]
        public sealed class EnemyDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public string Name { get; set; }
            [DataMember(Order = 3)] public int Awareness { get; set; }
            [DataMember(Order = 4)] public int Speed { get; set; }
            [DataMember(Order = 5)] public char Symbol { get; set; }
            [DataMember(Order = 6)] public string ColorRole { get; set; }
            [DataMember(Order = 7)] public string Behavior { get; set; }
            [DataMember(Order = 8)] public string HealthDice { get; set; }
            [DataMember(Order = 9)] public string AttackDice { get; set; }
            [DataMember(Order = 10)] public int AttackLevelDivisor { get; set; }
            [DataMember(Order = 11)] public string AttackChanceDice { get; set; }
            [DataMember(Order = 12)] public string DefenseDice { get; set; }
            [DataMember(Order = 13)] public int DefenseLevelDivisor { get; set; }
            [DataMember(Order = 14)] public string DefenseChanceDice { get; set; }
            [DataMember(Order = 15)] public string GoldDice { get; set; }
            [DataMember(Order = 16)] public List<DropDefinition> Drops { get; set; }
        }

        [DataContract]
        public sealed class DropDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public string Behavior { get; set; }
            [DataMember(Order = 3)] public string ItemId { get; set; }
            [DataMember(Order = 4)] public string ChanceDice { get; set; }
            [DataMember(Order = 5)] public int ChanceThreshold { get; set; }
        }

        [DataContract]
        public sealed class SpawnTableDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public int MinimumLayer { get; set; }
            [DataMember(Order = 3)] public int MaximumLayer { get; set; }
            [DataMember(Order = 4)] public string EnemyId { get; set; }
            [DataMember(Order = 5)] public string RoomChanceDice { get; set; }
            [DataMember(Order = 6)] public int BaseRoomThreshold { get; set; }
            [DataMember(Order = 7)] public int ThresholdDecreaseAfterLayer { get; set; }
            [DataMember(Order = 8)] public int ThresholdDecreasePerLayer { get; set; }
            [DataMember(Order = 9)] public string CountDiceTemplate { get; set; }
            [DataMember(Order = 10)] public int MinimumEnemyLevel { get; set; }
            [DataMember(Order = 11)] public int EnemyLevelDivisor { get; set; }
        }

        [DataContract]
        public sealed class PaletteDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public string Transform { get; set; }
            [DataMember(Order = 3)] public float RedMultiplier { get; set; }
            [DataMember(Order = 4)] public float GreenMultiplier { get; set; }
            [DataMember(Order = 5)] public float BlueMultiplier { get; set; }
            [DataMember(Order = 6)] public List<PaletteColorDefinition> Colors { get; set; }

            public RLNET.RLColor GetColor(string name)
            {
                PaletteColorDefinition color = Colors.SingleOrDefault(value =>
                    string.Equals(value.Name, name, StringComparison.OrdinalIgnoreCase));
                if (color == null)
                    throw new KeyNotFoundException($"Palette '{Id}' has no color role '{name}'.");
                return new RLNET.RLColor((byte)color.Red, (byte)color.Green, (byte)color.Blue);
            }

            public RLNET.RLColor Apply(RLNET.RLColor color)
            {
                float red;
                float green;
                float blue;
                switch (Transform)
                {
                    case "Identity":
                        red = color.r;
                        green = color.g;
                        blue = color.b;
                        break;
                    case "Grayscale":
                        red = green = blue = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
                        break;
                    case "Multiply":
                        red = color.r * RedMultiplier;
                        green = color.g * GreenMultiplier;
                        blue = color.b * BlueMultiplier;
                        break;
                    default:
                        throw new InvalidOperationException($"Palette '{Id}' has unsupported transform '{Transform}'.");
                }

                return new RLNET.RLColor(Clamp(red), Clamp(green), Clamp(blue));
            }

            private static float Clamp(float value)
            {
                return Math.Max(0, Math.Min(1, value));
            }
        }

        [DataContract]
        public sealed class PaletteColorDefinition
        {
            [DataMember(Order = 1)] public string Name { get; set; }
            [DataMember(Order = 2)] public int Red { get; set; }
            [DataMember(Order = 3)] public int Green { get; set; }
            [DataMember(Order = 4)] public int Blue { get; set; }
        }

        [DataContract]
        public abstract class MapContentDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public string MapId { get; set; }
            [DataMember(Order = 3)] public string StaticMapFile { get; set; }
            [DataMember(Order = 4)] public int ArrivalX { get; set; }
            [DataMember(Order = 5)] public int ArrivalY { get; set; }
        }

        [DataContract]
        public sealed class WorldDefinition : MapContentDefinition
        {
        }

        [DataContract]
        public sealed class CityDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public string MapId { get; set; }
            [DataMember(Order = 3)] public int ArrivalX { get; set; }
            [DataMember(Order = 4)] public int ArrivalY { get; set; }
            [DataMember(Order = 5)] public string DisplayName { get; set; }
            [DataMember(Order = 6)] public int Width { get; set; }
            [DataMember(Order = 7)] public int Height { get; set; }
            [DataMember(Order = 8)] public bool LightingEnabled { get; set; }
            [DataMember(Order = 9)] public string PaletteName { get; set; }
            [DataMember(Order = 10)] public CityTerrainDefinition Terrain { get; set; }
            [DataMember(Order = 11)] public List<CityFeatureDefinition> Features { get; set; }
            [DataMember(Order = 12)] public List<CityNpcPlacementDefinition> Npcs { get; set; }
            [DataMember(Order = 13)] public List<CityExitDefinition> Exits { get; set; }
        }

        [DataContract]
        public sealed class CityTerrainDefinition
        {
            [DataMember(Order = 1)] public CityTileDefinition Interior { get; set; }
            [DataMember(Order = 2)] public CityTileDefinition Border { get; set; }
        }

        [DataContract]
        public class CityTileDefinition
        {
            [DataMember(Order = 1)] public char Symbol { get; set; }
            [DataMember(Order = 2)] public string ColorRole { get; set; }
            [DataMember(Order = 3)] public string BackgroundColorRole { get; set; }
            [DataMember(Order = 4)] public bool Walkable { get; set; }
            [DataMember(Order = 5)] public bool Transparent { get; set; }
        }

        [DataContract]
        public sealed class CityFeatureDefinition : CityTileDefinition
        {
            [DataMember(Order = 6)] public int X { get; set; }
            [DataMember(Order = 7)] public int Y { get; set; }
            [DataMember(Order = 8)] public int Width { get; set; }
            [DataMember(Order = 9)] public int Height { get; set; }
        }

        [DataContract]
        public sealed class CityNpcPlacementDefinition
        {
            [DataMember(Order = 1)] public string NpcId { get; set; }
            [DataMember(Order = 2)] public int X { get; set; }
            [DataMember(Order = 3)] public int Y { get; set; }
        }

        [DataContract]
        public sealed class CityExitDefinition
        {
            [DataMember(Order = 1)] public int X { get; set; }
            [DataMember(Order = 2)] public int Y { get; set; }
            [DataMember(Order = 3)] public char Symbol { get; set; }
            [DataMember(Order = 4)] public string TargetMapId { get; set; }
            [DataMember(Order = 5)] public int? TargetX { get; set; }
            [DataMember(Order = 6)] public int? TargetY { get; set; }
        }

        [DataContract]
        public sealed class NpcDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public string Name { get; set; }
            [DataMember(Order = 3)] public char Symbol { get; set; }
            [DataMember(Order = 4)] public string ColorRole { get; set; }
            [DataMember(Order = 5)] public bool Wanders { get; set; }
            [DataMember(Order = 6)] public string DialogueTreeId { get; set; }
        }

        [DataContract]
        public sealed class DialogueDefinition : ContentDefinition
        {
            [DataMember(Order = 2)] public List<DialogueBranchDefinition> EntryNodes { get; set; }
            [DataMember(Order = 3)] public List<DialogueNodeDefinition> Nodes { get; set; }
        }

        [DataContract]
        public sealed class DialogueNodeDefinition
        {
            [DataMember(Order = 1)] public string Id { get; set; }
            [DataMember(Order = 2)] public string Type { get; set; }
            [DataMember(Order = 3)] public string Speaker { get; set; }
            [DataMember(Order = 4)] public string Text { get; set; }
            [DataMember(Order = 5)] public List<DialogueBranchDefinition> Branches { get; set; }
        }

        [DataContract]
        public sealed class DialogueBranchDefinition
        {
            [DataMember(Order = 1)] public int Priority { get; set; }
            [DataMember(Order = 2)] public string NodeId { get; set; }
        }
    }
}
