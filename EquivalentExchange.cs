using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using Microsoft.Xna.Framework.Input;

using EquivalentExchange.Models;
using System.IO;

namespace EquivalentExchange
{

    /// <summary>The mod entry point.</summary>
    public class EquivalentExchange : Mod
    {
        //instantiate config
        private ConfigurationModel Config;
        
        //"list" of players, intended for working around future multiplayer, maybe.
        public List<AlchemistFarmer> playerList = new List<AlchemistFarmer>();

        //this instance of the mod's helper class file, intialized by Entry
        public IModHelper eeHelper;

        //the mod's "static" instance, initialized by Entry. There caN ONly bE ONe
        public static EquivalentExchange instance;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            instance = this;
            instance.eeHelper = helper;
            //poached from horse whistles, get the configured keys
            Config = helper.ReadConfig<ConfigurationModel>();

            //add handler for the "transmute/copy" button.
            ControlEvents.KeyPressed += ControlEvents_KeyPressed;
            ControlEvents.KeyReleased += ControlEvents_KeyReleased;

            //wire up the library scraping function to occur on save-loading to defer recipe scraping until all mods are loaded, optimistically.
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;

            GameEvents.OneSecondTick += GameEvents_OneSecondTick;

            SaveEvents.BeforeSave += SaveEvents_BeforeSave;
        }

        private string GetSaveDirectory(StardewValley.Farmer player)
        {
            return Path.Combine(instance.eeHelper.DirectoryPath, "saveData", player.uniqueMultiplayerID.ToString(), ".json");
        }

