using System;
using System.Collections.Generic;
using RLNET;
using RoguelikeGame.Color;
using RoguelikeGame.Systems.Input;

namespace RoguelikeGame.Systems.Menus
{
    public class MenuItem : IClickable
    {
        public MenuItem(string text, RLKey button, Action action)
        {
            Text = text;
            Button = button;
            Action = action;
        }

        public string Text { get; set; }
        public RLKey Button { get; private set; }
        public Action Action { get; set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsHovered { get; set; }
        public bool WasClickedThisFrame { get; set; }

        public void Activate()
        {
            Action?.Invoke();
        }

        public void OnClick()
        {
            Activate();
        }

        public void SetBounds(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public string GetDisplayText()
        {
            return "[" + Button + "] " + Text;
        }
    }

    public class MenuScreen
    {
        private readonly List<MenuItem> _items = new List<MenuItem>();
        private int _selectedIndex = -1;

        public MenuScreen(string title)
        {
            Title = title;
        }

        public string Title { get; private set; }
        public Action EscapeAction { get; set; }
        public IList<MenuItem> Items => _items;

        public void AddItem(MenuItem item)
        {
            _items.Add(item);
        }

        public void MoveSelection(int direction)
        {
            if (_items.Count == 0)
                return;

            _selectedIndex = _selectedIndex < 0
                ? (direction < 0 ? _items.Count - 1 : 0)
                : (_selectedIndex + direction + _items.Count) % _items.Count;
        }

        public void ActivateSelected()
        {
            if (_items.Count > 0)
                _items[_selectedIndex < 0 ? 0 : _selectedIndex].Activate();
        }

        public void Draw(RLConsole console, int width, int height)
        {
            console.SetBackColor(0, 0, width, height, Palette.DbDark);

            int panelWidth = Math.Min(52, width - 4);
            int panelHeight = Math.Min(height - 8, _items.Count * 2 + 9);
            int panelX = (width - panelWidth) / 2;
            int panelY = (height - panelHeight) / 2;
            console.SetBackColor(panelX, panelY, panelWidth, panelHeight, Palette.PrimaryDarkest);

            int titleX = panelX + Math.Max(2, (panelWidth - Title.Length) / 2);
            console.Print(titleX, panelY + 2, Title, Palette.DbLight, Palette.PrimaryDarkest);

            for (int i = 0; i < _items.Count; i++)
            {
                int itemY = panelY + 5 + (i * 2);
                MenuItem item = _items[i];
                item.SetBounds(panelX + 1, itemY + 1, panelWidth - 2, 1);
                bool selected = (_selectedIndex >= 0 && i == _selectedIndex) || item.IsHovered;
                RLColor foreground = selected ? Palette.DbSun : Palette.AlternateLightest;
                RLColor background = selected ? Palette.PrimaryDarker : Palette.PrimaryDarkest;
                string marker = selected ? "> " : "  ";
                console.SetBackColor(panelX + 1, itemY, panelWidth - 2, 1, background);
                console.Print(panelX + 3, itemY,
                    marker + item.GetDisplayText(), foreground, background);
            }

            string hint = "UP/DOWN: SELECT    ENTER: CONFIRM    ESC: BACK";
            int hintX = Math.Max(0, (width - hint.Length) / 2);
            console.Print(hintX, height - 2, hint, Palette.Alternate, Palette.DbDark);
        }
    }

    public class MenuSystem
    {
        private readonly Stack<MenuScreen> _screens = new Stack<MenuScreen>();

        public bool IsOpen => _screens.Count > 0;
        public MenuScreen ActiveScreen => IsOpen ? _screens.Peek() : null;
        public IEnumerable<IClickable> ActiveClickables => IsOpen
            ? (IEnumerable<IClickable>)ActiveScreen.Items
            : new IClickable[0];
        public event Action OnChanged;

        public void Open(MenuScreen screen)
        {
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));

            _screens.Clear();
            _screens.Push(screen);
            OnChanged?.Invoke();
        }

        public void OpenSubmenu(MenuScreen screen)
        {
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));

            _screens.Push(screen);
            OnChanged?.Invoke();
        }

        public void Close()
        {
            _screens.Clear();
            OnChanged?.Invoke();
        }

        public void Back()
        {
            if (_screens.Count > 1)
                _screens.Pop();
            else
                Close();

            OnChanged?.Invoke();
        }

        public bool HandleInput(RLKey key)
        {
            if (!IsOpen)
                return false;

            foreach (MenuItem item in ActiveScreen.Items)
            {
                if (item.Button == key)
                {
                    item.Activate();
                    OnChanged?.Invoke();
                    return true;
                }
            }

            switch (key)
            {
                case RLKey.Up:
                    ActiveScreen.MoveSelection(-1);
                    OnChanged?.Invoke();
                    break;
                case RLKey.Down:
                    ActiveScreen.MoveSelection(1);
                    OnChanged?.Invoke();
                    break;
                case RLKey.Enter:
                    ActiveScreen.ActivateSelected();
                    OnChanged?.Invoke();
                    break;
                case RLKey.Escape:
                    if (_screens.Count > 1)
                        Back();
                    else if (ActiveScreen.EscapeAction != null)
                        ActiveScreen.EscapeAction();
                    else
                        Close();
                    break;
            }

            return true;
        }

        public void Draw(RLConsole console, int width, int height)
        {
            if (IsOpen)
                ActiveScreen.Draw(console, width, height);
        }
    }
}
