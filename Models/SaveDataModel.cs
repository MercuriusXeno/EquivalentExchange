using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    public class SaveDataModel
    {
        public int AlchemyLevel { get; set; }
        public int AlchemyExperience { get; set; }
        public long PlayerID { get; set; }

        //two flags to optimize a check for has-all-professions of either rank, for the all professions mod.
        public bool HasAllFirstRankProfessions { get; set; }
        public bool HasAllSecondRankProfessions { get; set; }

        //the 6 professions, Shaper -> Transmuter/Adept; Sage -> Aurumancer/Conduit
        public bool HasShaperProfession { get; set; }
        public bool HasTransmuterProfession { get; set; }
        public bool HasAdeptProfession { get; set; }

        //alt path
        public bool HasSageProfession { get; set; }
        public bool HasAurumancerProfession { get; set; }
        public bool HasConduitProfession { get; set; }
        

        public SaveDataModel(long playerID)
        {
            PlayerID = playerID;
            AlchemyLevel = 0;
            AlchemyExperience = 0;
            HasAllFirstRankProfessions = false;
            HasAllSecondRankProfessions = false;
            HasShaperProfession = false;
            HasTransmuterProfession = false;
            HasAdeptProfession = false;
            HasSageProfession = false;
            HasAurumancerProfession = false;
            HasConduitProfession = false;
        }
    }
}
