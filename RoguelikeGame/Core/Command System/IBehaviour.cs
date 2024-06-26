using RoguelikeGame.Map.Actors;

namespace RoguelikeGame.Systems.Command
{
    public interface IBehavior
    {
        bool Act(Monster monster, CommandSystem commandSystem);
    }
}
