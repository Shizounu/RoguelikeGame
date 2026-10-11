using RLNET;
using System.Collections.Generic;

using RoguelikeGame.Color;
using RoguelikeGame.Map;
using RoguelikeGame.Systems.Input;
using RoguelikeGame.Systems.Message;
using RoguelikeGame.Systems.RandomProvider;
using RoguelikeGame.Systems.Command;
using RoguelikeGame.Systems.Command.Commands;
using RoguelikeGame.Systems.Scheduling;
using RoguelikeGame.Systems.MapManagment;
using RoguelikeGame.Systems.Content;
using RoguelikeGame.Systems.Menus;

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

        private readonly MenuSystem _menuSystem = new MenuSystem();
        private bool _hasStartedGame;
        private bool _showEnemyPanel = true;
        private bool _showInventoryPanel = true;

        private static readonly string fontFileName = System.IO.Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "assets", "visual", "Terminal8x8.png");
        private const string consoleTitle = "Roguesharp Roguelike";
        public void Init()
        {
            ContentCatalog.LoadFromDirectory(
                System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "Definitions"));

            RLSettings settings = new RLSettings
            {
                BitmapFile = fontFileName,
                Width = ConsoleDefinitions.RootConsoleWidth,
                Height = ConsoleDefinitions.RootConsoleHeight,
                CharWidth = 8,
                CharHeight = 8,
                Scale = 1f,
                Title = consoleTitle,
                WindowBorder = RLWindowBorder.Resizable,
                ResizeType = RLResizeType.ResizeScale
            };

            _rootConsole = new RLRootConsole(settings);

            InitMap();
            InitStats();
            InitMessage();
            InitInventory();

            InitMenus();
            InitInput();

            // Set up a handler for RLNET's Update event
            _rootConsole.Update += OnRootConsoleUpdate;
            // Set up a handler for RLNET's Render event
            _rootConsole.Render += OnRootConsoleRender;
            // Begin RLNET's game loop
            _rootConsole.Run();
        }




        private void InitInput()
        {

            _rootConsole.Update += (obj, args) => InputSystem.Instance.CheckInput(_rootConsole);
            _rootConsole.Update += (obj, args) => {
                bool changed = _menuSystem.IsOpen
                    ? InputSystem.Instance.CheckClickables(_rootConsole, _menuSystem.ActiveClickables)
                    : InputSystem.Instance.CheckClickables(_rootConsole);
                if (changed)
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
                List<IInteractable> interactables = MapManager.Instance.GetActiveMap().GetInteractablesInReach(Player.X, Player.Y);
                foreach (var item in interactables)
                {
                    item.Interact();
                }
            };


        }

        private void InitMenus()
        {
            MenuScreen settingsMenu = new MenuScreen("SETTINGS");

            MenuItem enemyPanelItem = null;
            enemyPanelItem = new MenuItem("Enemy health panel: On", RLKey.E, () =>
            {
                _showEnemyPanel = !_showEnemyPanel;
                enemyPanelItem.Text = "Enemy health panel: " + (_showEnemyPanel ? "On" : "Off");
            });
            settingsMenu.AddItem(enemyPanelItem);

            MenuItem inventoryPanelItem = null;
            inventoryPanelItem = new MenuItem("Inventory panel: On", RLKey.I, () =>
            {
                _showInventoryPanel = !_showInventoryPanel;
                inventoryPanelItem.Text = "Inventory panel: " + (_showInventoryPanel ? "On" : "Off");
            });
            settingsMenu.AddItem(inventoryPanelItem);
            settingsMenu.AddItem(new MenuItem("Back", RLKey.B, () => _menuSystem.Back()));

            MenuScreen mainMenu = new MenuScreen("ROGUELIKE");
            mainMenu.EscapeAction = () => _rootConsole.Close();
            MenuItem startGameItem = new MenuItem("Start Game", RLKey.N, null);
            startGameItem.Action = () =>
            {
                _hasStartedGame = true;
                startGameItem.Text = "Continue";
                _menuSystem.Close();
            };
            mainMenu.AddItem(startGameItem);
            mainMenu.AddItem(new MenuItem("Settings", RLKey.S, () => _menuSystem.OpenSubmenu(settingsMenu)));
            mainMenu.AddItem(new MenuItem("Quit", RLKey.Q, () => _rootConsole.Close()));

            MenuScreen pauseMenu = new MenuScreen("PAUSED");
            pauseMenu.EscapeAction = () => _menuSystem.Close();
            pauseMenu.AddItem(new MenuItem("Resume", RLKey.R, () => _menuSystem.Close()));
            pauseMenu.AddItem(new MenuItem("Settings", RLKey.S, () => _menuSystem.OpenSubmenu(settingsMenu)));
            pauseMenu.AddItem(new MenuItem("Main Menu", RLKey.M, () => _menuSystem.Open(mainMenu)));
            pauseMenu.AddItem(new MenuItem("Quit", RLKey.Q, () => _rootConsole.Close()));

            InputSystem.Instance.InputOverride = _menuSystem.HandleInput;
            InputSystem.Instance.OnCloseInput += () =>
            {
                if (_hasStartedGame)
                    _menuSystem.Open(pauseMenu);
            };
            _menuSystem.OnChanged += () => _isDirty = true;
            _menuSystem.Open(mainMenu);
        }
        private static void InitMap()
        {
            _mapConsole = new SubConsole(ConsoleDefinitions.MapConsoleWidth, ConsoleDefinitions.MapConsoleHeight);

            MapManager.Instance.InitializeWorld();

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
            _mapConsole.OnDraw += (console, root) => DrawPanelHeader(root,
                MapManager.Instance.GetActiveMap().DisplayName, 0, 10,
                ConsoleDefinitions.MapConsoleWidth, Palette.DbLight, Palette.SecondaryDarkest);
        }
        private static void InitMessage()
        {
            _messageConsole = new SubConsole(ConsoleDefinitions.MessageConsoleWidth, ConsoleDefinitions.MessageConsoleHeight);
            MessageLog.Instance.Add("The rogue arrives in the wilds beyond Haven.");
            MessageLog.Instance.Add("'C' leads to Haven; 'D' marks the dungeon.");
            MessageLog.Instance.Add("Move beside a marker and press Q to travel.");
            MessageLog.Instance.Add($"Level created with seed '{RandomProvider.Instance.Seed}'");

            //Updates

            //Draws
            _messageConsole.OnDraw += (console, root) => MessageLog.Instance.Draw(console.console);
            _messageConsole.OnDraw += (console, root) => DrawPanelHeader(console.console, "MESSAGE LOG", 0, 0,
                console.Width, Palette.AlternateLightest, Palette.AlternateDarkest);
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
            _statConsole.OnDraw += (console, root) => DrawPanelHeader(console.console, "PLAYER", 0, 0,
                console.Width, Palette.DbLight, Palette.PrimaryDarker);
            _enemyHealthConsole.OnDraw += (console, root) => {
                int i = 0;
                foreach (var Monster in MapManager.Instance.GetActiveMap().Monsters) {
                    if(MapManager.Instance.GetActiveMap().IsInFov(Monster.X, Monster.Y))
                    {
                        if (i >= console.Height / 2)
                            break;

                        Monster.DrawStats(console.console, i);
                        i++;
                    }
                }
            };
            _enemyHealthConsole.OnDraw += (console, root) => DrawPanelHeader(console.console, "ENEMIES", 0, 0,
                console.Width, Palette.DbLight, Palette.DbOldBlood);
            
            _statConsole.OnDraw += (console, root) => console.Blit(root, 0, 0);
            _enemyHealthConsole.OnDraw += (console, root) => console.Blit(root, 20, 0);
        }
        private static void InitInventory()
        {
            _inventoryConsole = new SubConsole(ConsoleDefinitions.InventoryConsoleWidth, ConsoleDefinitions.InventoryConsoleHeight);

            //Update
            
            //Draw
            _inventoryConsole.OnDraw += (console, root) => Player.DrawInventory(console.console, 80, 0);
            _inventoryConsole.OnDraw += (console, root) => DrawPanelHeader(console.console, "INVENTORY", 0, 0,
                console.Width, Palette.DbLight, Palette.DbDeepWater);
            _inventoryConsole.OnDraw += (console, root) => console.Blit(root, 80, 0);
        }

        private static void DrawPanelHeader(RLConsole console, string title, int x, int y, int width,
            RLColor foreground, RLColor background)
        {
            console.SetBackColor(x, y, width, 1, background);
            console.Print(x + 1, y, title, foreground, background);
        }

        private void OnRootConsoleUpdate(object sender, UpdateEventArgs e)
        {
            if (_menuSystem.IsOpen)
                return;

            _mapConsole?.Update(_rootConsole);
            _messageConsole?.Update(_rootConsole);
            _statConsole?.Update(_rootConsole);
            _inventoryConsole?.Update(_rootConsole);
        }
        private void OnRootConsoleRender(object sender, UpdateEventArgs e)
        {
            if (!_isDirty)
                return;

            _rootConsole.Clear();
            _mapConsole?.Draw(_rootConsole);
            _messageConsole?.Draw(_rootConsole);
            _statConsole?.Draw(_rootConsole);
            if (_showInventoryPanel)
                _inventoryConsole?.Draw(_rootConsole);

            if (_showEnemyPanel)
                _enemyHealthConsole?.Draw(_rootConsole);

            _menuSystem.Draw(_rootConsole, ConsoleDefinitions.RootConsoleWidth, ConsoleDefinitions.RootConsoleHeight);
            _rootConsole.Draw();

            _isDirty = false;
        }
    }
}