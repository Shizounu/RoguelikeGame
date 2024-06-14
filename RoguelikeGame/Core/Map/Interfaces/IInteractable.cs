using RoguelikeGame.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame { 
    public interface IInteractable : IDrawable
    {
        void Interact();
    }
}
