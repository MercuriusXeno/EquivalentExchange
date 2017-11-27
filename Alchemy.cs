using System;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
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
                            performedAction = DoToolFunction(currentPlayerLocation, Game1.player, tool, (int)offsetPosition.X, (int)offsetPosition.Y);
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
                            //trees get removed automatically
                            performedAction = DoToolFunction(currentPlayerLocation, Game1.player, tool, (int)offsetPosition.X, (int)offsetPosition.Y);
                            
                            if (performedAction)
                            {                            
                                HandleToolTransmuteConsequence();
                            }
                        }
                        else if (terrainFeature is Grass && currentPlayerLocation is Farm && isScythe)
                        {
                            int oldHay = (currentPlayerLocation as Farm).piecesOfHay;
                            if (terrainFeature.performToolAction(tool, 0, offsetPosition))
                            {
                                currentPlayerLocation.terrainFeatures.Remove(offsetPosition);
                                //HandleToolTransmuteConsequence(); Scythe transmute is special and doesn't cost anything, but you don't get experience.
                                performedAction = true;
                            }

                            //hay get! spawn the sprite animation for acquisition of hay
                            if (oldHay < (currentPlayerLocation as Farm).piecesOfHay)
                            {
                                SpawnHayAnimationSprite(currentPlayerLocation, offsetPosition, Game1.player);
                            }
                        }
                        else if (terrainFeature is HoeDirt && isWateringCan && (tool as WateringCan).WaterLeft > 0)
                        {
                            //state of 0 is unwatered.
                            if ((terrainFeature as HoeDirt).state != 1) {
                                terrainFeature.performToolAction(tool, 0, offsetPosition);
                                (tool as WateringCan).WaterLeft = (tool as WateringCan).WaterLeft - 1;
                                SpawnWateringCanAnimationSprite(currentPlayerLocation, offsetPosition);
                                HandleToolTransmuteConsequence();
                                performedAction = true;
                            }
                        }
                        else if (isPickaxe && terrainFeature is HoeDirt)
                        {
                            performedAction = DoToolFunction(currentPlayerLocation, Game1.player, tool, (int)offsetPosition.X, (int)offsetPosition.Y);

                            if (performedAction)
                            {
                                HandleToolTransmuteConsequence();
                            }
                        }
                    }
                    else if (isHoe)
                    {
                        performedAction = DoToolFunction(currentPlayerLocation, Game1.player, tool, (int)offsetPosition.X, (int)offsetPosition.Y);

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

        //static boulder status var for tracking boulders to mash (vector and times struck);
        private static Dictionary<Vector2, int> BouldersStruck = new Dictionary<Vector2, int>();

        private static bool DoToolFunction(GameLocation location, StardewValley.Farmer who, Tool tool, int x, int y)
        {
            bool performedAction = false;
            Vector2 index = new Vector2(x, y);
            Vector2 vector2 = new Vector2((float)(x + 0.5), (float)(y + 0.5));
            if (tool is MeleeWeapon && tool.name.ToLower().Contains("scythe"))
            {
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
            }
            else if (tool is Axe)
            {
                Rectangle rectangle = new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize);                
                location.performToolAction(tool, x, y);
                if (location.terrainFeatures.ContainsKey(index) && location.terrainFeatures[index].performToolAction(tool, 0, index, (GameLocation)null))
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
                    Vector2 toolLocation = who.GetToolLocation(false);
                    boundingBox = who.GetBoundingBox();
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
            else if (tool is Pickaxe)
            {
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
                            location.TemporarySprites.Add(new TemporaryAnimatedSprite(objectHit.ParentSheetIndex + 1, 300f, 1, 2, new Vector2((float)(x - x % Game1.tileSize), (float)(y - y % Game1.tileSize)), true, objectHit.flipped)
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
                    else if (objectHit.Name.Contains("Boulder"))
                    {
                        if (tool.UpgradeLevel > 1)
                        {
                            Vector2 boulderVector = objectHit.tileLocation;
                            if (Alchemy.BouldersStruck.ContainsKey(boulderVector))
                            {
                                Alchemy.BouldersStruck[boulderVector] += (power + 1);
                                if (Alchemy.BouldersStruck[boulderVector] < 4)
                                {
                                    performedAction = true;
                                    return performedAction;
                                }
                            }
                            else
                            {
                                Alchemy.BouldersStruck.Add(boulderVector, 0);
                                performedAction = true;
                                return performedAction;
                            }
                            location.removeObject(index, false);
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(5, new Vector2((float)Game1.tileSize * index.X - (float)(Game1.tileSize / 2), (float)Game1.tileSize * (index.Y - 1f)), Color.Gray, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0)
                            {
                                delayBeforeAnimationStart = 0
                            });
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(5, new Vector2((float)Game1.tileSize * index.X + (float)(Game1.tileSize / 2), (float)Game1.tileSize * (index.Y - 1f)), Color.Gray, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0)
                            {
                                delayBeforeAnimationStart = 200
                            });
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(5, new Vector2((float)Game1.tileSize * index.X, (float)Game1.tileSize * (index.Y - 1f) - (float)(Game1.tileSize / 2)), Color.Gray, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0)
                            {
                                delayBeforeAnimationStart = 400
                            });
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(5, new Vector2((float)Game1.tileSize * index.X, (float)Game1.tileSize * index.Y - (float)(Game1.tileSize / 2)), Color.Gray, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0)
                            {
                                delayBeforeAnimationStart = 600
                            });
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(25, new Vector2((float)Game1.tileSize * index.X, (float)Game1.tileSize * index.Y), Color.White, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, Game1.tileSize * 2, 0));
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(25, new Vector2((float)Game1.tileSize * index.X + (float)(Game1.tileSize / 2), (float)Game1.tileSize * index.Y), Color.White, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, Game1.tileSize * 2, 0)
                            {
                                delayBeforeAnimationStart = 250
                            });
                            location.temporarySprites.Add(new TemporaryAnimatedSprite(25, new Vector2((float)Game1.tileSize * index.X - (float)(Game1.tileSize / 2), (float)Game1.tileSize * index.Y), Color.White, 8, Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, Game1.tileSize * 2, 0)
                            {
                                delayBeforeAnimationStart = 500
                            });
                            performedAction = true;
                        }
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
            }
            return performedAction;
        }
    }
}

