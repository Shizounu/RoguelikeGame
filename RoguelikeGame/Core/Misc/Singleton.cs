using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Interfaces_and_Abstracts
{
    public abstract class Singleton<T> where T : new() {

        private static T _Instance; 
        public static T Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new T();
                return _Instance;
            }
        }
    }
}
