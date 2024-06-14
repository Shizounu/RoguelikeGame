using RLNET;
using RoguelikeGame.Core;
using RoguelikeGame.Interfaces_and_Abstracts;
using RoguelikeGame.Systems;
using System.Collections.Generic;
using System.Linq;
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
        public static SubConsole _enemyHealthConsole;
        public static SubConsole _inventoryConsole;

        public static Player Player;
        public static CommandSystem CommandSystem;
        public static Dictionary<int, DungeonMap> GeneratedMaps;

        private const string fontFileName = "terminal8x8.png";
        private const string consoleTitle = "Roguesharp Roguelike";
        public void Init()
        {
            
            _rootConsole = new RLRootConsole(fontFileName, _screenWidth, _screenHeight, 8, 8, 1f, consoleTitle);
            CommandSystem = new CommandSystem();
            GeneratedMaps = new Dictionary<int, DungeonMap>();

            InitMap();
            InitStats();
            InitMessage();
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

            _rootConsole.Update += (obj, args) => InputSystem.Instance.CheckInput(_rootConsole);

            InputSystem.Instance.OnUserInput += () => _isDirty = true;
            InputSystem.Instance.OnUserInput += () => CommandSystem.EndPlayerTurn();
            InputSystem.Instance.OnUserInput += () =>
            {
                if (!CommandSystem.IsPlayerTurn) {
                    CommandSystem.ActivateMonsters();
                    _isDirty = true;
                }
            };

            InputSystem.Instance.OnUpInput += () => CommandSystem.MovePlayer(Direction.Up);
            InputSystem.Instance.OnDownInput += () => CommandSystem.MovePlayer(Direction.Down);
            InputSystem.Instance.OnLeftInput += () => CommandSystem.MovePlayer(Direction.Left);
            InputSystem.Instance.OnRightInput += () => CommandSystem.MovePlayer(Direction.Right);
            InputSystem.Instance.OnInteractInput += () =>
            {
                List<IInteractable> interactables = GetActiveMap().GetInteractablesAt(Player.X, Player.Y);
                if(interactables.Count > 0)
                {
                    interactables.First().Interact();
                }
            };


            InputSystem.Instance.OnCloseInput += () => _rootConsole.Close();
        }
        private static void InitMap()
        {
            _mapConsole = new SubConsole(80, 48);

            GetMap(1, true);

            //Update Calls
            _mapConsole.OnUpdate += (console, root) => GetActiveMap().UpdatePlayerFieldOfView();
            _mapConsole.OnUpdate += (console, root) =>
            {
                if(Player.Health <= 0)
                    _rootConsole.Close();
            };
            //Draw Calls
            _mapConsole.OnDraw += (console, root) => GetActiveMap().Draw(console.console);
            _mapConsole.OnDraw += (console, root) => Player.Draw(console.console, GetActiveMap());


            _mapConsole.OnDraw += (console, root) => console.Blit(root, 0, 11);
        }
        private static void InitMessage()
        {
            _messageConsole = new SubConsole(80, 11);
            MessageLog.Instance.Add($"The rogue arrives on level {Player.CurrentLayer}");
            MessageLog.Instance.Add($"Level created with seed '{RandomProvider.Instance.Seed}'");

            //Updates

            //Draws
            _messageConsole.OnDraw += (console, root) => MessageLog.Instance.Draw(console.console);
            _messageConsole.OnDraw += (console, root) => console.Blit(root, 0, _screenHeight - 11);
        }
        private static void InitStats()
        {
            //_statConsole = new SubConsole(20, 70);
            _statConsole = new SubConsole(20, 11);
            _enemyHealthConsole = new SubConsole(20, 11);

            //Update


            //Draw
            _statConsole.OnDraw += (console, root) => Player.DrawStats(console.console);

            
            _enemyHealthConsole.OnDraw += (console, root) => {
                int i = 0;
                foreach (var Monster in GetActiveMap().Monsters) {
                    if(GetActiveMap().IsInFov(Monster.X, Monster.Y))
                    {
                        Monster.DrawStats(console.console, i);
                        i++;
                    }
                }
            };
            

            //_statConsole.OnDraw += (console, root) => console.Blit(root, 80, 0);
            _statConsole.OnDraw += (console, root) =>  console.Blit(root, 0, 0);
            _enemyHealthConsole.OnDraw += (console, root) => console.Blit(root, 20, 0);
        }
        private static void InitInventory()
        {
            //_inventoryConsole = new SubConsole(80, 11);
            _inventoryConsole = new SubConsole(20, 70);

            //Update

            //Draw
            _inventoryConsole.OnDraw += (console, root) => _inventoryConsole.SetBackgroundColor(RLColor.Cyan);
            _inventoryConsole.OnDraw += (console, root) => console.console.Print(1, 1, "Inventory", RLColor.White);

            //_inventoryConsole.OnDraw += (console, root) => console.Blit(root, 0, 0);
            _inventoryConsole.OnDraw += (console, root) => console.Blit(root, 80, 0);
        }


        public static DungeonMap GetMap(int Layer, bool Descending) {
            if(GeneratedMaps.ContainsKey(Layer)) 
                return GeneratedMaps[Layer];

            DungeonMap map = MapGenerator.CreateMap(_mapConsole.Width, _mapConsole.Height, 20, 7, 13, Layer, Descending);
            GeneratedMaps.Add(Layer, map);
            return map;
        }
        public static DungeonMap GetActiveMap() {
            return GeneratedMaps[Player.CurrentLayer];
        }


        private void OnRootConsoleUpdate(object sender, UpdateEventArgs e)
        {
            _mapConsole?.Update(_rootConsole);
            _messageConsole?.Update(_rootConsole);
            _statConsole?.Update(_rootConsole);
            _inventoryConsole?.Update(_rootConsole);
        }
        private void OnRootConsoleRender(object sender, UpdateEventArgs e)
        {
            if (!_isDirty)
                return;

            _mapConsole?.Draw(_rootConsole);
            _messageConsole?.Draw(_rootConsole);
            _statConsole?.Draw(_rootConsole);
            _enemyHealthConsole?.Draw(_rootConsole);
            _inventoryConsole?.Draw(_rootConsole);

            _rootConsole.Draw();

            _isDirty = true;
        }
    }
}