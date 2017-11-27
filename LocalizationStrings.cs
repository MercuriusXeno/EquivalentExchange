using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    class LocalizationStrings
    {
        public const string LuckyTransmute = "LuckyTransmute";
        public const string Level = "Level";
        public const string Alchemy = "Alchemy";
        public const string LeylineDistanceNegatedBy = "LeylineDistanceNegatedBy";
        public const string StaminaDrain = "StaminaDrain";
        public const string Shaper = "Shaper";
        public const string Sage = "Sage";
        public const string Transmuter = "Transmuter";
        public const string Adept = "Adept";
        public const string Aurumancer = "Aurumancer";
        public const string Conduit = "Conduit";
        public const string ChooseAProfession = "ChooseAProfession";
        public const string CommandFormat = "CommandFormat";
        public const string BadExperienceAmount = "BadExperienceAmount";
        public const string Added = "Added";
        public const string alchemyExperience = "alchemyExperience";
        public const string Value = "Value";
        public const string Luck = "Luck";
        public const string Energy = "Energy";
        public const string Fail = "Fail";
        public const string HP = "HP";
        public const string orStick = "orStick";
        public const string orStone = "orStone";
        public const string toBreakIt = "toBreakIt";
        public const string BreaksA = "BreaksA";
        public const string area = "area";
        public const string andPress = "andPress";
        public const string MouseOverWeeds = "MouseOverWeeds";
        public const string orGrassAndPress = "orGrassAndPress";
        public const string CutsA = "CutsA";
        public const string toMowItDown = "toMowItDown";
        public const string MouseOverTilledSoil = "MouseOverTilledSoil";
        public const string waterA = "waterA";
        public const string to = "to";
        public const string MouseOverSoil = "MouseOverSoil";
        public const string toTillTheSoil = "toTillTheSoil";
        public const string orBreakA = "orBreakA";
        public const string ShaperDescription = "ShaperDescription";
        public const string SageDescription = "SageDescription";
        public const string TransmuterDescription = "TransmuterDescription";
        public const string AdeptDescription = "AdeptDescription";
        public const string AurumancerDescription = "AurumancerDescription";
        public const string ConduitDescription = "ConduitDescription";

        public static string amount { get; internal set; }

        public static string Get(string localizationStringName)
        {
            return EquivalentExchange.instance.eeHelper.Translation.Get(localizationStringName);
        }
    }
}
