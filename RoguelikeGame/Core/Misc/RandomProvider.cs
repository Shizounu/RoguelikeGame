using System;
using RogueSharp.Random;

namespace RoguelikeGame.Systems.RandomProvider
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
