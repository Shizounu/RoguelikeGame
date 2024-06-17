﻿using RLNET;
using RoguelikeGame.Core.MouseSelection;
using RoguelikeGame.Interfaces_and_Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems
{
    public delegate void InputHandler(); 

    public class InputSystem : Singleton<InputSystem> {

        public event InputHandler OnUpInput;
        public event InputHandler OnDownInput;
        public event InputHandler OnLeftInput;
        public event InputHandler OnRightInput;
        public event InputHandler OnInteractInput;

        public event InputHandler OnCloseInput;

        public event InputHandler OnUserInput;

        public List<IClickable> clickables = new List<IClickable>();

        public void CheckInput(RLRootConsole rootConsole) {

            RLKeyPress keyPress = rootConsole.Keyboard.GetKeyPress();

            if (keyPress == null)
                return;

            switch (keyPress.Key) {
                case RLKey.Up :
                case RLKey.W:
                    OnUpInput?.Invoke();
                    OnUserInput?.Invoke();
                    break;
                case RLKey.Down :
                case RLKey.S:
                    OnDownInput?.Invoke();
                    OnUserInput?.Invoke();
                    break;
                case RLKey.A:
                case RLKey.Left :
                    OnLeftInput?.Invoke();
                    OnUserInput?.Invoke();
                    break;
                case RLKey.Right :
                case RLKey.D:
                    OnRightInput?.Invoke();
                    OnUserInput?.Invoke();
                    break;

                case RLKey.Period:
                case RLKey.Q: 
                    OnInteractInput?.Invoke();
                    OnUserInput.Invoke();
                    break;


                case RLKey.Escape:
                    OnCloseInput?.Invoke();
                    OnUserInput?.Invoke();
                    break;
            }


        }


        public void AddClickable(IClickable clickable)
        {
            clickables.Add(clickable);
        }
        public void RemoveClickable(IClickable clickable) {
            clickables.Remove(clickable);
        }
        public void CheckClickables(RLRootConsole rootConsole) {

            foreach (var clickable in clickables)
            {
                clickable.IsHovered = false; 
                clickable.WasClickedThisFrame = false;
            }

            int mouseX = rootConsole.Mouse.X;
            int mouseY = rootConsole.Mouse.Y; 
            foreach (var clickable in clickables) {
                if(IsMouseInBounds(clickable, mouseX, mouseY)) {
                    clickable.IsHovered = true;
                    if (rootConsole.Mouse.GetLeftClick())
                    {
                        clickable.OnClick();
                        clickable.WasClickedThisFrame = true;
                    }
                    break; //only one item can be clicked at a time
                }
            }
        }
        private bool IsMouseInBounds(IClickable clickable, int mouseX, int mouseY) {
            //Clickable X/Y is always the top left corner of a window
            return mouseX >= clickable.X && mouseX <= (clickable.X + clickable.Width)
                && mouseY <= clickable.Y && mouseY >= (clickable.Y - clickable.Height);
        }

    }
}
