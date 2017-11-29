﻿using System;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Reflection;
using StardewValley.Locations;
//using xTile.ObjectModel;
//using xTile.Dimensions;

namespace EquivalentExchange
{
    public class Alchemy
    {
        //constants for storing some important formula values as non-magic numbers, this is the impact level ups and other factors have on formulas, stored in constants for easy edits.
        //public const double TRANSMUTATION_BONUS_PER_LEVEL = 0.2D;
        //public const double LIQUIDATION_BONUS_PER_LEVEL = 0.02D;
        public const double SKILL_STAMINA_DRAIN_IMPACT_PER_LEVEL = 0.08D;
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

        //the cost of executing a single action of a tool transmutation
        public static int BASE_TOOL_TRANSMUTE_COST_PER_ACTION = 5;


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
        public static double GetAlchemyEnergyCostSkillMultiplierForLevel(int level)
        {
            //base of 1 - 0.08 per skill level - profession modifiers
            return 1 - (level * SKILL_STAMINA_DRAIN_IMPACT_PER_LEVEL);
        }

        //get the coefficient for stamina drain
        public static double GetAlchemyEnergyCostSkillMultiplier()
        {
            //base of 1 - 0.075 per skill level - profession modifiers
            return GetAlchemyEnergyCostSkillMultiplierForLevel(EquivalentExchange.instance.currentPlayerData.AlchemyLevel);
        }

        //algorithm to return stamina cost for the act of transmuting/liquidating an item, based on player skill and item value
        public static double GetStaminaCostForTransmutation(int itemValue)
        {
            return Math.Sqrt(itemValue) * GetAlchemyEnergyCostSkillMultiplier();
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
            distanceFactor *= MAP_DISTANCE_FACTOR;

            //calculate luck's impact on rebound
            double luckFactor = Math.Max(0, (Game1.player.LuckLevel * LUCK_REBOUND_IMPACT) + Game1.dailyLuck);

            return Math.Max(0, (BASE_REBOUND_RATE + distanceFactor) - luckFactor);
        }

