using Microsoft.Xna.Framework.Input;

namespace EquivalentExchange.Models
{
    class ConfigurationModel
    {
        public string TransmuteKey { get; set; }
        public string LiquidateKey { get; set; }
        public string TransmuteInfoKey { get; set; }
        public ConfigurationModel()
        {
            TransmuteKey = Keys.T.ToString();
            LiquidateKey = Keys.Y.ToString();
            TransmuteInfoKey = Keys.H.ToString();            
        }
    }
}