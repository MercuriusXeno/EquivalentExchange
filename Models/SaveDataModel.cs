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
        public Dictionary<long, float> MaxEnergy { get; set; }
        public Dictionary<long, float> CurrentEnergy { get; set; }
        public Dictionary<long, int> TotalValueTransmuted { get; set; }
        public ulong GameUniqueID { get; set; }
        

        public SaveDataModel(ulong gameID)
        {
            GameUniqueID = gameID;
        }

        public void InitializePlayer(long playerId)
        {
            AlchemyLevel.Add(playerId, 0);
            AlchemyExperience.Add(playerId, 0);
            MaxEnergy.Add(playerId, 0F);
            CurrentEnergy.Add(playerId, 0F);
            TotalValueTransmuted.Add(playerId, 0);
        }
    }
}
