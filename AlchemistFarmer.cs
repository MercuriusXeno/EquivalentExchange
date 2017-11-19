using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using EquivalentExchange;
using Microsoft.Xna.Framework;

namespace EquivalentExchange
{
    public class AlchemistFarmer : StardewValley.Farmer
    {
        //constants for storing some important formula values as non-magic numbers, this is the impact level ups and other factors have on formulas, stored in constants for easy edits.
        public const double transmutationBonusPerLevel = 0.1D;
        public const double liquidationBonusPerLevel = 0.025D;
        public const double skillStaminaDrainImpactPerLevel = 0.075D;
        public const double sageProfessionStaminaDrainBonus = 0.15D;
        public const double baseValueCoefficient = 0.5D;
        public const double aurumancerLiquidationBonus = 0.25D;
        public const double baseCostCoefficient = 3D;
        public const double luckReboundImpact = 0.01D;
        public const double baseReboundRate = 0.05D;
        public const double transmuterTransmutationBonus = 1D;
        public const double shaperDailyLuckBonus = 2D;
        public const double luckNormalizationForFreeTransmutes = 0.13D;
        public const double luckFreeTransmuteImpact = 0.01D;
        public const double skillFreeTransmuteImpact = 0.03D;
        public const double maxDistanceFactor = 10D;
        public const double distanceBonusForLuckFactorNormalization = (200D / 3D);

