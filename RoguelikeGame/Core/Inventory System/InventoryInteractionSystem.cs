using RLNET;
using RoguelikeGame.Core;
using RoguelikeGame.Core.Inventory_System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame
{
    public class InventoryDrawingSystem {
        
        private List<InventoryDisplayItem> InventoryDisplayItems;
        public void AddItemDisplay(IItem item)
        {

        }

        public static void DrawInventory(Dictionary<string, ItemCountPair> Items, RLConsole InventoryConsole)
        {
            if (Items == null)
                return;

            int i = 0;
            foreach (var item in Items)
            {
                InventoryConsole.Print(1, 1 + (2 * i), $"{item.Key} [{item.Value.count}]", Colors.Text);
                i++;
            }
        }

        
    }

    public class InventoryDisplayItem {
        public int StartX; 
        public int StartY;

        public int EndX;
        public int EndY;

        public RLColor CurrentBackroundColor;

        public RLColor BaseBackgroundColor = RLColor.Black;
        public RLColor HoveredBackroundColor = RLColor.LightGray;

        public bool IsInBox(int x, int y) {
            return x >= StartX && x <= EndX && y >= StartY && y <= EndY;
        }
    }
}
