using RLNET;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace RoguelikeGame.Systems.Input
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
        public bool CheckClickables(RLRootConsole rootConsole) {
            if(clickables.Count == 0)
                return false;
            bool returnVal = false;
            foreach (var clickable in clickables) {
                if (clickable.IsHovered || clickable.WasClickedThisFrame)
                    returnVal = true;
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
                    //only one item can be hovered  at a time
                    returnVal = true;
                    break; 
                }
            }
            return returnVal; 
        }
        private bool IsMouseInBounds(IClickable clickable, int mouseX, int mouseY) {
            //Clickable X/Y is always the top left corner of a window
            return mouseX >= clickable.X && mouseX <= (clickable.X + clickable.Width)
                && mouseY <= clickable.Y && mouseY >= (clickable.Y - clickable.Height);
        }

    }
}
