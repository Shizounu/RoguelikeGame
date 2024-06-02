using RLNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeGame.Core
{
    public static class Colors
    {
        public static readonly RLColor FloorBackground = RLColor.Black;
        public static readonly RLColor Floor = Palette.AlternateDarkest;
        public static readonly RLColor FloorBackgroundFov = Palette.DbDark;
        public static readonly RLColor FloorFov = Palette.Alternate;

        public static readonly RLColor WallBackground = Palette.SecondaryDarkest;
        public static readonly RLColor Wall = Palette.Secondary;
        public static readonly RLColor WallBackgroundFov = Palette.SecondaryDarker;
        public static readonly RLColor WallFov = Palette.SecondaryLighter;

        public static readonly RLColor TextHeading = Palette.DbLight;
        public static readonly RLColor Player = Palette.DbLight;

        public static RLColor Text = Palette.DbLight;
        public static RLColor Gold = Palette.DbSun;

        public static RLColor KoboldColor = Palette.DbBrightWood;

        public static RLColor DoorBackground = Palette.ComplimentDarkest;
        public static RLColor Door = Palette.ComplimentLighter;
        public static RLColor DoorBackgroundFov = Palette.ComplimentDarker;
        public static RLColor DoorFov = Palette.ComplimentLightest;
    }
}
