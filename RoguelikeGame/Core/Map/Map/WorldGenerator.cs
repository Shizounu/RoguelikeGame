using RLNET;
using System;
using RoguelikeGame.Color;
using RoguelikeGame.Map.Actors;
using RoguelikeGame.Systems.Content;
using RoguelikeGame.Systems.Message;

namespace RoguelikeGame.Map
{
    public static class WorldGenerator
    {
        public const string OverworldMapId = "overworld";
        public const string CityMapId = "city:haven";

        public const int OverworldArrivalX = 8;
        public const int OverworldArrivalY = 24;
        public const int CityEntranceX = 55;
        public const int CityEntranceY = 24;
        public const int DungeonEntranceX = 15;
        public const int DungeonEntranceY = 12;
        public const int CityArrivalX = 5;
        public const int CityArrivalY = 24;

        public static DungeonMap CreateOverworld(int width, int height)
        {
            DungeonMap map = CreateGrassMap(width, height, "OVERWORLD");

            for (int x = 1; x < width - 1; x++)
                SetTile(map, x, OverworldArrivalY, '.', Palette.DbBrightWood, true);

            for (int y = DungeonEntranceY; y <= OverworldArrivalY; y++)
                SetTile(map, DungeonEntranceX, y, '.', Palette.DbBrightWood, true);

            SetTile(map, CityEntranceX, CityEntranceY, '.', Palette.DbBrightWood, true);
            SetTile(map, DungeonEntranceX, DungeonEntranceY, 'D', Palette.DbLight, true);
            SetTile(map, CityEntranceX, CityEntranceY, 'C', Palette.DbLight, true);

            for (int y = 2; y < height - 2; y++)
                SetTile(map, 35, y, '~', Palette.DbWater, false, false);
            for (int y = OverworldArrivalY - 1; y <= OverworldArrivalY + 1; y++)
                SetTile(map, 35, y, '=', Palette.DbBrightWood, true);

            for (int y = 3; y < height - 3; y += 5)
            {
                SetTile(map, 5, y, 'T', Palette.DbVegetation, false, false);
                SetTile(map, 25, y, 'T', Palette.DbVegetation, false, false);
                SetTile(map, 45, y, 'T', Palette.DbVegetation, false, false);
                SetTile(map, 70, y, 'T', Palette.DbVegetation, false, false);
            }

            map.AddInteractable(new RoguelikeGame.Map.Object.Stairs
            {
                X = CityEntranceX,
                Y = CityEntranceY,
                Symbol = 'C',
                TargetMapId = CityMapId,
                TargetX = CityArrivalX,
                TargetY = CityArrivalY
            });
            map.AddInteractable(new RoguelikeGame.Map.Object.Stairs
            {
                X = DungeonEntranceX,
                Y = DungeonEntranceY,
                Symbol = 'D',
                TargetMapId = Systems.MapManagment.MapManager.GetDungeonMapId(1)
            });

            return map;
        }

        public static DungeonMap CreateCity(ContentCatalog.CityDefinition definition, ContentCatalog catalog)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            DungeonMap map = new DungeonMap
            {
                MapId = definition.MapId,
                DisplayName = definition.DisplayName,
                LightingEnabled = definition.LightingEnabled,
                PaletteName = definition.PaletteName
            };
            map.Initialize(definition.Width, definition.Height);
            ApplyCityTile(map, catalog, definition.Terrain.Interior, 0, 0, definition.Width, definition.Height);

            for (int x = 0; x < definition.Width; x++)
            {
                SetCityTile(map, catalog, definition.Terrain.Border, x, 0);
                SetCityTile(map, catalog, definition.Terrain.Border, x, definition.Height - 1);
            }
            for (int y = 1; y < definition.Height - 1; y++)
            {
                SetCityTile(map, catalog, definition.Terrain.Border, 0, y);
                SetCityTile(map, catalog, definition.Terrain.Border, definition.Width - 1, y);
            }

            foreach (ContentCatalog.CityFeatureDefinition feature in definition.Features)
                ApplyCityTile(map, catalog, feature, feature.X, feature.Y, feature.Width, feature.Height);

            foreach (ContentCatalog.CityNpcPlacementDefinition placement in definition.Npcs)
            {
                ContentCatalog.NpcDefinition npcDefinition = catalog.GetNpc(placement.NpcId);
                NonPlayerCharacter npc = new NonPlayerCharacter
                {
                    Name = npcDefinition.Name,
                    X = placement.X,
                    Y = placement.Y,
                    Symbol = npcDefinition.Symbol,
                    Color = catalog.GetColor(npcDefinition.ColorRole),
                    Wanders = npcDefinition.Wanders,
                    Interaction = actor => MessageLog.Instance.Add(
                        catalog.GetOpeningLine(npcDefinition.DialogueTreeId))
                };
                map.AddNonPlayerCharacter(npc);
            }

            foreach (ContentCatalog.CityExitDefinition exit in definition.Exits)
            {
                map.AddInteractable(new RoguelikeGame.Map.Object.Stairs
                {
                    X = exit.X,
                    Y = exit.Y,
                    Symbol = exit.Symbol,
                    TargetMapId = exit.TargetMapId,
                    TargetX = exit.TargetX,
                    TargetY = exit.TargetY
                });
            }

            return map;
        }

        private static DungeonMap CreateGrassMap(int width, int height, string name)
        {
            DungeonMap map = new DungeonMap { DisplayName = name };
            map.Initialize(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool insideBorder = x > 0 && y > 0 && x < width - 1 && y < height - 1;
                    map.SetTerrainTile(x, y, insideBorder ? '.' : '#',
                        insideBorder ? Palette.DbGrass : Palette.DbOldStone,
                        insideBorder ? Palette.PrimaryDarkest : Palette.SecondaryDarkest,
                        insideBorder, insideBorder);
                }
            }

            return map;
        }

        private static void ApplyCityTile(DungeonMap map, ContentCatalog catalog,
            ContentCatalog.CityTileDefinition tile, int x, int y, int width, int height)
        {
            for (int tileX = x; tileX < x + width; tileX++)
            {
                for (int tileY = y; tileY < y + height; tileY++)
                    SetCityTile(map, catalog, tile, tileX, tileY);
            }
        }

        private static void SetCityTile(DungeonMap map, ContentCatalog catalog,
            ContentCatalog.CityTileDefinition tile, int x, int y)
        {
            map.SetTerrainTile(x, y, tile.Symbol, catalog.GetColor(tile.ColorRole),
                catalog.GetColor(tile.BackgroundColorRole), tile.Walkable, tile.Transparent);
        }

        private static void SetTile(DungeonMap map, int x, int y, char symbol, RLColor color, bool walkable,
            bool transparent = true)
        {
            map.SetTerrainTile(x, y, symbol, color, Palette.PrimaryDarkest, walkable, transparent);
        }
    }
}
