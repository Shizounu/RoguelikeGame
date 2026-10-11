using RLNET;
using RogueSharp;
using RogueSharp.DiceNotation;

using RoguelikeGame.Map;
using RoguelikeGame.Color;
using RoguelikeGame.Systems.Message;
using RoguelikeGame.Systems.MapManagment;
using RoguelikeGame.Systems.Content;

namespace RoguelikeGame.Systems.Inventory.ItemDefinition
{
    public class HealingPotion : IItem, IInteractable, IConsumable
    {
        private readonly ContentCatalog.ItemDefinition _definition;

        public HealingPotion()
        {
            _definition = ContentCatalog.Current.GetItem("Items.Consumable.HealthPotion");
        }

        public HealingPotion(ContentCatalog.ItemDefinition definition)
        {
            _definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
        }

        public string ContentId => _definition.Id;
        public string Name => _definition.Name;
        public float Weight => _definition.Weight;
        public ItemType Type => (ItemType)System.Enum.Parse(typeof(ItemType), _definition.Type, true);
        public RLColor Color => ContentCatalog.Current.GetColor(_definition.ColorRole);

        public char Symbol => _definition.Symbol;

        public int X { get; set; }
        public int Y { get; set; }


        public void Draw(RLConsole console, IMap map)
        {
            if (!map.GetCell(X, Y).IsExplored)
                return;

            DungeonMap dungeonMap = map as DungeonMap;
            RLColor color = dungeonMap == null ? Color : dungeonMap.ApplyPalette(Color);
            if (map.IsInFov(X, Y))
                console.Set(X, Y, color,
                    dungeonMap == null ? Colors.FloorBackgroundFov : dungeonMap.ApplyPalette(Colors.FloorBackgroundFov),
                    Symbol);
            else // When not in field-of-view just draw a normal floor
                console.Set(X, Y, dungeonMap == null ? Colors.Floor : dungeonMap.ApplyPalette(Colors.Floor),
                    dungeonMap == null ? Colors.FloorBackground : dungeonMap.ApplyPalette(Colors.FloorBackground), '.');
        }

        public void Interact()
        {
            Game.Player.AddItem(this);
            MapManager.Instance.GetActiveMap().interactables.Remove(this);
        }

        public void Use()
        {
            int amount = Dice.Roll(_definition.EffectDice);
            MessageLog.Instance.Add($"{Game.Player.Name} drank {Name} and healed {amount}");
            Game.Player.Heal(amount);
            Game.Player.RemoveItem(this);
        }
    }
}
