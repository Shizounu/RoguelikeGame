using RLNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems
{
    public delegate void InputHandler(); 

    public class InputSystem {
        

        public event InputHandler OnUpInput;
        public event InputHandler OnDownInput;
        public event InputHandler OnLeftInput;
        public event InputHandler OnRightInput;
        public event InputHandler OnInteractInput;

        public event InputHandler OnCloseInput;

        public event InputHandler OnUserInput;

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



    }
}
