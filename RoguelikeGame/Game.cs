using RLNET;
using RoguelikeGame.Core;
using RoguelikeGame.Systems;
using System.Runtime.InteropServices.ComTypes;

namespace RoguelikeGame
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
        public static bool _isDirty = true; 


        // The screen height and width are in number of tiles
        private static readonly int _screenWidth = 100;
        private static readonly int _screenHeight = 70;
        private static RLRootConsole _rootConsole;

        public static SubConsole _mapConsole;
        public static SubConsole _messageConsole;
        public static SubConsole _statConsole;
        public static SubConsole _inventoryConsole;

        public static Player Player;
        public static InputSystem _inputSystem;
        public static CommandSystem CommandSystem;
        public static DungeonMap DungeonMap;
        public static MessageLog MessageLog;
        public static SchedulingSystem SchedulingSystem;

        private static int _mapLevel = 1;

        private const string fontFileName = "terminal8x8.png";
        private const string consoleTitle = "Roguesharp Roguelike";
        public void Init()
        {
            
            _rootConsole = new RLRootConsole(fontFileName, _screenWidth, _screenHeight, 8, 8, 1f, consoleTitle);
            CommandSystem = new CommandSystem();
            SchedulingSystem = new SchedulingSystem();

            InitMap();
            InitMessage();
            InitStats();
            InitInventory();

            InitInput();

            // Set up a handler for RLNET's Update event
            _rootConsole.Update += OnRootConsoleUpdate;
            // Set up a handler for RLNET's Render event
            _rootConsole.Render += OnRootConsoleRender;
            // Begin RLNET's game loop
            _rootConsole.Run();
        }




        private static void InitInput()
        {
            _inputSystem = new InputSystem();

            _rootConsole.Update += (obj, args) => _inputSystem.CheckInput(_rootConsole);

            _inputSystem.OnUserInput += () => _isDirty = true;
            _inputSystem.OnUserInput += () => CommandSystem.EndPlayerTurn();
            _inputSystem.OnUserInput += () =>
            {
                if (!CommandSystem.IsPlayerTurn) {
                    CommandSystem.ActivateMonsters();
                    _isDirty = true;
                }
            };

            _inputSystem.OnUpInput += () => CommandSystem.MovePlayer(Direction.Up);
            _inputSystem.OnDownInput += () => CommandSystem.MovePlayer(Direction.Down);
            _inputSystem.OnLeftInput += () => CommandSystem.MovePlayer(Direction.Left);
            _inputSystem.OnRightInput += () => CommandSystem.MovePlayer(Direction.Right);
            _inputSystem.OnInteractInput += () =>
            {
                if (DungeonMap.CanMoveDownToNextLevel())
                {
                    MapGenerator mapGenerator = new MapGenerator(_mapConsole.Width, _mapConsole.Height, 20, 13, 7, ++_mapLevel);
                    DungeonMap = mapGenerator.CreateMap();
                    MessageLog = new MessageLog();
                    CommandSystem = new CommandSystem();
                    //_rootConsole.Title = $"RougeSharp RLNet Tutorial - Level {_mapLevel}";
                }
            };


            _inputSystem.OnCloseInput += () => _rootConsole.Close();
        }
        private static void InitMap()
        {
            _mapConsole = new SubConsole(80, 48);

            MapGenerator mapGenerator = new MapGenerator(_mapConsole.Width, _mapConsole.Height, 20, 13, 7, _mapLevel);
            DungeonMap = mapGenerator.CreateMap();

            //Update Calls
            _mapConsole.OnUpdate += (console, root) => DungeonMap.UpdatePlayerFieldOfView();

            //Draw Calls
            _mapConsole.OnDraw += (console, root) => DungeonMap.Draw(console.console);
            _mapConsole.OnDraw += (console, root) => Player.Draw(console.console, DungeonMap);


            _mapConsole.OnDraw += (console, root) => console.Blit(root, 0, 11);
        }
        private static void InitMessage()
        {
            _messageConsole = new SubConsole(80, 11);
            MessageLog = new MessageLog();
            MessageLog.Add("The rogue arrives on level 1");
            MessageLog.Add($"Level created with seed '{RandomProvider.Instance.Seed}'");

            //Updates

            //Draws
            _messageConsole.OnDraw += (console, root) => MessageLog.Draw(console.console);
            _messageConsole.OnDraw += (console, root) => console.Blit(root, 0, _screenHeight - 11);
        }
        private static void InitStats()
        {
            _statConsole = new SubConsole(20, 70);

            //Update
            

            //Draw
            _statConsole.OnDraw += (console, root) => Player.DrawStats(console.console);

            _statConsole.OnDraw += (console, root) => {
                int i = 0;
                foreach (var Monster in DungeonMap.Monsters) {
                    if(DungeonMap.IsInFov(Monster.X, Monster.Y))
                    {
                        Monster.DrawStats(console.console, i);
                        i++;
                    }
                }
            };

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



        private void OnRootConsoleUpdate(object sender, UpdateEventArgs e)
        {
            _mapConsole.Update(_rootConsole);
            _messageConsole.Update(_rootConsole);
            _statConsole.Update(_rootConsole);
            _inventoryConsole.Update(_rootConsole);
        }
        private void OnRootConsoleRender(object sender, UpdateEventArgs e)
        {
            if (!_isDirty)
                return;

            // Blit the sub consoles to the root console in the correct locations
            _mapConsole.Draw(_rootConsole);
            _messageConsole.Draw(_rootConsole);
            _statConsole.Draw(_rootConsole);
            _inventoryConsole.Draw(_rootConsole);

            // Tell RLNET to draw the console that we set
            _rootConsole.Draw();

            _isDirty = true;
        }
    }
}