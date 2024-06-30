using RoguelikeGame.Map;
using RoguelikeGame.Map.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.MapManagment 
{
    public class MapManager : Singleton<MapManager> {

        public Dictionary<int, DungeonMap> GeneratedMaps = new Dictionary<int, DungeonMap>();
        private int CurrentLayer => Game.Player.CurrentLayer;

        public DungeonMap GetMap(int Layer, bool Descending)
        {
            if (GeneratedMaps.ContainsKey(Layer))
                return GeneratedMaps[Layer];

            DungeonMap map = MapGenerator.CreateMap(ConsoleDefinitions.MapConsoleWidth, ConsoleDefinitions.MapConsoleHeight, 20, 7, 13, Layer, Descending);
            GeneratedMaps.Add(Layer, map);
            return map;
        }
        public DungeonMap GetActiveMap()
        {
            return GeneratedMaps[CurrentLayer];
        }
    }
}
