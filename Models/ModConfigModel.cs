using Microsoft.Xna.Framework.Input;

namespace EquivalentExchange.Models
{
    class ConfigurationModel
    {
        public string TransmuteKey { get; set; }
        public string LiquidateKey { get; set; }
        public bool IsSoundEnabled { get; set; }
        public int RepeatSoundDelay { get; set; }
        public ConfigurationModel()
        {
            TransmuteKey = Keys.T.ToString();
            LiquidateKey = Keys.Y.ToString();

            IsSoundEnabled = true;
            RepeatSoundDelay = 1;
        }
    }
}