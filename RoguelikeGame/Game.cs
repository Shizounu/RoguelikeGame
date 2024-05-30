using RLNET;
using RoguelikeGame.Core;
using System.Runtime.InteropServices.ComTypes;

namespace RogueSharpV3Tutorial
{
    public class Program
    {
        static void Main()
        {
            Game game = new Game();
            game.Init();
        }
    }



    public class Game
    {

        // The screen height and width are in number of tiles
        private static readonly int _screenWidth = 100;
        private static readonly int _screenHeight = 70;
        private static RLRootConsole _rootConsole;

        public static SubConsole _mapConsole;
        public static SubConsole _messageConsole;
        public static SubConsole _statConsole;
        public static SubConsole _inventoryConsole;

        public static DungeonMap DungeonMap { get; private set; }

        public void Init()
        {
            // This must be the exact name of the bitmap font file we are using or it will error.
            string fontFileName = "terminal8x8.png";
            // The title will appear at the top of the console window
            string consoleTitle = "RougeSharp V3 Tutorial - Level 1";

            // Tell RLNet to use the bitmap font that we specified and that each tile is 8 x 8 pixels
            _rootConsole = new RLRootConsole(fontFileName, _screenWidth, _screenHeight, 8, 8, 1f, consoleTitle);

            InitMap();

            InitMessage();

            InitStats();

            InitInventory();

            MapGenerator mapGenerator = new MapGenerator(_mapConsole.Width, _mapConsole.Height);
            DungeonMap = mapGenerator.CreateMap();

            // Set up a handler for RLNET's Update event
            _rootConsole.Update += OnRootConsoleUpdate;
            // Set up a handler for RLNET's Render event
            _rootConsole.Render += OnRootConsoleRender;
            // Begin RLNET's game loop
            _rootConsole.Run();
        }

        private static void InitMap()
        {
            _mapConsole = new SubConsole(80, 48);

            //Draw Calls
            _mapConsole.OnDraw += (console, root) => console.SetBackgroundColor(RLColor.Black);
            _mapConsole.OnDraw += (console, root) => DungeonMap.Draw(console.console);
            _mapConsole.OnDraw += (console, root) => console.Blit(root, 0, 11);
        }
        private static void InitMessage()
        {
            _messageConsole = new SubConsole(80, 11);

            //Updates
            _messageConsole.OnUpdate += (console, root) => console.SetBackgroundColor(RLColor.Gray);
            _messageConsole.OnUpdate += (console, root) => console.console.Print(1, 1, "Messages", RLColor.White);
            
            //Draws
            _messageConsole.OnDraw += (console, root) => console.Blit(root, 0, _screenHeight - 11);
        }
        private static void InitStats()
        {
            _statConsole = new SubConsole(20, 70);

            //Update
            _statConsole.OnUpdate += (console, root) => console.SetBackgroundColor(RLColor.Brown);
            _statConsole.OnUpdate += (console, root) => console.console.Print(1, 1, "Stats", RLColor.White);

            //Draw
            _statConsole.OnDraw += (console, root) => console.Blit(root, 80, 0);
        }
        private static void InitInventory()
        {
            _inventoryConsole = new SubConsole(80, 11);

            //Update
            _inventoryConsole.OnUpdate += (console, root) => _inventoryConsole.SetBackgroundColor(RLColor.Cyan);
            _inventoryConsole.OnUpdate += (console, root) => console.console.Print(1, 1, "Inventory", RLColor.White);

            //Draw
            _inventoryConsole.OnDraw += (console, root) => console.Blit(root, 0, 0);
        }

        // Event handler for RLNET's Update event
        private void OnRootConsoleUpdate(object sender, UpdateEventArgs e)
        {
            _mapConsole.Update(_rootConsole);
            _messageConsole.Update(_rootConsole);
            _statConsole.Update(_rootConsole);
            _inventoryConsole.Update(_rootConsole);
        }

        // Event handler for RLNET's Render event
        private void OnRootConsoleRender(object sender, UpdateEventArgs e)
        {
            // Blit the sub consoles to the root console in the correct locations
            _mapConsole.Draw(_rootConsole);
            _messageConsole.Draw(_rootConsole);
            _statConsole.Draw(_rootConsole);
            _inventoryConsole.Draw(_rootConsole);

            // Tell RLNET to draw the console that we set
            _rootConsole.Draw();
        }
    }
}