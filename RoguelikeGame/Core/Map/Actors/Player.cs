using RLNET;
using RoguelikeGame.Core.Inventory_System;
using RoguelikeGame.Core.MouseSelection;
using RoguelikeGame.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Core
{
    public class Player : Actor
    {
        public Player()
        {
            Attack = 2;
            AttackChance = 50;
            Awareness = 15;
            Color = Colors.Player;
            Defense = 2;
            DefenseChance = 40;
            Gold = 0;
            Health = 100;
            MaxHealth = 100;
            Name = "Rogue";
            Speed = 10;
            Symbol = '@';
        }

        public int CurrentLayer = 1;

        public void Heal(int amount)
        {
            Health += amount;
            if(Health > MaxHealth)
                Health = MaxHealth;
        }

        #region Gold Managment
        public void AddGold(int Amount) {
            Gold += Amount;
        }
        public bool CanPay(int Amount) {
            return Gold >= Amount;
        }
        public void RemoveGold(int Amount) {
            Gold -= Amount; 
        }
        public bool RemoveGoldIfAble(int Amount) {
            if(CanPay(Amount)) { 
                RemoveGold(Amount);
                return true;
            }
            return false;   
        }
        #endregion

        #region Inventory
        private Dictionary<string, ItemCountPair> Items; 

        public void AddItem(IItem item) {
            if(Items == null) 
                Items = new Dictionary<string, ItemCountPair>();
            
            if(!Items.ContainsKey(item.Name))
                Items.Add(item.Name, new ItemCountPair(item, 0));
            Items[item.Name].count += 1;
            InputSystem.Instance.AddClickable(Items[item.Name]);
        }
        public bool RemoveItem(IItem item) {
            if(Items == null)
                return false;

            if(!Items.ContainsKey(item.Name))
                return false;

            if(Items[item.Name].count == 1) {
                InputSystem.Instance.RemoveClickable(Items[item.Name]);
                Items.Remove(item.Name);
                return true;
            }

            Items[item.Name].count -= 1;
            return true;
        }
        #endregion

        public void DrawStats(RLConsole statConsole)
        {
            statConsole.Print(1, 1, $"Name:    {Name}", Colors.Text);
            statConsole.Print(1, 3, $"Health:  {Health}/{MaxHealth}", Colors.Text);
            statConsole.Print(1, 5, $"Attack:  {Attack} ({AttackChance}%)", Colors.Text);
            statConsole.Print(1, 7, $"Defense: {Defense} ({DefenseChance}%)", Colors.Text);
            statConsole.Print(1, 9, $"Gold:    {Gold}", Colors.Gold);
        }

        public void DrawInventory(RLConsole inventoryConsole, int consoleXPos, int consoleYPos) {
            if(Items == null || Items.Count == 0) return;
            int i = 0;             
            foreach (var item in Items) {
                item.Value.X = consoleXPos + 1; 
                item.Value.Y = consoleYPos + 1 + (2 * i);

                if(!item.Value.IsHovered)
                    inventoryConsole.Print(1, 1 + (2 * i), $"{item.Key} [{item.Value.count}]", Colors.Text);
                else
                    inventoryConsole.Print(1, 1 + (2 * i), $"{item.Key} [{item.Value.count}]", Colors.Text, RLColor.LightGray);
                i += 1; 
            }
        }
    }

    public class ItemCountPair : IClickable {
        public ItemCountPair(IItem _item, int _count)
        {
            item = _item;
            count = _count;
        }
        public IItem item;
        public int count;

        public int X { get; set; }

        public int Y { get; set; }

        public int Width => 18;

        public int Height => 1;

        public bool IsHovered { get; set; }
        public bool WasClickedThisFrame { get; set; }

        public void OnClick() {
            MessageLog.Instance.Add($"{Game.Player.Name} used {item.Name}");
            item.Use();
        }
    }
}
