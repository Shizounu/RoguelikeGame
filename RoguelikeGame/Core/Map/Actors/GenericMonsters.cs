using RoguelikeGame.Map.Actors;
using RoguelikeGame.Systems.Content;
using System.Collections.Generic;

namespace RoguelikeGame.Core.Map.Actors
{
    public static class GenericMonsters
    {
        public static Monster CreateKobold(int level)
        {
            return ContentCatalog.Current.CreateEnemy("Enemies.Kobold", level);
        }
    }
}
