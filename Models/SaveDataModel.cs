using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    public class SaveDataModel
    {
        public Dictionary<long, int> AlchemyLevel { get; set; }
        public Dictionary<long, int> AlchemyExperience { get; set; }
        public Dictionary<long, float> AlkahestryMaxEnergy { get; set; }
        public Dictionary<long, float> AlkahestryCurrentEnergy { get; set; }
        public Dictionary<long, int> TotalValueTransmuted { get; set; }
        public ulong GameUniqueID { get; set; }
        

        public SaveDataModel(ulong gameID, long playerId)
        {
            GameUniqueID = gameID;
            AlchemyLevel.Add(playerId, 0);
            AlchemyExperience.Add(playerId, 0);
            AlkahestryMaxEnergy.Add(playerId, 0F);
            AlkahestryCurrentEnergy.Add(playerId, 0F);
            TotalValueTransmuted.Add(playerId, 0);
        }
    }
}
