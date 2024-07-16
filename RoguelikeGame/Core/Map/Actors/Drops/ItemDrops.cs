namespace RoguelikeGame.Map.Actors.Drops
{
    public abstract class ItemDrop
    {
        public abstract void DoDrop(DungeonMap map, Monster monster);
    }
}