        //fires when loading a save, initializes the item blacklist and loads player save data.
        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            InitializePlayerData();
            PopulateItemLibrary();
        }

        //placeholder method that currently just holds one player in an array. Replace this with a multiplayer method when the time comes.
        private StardewValley.Farmer[] GetPlayerList()
        {
            return new StardewValley.Farmer[]{ Game1.player };
        }

        private void InitializePlayerData()
        {
            // save is loaded
            if (Context.IsWorldReady)
            {
                StardewValley.Farmer[] players = GetPlayerList();
                foreach (StardewValley.Farmer player in players)
                {
                    //fetch each player's data, we're using it to populate a list, and using those to build custom player class instances.
                    SaveDataModel currentPlayerData = instance.eeHelper.ReadJsonFile<SaveDataModel>(GetSaveDirectory(player));

                    //construct a player with custom metadata which extends player
                    AlchemistFarmer alchemistPlayer = new AlchemistFarmer(currentPlayerData);
                    
                    //add the player data to the instance list
                    instance.playerList.Add(alchemistPlayer);                    
                }
            }
        }

        private void SaveEvents_BeforeSave(object sender, EventArgs e)
        {
            foreach (AlchemistFarmer player in instance.playerList)
            instance.eeHelper.WriteJsonFile<SaveDataModel>(GetSaveDirectory(player), player.playerSaveData);
        }

        private static int soundDelay = 0;

        private bool HasPassedSoundDelay()
        {
            return soundDelay == 0;            
        }

        private void SetSoundDelay()
        {
            soundDelay = Config.RepeatSoundDelay;            
        }

        private void PlayMagickySound()
        {
            if (HasPassedSoundDelay())
            {
                SetSoundDelay();

                Game1.playSound("healSound");
            }
        }

        private void PlayMoneySound()
        {
            if (HasPassedSoundDelay())
            {
                SetSoundDelay();

                Game1.playSound("purchaseClick");
            }
        }

        private void GameEvents_OneSecondTick(object sender, EventArgs e)
        {
            if (soundDelay > 0)
            {
                soundDelay--;
            }
        }

        /// <summary>Update the mod's config.json file from the current <see cref="Config"/>.</summary>
        internal void SaveConfig()
        {
            eeHelper.WriteConfig(Config);
        }

        //control key modifiers [shift and ctrl], I include both for a more robust "is either pressed" mechanic.
        public static bool leftShiftKeyPressed = false;
        public static bool rightShiftKeyPressed = false;

        public static bool leftControlKeyPressed = false;
        public static bool rightControlKeyPressed = false;

        //convenience methods for detecting when either keys are pressed to modify amount desired from liquidation/transmutes.
        private bool IsShiftKeyPressed ()
        {
            return leftShiftKeyPressed || rightShiftKeyPressed;
        }

        private bool IsControlKeyPressed()
        {
            return leftControlKeyPressed || rightControlKeyPressed;
        }

        //simple consts/arrays to keep code clean, both shift keys, both control keys.
        public const Keys leftShiftKey = Keys.LeftShift;
        public const Keys rightShiftKey = Keys.RightShift;
        public const Keys leftControlKey = Keys.LeftControl;
        public const Keys rightControlKey = Keys.RightControl;

        public static Keys[] modifyingControlKeys = { leftShiftKey, rightShiftKey, leftControlKey, rightControlKey };

        private void SetModifyingControlKeyState(Keys keyChanged, bool isPressed)
        {
            switch (keyChanged)
            {
                case leftShiftKey:
                    leftShiftKeyPressed = isPressed;
                    break;
                case rightShiftKey:
                    rightShiftKeyPressed = isPressed;
                    break;
                case leftControlKey:
                    leftControlKeyPressed = isPressed;
                    break;
                case rightControlKey:
                    rightControlKeyPressed = isPressed;
                    break;
                default:
                    break;
            }            
        }

        private void ControlEvents_KeyReleased(object sender, EventArgsKeyPressed e)
        {
            if (modifyingControlKeys.Contains(e.KeyPressed))
            {
                SetModifyingControlKeyState(e.KeyPressed, false);
            }
        }

        /*********
        ** Private methods
        *********/
        /// <summary>The method invoked when the player presses a keyboard button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void ControlEvents_KeyPressed(object sender, EventArgsKeyPressed e)
        {
            if (modifyingControlKeys.Contains(e.KeyPressed))
            {
                SetModifyingControlKeyState(e.KeyPressed, true);
            }
            
            //valid keys to trigger any sort of scan on are:
            string[] transmuteOrLiquidateKeys = { Config.TransmuteKey, Config.LiquidateKey };

            //the key pressed is one of the mods keys.. I'm doing this so I don't fire logic for anything unless either of the mod's keys were pressed.
            //later in this conditional, I check which key, specifically, has been pressed.
            if (transmuteOrLiquidateKeys.Contains(e.KeyPressed.ToString()))
            {
                HandleEitherTransmuteEvent(e.KeyPressed);
            }
        }

        private long GetCurrentPlayerID()
        {
            return Game1.player.uniqueMultiplayerID;
        }

        private void HandleEitherTransmuteEvent(Keys keyPressed)
        {

            bool bothModifierKeysPressed = IsShiftKeyPressed() && IsControlKeyPressed();

            //ctrl + shift = 16, ctrl = 9, shift = 4, default is 1
            int amount = (bothModifierKeysPressed ? 16 : (IsControlKeyPressed() ? 9 : (IsShiftKeyPressed() ? 4 : 1)));

            //player gets a substantial bonus for doing things in batches. 
            //transmutation stamina costs are only multiplied by a portion of the quantity - should be 4x, 3x, 2x, 1x respectively
            //number of rebound rolls is equal to the costMultiplier, so a 16x transmute only has 4 opportunities to fail, and will partially succeed.
            int costMultiplier = (int)Math.Sqrt(amount);

            // save is loaded
            if (Context.IsWorldReady)
            {
                long playerID = GetCurrentPlayerID();
                AlchemistFarmer player = playerList.Where(p => p.uniqueMultiplayerID == playerID).FirstOrDefault();
                if (player != null) {
                    Item heldItem = player.CurrentItem;

                    //player is holding item
                    if (heldItem != null)
                    {
                        string heldItemName = heldItem.DisplayName;
                        //get the item in-hand
                        int heldItemID = heldItem.parentSheetIndex;

                        //abort any transmutation event [liquidate or transmute] if the item is a recipe item, as those are restricted.
                        if (blackListedItemIDs.Contains(heldItemID) || !heldItem.canBeDropped())
                        {
                            return;
                        }

                        //get the transmutation value, it's based on what it's worth to the player, including profession bonuses - this is for balance reasons.
                        int actualValue = ((StardewValley.Object)heldItem).sellToStorePrice();

                        //try to transmute [copy] the item if the player has enough gold.
                        if (keyPressed.ToString() == Config.TransmuteKey)
                        {
                            HandleTransmuteEvent(player, heldItem, amount, actualValue, costMultiplier);
                        }

                        //liquidate the item [sell for gold]
                        if (keyPressed.ToString() == Config.LiquidateKey)
                        {
                            HandleLiquidateEvent(player, heldItem, amount, actualValue, costMultiplier);
                        }
                    }
                }
            }
        }

        private double GetLiquidationValuePercentage(StardewValley.Farmer player)
        {
            return Config.LiquidationValuePercentage;
        }

        private double GetTransmutationMarkupPercentage(StardewValley.Farmer player)
        {
            return Config.TransmutationMarkupPercentage;
        }

        //algorithm to return stamina cost for the act of transmuting/liquidating an item, based on player skill, the amount and item value.
        private float GetStaminaCostForTransmutation(AlchemistFarmer player, int itemValue)
        {
            return (float)Math.Sqrt(itemValue) * player.GetAlchemyStaminaCostSkillMultiplier();
        }

        public void HandleLiquidateEvent(AlchemistFarmer player, Item heldItem, int attemptedAmount, int actualValue, int costMultiplier)
        {            
            //reduce the amount if the player's modifying the count and it is greater than what the stack holds.
            //for player convenience, the mod won't let you liquidate the last item
            attemptedAmount = Math.Min(attemptedAmount, heldItem.getStack() - 1);
            bool didTransmuteOccur = false;
            while (attemptedAmount > 0)
            {
                int amount = Math.Min(attemptedAmount, costMultiplier);

                if (player.Stamina <= GetStaminaCostForTransmutation(player, actualValue))
                    return;

                didTransmuteOccur = true;

                //we floor the math here because we don't want weirdly divergent values based on stack count - the rate is fixed regardless of quantity
                //this occurs at the expense of rounding - liquidation is lossy.
                int liquidationValue = (int)Math.Floor(GetLiquidationValuePercentage(player) * actualValue);

                int totalValue = liquidationValue * amount;

                Game1.player.money += totalValue;

                ReduceActiveItemByAmount(Game1.player, amount);

                attemptedAmount -= amount;                
            }
            if (didTransmuteOccur)
                PlayMoneySound();
        }

        public void HandleTransmuteEvent (AlchemistFarmer player, Item heldItem, int attemptedAmount, int actualValue, int costMultiplier)
        {
            int transmutationCost = (int)Math.Ceiling(GetTransmutationMarkupPercentage(player) * actualValue);

            int amount = costMultiplier;

            int totalCost = transmutationCost * amount;

            if (player.Stamina <= GetStaminaCostForTransmutation(player, actualValue))
                return;

            while (Game1.player.money >= totalCost && amount > 0)
            {
                Game1.player.money -= totalCost;

                Item spawnedItem = heldItem.getOne();

                //since we have at least 1 already, add amount - 1 to the stack. if amount is 1, we're adding nothing.
                if (amount > 1)
                {
                    spawnedItem.addToStack(amount - 1);
                }

                Game1.createItemDebris(spawnedItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection, (GameLocation)null);

                PlayMagickySound();
            }
        }

        //reimplementation of reduceActiveItemByOne, designed for varying stack amounts.
        private void ReduceActiveItemByAmount(StardewValley.Farmer player, int amount)
        {

            if (player.CurrentItem == null)
                return;
            player.CurrentItem.Stack -= amount;
            if (player.CurrentItem.Stack > 0)
                return;
            player.removeItemFromInventory(player.CurrentItem);
            player.showNotCarrying();
        }     

        //holds a list of item IDs which are invalid for transmutation due to being created by recipes. This is to help avoid positive value feedback loops.
        private static List<int> blackListedItemIDs = new List<int>();   

        private void PopulateItemLibrary()
        {
            //the point of this routine is to find all of the objects that are created from a recipe. This mod will only transmute raw materials
            //so anything that is cooked and crafted should not be possible to transmute. This is sort of for balance reasons? It's OP anyway.
                        
            //Now we're iterating over these two lists to obtain a list of IDs which are invalid for transmutation
            Dictionary<string, string>[] recipeDictionaries = { CraftingRecipe.craftingRecipes, CraftingRecipe.cookingRecipes };
            foreach (Dictionary<string, string> recipeDictionary in recipeDictionaries)
            {
                foreach (KeyValuePair<string, string> recipe in recipeDictionary)
                {
                    //values are tokenized by a / and then subtokenized by spaces
                    string[] recipeValues = recipe.Value.Split('/');

                    //index 2 of this array is the output ID and amount, tokenized by spaces. Not all outputs have an amount, it defaults to 1.
                    //we don't care about quantity anyway.
                    string[] recipeOutputs = recipeValues[2].Split(' ');

                    //index 0 of this array is, thus, the output ID.
                    int.TryParse(recipeOutputs[0], out int recipeItemID);

                    //add the recipe item ID to the list of items the player can't transmute
                    blackListedItemIDs.Add(recipeItemID);
                }

            }

            //this.Monitor.Log($"Scanning item list:");
            //iterate over game objects
            foreach (KeyValuePair<int, string> entry in Game1.objectInformation)
            {
                //get basic vars

                //id
                int itemID = entry.Key;
                
                //literally everything else
                string itemValue = entry.Value;

                //split 'everything' into an array
                string[] parsedValues = itemValue.Split('/');

                //item cost/value is index 1
                int.TryParse(parsedValues[1], out int itemCost);

                //objects with a cost of 1 or less are blacklisted
                if (itemCost < 1)
                    blackListedItemIDs.Add(itemID);
            }

            blackListedItemIDs.Add(StardewValley.Object.prismaticShardIndex);
        }

        private bool hasAllProfessionsMod = false;
        private Professions[] firstRankProfessions = { Professions.Shaper, Professions.Sage };
        private Professions[] secondRankProfessions = { Professions.Transmuter, Professions.Adept, Professions.Aurumancer, Professions.Conduit };
        private void CheckForAllProfessionsMod()
        {
            if (!eeHelper.ModRegistry.IsLoaded("community.AllProfessions"))
            {
                Log.info("[Equivalent Exchange] All Professions not found.");
                return;
            }

            Log.info("[Equivalent Exchange] All Professions found. You will get every alchemy profession for your level.");
            hasAllProfessionsMod = true;
        }

        public enum Professions
        { 
            Shaper,
            Sage,
            Transmuter,
            Adept,
            Aurumancer,
            Conduit
        }
    }
}