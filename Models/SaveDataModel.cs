using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    public class SaveDataModel
    {
        public int alchemyLevel { get; set; }
        public int alchemyExp { get; set; }
        public long playerID { get; set; }

        //the 6 professions, Shaper -> Transmuter/Adept; Sage -> Aurumancer/Conduit
        public bool hasShaperProfession { get; set; }
        public bool hasTransmuterProfession { get; set; }
        public bool hasAdeptProfession { get; set; }

        //alt path
        public bool hasSageProfession { get; set; }
        public bool hasAurumancerProfession { get; set; }
        public bool hasConduitProfession { get; set; }

        public SaveDataModel()
        {
            playerID = 0;
            alchemyLevel = 0;
            alchemyExp = 0;
            hasShaperProfession = false;
            hasTransmuterProfession = false;
            hasAdeptProfession = false;
            hasSageProfession = false;
            hasAurumancerProfession = false;
            hasConduitProfession = false;
        }
    }
}
