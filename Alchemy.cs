using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using EquivalentExchange;
using Microsoft.Xna.Framework;
using StardewValley.Tools;

namespace EquivalentExchange
{
    public class Alchemy
    {
        //constants for storing some important formula values as non-magic numbers, this is the impact level ups and other factors have on formulas, stored in constants for easy edits.
        //public const double TRANSMUTATION_BONUS_PER_LEVEL = 0.2D;
        //public const double LIQUIDATION_BONUS_PER_LEVEL = 0.02D;
        public const double SKILL_STAMINA_DRAIN_IMPACT_PER_LEVEL = 0.075D;
        //public const double BASE_VALUE_COEFFICIENT = 0.8D;
        //public const double BASE_COST_COEFFICIENT = 3D;
        public const double LUCK_REBOUND_IMPACT = 0.01D;
        public const double BASE_REBOUND_RATE = 0.05D;
        public const double SHAPER_DAILY_LUCK_BONUS = 2D;
        public const double LUCK_NORMALIZATION_FOR_FREE_TRANSMUTES = 0.13D;
        public const double LUCK_FREE_TRANSMUTE_IMPACT = 0.01D;
        public const double SKILL_FREE_TRANSMUTE_IMPACT = 0.03D;
        public const double MAX_DISTANCE_FACTOR = 10D;
        public const double MAP_DISTANCE_FACTOR = 0.05D;
        public const string LEYLINE_PROPERTY_INDICATOR = "AlchemyLeyline";


        //old profession constants
        //public const double SAGE_PROFESSION_STAMINA_DRAIN_BONUS = 0.15D;
        //public const double AURUMANCER_LIQUIDATION_BONUS = 0.25D;
        //public const double TRANSMUTER_TRANSMUTATION_BONUS = 1D;
        public const double DISTANCE_BONUS_FOR_LUCK_FACTOR_NORMALIZATION = (200D / 3D);

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
            return 1 - (level * SKILL_STAMINA_DRAIN_IMPACT_PER_LEVEL);
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
            return GetLiquidationValuePercentage(EquivalentExchange.instance.currentPlayerData.AlchemyLevel);
        }

        //get the coefficient for item price for transmutation
        public static double GetTransmutationMarkupPercentage()
        {
            return GetTransmutationMarkupPercentage(EquivalentExchange.instance.currentPlayerData.AlchemyLevel);
        }

