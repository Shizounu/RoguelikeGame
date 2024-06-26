using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems.Input
{
    public interface IClickable {


        int X { get; }
        int Y { get; }

        int Width { get; }
        int Height { get; }

        bool IsHovered { get; set; } 
        bool WasClickedThisFrame { get; set; }
        void OnClick();
    }
}
