using RLNET;
using System.Collections.Generic;

using RoguelikeGame.Map;
using RoguelikeGame.Systems.Input;
using RoguelikeGame.Systems.Message;
using RoguelikeGame.Systems.RandomProvider;
using RoguelikeGame.Systems.Command;
using RoguelikeGame.Systems.Command.Commands;
using RoguelikeGame.Systems.Scheduling;
using RoguelikeGame.Systems.MapManagment;

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
        private static RLRootConsole _rootConsole;

        public static SubConsole _mapConsole;
        public static SubConsole _messageConsole;
        public static SubConsole _statConsole;
        public static SubConsole _enemyHealthConsole;
        public static SubConsole _inventoryConsole;

        public static Map.Actors.Player Player;

        private const string fontFileName = "terminal8x8.png";
        private const string consoleTitle = "Roguesharp Roguelike";
        public void Init()
        {
            
            _rootConsole = new RLRootConsole(fontFileName, ConsoleDefinitions.RootConsoleWidth, ConsoleDefinitions.RootConsoleHeight, 8, 8, 1f, consoleTitle);

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
            _rootConsole.Update += (obj, args) => {
                if (InputSystem.Instance.CheckClickables(_rootConsole))
                    _isDirty = true; 
                };

            InputSystem.Instance.OnUserInput += () => _isDirty = true;
            InputSystem.Instance.OnUserInput += () => Player.IsPlayerTurn = false;
            InputSystem.Instance.OnUserInput += () =>
            {
                if (!Player.IsPlayerTurn) {
                    MapManager.Instance.GetActiveMap().SchedulingSystem.Get().OnSchedule();
                    _isDirty = true;
                }
            };

            InputSystem.Instance.OnUpInput += () => CommandSystem.Instance.EnqueueCommand(new MovePlayer(Direction.Up)) ;
            InputSystem.Instance.OnDownInput += () => CommandSystem.Instance.EnqueueCommand(new MovePlayer(Direction.Down));
            InputSystem.Instance.OnLeftInput += () => CommandSystem.Instance.EnqueueCommand(new MovePlayer(Direction.Left));
            InputSystem.Instance.OnRightInput += () => CommandSystem.Instance.EnqueueCommand(new MovePlayer(Direction.Right));
            InputSystem.Instance.OnInteractInput += () =>
            {
                List<IInteractable> interactables = MapManager.Instance.GetActiveMap().GetInteractablesAt(Player.X, Player.Y);
                foreach (var item in interactables)
                {
                    item.Interact();
                }
            };


            InputSystem.Instance.OnCloseInput += () => _rootConsole.Close();
        }
        private static void InitMap()
        {
            _mapConsole = new SubConsole(ConsoleDefinitions.MapConsoleWidth, ConsoleDefinitions.MapConsoleHeight);

            MapManager.Instance.GetMap(1, true);

            //Update Calls
            _mapConsole.OnUpdate += (console, root) => MapManager.Instance.GetActiveMap().UpdatePlayerFieldOfView();
            /*_mapConsole.OnUpdate += (console, root) =>
            {
                if(Player.Health <= 0)
                    _rootConsole.Close();
            };*/
            _mapConsole.OnUpdate += (console, root) => CommandSystem.Instance.DoAllCommands();
            //Draw Calls
            _mapConsole.OnDraw += (console, root) => MapManager.Instance.GetActiveMap().Draw(console.console);
            _mapConsole.OnDraw += (console, root) => Player.Draw(console.console, MapManager.Instance.GetActiveMap());


            _mapConsole.OnDraw += (console, root) => console.Blit(root, 0, 11);
        }
        private static void InitMessage()
        {
            _messageConsole = new SubConsole(ConsoleDefinitions.MessageConsoleWidth, ConsoleDefinitions.MessageConsoleHeight);
            MessageLog.Instance.Add($"The rogue arrives on level {Player.CurrentLayer}");
            MessageLog.Instance.Add($"Level created with seed '{RandomProvider.Instance.Seed}'");

            //Updates

            //Draws
            _messageConsole.OnDraw += (console, root) => MessageLog.Instance.Draw(console.console);
            _messageConsole.OnDraw += (console, root) => console.Blit(root, 0, ConsoleDefinitions.RootConsoleHeight - ConsoleDefinitions.MessageConsoleHeight);
        }
        private static void InitStats()
        {
            //_statConsole = new SubConsole(20, 70);
            _statConsole = new SubConsole(ConsoleDefinitions.StatConsoleWidth, ConsoleDefinitions.StatConsoleHeight);
            _enemyHealthConsole = new SubConsole(ConsoleDefinitions.StatConsoleWidth, ConsoleDefinitions.StatConsoleHeight);

            //Update


            //Draw
            _statConsole.OnDraw += (console, root) => Player.DrawStats(console.console);
            _enemyHealthConsole.OnDraw += (console, root) => {
                int i = 0;
                foreach (var Monster in MapManager.Instance.GetActiveMap().Monsters) {
                    if(MapManager.Instance.GetActiveMap().IsInFov(Monster.X, Monster.Y))
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
            _inventoryConsole = new SubConsole(ConsoleDefinitions.InventoryConsoleWidth, ConsoleDefinitions.InventoryConsoleHeight);

            //Update
            
            //Draw
            _inventoryConsole.OnDraw += (console, root) => Player.DrawInventory(console.console, 80, 0);
            _inventoryConsole.OnDraw += (console, root) => console.Blit(root, 80, 0);
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

            _isDirty = false;
        }
    }
}