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
            playerSaveData.alchemyExp += exp;

            while (playerSaveData.alchemyLevel < 10 && playerSaveData.alchemyExp >= GetAlchemyExperienceNeededForNextLevel())
            {
                playerSaveData.alchemyLevel++;
            }
        }

        //how much experience is needed to reach next level
        public int GetAlchemyExperienceNeededForNextLevel()
        {            
            if (playerSaveData.alchemyLevel < 10)
                return alchemyExperienceNeededPerLevel[playerSaveData.alchemyLevel];
            return int.MaxValue;
        }

        //get the coefficient for stamina drain
        public float GetAlchemyStaminaCostSkillMultiplier()
        {
            //base of 1 - 0.075 per skill level - profession modifiers
            return 1 - (playerSaveData.alchemyLevel * 0.075F) - (playerSaveData.hasSageProfession ? 0.15F : 0.0F);
        }

        //algorithm to return stamina cost for the act of transmuting/liquidating an item, based on player skill and item value
        public float GetStaminaCostForTransmutation(int itemValue)
        {
            return (float)Math.Sqrt(itemValue) * GetAlchemyStaminaCostSkillMultiplier();
        }

        //get the coefficient for item sell value
        public float GetLiquidationValuePercentage()
        {
            //base of 0.5 + 0.03 per skill level + profession modifiers
            return 0.5F + (0.03F * playerSaveData.alchemyLevel) + (playerSaveData.hasAurumancerProfession ? 0.25F : 0.0F);
        }

        //get the coefficient for item price for transmutation
        public float GetTransmutationMarkupPercentage()
        {
            //base of 3.0 - 0.1 per skill level - profession modifiers
            return 3.0F - (0.1F * playerSaveData.alchemyLevel) - (playerSaveData.hasTransmuterProfession ? 1.0F : 0.0F);
        }

        //the chance a player will fail to transmute/liquidate an item
        public double GetReboundChance()
        {
            double baseReboundRate = 0.05D;
            double distanceFactor = DistanceCalculator.GetPathDistance(this.currentLocation);
            double luckFactor = (this.LuckLevel * 0.01D) + Game1.dailyLuck;
            return Math.Max(0, (baseReboundRate + distanceFactor) - luckFactor);
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
            double dailyLuck = Game1.dailyLuck * (playerSaveData.hasShaperProfession ? 2D : 1D);
            double luckFactor = (this.LuckLevel * 0.01) +  + 0.13D;

            //player gets a lucky bonus based on proximity to an alchemy leyline
            if (playerSaveData.hasAdeptProfession)
            {
                double distanceFactor = DistanceCalculator.GetPathDistance(this.currentLocation);

                //current formula accounts for as much as a distance of 10 from the leyline.
                //normalizes being on a 0 "distance" leyline as a 15% bonus lucky transmute chance.
                //any distance factor farther than 10 receives 0% bonus. There are no penalties.
                luckFactor += Math.Max((10D - distanceFactor) / (200D / 3D), 0);
            }            
            
            return luckFactor;
        }

        //check to see if this is a lucky [free] transmute
        public bool IsLuckyTransmute()
        {
            return alchemyRandom.NextDouble() <= GetLuckyTransmuteChance();
        }

        //handles draining stamina on successful transmute, and checking for lucky transmutes.
        public void HandleStaminaDeduction(float staminaCost)
        {
            if (IsLuckyTransmute())
                return;
            this.Stamina -= staminaCost;
        }
    }
}
