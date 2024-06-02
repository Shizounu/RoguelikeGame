﻿using RoguelikeGame.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Interfaces_and_Abstracts
{
    public interface IInteractable : IDrawable
    {
        void Interact();
    }
}