        internal static double GetTransmutationMarkupPercentage(int whichLevel)
        {
            //base of 3.0 - 0.1 per skill level - profession modifiers
            return 2.0D;
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
        public static void HandleAlchemyEnergyDeduction(double staminaCost, bool isItemWorthLessThanOnePercentOfMoney)
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
                    
                    Alchemy.HandleAlchemyEnergyDeduction(staminaCost, true);

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
                
                Alchemy.HandleAlchemyEnergyDeduction(staminaCost, false);

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
            EquivalentExchange.instance.currentPlayerData.AlkahestryCurrentEnergy = Alchemy.GetMaxAlkahestryEnergy();
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

        public static int GetToolTransmuteRadius()
        {
            if (EquivalentExchange.IsShiftKeyPressed())
                return 0;
            return (int)Math.Floor(EquivalentExchange.instance.currentPlayerData.AlchemyLevel / 3D);
        }

        //this was almost entirely stolen from spacechase0 with very little contribution on my part.
        internal static void HandleToolTransmute(Tool tool)
        {
            int alchemyLevel = (EquivalentExchange.IsShiftKeyPressed() ? 0 : Alchemy.GetToolTransmuteRadius());
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

            //getting this out of the way, helps with easily determining tool types
            bool isScythe = tool is MeleeWeapon && tool.name.ToLower().Contains("scythe");
            bool isAxe = tool is StardewValley.Tools.Axe;
            bool isPickaxe = tool is StardewValley.Tools.Pickaxe;
            bool isHoe = tool is StardewValley.Tools.Hoe;
            bool isWateringCan = tool is StardewValley.Tools.WateringCan;

            for (int xOffset = -alchemyLevel; xOffset <= alchemyLevel; xOffset++)
            {
                for (int yOffset = -alchemyLevel; yOffset <= alchemyLevel; yOffset++)
                {
                    if (!isScythe)
                    {
                        if (Alchemy.GetCurrentAlkahestryEnergy() + Game1.player.Stamina - (GetToolTransmutationEnergyCost() + 1) <= 0)
                            return;
                    }

                    Vector2 offsetPosition = new Vector2(xOffset + hitLocation.X, yOffset + hitLocation.Y);

                    if (currentPlayerLocation.objects.ContainsKey(offsetPosition))
                    {
                        if (isAxe || isScythe || isPickaxe || isHoe)
                        {
                            var snapshotPlayerExperience = Game1.player.experiencePoints;
                            performedAction = DoToolFunction(currentPlayerLocation, Game1.player, tool, (int)offsetPosition.X, (int)offsetPosition.Y);
                            RestorePlayerExperience(snapshotPlayerExperience);
                            if (performedAction && !isScythe)
                            {
                                HandleToolTransmuteConsequence();
                            }
                            
                        }
                    }
                    else if (currentPlayerLocation.terrainFeatures.ContainsKey(offsetPosition))
                    {
                        //a terrain feature, rather than a tool check, might respond to the tool
                        TerrainFeature terrainFeature = currentPlayerLocation.terrainFeatures[offsetPosition];

                        //don't break stumps unless the player is in precision mode.
                        if (terrainFeature is Tree && isAxe && (!(terrainFeature as Tree).stump || EquivalentExchange.IsShiftKeyPressed()))
                        {
                            var snapshotPlayerExperience = Game1.player.experiencePoints;
                            //trees get removed automatically
                            performedAction = DoToolFunction(currentPlayerLocation, Game1.player, tool, (int)offsetPosition.X, (int)offsetPosition.Y);
                            RestorePlayerExperience(snapshotPlayerExperience);

                            if (performedAction)
                            {                            
                                HandleToolTransmuteConsequence();
                            }
                        }
                        else if (terrainFeature is Grass && currentPlayerLocation is Farm && isScythe)
                        {
                            int oldHay = (currentPlayerLocation as Farm).piecesOfHay;
                            var snapshotPlayerExperience = Game1.player.experiencePoints;
                            if (terrainFeature.performToolAction(tool, 0, offsetPosition))
                            {
                                currentPlayerLocation.terrainFeatures.Remove(offsetPosition);
                                //HandleToolTransmuteConsequence(); Scythe transmute is special and doesn't cost anything, but you don't get experience.
                                performedAction = true;
                            }
                            RestorePlayerExperience(snapshotPlayerExperience);

                            //hay get! spawn the sprite animation for acquisition of hay
                            if (oldHay < (currentPlayerLocation as Farm).piecesOfHay)
                            {
                                SpawnHayAnimationSprite(currentPlayerLocation, offsetPosition, Game1.player);
                            }
                        }
                        else if (terrainFeature is HoeDirt && isWateringCan && (tool as WateringCan).WaterLeft > 0)
                        {
                            //state of 0 is unwatered.
                            if ((terrainFeature as HoeDirt).state != 1)
                            {
                                var snapshotPlayerExperience = Game1.player.experiencePoints;
                                terrainFeature.performToolAction(tool, 0, offsetPosition);
                                RestorePlayerExperience(snapshotPlayerExperience);
                                (tool as WateringCan).WaterLeft = (tool as WateringCan).WaterLeft - 1;
                                SpawnWateringCanAnimationSprite(currentPlayerLocation, offsetPosition);
                                HandleToolTransmuteConsequence();
                                performedAction = true;
                            }
                        }
                        else if (isPickaxe && terrainFeature is HoeDirt)
                        {
                            var snapshotPlayerExperience = Game1.player.experiencePoints;
                            performedAction = DoToolFunction(currentPlayerLocation, Game1.player, tool, (int)offsetPosition.X, (int)offsetPosition.Y);
                            RestorePlayerExperience(snapshotPlayerExperience);

                            if (performedAction)
                            {
                                HandleToolTransmuteConsequence();
                            }                         
                        }
                    }
                    else if ((isPickaxe || isAxe))
                    {
                        var largeResourceClusters = (List<ResourceClump>)currentPlayerLocation.GetType().GetField("resourceClumps", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(currentPlayerLocation);
                        if (currentPlayerLocation is Woods)
                            largeResourceClusters = (currentPlayerLocation as Woods).stumps;
                        if (largeResourceClusters != null)
                        {
                            foreach (var resourceCluster in largeResourceClusters)
                            {
                                if (new Rectangle((int)resourceCluster.tile.X, (int)resourceCluster.tile.Y, resourceCluster.width, resourceCluster.height).Contains((int)offsetPosition.X, (int)offsetPosition.Y))
                                {
                                    if (TryToDestroyResourceCluster(resourceCluster, tool, 1, offsetPosition))
                                    {
                                        performedAction = true;
                                        if (resourceCluster.health <= 0)
                                        {
                                            largeResourceClusters.Remove(resourceCluster);
                                        }
                                    }

                                    if (performedAction)
                                    {
                                        HandleToolTransmuteConsequence();
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    else if (isHoe)
                    {
                        var snapshotPlayerExperience = Game1.player.experiencePoints;
                        performedAction = DoToolFunction(currentPlayerLocation, Game1.player, tool, (int)offsetPosition.X, (int)offsetPosition.Y);
                        RestorePlayerExperience(snapshotPlayerExperience);

                        if (performedAction)
                        {
                            HandleToolTransmuteConsequence();
                        }
                    }
                }
            }

            if (performedAction)
            {
                SoundUtil.PlayMagickySound();
            }
        }

        private static void RestorePlayerExperience(int[] snapshotPlayerExperience)
        {
            for (int i = 0; i < Game1.player.experiencePoints.Length; i++)
            {
                Game1.player.experiencePoints[i] = snapshotPlayerExperience[i];
            }
        }

        //this is a cold copy of the performToolAction from resource cluster with all the dialog warnings removed
        //using reflection to set the shake timer since it's private.
        private static bool TryToDestroyResourceCluster(ResourceClump resourceCluster, Tool tool, int damage, Vector2 offsetPosition)
        {
            if (resourceCluster.tile != offsetPosition)
            {
                offsetPosition = resourceCluster.tile;
            }
            bool performedAction = false;
            if (tool == null)
                return performedAction;
            int debrisType = 12;
            switch (resourceCluster.parentSheetIndex)
            {
                case 622:
                    if (tool is Pickaxe && tool.upgradeLevel < 3)
                    {
                        Game1.playSound("clubhit");
                        Game1.playSound("clank");
                        Game1.player.jitterStrength = 1f;
                        return performedAction;
                    }
                    if (!(tool is Pickaxe))
                        return false;
                    Game1.playSound("hammer");
                    debrisType = 14;
                    break;
                case 672:
                    if (tool is Pickaxe && tool.upgradeLevel < 2)
                    {
                        Game1.playSound("clubhit");
                        Game1.playSound("clank");
                        Game1.player.jitterStrength = 1f;
                        return performedAction;
                    }
                    if (!(tool is Pickaxe))
                        return performedAction;
                    Game1.playSound("hammer");
                    debrisType = 14;
                    break;
                case 752:
                case 754:
                case 756:
                case 758:
                    if (!(tool is Pickaxe))
                        return performedAction;
                    Game1.playSound("hammer");
                    debrisType = 14;
                    
                    //set shake timer with reflection, because it is private.
                    var resourceClusterShakeTimerFieldReflector = resourceCluster.GetType().GetField("shakeTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    resourceClusterShakeTimerFieldReflector.SetValue(resourceCluster, 500F);
                    break;
                case 600:
                    if (tool is Axe && tool.upgradeLevel < 1)
                    {
                        Game1.playSound("axe");
                        Game1.player.jitterStrength = 1f;
                        return performedAction;
                    }
                    if (!(tool is Axe))
                        return performedAction;
                    Game1.playSound("axchop");
                    break;
                case 602:
                    if (tool is Axe && tool.upgradeLevel < 2)
                    {
                        Game1.playSound("axe");
                        Game1.player.jitterStrength = 1f;
                        return performedAction;
                    }
                    if (!(tool is Axe))
                        return performedAction;
                    Game1.playSound("axchop");
                    break;
            }
            performedAction = true;
            resourceCluster.health = resourceCluster.health - Math.Max(1f, (float)(tool.upgradeLevel + 1) * 0.75f);
            Game1.createRadialDebris(Game1.currentLocation, debrisType, (int)offsetPosition.X + Game1.random.Next(resourceCluster.width / 2 + 1), (int)offsetPosition.Y + Game1.random.Next(resourceCluster.height / 2 + 1), Game1.random.Next(4, 9), false, -1, false, -1);
            if ((double)resourceCluster.health <= 0.0)
            {
                if (Game1.IsMultiplayer)
                {
                    Random multiplayerRandom1 = Game1.recentMultiplayerRandom;
                }
                else
                {
                    Random random1 = new Random((int)((double)Game1.uniqueIDForThisGame + (double)offsetPosition.X * 7.0 + (double)offsetPosition.Y * 11.0 + (double)Game1.stats.DaysPlayed + (double)resourceCluster.health));
                }
                switch (resourceCluster.parentSheetIndex)
                {
                    case 622:
                        int number1 = 6;
                        if (Game1.IsMultiplayer)
                        {
                            Game1.recentMultiplayerRandom = new Random((int)offsetPosition.X * 1000 + (int)offsetPosition.Y);
                            Random multiplayerRandom2 = Game1.recentMultiplayerRandom;
                        }
                        else
                        {
                            Random random2 = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed + (int)offsetPosition.X * 7 + (int)offsetPosition.Y * 11);
                        }
                        if (Game1.IsMultiplayer)
                        {
                            Game1.createMultipleObjectDebris(386, (int)offsetPosition.X, (int)offsetPosition.Y, number1, tool.getLastFarmerToUse().uniqueMultiplayerID);
                            Game1.createMultipleObjectDebris(390, (int)offsetPosition.X, (int)offsetPosition.Y, number1, tool.getLastFarmerToUse().uniqueMultiplayerID);
                            Game1.createMultipleObjectDebris(535, (int)offsetPosition.X, (int)offsetPosition.Y, 2, tool.getLastFarmerToUse().uniqueMultiplayerID);
                        }
                        else
                        {
                            Game1.createMultipleObjectDebris(386, (int)offsetPosition.X, (int)offsetPosition.Y, number1);
                            Game1.createMultipleObjectDebris(390, (int)offsetPosition.X, (int)offsetPosition.Y, number1);
                            Game1.createMultipleObjectDebris(535, (int)offsetPosition.X, (int)offsetPosition.Y, 2);
                        }
                        Game1.playSound("boulderBreak");
                        Game1.createRadialDebris(Game1.currentLocation, 32, (int)offsetPosition.X, (int)offsetPosition.Y, Game1.random.Next(6, 12), false, -1, false, -1);
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(5, offsetPosition * (float)Game1.tileSize, Color.White, 8, false, 100f, 0, -1, -1f, -1, 0));
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(5, (offsetPosition + new Vector2(1f, 0.0f)) * (float)Game1.tileSize, Color.White, 8, false, 110f, 0, -1, -1f, -1, 0));
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(5, (offsetPosition + new Vector2(1f, 1f)) * (float)Game1.tileSize, Color.White, 8, true, 80f, 0, -1, -1f, -1, 0));
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(5, (offsetPosition + new Vector2(0.0f, 1f)) * (float)Game1.tileSize, Color.White, 8, false, 90f, 0, -1, -1f, -1, 0));
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(5, offsetPosition * (float)Game1.tileSize + new Vector2((float)(Game1.tileSize / 2), (float)(Game1.tileSize / 2)), Color.White, 8, false, 70f, 0, -1, -1f, -1, 0));
                        return performedAction;
                    case 672:
                    case 752:
                    case 754:
                    case 756:
                    case 758:
                        int num = resourceCluster.parentSheetIndex == 672 ? 15 : 10;
                        if (Game1.IsMultiplayer)
                        {
                            Game1.recentMultiplayerRandom = new Random((int)offsetPosition.X * 1000 + (int)offsetPosition.Y);
                            Random multiplayerRandom2 = Game1.recentMultiplayerRandom;
                        }
                        else
                        {
                            Random random3 = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed + (int)offsetPosition.X * 7 + (int)offsetPosition.Y * 11);
                        }
                        if (Game1.IsMultiplayer)
                            Game1.createMultipleObjectDebris(390, (int)offsetPosition.X, (int)offsetPosition.Y, num, tool.getLastFarmerToUse().uniqueMultiplayerID);
                        else
                            Game1.createRadialDebris(Game1.currentLocation, 390, (int)offsetPosition.X, (int)offsetPosition.Y, num, false, -1, true, -1);
                        Game1.playSound("boulderBreak");
                        Game1.createRadialDebris(Game1.currentLocation, 32, (int)offsetPosition.X, (int)offsetPosition.Y, Game1.random.Next(6, 12), false, -1, false, -1);
                        Color color = Color.White;
                        switch (resourceCluster.parentSheetIndex)
                        {
                            case 752:
                                color = new Color(188, 119, 98);
                                break;
                            case 754:
                                color = new Color(168, 120, 95);
                                break;
                            case 756:
                            case 758:
                                color = new Color(67, 189, 238);
                                break;
                        }
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(48, offsetPosition * (float)Game1.tileSize, color, 5, false, 180f, 0, Game1.tileSize * 2, -1f, Game1.tileSize * 2, 0)
                        {
                            alphaFade = 0.01f
                        });
                        return performedAction;
                    case 600:
                    case 602:
                        int number2 = resourceCluster.parentSheetIndex == 602 ? 8 : 2;
                        if (Game1.IsMultiplayer)
                        {
                            Game1.recentMultiplayerRandom = new Random((int)offsetPosition.X * 1000 + (int)offsetPosition.Y);
                            Random multiplayerRandom2 = Game1.recentMultiplayerRandom;
                        }
                        else
                        {
                            Random random4 = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed + (int)offsetPosition.X * 7 + (int)offsetPosition.Y * 11);
                        }
                        if (Game1.IsMultiplayer)
                            Game1.createMultipleObjectDebris(709, (int)offsetPosition.X, (int)offsetPosition.Y, number2, tool.getLastFarmerToUse().uniqueMultiplayerID);
                        else
                            Game1.createMultipleObjectDebris(709, (int)offsetPosition.X, (int)offsetPosition.Y, number2);
                        Game1.playSound("stumpCrack");
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(23, offsetPosition * (float)Game1.tileSize, Color.White, 4, false, 140f, 0, Game1.tileSize * 2, -1f, Game1.tileSize * 2, 0));
                        Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(Game1.animations, new Rectangle(385, 1522, (int)sbyte.MaxValue, 79), 2000f, 1, 1, offsetPosition * (float)Game1.tileSize + new Vector2(0.0f, 49f), false, false, 1E-05f, 0.016f, Color.White, 1f, 0.0f, 0.0f, 0.0f, false));
                        Game1.createRadialDebris(Game1.currentLocation, 34, (int)offsetPosition.X, (int)offsetPosition.Y, Game1.random.Next(4, 9), false, -1, false, -1);
                        return performedAction;
                }
            }
            else
            {
                var resourceClusterShakeTimerFieldReflector = resourceCluster.GetType().GetField("shakeTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                resourceClusterShakeTimerFieldReflector.SetValue(resourceCluster, 100F);
            }
            return performedAction;        
        }

        private static void SpawnWateringCanAnimationSprite(GameLocation currentPlayerLocation, Vector2 offsetPosition)
        {
            currentPlayerLocation.temporarySprites.Add(new TemporaryAnimatedSprite(13, new Vector2(offsetPosition.X * (float)Game1.tileSize, offsetPosition.Y * (float)Game1.tileSize), Color.White, 10, Game1.random.NextDouble() < 0.5, 70f, 0, Game1.tileSize, (float)(((double)offsetPosition.Y * (double)Game1.tileSize + (double)(Game1.tileSize / 2)) / 10000.0 - 0.00999999977648258), -1, 0));
        }

        private static void SpawnHayAnimationSprite(GameLocation currentPlayerLocation, Vector2 offsetPosition, StardewValley.Farmer player)
        {
            currentPlayerLocation.temporarySprites.Add(new TemporaryAnimatedSprite(28, offsetPosition * (float)Game1.tileSize + new Vector2((float)Game1.random.Next(-Game1.pixelZoom * 4, Game1.pixelZoom * 4), (float)Game1.random.Next(-Game1.pixelZoom * 4, Game1.pixelZoom * 4)), Color.Green, 8, Game1.random.NextDouble() < 0.5, (float)Game1.random.Next(60, 100), 0, -1, -1f, -1, 0));
            currentPlayerLocation.temporarySprites.Add(new TemporaryAnimatedSprite(Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 178, 16, 16), 750f, 1, 0, player.position - new Vector2(0.0f, (float)(Game1.tileSize * 2)), false, false, player.position.Y / 10000f, 0.005f, Color.White, (float)Game1.pixelZoom, -0.005f, 0.0f, 0.0f, false)
            {
                motion = { Y = -1f },
                layerDepth = (float)(1.0 - (double)Game1.random.Next(100) / 10000.0),
                delayBeforeAnimationStart = Game1.random.Next(350)
            });
        }

        private static double GetToolTransmutationEnergyCost()
        {
            return BASE_TOOL_TRANSMUTE_COST_PER_ACTION * GetAlchemyEnergyCostSkillMultiplier();
        }

        private static void HandleToolTransmuteConsequence()
        {            
            Alchemy.HandleAlchemyEnergyDeduction(GetToolTransmutationEnergyCost(), false);
            Alchemy.AddAlchemyExperience(BASE_TOOL_TRANSMUTE_COST_PER_ACTION);
            Alchemy.IncreaseTotalTransmuteValue(BASE_TOOL_TRANSMUTE_COST_PER_ACTION);
        }

        private static bool DoToolFunction(GameLocation location, StardewValley.Farmer who, Tool tool, int x, int y)
        {
            bool performedAction = false;
            Vector2 index = new Vector2(x, y);
            Vector2 vector2 = new Vector2((float)(x + 0.5), (float)(y + 0.5));
            if (tool is MeleeWeapon && tool.name.ToLower().Contains("scythe"))
            {
                var snapshotPlayerExperience = Game1.player.experiencePoints;
                if (location.objects[index] != null)
                {
                    StardewValley.Object hitObject = location.objects[index];
                    if (hitObject.name.Contains("Weed") && hitObject.performToolAction(tool))
                    {
                        if (hitObject.type == "Crafting" && hitObject.fragility != 2)
                        {
                            location.debris.Add(new Debris(hitObject.bigCraftable ? -hitObject.parentSheetIndex : hitObject.parentSheetIndex, index, index));
                        }
                        hitObject.performRemoveAction(index, location);
                        location.objects.Remove(index);
                        performedAction = true;
                    }
                }
                else if (location.terrainFeatures.ContainsKey(index) && location.terrainFeatures[index].performToolAction(tool, 0, index, (GameLocation)null))
                {
                    location.terrainFeatures.Remove(index);
                    performedAction = true;
                }
                RestorePlayerExperience(snapshotPlayerExperience);
            }
            else if (tool is Axe)
            {
                var snapshotPlayerExperience = Game1.player.experiencePoints;
                Rectangle rectangle = new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize);                
                location.performToolAction(tool, x, y);
                if (location.terrainFeatures.ContainsKey(index) && location.terrainFeatures[index] is Tree && PerformTreeTerrainFeatureToolAction(tool, 0, index, location.terrainFeatures[index] as Tree, (GameLocation)null))
                {
                    location.terrainFeatures.Remove(index);
                    performedAction = true;
                }
                Rectangle boundingBox;
                if (location.largeTerrainFeatures != null)
                {
                    for (int index2 = location.largeTerrainFeatures.Count - 1; index2 >= 0; --index2)
                    {
                        boundingBox = location.largeTerrainFeatures[index2].getBoundingBox();
                        if (boundingBox.Intersects(rectangle) && location.largeTerrainFeatures[index2].performToolAction(tool, 0, index, (GameLocation)null))
                        {
                            location.largeTerrainFeatures.RemoveAt(index2);
                        }
                    }
                }
                if (location.terrainFeatures.ContainsKey(index) && location.terrainFeatures[index] is Tree)
                {
                    if (!(location.terrainFeatures[index] as Tree).stump || EquivalentExchange.IsShiftKeyPressed())
                        performedAction = true;
                }
                if (!location.Objects.ContainsKey(index) || location.Objects[index].Type == null || !location.Objects[index].performToolAction(tool))
                    return performedAction;
                if (location.Objects[index].type.Equals("Crafting") && location.Objects[index].fragility != 2)
                {
                    List<Debris> debris1 = location.debris;
                    int objectIndex = location.Objects[index].bigCraftable ? -location.Objects[index].ParentSheetIndex : location.Objects[index].ParentSheetIndex;                    
                    Debris debris2 = new Debris(objectIndex, index, index);
                    debris1.Add(debris2);
                }
                location.Objects[index].performRemoveAction(index, location);
                location.Objects.Remove(index);
                performedAction = true;
                RestorePlayerExperience(snapshotPlayerExperience);
            }
            else if (tool is Pickaxe)
            {
                var snapshotPlayerExperience = Game1.player.experiencePoints;
                int power = who.toolPower;
                if (location.performToolAction(tool, x, y))
                    return true;
                StardewValley.Object objectHit = (StardewValley.Object)null;
                location.Objects.TryGetValue(index, out objectHit);
                if (objectHit == null)
                {
                    if (location.terrainFeatures.ContainsKey(index) && location.terrainFeatures[index].performToolAction(tool, 0, index, (GameLocation)null))
                    {
                        location.terrainFeatures.Remove(index);
                        performedAction = true;
                    }
                }

                if (objectHit != null)
                {
                    if (objectHit.Name.Equals("Stone"))
                    {
                        Game1.playSound("hammer");
                        if (objectHit.minutesUntilReady > 0)
                        {
                            int num3 = Math.Max(1, tool.upgradeLevel + 1);
                            objectHit.minutesUntilReady -= num3;
                            objectHit.shakeTimer = 200;
                            if (objectHit.minutesUntilReady > 0)
                            {
                                Game1.createRadialDebris(Game1.currentLocation, 14, x, y, Game1.random.Next(2, 5), false, -1, false, -1);
                                return performedAction;
                            }
                        }
                        if (objectHit.ParentSheetIndex < 200 && !Game1.objectInformation.ContainsKey(objectHit.ParentSheetIndex + 1))
                            location.TemporarySprites.Add(new TemporaryAnimatedSprite(objectHit.ParentSheetIndex + 1, 300f, 1, 2, new Vector2((float)(x) * Game1.tileSize, (float)(y) * Game1.tileSize), true, objectHit.flipped)
                            {
                                alphaFade = 0.01f
                            });
                        else
                            location.TemporarySprites.Add(new TemporaryAnimatedSprite(47, new Vector2((float)(x * Game1.tileSize), (float)(y * Game1.tileSize)), Color.Gray, 10, false, 80f, 0, -1, -1f, -1, 0));
                        Game1.createRadialDebris(location, 14, x, y, Game1.random.Next(2, 5), false, -1, false, -1);
                        location.TemporarySprites.Add(new TemporaryAnimatedSprite(46, new Vector2((float)(x * Game1.tileSize), (float)(y * Game1.tileSize)), Color.White, 10, false, 80f, 0, -1, -1f, -1, 0)
                        {
                            motion = new Vector2(0.0f, -0.6f),
                            acceleration = new Vector2(0.0f, 1f / 500f),
                            alphaFade = 0.015f
                        });
                        if (!location.Name.Equals("UndergroundMine"))
                        {
                            if (objectHit.parentSheetIndex == 343 || objectHit.parentSheetIndex == 450)
                            {
                                Random random = new Random((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2 + x * 2000 + y);
                                if (random.NextDouble() < 0.035 && Game1.stats.DaysPlayed > 1U)
                                    Game1.createObjectDebris(535 + (Game1.stats.DaysPlayed <= 60U || random.NextDouble() >= 0.2 ? (Game1.stats.DaysPlayed <= 120U || random.NextDouble() >= 0.2 ? 0 : 2) : 1), x, y, tool.getLastFarmerToUse().uniqueMultiplayerID);
                                if (random.NextDouble() < 0.035 * (who.professions.Contains(21) ? 2.0 : 1.0) && Game1.stats.DaysPlayed > 1U)
                                    Game1.createObjectDebris(382, x, y, tool.getLastFarmerToUse().uniqueMultiplayerID);
                                if (random.NextDouble() < 0.01 && Game1.stats.DaysPlayed > 1U)
                                    Game1.createObjectDebris(390, x, y, tool.getLastFarmerToUse().uniqueMultiplayerID);
                            }
                            location.breakStone(objectHit.parentSheetIndex, x, y, who, new Random((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2 + x * 4000 + y));
                        }
                        else
                            Game1.mine.checkStoneForItems(objectHit.ParentSheetIndex, x, y, who);
                        if (objectHit.minutesUntilReady > 0)
                            return performedAction;
                        location.Objects.Remove(index);
                        Game1.playSound("stoneCrack");
                        performedAction = true;
                    }                    
                    else
                    {
                        if (!objectHit.performToolAction(tool))
                        {                            
                            return performedAction;
                        }
                        objectHit.performRemoveAction(index, location);
                        if (objectHit.type.Equals("Crafting") && objectHit.fragility != 2)
                        {
                            List<Debris> debris1 = Game1.currentLocation.debris;
                            int objectIndex = objectHit.bigCraftable ? -objectHit.ParentSheetIndex : objectHit.ParentSheetIndex;
                            Vector2 toolLocation = who.GetToolLocation(false);
                            Rectangle boundingBox = who.GetBoundingBox();
                            double x1 = (double)boundingBox.Center.X;
                            boundingBox = who.GetBoundingBox();
                            double y1 = (double)boundingBox.Center.Y;
                            Vector2 playerPosition = new Vector2((float)x1, (float)y1);
                            Debris debris2 = new Debris(objectIndex, toolLocation, playerPosition);
                            debris1.Add(debris2);
                        }
                        Game1.currentLocation.Objects.Remove(index);
                        performedAction = true;
                    }
                    RestorePlayerExperience(snapshotPlayerExperience);
                }
                else
                {
                    Game1.playSound("woodyHit");
                    if (location.doesTileHaveProperty(x, y, "Diggable", "Back") == null)
                        return false;
                    location.TemporarySprites.Add(new TemporaryAnimatedSprite(12, new Vector2((float)(x * Game1.tileSize), (float)(y * Game1.tileSize)), Color.White, 8, false, 80f, 0, -1, -1f, -1, 0)
                    {
                        alphaFade = 0.015f
                    });
                }
            }
            else if (tool is Hoe)
            {
                var snapshotPlayerExperience = Game1.player.experiencePoints;
                if (location.terrainFeatures.ContainsKey(index))
                {
                    if (location.terrainFeatures[index].performToolAction(tool, 0, index, (GameLocation)null))
                    {
                        location.terrainFeatures.Remove(index);
                        performedAction = true;
                    }
                }
                else
                {
                    if (location.objects.ContainsKey(index) && location.Objects[index].performToolAction(tool))
                    {
                        if (location.Objects[index].type.Equals("Crafting") && location.Objects[index].fragility != 2)
                        {
                            List<Debris> debris1 = location.debris;
                            int objectIndex = location.Objects[index].bigCraftable ? -location.Objects[index].ParentSheetIndex : location.Objects[index].ParentSheetIndex;
                            Vector2 toolLocation = who.GetToolLocation(false);
                            Microsoft.Xna.Framework.Rectangle boundingBox = who.GetBoundingBox();
                            double x1 = (double)boundingBox.Center.X;
                            boundingBox = who.GetBoundingBox();
                            double y1 = (double)boundingBox.Center.Y;
                            Vector2 playerPosition = new Vector2((float)x1, (float)y1);
                            Debris debris2 = new Debris(objectIndex, toolLocation, playerPosition);
                            debris1.Add(debris2);
                        }
                        location.Objects[index].performRemoveAction(index, location);
                        location.Objects.Remove(index);
                        performedAction = true;
                    }
                    if (location.doesTileHaveProperty((int)index.X, (int)index.Y, "Diggable", "Back") != null)
                    {
                        if (location.Name.Equals("UndergroundMine") && !location.isTileOccupied(index, ""))
                        {
                            location.terrainFeatures.Add(index, (TerrainFeature)new HoeDirt());
                            performedAction = true;
                            Game1.removeSquareDebrisFromTile((int)index.X, (int)index.Y);
                            location.checkForBuriedItem((int)index.X, (int)index.Y, false, false);
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(12, new Vector2(vector2.X * (float)Game1.tileSize, vector2.Y * (float)Game1.tileSize), Color.White, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0));
                        }
                        else if (!location.isTileOccupied(index, "") && location.isTilePassable(new xTile.Dimensions.Location((int)index.X, (int)index.Y), Game1.viewport))
                        {
                            location.makeHoeDirt(index);
                            performedAction = true;
                            Game1.removeSquareDebrisFromTile((int)index.X, (int)index.Y);
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(12, new Vector2(index.X * (float)Game1.tileSize, index.Y * (float)Game1.tileSize), Color.White, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0));
                            location.checkForBuriedItem((int)index.X, (int)index.Y, false, false);
                        }
                    }
                }
                RestorePlayerExperience(snapshotPlayerExperience);
            }
            return performedAction;
        }

        //tree terrain feature perform action
        //because it's full of dumb nonsense that makes stupid decisions, this is full of reflection and terrible
        public static bool PerformTreeTerrainFeatureToolAction(Tool t, int explosion, Vector2 tileLocation, Tree tf, GameLocation location = null)
        {
            if (location == null)
                location = Game1.currentLocation;
            if (explosion > 0)
                tf.tapped = false;
            if (tf.tapped)
                return false;
            Console.WriteLine("TREE: IsClient:" + Game1.IsClient.ToString() + " randomOutput: " + (object)Game1.recentMultiplayerRandom.Next(9999));
            if ((double)tf.health <= -99.0)
                return false;
            if (tf.growthStage >= 5)
            {
                if (t != null && t is Axe)
                {
                    Game1.playSound("axchop");
                    //yet another bad debris call
                    Vector2 debrisVector = new Vector2(tileLocation.X * Game1.tileSize + (Game1.tileSize / 2), tileLocation.Y * Game1.tileSize + (Game1.tileSize / 2));
                    location.debris.Add(new Debris(12, Game1.random.Next(1, 3), debrisVector, tileLocation, 0, -1));

                    //set last user hit via reflection
                    var treePlayerToHitReflectorField = tf.GetType().GetField("lastPlayerToHit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    treePlayerToHitReflectorField.SetValue(tf, t.getLastFarmerToUse().uniqueMultiplayerID);
                }
                else if (explosion <= 0)
                    return false;
                //shake via reflection
                typeof(Tree).GetMethod("shake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tf, new object[] { tileLocation, true });
                        
                float num = 1f;
                if (explosion > 0)
                {
                    num = (float)explosion;
                }
                else
                {
                    if (t == null)
                        return false;
                    switch (t.upgradeLevel)
                    {
                        case 0:
                            num = 1f;
                            break;
                        case 1:
                            num = 1.25f;
                            break;
                        case 2:
                            num = 1.67f;
                            break;
                        case 3:
                            num = 2.5f;
                            break;
                        case 4:
                            num = 5f;
                            break;
                    }
                }
                tf.health = tf.health - num;
                if ((double)tf.health <= 0.0)
                {
                    if (!tf.stump)
                    {
                        if ((t != null || explosion > 0) && location.Equals((object)Game1.currentLocation))
                            Game1.playSound("treecrack");
                        tf.stump = true;
                        tf.health = 5f;
                        //set tree falling via reflection
                        var treeFallingFieldReflector = tf.GetType().GetField("falling", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        treeFallingFieldReflector.SetValue(tf, true);

                        //this gets reset...
                        if (t != null)
                            t.getLastFarmerToUse().gainExperience(2, 12);

                        //set tree shake left via reflection
                        var shakeLeft = t == null || t.getLastFarmerToUse() == null || ((double)t.getLastFarmerToUse().getTileLocation().X > (double)tileLocation.X || (double)t.getLastFarmerToUse().getTileLocation().Y < (double)tileLocation.Y && (double)tileLocation.X % 2.0 == 0.0);                    
                        var treeShakeLeftFieldReflector = tf.GetType().GetField("shakeLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        treeFallingFieldReflector.SetValue(tf, shakeLeft);
                    }
                    else
                    {
                        tf.health = -100f;
                        Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(30, 40), false, -1, false, -1);
                        int index = tf.treeType != 7 || (double)tileLocation.X % 7.0 != 0.0 ? (tf.treeType == 7 ? 420 : 92) : 422;
                        if (Game1.IsMultiplayer)
                        {
                            Game1.recentMultiplayerRandom = new Random((int)tileLocation.X * 2000 + (int)tileLocation.Y);
                            Random multiplayerRandom = Game1.recentMultiplayerRandom;
                        }
                        else
                        {
                            Random random = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed + (int)tileLocation.X * 7 + (int)tileLocation.Y * 11);
                        }
                        if (t == null || t.getLastFarmerToUse() == null)
                        {
                            if (location.Equals((object)Game1.currentLocation))
                            {
                                Game1.createMultipleObjectDebris(92, (int)tileLocation.X, (int)tileLocation.Y, 2);
                            }
                            else
                            {
                                Game1.createItemDebris((Item)new StardewValley.Object(92, 1, false, -1, 0), tileLocation * (float)Game1.tileSize, 2, location);
                                Game1.createItemDebris((Item)new StardewValley.Object(92, 1, false, -1, 0), tileLocation * (float)Game1.tileSize, 2, location);
                            }
                        }
                        else if (Game1.IsMultiplayer)
                        {
                            long lastPlayerToHit = (long)tf.GetType().GetField("lastPlayerToHit", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tf);
                            Game1.createMultipleObjectDebris(index, (int)tileLocation.X, (int)tileLocation.Y, 1, lastPlayerToHit);
                            if (tf.treeType != 7)
                                Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, 4, true, -1, false, -1);
                        }
                        else
                        {
                            if (tf.treeType != 7)
                            {
                                //extra wood calculation via reflection
                                int extraWoodCalculator = (int)typeof(Tree).GetMethod("extraWoodCalculator", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tf, new object[] { tileLocation });
                                Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, 5 + extraWoodCalculator, true, -1, false, -1);
                            }
                            Game1.createMultipleObjectDebris(index, (int)tileLocation.X, (int)tileLocation.Y, 1);
                        }
                        if (location.Equals((object)Game1.currentLocation))
                            Game1.playSound("treethud");

                        //reflection to detect if the tree is falling :\
                        bool isFalling = (bool)tf.GetType().GetField("falling", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tf);
                    
                        if (!isFalling)
                            return true;
                    }
                }
            }
            else if (tf.growthStage >= 3)
            {
                if (t != null && t.name.Contains("Ax"))
                {
                    Game1.playSound("axchop");
                    if (tf.treeType != 7)
                        Game1.playSound("leafrustle");
                    //I think this is the line responsible for spawning some stupid particles I don't want, and the whole reason for this
                    //method existing.
                    Vector2 debrisVector = new Vector2(tileLocation.X * Game1.tileSize + (Game1.tileSize / 2), tileLocation.Y * Game1.tileSize + (Game1.tileSize / 2));

                    location.debris.Add(new Debris(12, Game1.random.Next(t.upgradeLevel * 2, t.upgradeLevel * 4), debrisVector, tileLocation, 0, -1));
                }
                else if (explosion <= 0)
                    return false;

                //shake via reflection
                typeof(Tree).GetMethod("shake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tf, new object[] { tileLocation, true });

                float num = 1f;
                if (Game1.IsMultiplayer)
                {
                    Random multiplayerRandom = Game1.recentMultiplayerRandom;
                }
                else
                {
                    Random random = new Random((int)((double)Game1.uniqueIDForThisGame + (double)tileLocation.X * 7.0 + (double)tileLocation.Y * 11.0 + (double)Game1.stats.DaysPlayed + (double)tf.health));
                }
                if (explosion > 0)
                {
                    num = (float)explosion;
                }
                else
                {
                    switch (t.upgradeLevel)
                    {
                        case 0:
                            num = 2f;
                            break;
                        case 1:
                            num = 2.5f;
                            break;
                        case 2:
                            num = 3.34f;
                            break;
                        case 3:
                            num = 5f;
                            break;
                        case 4:
                            num = 10f;
                            break;
                    }
                }
                tf.health = tf.health - num;
                if ((double)tf.health <= 0.0)
                {
                    Game1.createDebris(12, (int)tileLocation.X, (int)tileLocation.Y, 4, (GameLocation)null);
                    Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(20, 30), false, -1, false, -1);
                    return true;
                }
            }
            else if (tf.growthStage >= 1)
            {
                if (explosion > 0)
                    return true;
                if (location.Equals((object)Game1.currentLocation))
                    Game1.playSound("cut");
                if (t != null && t.name.Contains("Axe"))
                {
                    Game1.playSound("axchop");
                    Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(10, 20), false, -1, false, -1);
                }
                if (t is Axe || t is Pickaxe || (t is Hoe || t is MeleeWeapon))
                {
                    Game1.createRadialDebris(location, 12, (int)tileLocation.X, (int)tileLocation.Y, Game1.random.Next(10, 20), false, -1, false, -1);
                    if (t.name.Contains("Axe") && Game1.recentMultiplayerRandom.NextDouble() < (double)t.getLastFarmerToUse().ForagingLevel / 10.0)
                        Game1.createDebris(12, (int)tileLocation.X, (int)tileLocation.Y, 1, (GameLocation)null);
                    location.temporarySprites.Add(new TemporaryAnimatedSprite(17, tileLocation * (float)Game1.tileSize, Color.White, 8, false, 100f, 0, -1, -1f, -1, 0));
                    return true;
                }
            }
            else
            {
                if (explosion > 0)
                    return true;
                if (t.name.Contains("Axe") || t.name.Contains("Pick") || t.name.Contains("Hoe"))
                {
                    Game1.playSound("woodyHit");
                    Game1.playSound("axchop");
                    location.temporarySprites.Add(new TemporaryAnimatedSprite(17, tileLocation * (float)Game1.tileSize, Color.White, 8, false, 100f, 0, -1, -1f, -1, 0));

                    long lastPlayerToHit = (long)tf.GetType().GetField("lastPlayerToHit", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tf);

                    if (lastPlayerToHit != 0L && Game1.getFarmer(lastPlayerToHit).getEffectiveSkillLevel(2) >= 1)
                        Game1.createMultipleObjectDebris(308 + tf.treeType, (int)tileLocation.X, (int)tileLocation.Y, 1, t.getLastFarmerToUse().uniqueMultiplayerID, location);
                    else if (!Game1.IsMultiplayer && Game1.player.getEffectiveSkillLevel(2) >= 1)
                        Game1.createMultipleObjectDebris(308 + tf.treeType, (int)tileLocation.X, (int)tileLocation.Y, 1, t.getLastFarmerToUse().uniqueMultiplayerID, location);
                    return true;
                }
            }
            return false;
        }
    }
}

