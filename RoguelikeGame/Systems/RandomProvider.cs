using RogueSharp.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Systems
{
    /// <summary>
    /// Singleton
    /// </summary>
    public class RandomProvider {
        private static RandomProvider _Instance;
        public static RandomProvider Instance { 
            get {
                if(_Instance == null )  
                    _Instance = new RandomProvider();
                return _Instance;
            }
        }
        public IRandom Provider;
        public int Seed; 
        public RandomProvider() {
            Seed = (int)DateTime.UtcNow.Ticks;
            Provider = new DotNetRandom(Seed);
        }

    }
}
