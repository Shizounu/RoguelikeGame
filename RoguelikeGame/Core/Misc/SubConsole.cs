using RLNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Core
{
    public class SubConsole {
        public SubConsole(int _width, int _height) {
            Width = _width; 
            Height = _height;   
            
            console = new RLConsole(_width, _height);
        }

        public readonly RLConsole console;
        public readonly int Width;
        public readonly int Height;

        public event OnUpdate OnUpdate;
        public event OnRender OnDraw;

        public void Update(RLRootConsole root) => OnUpdate?.Invoke(this, root);
        public void Draw(RLRootConsole root) {
            console.Clear();
            OnDraw?.Invoke(this, root);
        } 
        
        public void Blit(RLRootConsole root,int DestX, int DestY, int startX = 0, int startY = 0)
        {
            RLConsole.Blit(console, startX, startY, Width, Height, root, DestX, DestY);
        }
        public void SetBackgroundColor(RLColor color)
        {
            console.SetBackColor(0, 0, Width, Height, color);
        }
            
    }
    public delegate void OnRender(SubConsole console, RLRootConsole root);
    public delegate void OnUpdate(SubConsole console, RLRootConsole root); 
}
