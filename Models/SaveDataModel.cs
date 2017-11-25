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
        public float AlkahestryMaxEnergy { get; set; }
        public float AlkahestryCurrentEnergy { get; set; }
        public int TotalValueTransmuted { get; set; }
        public ulong GameUniqueID { get; set; }
        

        public SaveDataModel(ulong gameID)
        {
            GameUniqueID = gameID;
            AlchemyLevel = 0;
            AlchemyExperience = 0;
            AlkahestryMaxEnergy = 0F;
            AlkahestryCurrentEnergy = 0F;
            TotalValueTransmuted = 0;
        }
    }
}