        //default experience progression values that I'm gonna try to balance around, somehow.
        public static readonly int[] alchemyExperienceNeededPerLevel = new int[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

        //needed for rebound rolls
        public static Random alchemyRandom = new Random();

        //save data for the mod's alchemy skill, in list form for per-player lookups.
        public SaveDataModel playerSaveData = new SaveDataModel();

        //constructor for keeping the player's data in this class.
        public AlchemistFarmer (SaveDataModel playerData)
        {
            if (playerData == null)
                playerData = new SaveDataModel();
            this.playerSaveData = playerData;
        }

        //increment alchemy experience and handle levelups if applicable
        public void AddAlchemyExperience(int exp)
        {
            playerSaveData.AlchemyExperience += exp;

            while (playerSaveData.AlchemyLevel < 10 && playerSaveData.AlchemyExperience >= GetAlchemyExperienceNeededForNextLevel())
            {
                playerSaveData.AlchemyLevel++;
                //player gained a skilllevel, flag the night time skill up to appear.
                EquivalentExchange.instance.AddSkillUpMenuAppearance(playerSaveData.AlchemyLevel);
            }
        }

        //overloaded method for how much experience is needed to reach a specific level.
        public int GetAlchemyExperienceNeededForLevel(int level)
        {
            if (level > 0 && level < 11)
                return alchemyExperienceNeededPerLevel[level - 1];
            return int.MaxValue;
        }

        //how much experience is needed to reach next level
        public int GetAlchemyExperienceNeededForNextLevel()
        {
            return GetAlchemyExperienceNeededForLevel(playerSaveData.AlchemyLevel + 1);
        }

        //get the coefficient for stamina drain
        public double GetAlchemyStaminaCostSkillMultiplier()
        {
            //base of 1 - 0.075 per skill level - profession modifiers
            return 1 - (playerSaveData.AlchemyLevel * skillStaminaDrainImpactPerLevel) - (playerSaveData.HasSageProfession ? sageProfessionStaminaDrainBonus : 0.0F);
        }

        //algorithm to return stamina cost for the act of transmuting/liquidating an item, based on player skill and item value
        public double GetStaminaCostForTransmutation(int itemValue)
        {
            return Math.Sqrt(itemValue) * GetAlchemyStaminaCostSkillMultiplier();
        }

        //get the coefficient for item sell value
        public double GetLiquidationValuePercentage()
        {
            return GetLiquidationValuePercentage(playerSaveData.AlchemyLevel, playerSaveData.HasAurumancerProfession);
        }

        //get the coefficient for item price for transmutation
        public double GetTransmutationMarkupPercentage()
        {
            return GetTransmutationMarkupPercentage(playerSaveData.AlchemyLevel, playerSaveData.HasTransmuterProfession);
        }

        //the chance a player will fail to transmute/liquidate an item
        public double GetReboundChance()
        {            
            double distanceFactor = Math.Max(0D, DistanceCalculator.GetPathDistance(this.currentLocation) - this.playerSaveData.AlchemyLevel);            
            double luckFactor = (this.LuckLevel * luckReboundImpact) + Game1.dailyLuck;
            return Math.Max(0, (baseReboundRate + distanceFactor) - luckFactor);
        }

        internal static double GetTransmutationMarkupPercentage(int whichLevel, bool hasTransmuterProfession)
        {
            //base of 3.0 - 0.1 per skill level - profession modifiers
            return baseCostCoefficient - (transmutationBonusPerLevel * whichLevel) - (hasTransmuterProfession ? transmuterTransmutationBonus : 0.0F);
        }

        internal static double GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses(int whichLevel, int luckLevel)
        {
            return (luckLevel * luckFreeTransmuteImpact) + (whichLevel * skillFreeTransmuteImpact);
        }

        internal static double GetLiquidationValuePercentage(int whichLevel, bool hasAurumancerProfession)
        {
            //base of 0.5 + 0.03 per skill level + profession modifiers
            return baseValueCoefficient + (liquidationBonusPerLevel * whichLevel) + (hasAurumancerProfession ? aurumancerLiquidationBonus : 0.0F);
        }

        //check if the player failed a rebound check
        public bool DidPlayerFailReboundCheck()
        {             
            return alchemyRandom.NextDouble() <= GetReboundChance();
        }


        //get rebound damage based on item value. there is no resistance to this damage.
        public int GetReboundDamage(int itemValue)
        {
            return (int)Math.Ceiling(Math.Sqrt(itemValue));
        }

        //apply rebound damage to the player. A separate routine is responsible for playing the sound.
        public void TakeDamageFromRebound(int itemValue)
        {
            int damage = GetReboundDamage(itemValue);
            this.health -= damage;
            this.currentLocation.debris.Add(new Debris(damage, new Vector2((float)(this.getStandingX() + 8), (float)this.getStandingY()), Color.Red, 1f, (Character)this));
        }

        //check to see if the player could take a rebound without dying
        public bool CanSurviveRebound(int itemValue)
        {
            //automatically pass the chance if your current rebound chance is essentially zero.
            if (GetReboundChance() <= 0.0D)
                return true;
            //otherwise fail this check if your health is lower than rebound damage, we don't let you kill yourself, but you can't transmute.
            if (GetReboundDamage(itemValue) >= this.health)
                return false;
            //if we made it here we're healthy enough to take the damage that might happen.
            return true;
        }

        //lucky transmutes are basically transmutes that don't cost stamina. this is your chance to get one.
        public double GetLuckyTransmuteChance()
        {
            //normalize luck to a non-negative between 1% and 25%, it increases based on a profession
            double dailyLuck = (Game1.dailyLuck + luckNormalizationForFreeTransmutes) * (playerSaveData.HasShaperProfession ? shaperDailyLuckBonus : 1D);

            double luckFactor = GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses();

            //player gets a lucky bonus based on proximity to an alchemy leyline
            if (playerSaveData.HasAdeptProfession)
            {
                double distanceFactor = DistanceCalculator.GetPathDistance(this.currentLocation);

                //current formula accounts for as much as a distance of 10 from the leyline.
                //normalizes being on a 0 "distance" leyline as a 15% bonus lucky transmute chance.
                //any distance factor farther than 10 receives 0% bonus. There are no penalties.
                luckFactor += Math.Max((maxDistanceFactor - distanceFactor) / distanceBonusForLuckFactorNormalization, 0D);
            }            
            
            return luckFactor + dailyLuck;
        }

        public double GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses()
        {
            return GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses(playerSaveData.AlchemyLevel, this.LuckLevel);
        }

        //check to see if this is a lucky [free] transmute
        public bool IsLuckyTransmute()
        {
            return alchemyRandom.NextDouble() <= GetLuckyTransmuteChance();
        }

        //handles draining stamina on successful transmute, and checking for lucky transmutes.
        public void HandleStaminaDeduction(double staminaCost)
        {
            if (IsLuckyTransmute())
                return;
            this.Stamina -= (float)staminaCost;
        }

        internal void EnableAlchemistProfession(EquivalentExchange.Professions profession)
        {
            switch (profession)
            {
                case EquivalentExchange.Professions.Shaper:
                    playerSaveData.HasShaperProfession = true;
                    break;
                case EquivalentExchange.Professions.Sage:
                    playerSaveData.HasSageProfession = true;
                    break;
                case EquivalentExchange.Professions.Transmuter:
                    playerSaveData.HasTransmuterProfession = true;
                    break;
                case EquivalentExchange.Professions.Adept:
                    playerSaveData.HasAdeptProfession = true;
                    break;
                case EquivalentExchange.Professions.Aurumancer:
                    playerSaveData.HasAurumancerProfession = true;
                    break;
                case EquivalentExchange.Professions.Conduit:
                    playerSaveData.HasConduitProfession = true;
                    break;
            }
        }
    }
}
