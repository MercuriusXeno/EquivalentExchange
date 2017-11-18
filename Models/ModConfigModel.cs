using Microsoft.Xna.Framework.Input;

namespace EquivalentExchange.Models
{
    class ConfigurationModel
    {
        public string TransmuteKey { get; set; }
        public string LiquidateKey { get; set; }
        public double TransmutationMarkupPercentage { get; set; }
        public double LiquidationValuePercentage { get; set; }
        public bool IsSoundEnabled { get; set; }
        public int RepeatSoundDelay { get; set; }
        public ConfigurationModel()
        {
            TransmuteKey = Keys.T.ToString();
            LiquidateKey = Keys.Y.ToString();

            TransmutationMarkupPercentage = 1D;
            LiquidationValuePercentage = 1D;
            IsSoundEnabled = true;
            RepeatSoundDelay = 1;
        }
    }
}