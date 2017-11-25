using Microsoft.Xna.Framework.Input;

namespace EquivalentExchange.Models
{
    class ConfigurationModel
    {
        public string TransmuteKey { get; set; }
        public string LiquidateKey { get; set; }
        public string NormalizeKey { get; set; }
        public string TransmuteInfoKey { get; set; }
        public bool DisableRecipeItems { get; set; }

        public ConfigurationModel()
        {
            TransmuteKey = Keys.T.ToString();
            LiquidateKey = Keys.Y.ToString();
            NormalizeKey = Keys.N.ToString();
            TransmuteInfoKey = Keys.H.ToString();
            DisableRecipeItems = false;
        }
    }
}