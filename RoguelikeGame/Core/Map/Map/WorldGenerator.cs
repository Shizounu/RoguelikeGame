using RLNET;
using RoguelikeGame.Color;
using RoguelikeGame.Map.Actors;

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

        public static DungeonMap CreateCity(int width, int height)
        {
            DungeonMap map = CreateGrassMap(width, height, "HAVEN");

            for (int x = 3; x < width - 3; x++)
                SetTile(map, x, CityArrivalY, '.', Palette.DbBrightWood, true);
            for (int y = 3; y < height - 3; y++)
                SetTile(map, width / 2, y, '.', Palette.DbBrightWood, true);

            PlaceBuilding(map, 10, 9, 18, 15, 'I');
            PlaceBuilding(map, 27, 8, 35, 14, 'M');
            PlaceBuilding(map, 46, 9, 54, 15, 'H');
            PlaceBuilding(map, 13, 30, 21, 36, 'H');
            PlaceBuilding(map, 43, 30, 52, 37, 'B');

            AddNpc(map, "Mara the baker", 24, 24);
            AddNpc(map, "Tomas the guard", 39, 23);
            AddNpc(map, "Elin the traveler", 58, 28);
            AddNpc(map, "Perrin the merchant", 32, 27);

            map.AddInteractable(new RoguelikeGame.Map.Object.Stairs
            {
                X = CityArrivalX,
                Y = CityArrivalY,
                Symbol = '<',
                TargetMapId = OverworldMapId,
                TargetX = CityEntranceX,
                TargetY = CityEntranceY
            });

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

        private static void PlaceBuilding(DungeonMap map, int left, int top, int right, int bottom, char symbol)
        {
            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                    SetTile(map, x, y, symbol, Palette.DbWood, false, false);
            }
        }

        private static void AddNpc(DungeonMap map, string name, int x, int y)
        {
            map.AddNonPlayerCharacter(new NonPlayerCharacter
            {
                Name = name,
                X = x,
                Y = y,
                Color = Palette.DbSkin,
                Symbol = 'n'
            });
        }

        private static void SetTile(DungeonMap map, int x, int y, char symbol, RLColor color, bool walkable,
            bool transparent = true)
        {
            map.SetTerrainTile(x, y, symbol, color, Palette.PrimaryDarkest, walkable, transparent);
        }
    }
}