        //the chance a player will fail to transmute/liquidate an item
        public static double GetReboundChance(bool isItemWorthLessThanOnePercentOfMoney, bool isLiquidate)
        {
            if (isItemWorthLessThanOnePercentOfMoney && isLiquidate && Game1.player.professions.Contains(Professions.Aurumancer))
                return 0.0D;

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

        internal static double GetTransmutationMarkupPercentage(int whichLevel)
        {
            //base of 3.0 - 0.1 per skill level - profession modifiers
            return 1.0D;
            //return BASE_COST_COEFFICIENT - (TRANSMUTATION_BONUS_PER_LEVEL * whichLevel);
        }

        internal static double GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses(int whichLevel, int luckLevel)
        {
            return (luckLevel * LUCK_FREE_TRANSMUTE_IMPACT) + (whichLevel * SKILL_FREE_TRANSMUTE_IMPACT);
        }

        internal static double GetLiquidationValuePercentage(int whichLevel)
        {
            //base of 0.5 + 0.03 per skill level + profession modifiers
            return 1.0D;
            //return BASE_VALUE_COEFFICIENT + (LIQUIDATION_BONUS_PER_LEVEL * whichLevel);
        }

        //check if the player failed a rebound check
        public static bool DidPlayerFailReboundCheck(bool isItemWorthLessThanOnePercentOfMoney, bool isLiquidate)
        {             
            return alchemyRandom.NextDouble() <= GetReboundChance(isItemWorthLessThanOnePercentOfMoney, isLiquidate);
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
        public static bool CanSurviveRebound(int itemValue, bool isItemWorthLessThanOnePercentOfMoney, bool isLiquidate)
        {
            GameLocation currentLocation = Game1.player.currentLocation;
            //automatically pass the chance if your current rebound chance is essentially zero.
            if (GetReboundChance(isItemWorthLessThanOnePercentOfMoney, isLiquidate) <= 0.0D)
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

            return Math.Min(1D, (luckFactor + dailyLuck) * (Game1.player.professions.Contains(Professions.Transmuter) ? 2D : 1D));
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
        public static void HandleStaminaDeduction(double staminaCost, bool isItemWorthLessThanOnePercentOfMoney)
        {
            if (IsLuckyTransmute())
                return;

            if (Game1.player.professions.Contains(Professions.Conduit) && isItemWorthLessThanOnePercentOfMoney)
                return;

            double remainingStaminaCost = staminaCost;

            //if you have any alkahestry energy, it will try to use as much as it can
            double alkahestryCost = Math.Min(Alchemy.GetCurrentAlkahestryEnergy(), staminaCost);

            //and deduct that from whatever stamina cost might be left over (which may be all of it)
            remainingStaminaCost -= alkahestryCost;

            Alchemy.ReduceAlkahestryEnergy(alkahestryCost);
            
            Game1.player.Stamina -= (float)remainingStaminaCost;                        
        }

        public static void HandleTransmuteEvent(Item heldItem, int actualValue)
        {
            //cost of a single item, multiplied by the cost multiplier below
            int transmutationCost = (int)Math.Ceiling(Alchemy.GetTransmutationMarkupPercentage() * actualValue);

            //nor should totalCost of a single cycle
            int totalCost = transmutationCost;

            //placeholder for determining if the transmute occurs, so it knows to play a sound.
            bool didTransmuteOccur = false;

            //placeholder for determining if the transmute rebounds, so it knows to play a different sound.
            bool didTransmuteFail = false;

            //if the transmute did fail, this preserves the damage so we can apply it in one cycle, otherwise batches look weird af
            int reboundDamageTaken = 0;

            //needed for some profession effects
            bool isItemWorthLessThanOnePercentOfMoney = (Game1.player.money * 0.01F > actualValue);

            //stamina cost is overridden for conduits if money is > 100x the item's value.
            double staminaCost = (isItemWorthLessThanOnePercentOfMoney && Game1.player.professions.Contains(Professions.Conduit)) ? 0D : Alchemy.GetStaminaCostForTransmutation(actualValue);

            //loop for each transmute-cycle attempt
            if (Game1.player.money >= totalCost)
            {                
                //if the player lacks the stamina to execute a transmute, abort
                if (Game1.player.Stamina - 1F + Alchemy.GetCurrentAlkahestryEnergy() <= staminaCost)
                {
                    return;
                }

                //if we fail this check, it's because a rebound would kill the player.
                //if the rebound chance is zero, this check will automatically pass.
                if (!Alchemy.CanSurviveRebound(actualValue, isItemWorthLessThanOnePercentOfMoney, false))
                {
                    return;
                }

                //if we fail this check, transmutation will fail this cycle.
                //this is our "rebound check"
                if (Alchemy.DidPlayerFailReboundCheck(isItemWorthLessThanOnePercentOfMoney, false))
                {
                    reboundDamageTaken += Alchemy.GetReboundDamage(actualValue);
                    didTransmuteFail = true;
                    //the conduit profession makes it so that the transmutation succeeds anyway, after taking damage.

                }
                if (Game1.player.professions.Contains((int)Professions.Sage) || !didTransmuteFail)
                {

                    didTransmuteOccur = true;                    
                    
                    Alchemy.HandleStaminaDeduction(staminaCost, true);

                    Game1.player.Money -= totalCost;

                    Alchemy.IncreaseTotalTransmuteValue(totalCost);

                    Item spawnedItem = heldItem.getOne();

                    Game1.createItemDebris(spawnedItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection, (GameLocation)null);

                    //the percentage of experience you get is increased by the lossiness of the transmute
                    //as you increase in levels, this amount diminishes to a minimum of 1.
                    double experienceValueCoefficient = Alchemy.GetTransmutationMarkupPercentage() - 1D;
                    int experienceValue = (int)Math.Floor(Math.Sqrt(experienceValueCoefficient * actualValue / 10 + 1));

                    Alchemy.AddAlchemyExperience(experienceValue);

                }
            }

            //a transmute (at least one) happened, play the magicky sound
            if (didTransmuteOccur && !didTransmuteFail)
                SoundUtil.PlayMagickySound();

            //a rebound occurred, apply the damage and also play the ouchy sound.
            if (didTransmuteFail)
            {
                Alchemy.TakeDamageFromRebound(reboundDamageTaken);
                SoundUtil.PlayReboundSound();
            }
        }

        public static void HandleLiquidateEvent(Item heldItem, int actualValue)
        {
            //if the player is holding only one item, don't let them transmute it to money unless they're holding shift.
            if (heldItem.Stack == 1 && !EquivalentExchange.IsShiftKeyPressed())
                return;

            //placeholder for determining if the transmute occurs, so it knows to play a sound.
            bool didTransmuteOccur = false;

            //placeholder for determining if the transmute rebounds, so it knows to play a different sound.
            bool didTransmuteFail = false;

            //if the transmute did fail, this preserves the damage so we can apply it in one cycle, otherwise batches look weird af
            int reboundDamageTaken = 0;

            //needed for some profession effects
            bool isItemWorthLessThanOnePercentOfMoney = (Game1.player.money * 0.01F > actualValue);
            
            //stamina cost is overridden for conduits if money is > 100x the item's value.
            double staminaCost = (isItemWorthLessThanOnePercentOfMoney && Game1.player.professions.Contains(Professions.Conduit)) ? 0D : Alchemy.GetStaminaCostForTransmutation(actualValue);

            //if the player lacks the stamina to execute a transmute, abort
            if (Game1.player.Stamina - 1F + Alchemy.GetCurrentAlkahestryEnergy() <= staminaCost)
            {
                return;
            }

            //if we fail this check, it's because a rebound would kill the player.
            //if the rebound chance is zero, this check will automatically pass.
            if (!Alchemy.CanSurviveRebound(actualValue, isItemWorthLessThanOnePercentOfMoney, true))
            {
                return;
            }

            //if we fail this check, transmutation will fail this cycle.
            //this is our "rebound check"
            if (Alchemy.DidPlayerFailReboundCheck(isItemWorthLessThanOnePercentOfMoney, true))
            {
                reboundDamageTaken += Alchemy.GetReboundDamage(actualValue);
                didTransmuteFail = true;
            }

            //the conduit profession makes it so that the transmutation succeeds anyway, after taking damage.
            if (Game1.player.professions.Contains((int)Professions.Sage) || !didTransmuteFail)
            {

                //if we reached this point transmutation will succeed
                didTransmuteOccur = true;
                
                Alchemy.HandleStaminaDeduction(staminaCost, false);

                //we floor the math here because we don't want weirdly divergent values based on stack count - the rate is fixed regardless of quantity
                //this occurs at the expense of rounding - liquidation is lossy.
                int liquidationValue = (int)Math.Floor(Alchemy.GetLiquidationValuePercentage() * actualValue);

                int totalValue = liquidationValue;

                Game1.player.Money += totalValue;

                Game1.player.reduceActiveItemByOne();

                Alchemy.IncreaseTotalTransmuteValue(totalValue);

                //the percentage of experience you get is increased by the lossiness of the transmute
                //as you increase in levels, this amount diminishes to a minimum of 1.
                double experienceValueCoefficient = 1D - Alchemy.GetLiquidationValuePercentage();
                int experienceValue = (int)Math.Floor(Math.Sqrt(experienceValueCoefficient * actualValue / 10 + 1));

                Alchemy.AddAlchemyExperience(experienceValue);

                //a transmute (at least one) happened, play the cash money sound
                if (didTransmuteOccur && !didTransmuteFail)
                    SoundUtil.PlayMoneySound();
            }

            //a rebound occurred, apply the damage and also play the ouchy sound.
            if (didTransmuteFail)
            {

                Alchemy.TakeDamageFromRebound(reboundDamageTaken);
                SoundUtil.PlayReboundSound();
            }
        }

        public static float GetCurrentAlkahestryEnergy()
        {
            return EquivalentExchange.instance.currentPlayerData.AlkahestryCurrentEnergy;
        }

        public static float GetMaxAlkahestryEnergy()
        {
            return EquivalentExchange.instance.currentPlayerData.AlkahestryMaxEnergy;
        }

        public static void IncreaseTotalTransmuteValue (int transmuteValue)
        {
            EquivalentExchange.instance.currentPlayerData.TotalValueTransmuted += transmuteValue;
            EquivalentExchange.instance.currentPlayerData.AlkahestryMaxEnergy = (int)Math.Floor(Math.Sqrt(EquivalentExchange.instance.currentPlayerData.TotalValueTransmuted));
        }

        public static void ReduceAlkahestryEnergy(double energyCost)
        {
            EquivalentExchange.instance.currentPlayerData.AlkahestryCurrentEnergy -= (float)energyCost;
        }

        internal static void RestoreAlkahestryEnergyForNewDay()
        {
            EquivalentExchange.instance.currentPlayerData.AlkahestryCurrentEnergy = EquivalentExchange.instance.currentPlayerData.AlkahestryMaxEnergy;
        }

        public static void HandleNormalizeEvent(Item heldItem, int actualValue)
        {
            //get the id of the item the player is holding
            int itemID = heldItem.parentSheetIndex;

            //if it's a blacklisted item, abort.
            if (EquivalentExchange.blackListedItemIDs.Contains(itemID))
                return;

            //declare vars to remember how many items of each quality the player has.
            float normalQuality = 0;
            int silverQuality = 0;
            int goldQuality = 0;
            int iridiumQuality = 0;

            //search the inventory for items of the same type
            foreach (Item inventoryItem in Game1.player.items)
            {
                if (inventoryItem == null)
                    continue;

                if (inventoryItem.parentSheetIndex != itemID)
                    continue;

                //if the item can't be cast as an object, abort.
                StardewValley.Object itemObject = inventoryItem as StardewValley.Object;
                if (itemObject == null)
                    return;
                       
                switch (itemObject.quality)
                {
                    case 0:
                        normalQuality += itemObject.Stack;
                        break;
                    case 1:
                        silverQuality += itemObject.Stack;
                        break;
                    case 2:
                        goldQuality += itemObject.Stack;
                        break;
                    case 4:
                        iridiumQuality += itemObject.Stack;
                        break;
                    default:
                        break;                    
                }
            }

            //destroy all the items
            while (Game1.player.hasItemInInventory(itemID, 1))
            {
                Game1.player.removeFirstOfThisItemFromInventory(itemID);
            }

            //calculate the normalized value of all qualities
            normalQuality += (5F / 4F) * silverQuality;
            normalQuality += (3F / 2F) * goldQuality;
            normalQuality += (2F) * iridiumQuality;

            float remainder = normalQuality % 1F;
            normalQuality -= normalQuality % 1F;

            StardewValley.Object newItemObject = new StardewValley.Object(itemID, 1);

            //the remainder is liquidated, and the liquidation factor of your skill level is applied.
            remainder *= newItemObject.sellToStorePrice() * (float)Alchemy.GetLiquidationValuePercentage();

            while (normalQuality > 0)
            {
                newItemObject = new StardewValley.Object(itemID, 1);

                if (Game1.player.couldInventoryAcceptThisItem((Item)newItemObject))
                {
                    Game1.player.addItemToInventory((Item)newItemObject);
                } else
                {
                    Game1.createItemDebris((Item)newItemObject, Game1.player.getStandingPosition(), Game1.player.FacingDirection, (GameLocation)null);
                }
                normalQuality--;
            }

            //floored, any excess is truncated. Sorry math.
            Game1.player.Money += (int)Math.Floor(remainder);
        }

        //this was almost entirely stolen from spacechase0 with very little contribution on my part.
        internal static void HandleToolTransmute(Tool tool)
        {
            int alchemyLevel = (int)Math.Floor(Math.Round(EquivalentExchange.instance.currentPlayerData.AlchemyLevel / 3D));
            int toolLevel = tool.UpgradeLevel;

            //set last user to dodge a null pointer
            var toolPlayerFieldReflector = tool.GetType().GetField("lastUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            toolPlayerFieldReflector.SetValue(tool, Game1.player);
            
            //I wrote this part, basically.
            double mouseX = (double)(Game1.getMouseX() + Game1.viewport.X - Game1.player.getStandingX());
            double mouseY = (double)(Game1.getMouseY() + Game1.viewport.Y - Game1.player.getStandingY());

            //figure out where the cursor position should be, relative to the player.
            Point hitLocation = new Point((int)Math.Round((mouseX + Game1.player.getStandingX() - (Game1.tileSize / 2)) / Game1.tileSize), (int)Math.Round((mouseY + Game1.player.getStandingY() - (Game1.tileSize / 2)) / Game1.tileSize));            

            GameLocation currentPlayerLocation = Game1.player.currentLocation;

            bool performedAction = false;

            for (int xOffset = -alchemyLevel; xOffset <= alchemyLevel; xOffset++)
            {
                for (int yOffset = -alchemyLevel; yOffset <= alchemyLevel; yOffset++)
                {
                    if (Alchemy.GetCurrentAlkahestryEnergy() + Game1.player.Stamina - 2F <= 0)
                        return;
                    
                    Vector2 offsetPosition = new Vector2(xOffset + hitLocation.X, yOffset + hitLocation.Y);

                    if (currentPlayerLocation.objects.ContainsKey(offsetPosition))
                    {
                        StardewValley.Object hitObject = currentPlayerLocation.objects[offsetPosition];
                        if (hitObject.performToolAction(tool))
                        {
                            if (tool is StardewValley.Tools.Axe)
                            {
                                //stolen from spacechase0, I don't know what this means..
                                if (hitObject.type == "Crafting" && hitObject.fragility != 2)
                                {
                                    //unclear what this does yet.
                                    currentPlayerLocation.debris.Add(new Debris(hitObject.bigCraftable ? -hitObject.parentSheetIndex : hitObject.parentSheetIndex, offsetPosition, offsetPosition));
                                }
                                hitObject.performRemoveAction(offsetPosition, currentPlayerLocation);
                                currentPlayerLocation.objects.Remove(offsetPosition);
                                Alchemy.HandleStaminaDeduction(1, false);
                                Alchemy.AddAlchemyExperience(1);
                                Alchemy.IncreaseTotalTransmuteValue(1);
                                SoundUtil.PlayMagickySound();
                                performedAction = true;
                            } else if(tool is StardewValley.Tools.Pickaxe)
                            {
                                var oldStamina = Game1.player.stamina;
                                tool.DoFunction(currentPlayerLocation, (int)offsetPosition.X * Game1.tileSize, (int)offsetPosition.Y * Game1.tileSize, 0, Game1.player);
                                //restore stamina prior to pickaxe function
                                Game1.player.stamina = oldStamina;
                                Alchemy.HandleStaminaDeduction(1, false);
                                Alchemy.AddAlchemyExperience(1);
                                Alchemy.IncreaseTotalTransmuteValue(1);
                                performedAction = true;
                            }
                        }
                    }
                }
            }

            if (performedAction)
            {
                SoundUtil.PlayMagickySound();
            }
        }
    }
}
