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

        public OnUpdate OnUpdate;
        public OnRender OnDraw;

        public void Update(RLRootConsole root) => OnUpdate?.Invoke(this, root); 
        public void Draw(RLRootConsole root) => OnDraw?.Invoke(this, root);
        
        public void Blit(RLRootConsole root,int DestX, int DestY)
        {
            RLConsole.Blit(console, 0, 0, Width, Height, root, DestX, DestY);
        }
        public void SetBackgroundColor(RLColor color)
        {
            console.SetBackColor(0, 0, Width, Height, color);
        }
            
    }
    public delegate void OnRender(SubConsole console, RLRootConsole root);
    public delegate void OnUpdate(SubConsole console, RLRootConsole root); 
}
