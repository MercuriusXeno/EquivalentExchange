using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    class Reference
    {
        // struct for remembering important item Ids, uses the statics from the game when possible.
        // not all of them are mapped, so some are hard coded.
        public struct Items
        {
            public const int Sap = 92;
            public const int Clay = 330;
            public const int CopperOre = StardewValley.Object.copper;
            public const int IronOre = StardewValley.Object.iron;
            public const int GoldOre = StardewValley.Object.gold;
            public const int IridiumOre = StardewValley.Object.iridium;
            public const int Coal = StardewValley.Object.coal;
            public const int Wood = StardewValley.Object.wood;
            public const int Stone = StardewValley.Object.stone;
            public const int Hardwood = 709;
            public const int Fiber = 771;
            public const int Slime = 766;            
        }
    }
}
