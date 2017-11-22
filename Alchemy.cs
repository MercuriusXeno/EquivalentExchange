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
    public class Alchemy
    {
        //constants for storing some important formula values as non-magic numbers, this is the impact level ups and other factors have on formulas, stored in constants for easy edits.
        public const double TRANSMUTATION_BONUS_PER_LEVEL = 0.1D;
        public const double LIQUIDATION_BONUS_PER_LEVEL = 0.025D;
        public const double SKILL_STAMINA_DRAIN_IMPACT_PER_LEVEL = 0.075D;
        public const double SAGE_PROFESSION_STAMINA_DRAIN_BONUS = 0.15D;
        public const double BASE_VALUE_COEFFICIENT = 0.5D;
        public const double AURUMANCER_LIQUIDATION_BONUS = 0.25D;
        public const double BASE_COST_COEFFICIENT = 3D;
        public const double LUCK_REBOUND_IMPACT = 0.01D;
        public const double BASE_REBOUND_RATE = 0.05D;
        public const double TRANSMUTER_TRANSMUTATION_BONUS = 1D;
        public const double SHAPER_DAILY_LUCK_BONUS = 2D;
        public const double LUCK_NORMALIZATION_FOR_FREE_TRANSMUTES = 0.13D;
        public const double LUCK_FREE_TRANSMUTE_IMPACT = 0.01D;
        public const double SKILL_FREE_TRANSMUTE_IMPACT = 0.03D;
        public const double MAX_DISTANCE_FACTOR = 10D;
        public const double DISTANCE_BONUS_FOR_LUCK_FACTOR_NORMALIZATION = (200D / 3D);
        public const double MAP_DISTANCE_FACTOR = 0.05D;
        public const string LEYLINE_PROPERTY_INDICATOR = "AlchemyLeyline";

        //default experience progression values, only multiplied by 10... that I'm gonna try to balance around, somehow.
        public static readonly int[] alchemyExperienceNeededPerLevel = new int[] { 1000, 3800, 7700, 13000, 21500, 33000, 48000, 69000, 100000, 150000 };

        //needed for rebound rolls
        public static Random alchemyRandom = new Random();

        //increment alchemy experience and handle levelups if applicable
        public static void AddAlchemyExperience(int exp)
        {
            EquivalentExchange.instance.currentPlayerData.AlchemyExperience += exp;

            while (EquivalentExchange.instance.currentPlayerData.AlchemyLevel < 10 && EquivalentExchange.instance.currentPlayerData.AlchemyExperience >= GetAlchemyExperienceNeededForNextLevel())
            {
                EquivalentExchange.instance.currentPlayerData.AlchemyLevel++;
                //player gained a skilllevel, flag the night time skill up to appear.
                EquivalentExchange.instance.AddSkillUpMenuAppearance(EquivalentExchange.instance.currentPlayerData.AlchemyLevel);
            }
        }

        //overloaded method for how much experience is needed to reach a specific level.
        public static int GetAlchemyExperienceNeededForLevel(int level)
        {
            if (level > 0 && level < 11)
                return alchemyExperienceNeededPerLevel[level - 1];
            return int.MaxValue;
        }

        //how much experience is needed to reach next level
        public static int GetAlchemyExperienceNeededForNextLevel()
        {
            return GetAlchemyExperienceNeededForLevel(EquivalentExchange.instance.currentPlayerData.AlchemyLevel + 1);
        }

        //get the coefficient for stamina drain
        public static double GetAlchemyStaminaCostSkillMultiplierForLevel(int level)
        {
            //base of 1 - 0.075 per skill level - profession modifiers
            return (1 - (level * SKILL_STAMINA_DRAIN_IMPACT_PER_LEVEL)) - (Game1.player.professions.Contains(Professions.Sage) ? SAGE_PROFESSION_STAMINA_DRAIN_BONUS : 0.0F);
        }

        //get the coefficient for stamina drain
        public static double GetAlchemyStaminaCostSkillMultiplier()
        {
            //base of 1 - 0.075 per skill level - profession modifiers
            return GetAlchemyStaminaCostSkillMultiplierForLevel(EquivalentExchange.instance.currentPlayerData.AlchemyLevel);
        }

        //algorithm to return stamina cost for the act of transmuting/liquidating an item, based on player skill and item value
        public static double GetStaminaCostForTransmutation(int itemValue)
        {
            return Math.Sqrt(itemValue) * GetAlchemyStaminaCostSkillMultiplier();
        }

        //get the coefficient for item sell value
        public static double GetLiquidationValuePercentage()
        {
            return GetLiquidationValuePercentage(EquivalentExchange.instance.currentPlayerData.AlchemyLevel, Game1.player.professions.Contains(Professions.Aurumancer));
        }

        //get the coefficient for item price for transmutation
        public static double GetTransmutationMarkupPercentage()
        {
            return GetTransmutationMarkupPercentage(EquivalentExchange.instance.currentPlayerData.AlchemyLevel, Game1.player.professions.Contains(Professions.Transmuter));
        }

        //the chance a player will fail to transmute/liquidate an item
        public static double GetReboundChance()
        {
            double distance = DistanceCalculator.GetPathDistance(Game1.player.currentLocation);
            if (distance == double.MaxValue)
            {
                distance = 5; //a middling distance value is subbed in when the distance can't be calculated by the algorithm for whatever reason.
            }

            //the distance factor is whatever the raw distance is minus the player's alchemy level, which reduces the impact of distance from leylines to rebounds
            double distanceFactor = Math.Max(0D, distance - EquivalentExchange.instance.currentPlayerData.AlchemyLevel);

            //normalize distance factor - each map adds roughly 5% rebound, so dividing by 20D is what we're going for.
            distanceFactor /= (1 / MAP_DISTANCE_FACTOR);

            //calculate luck's impact on rebound
            double luckFactor = (Game1.player.LuckLevel * LUCK_REBOUND_IMPACT) + Game1.dailyLuck;

            return Math.Max(0, (BASE_REBOUND_RATE + distanceFactor) - luckFactor);
        }

        internal static double GetTransmutationMarkupPercentage(int whichLevel, bool hasTransmuterProfession)
        {
            //base of 3.0 - 0.1 per skill level - profession modifiers
            return BASE_COST_COEFFICIENT - (TRANSMUTATION_BONUS_PER_LEVEL * whichLevel) - (hasTransmuterProfession ? TRANSMUTER_TRANSMUTATION_BONUS : 0.0F);
        }

        internal static double GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses(int whichLevel, int luckLevel)
        {
            return (luckLevel * LUCK_FREE_TRANSMUTE_IMPACT) + (whichLevel * SKILL_FREE_TRANSMUTE_IMPACT);
        }

        internal static double GetLiquidationValuePercentage(int whichLevel, bool hasAurumancerProfession)
        {
            //base of 0.5 + 0.03 per skill level + profession modifiers
            return BASE_VALUE_COEFFICIENT + (LIQUIDATION_BONUS_PER_LEVEL * whichLevel) + (hasAurumancerProfession ? AURUMANCER_LIQUIDATION_BONUS : 0.0F);
        }

        //check if the player failed a rebound check
        public static bool DidPlayerFailReboundCheck()
        {             
            return alchemyRandom.NextDouble() <= GetReboundChance();
        }

        //get rebound damage based on item value. there is no resistance to this damage.
        public static int GetReboundDamage(int itemValue)
        {
            return (int)Math.Ceiling(Math.Sqrt(itemValue));
        }

        //apply rebound damage to the player. A separate routine is responsible for playing the sound.
        public static void TakeDamageFromRebound(int itemValue)
        {
            int damage = GetReboundDamage(itemValue);
            Game1.player.health -= damage;
            Game1.player.currentLocation.debris.Add(new Debris(damage, new Vector2((float)(Game1.player.getStandingX() + 8), (float)Game1.player.getStandingY()), Color.Red, 1f, (Character)Game1.player));
        }

        //check to see if the player could take a rebound without dying
        public static bool CanSurviveRebound(int itemValue)
        {
            GameLocation currentLocation = Game1.player.currentLocation;
            //automatically pass the chance if your current rebound chance is essentially zero.
            if (GetReboundChance() <= 0.0D)
                return true;
            //otherwise fail this check if your health is lower than rebound damage, we don't let you kill yourself, but you can't transmute.
            if (GetReboundDamage(itemValue) >= Game1.player.health)
                return false;
            //if we made it here we're healthy enough to take the damage that might happen.
            return true;
        }

        //lucky transmutes are basically transmutes that don't cost stamina. this is your chance to get one.
        public static double GetLuckyTransmuteChance()
        {
            GameLocation currentLocation = Game1.player.currentLocation;
            //normalize luck to a non-negative between 1% and 25%, it increases based on a profession
            double dailyLuck = (Game1.dailyLuck + LUCK_NORMALIZATION_FOR_FREE_TRANSMUTES) * (Game1.player.professions.Contains(Professions.Shaper) ? SHAPER_DAILY_LUCK_BONUS : 1D);

            double luckFactor = GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses();

            //player gets a lucky bonus based on proximity to an alchemy leyline
            if (Game1.player.professions.Contains(Professions.Adept))
            {
                double distanceFactor = DistanceCalculator.GetPathDistance(currentLocation);

                //current formula accounts for as much as a distance of 10 from the leyline.
                //normalizes being on a 0 "distance" leyline as a 15% bonus lucky transmute chance.
                //any distance factor farther than 10 receives 0% bonus. There are no penalties.
                luckFactor += Math.Max((MAX_DISTANCE_FACTOR - distanceFactor) / DISTANCE_BONUS_FOR_LUCK_FACTOR_NORMALIZATION, 0D);
            }            
            
            return luckFactor + dailyLuck;
        }

        public static double GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses()
        {
            return GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses(EquivalentExchange.instance.currentPlayerData.AlchemyLevel, Game1.player.LuckLevel);
        }

        //check to see if this is a lucky [free] transmute
        public static bool IsLuckyTransmute()
        {
            return alchemyRandom.NextDouble() <= GetLuckyTransmuteChance();
        }

        //handles draining stamina on successful transmute, and checking for lucky transmutes.
        public static void HandleStaminaDeduction(double staminaCost)
        {
            if (IsLuckyTransmute())
                return;
            Game1.player.Stamina -= (float)staminaCost;
        }
    }
}
