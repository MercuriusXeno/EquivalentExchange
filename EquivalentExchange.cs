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

        //config for if the mod is allowed to play sounds
        public static bool canPlaySounds;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            //set the static instance variable. is this an oxymoron?
            instance = this;

            //preserve this entry method's helper class because it's.. helpful.
            instance.eeHelper = helper;

            //read the config file, poached from horse whistles, get the configured keys and settings
            Config = helper.ReadConfig<ConfigurationModel>();

            //set the mod's ability to play sounds
            canPlaySounds = Config.IsSoundEnabled;

            //add handler for the "transmute/copy" button.
            ControlEvents.KeyPressed += ControlEvents_KeyPressed;

            //exclusively to figure out if ctrl or shift have been let go of.
            ControlEvents.KeyReleased += ControlEvents_KeyReleased;

            //wire up the library scraping function to occur on save-loading to defer recipe scraping until all mods are loaded, optimistically.
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;

            //used to count down the delay until a sound is allowed to be played
            GameEvents.OneSecondTick += GameEvents_OneSecondTick;

            //we need this to save our alchemists['] data
            SaveEvents.BeforeSave += SaveEvents_BeforeSave;
        }

        //bit of helpful abstraction in dealing with cross-platform paths for save data.
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

        //handles reading "each" player json file and loading them into memory
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

        //handles writing "each" player's json save to the appropriate file.
        private void SaveEvents_BeforeSave(object sender, EventArgs e)
        {
            foreach (AlchemistFarmer player in instance.playerList)
                instance.eeHelper.WriteJsonFile<SaveDataModel>(GetSaveDirectory(player), player.playerSaveData);
        }

        //used to hold how many seconds to wait before the mod is allowed to play a transmutation sound
        private static int soundDelay = 0;

        //checks to see if sound delay has passed
        private bool HasPassedSoundDelay()
        {
            return soundDelay == 0;            
        }

        //set the sound delay to whatever it is in configs, defaults to 0 because my wife thinks it's better that way.
        private void SetSoundDelay()
        {
            soundDelay = Config.RepeatSoundDelay;            
        }

        //play sound method wired up to handle configurable sound delay
        private void PlaySound(string sound)
        {
            //check to see if the sound delay has passed
            if (HasPassedSoundDelay())
            {
                //reset sound delay
                SetSoundDelay();

                //play dat sound
                Game1.playSound(sound);
            }
        }

        //a nice magicky sound suggested by spacechase0
        private void PlayMagickySound()
        {
            PlaySound("healSound");        
        }

        //sound of cash moneeeeeeeh, git moneeeeeeeh I'm pickle rick
        private void PlayMoneySound()
        {
            PlaySound("purchaseClick");
        }

        //de facto player-grunting-from-damage sound
        private void PlayReboundSound()
        {
            PlaySound("ow");
        }

        //if the user opts to use the sound replay delay in configs, this counts the seconds until a sound can be played again.
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

        //handler for which flag to set when X key is pressed/released
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

        //handles the release key event for figuring out if control or shift is let go of
        private void ControlEvents_KeyReleased(object sender, EventArgsKeyPressed e)
        {
            if (modifyingControlKeys.Contains(e.KeyPressed))
            {
                SetModifyingControlKeyState(e.KeyPressed, false);
            }
        }

        //handles the key press event for figuring out if control or shift is held down, or either of the mod's major transmutation actions is being attempted.
        private void ControlEvents_KeyPressed(object sender, EventArgsKeyPressed e)
        {
            if (modifyingControlKeys.Contains(e.KeyPressed))
            {
                SetModifyingControlKeyState(e.KeyPressed, true);
            }
            
            //valid keys to trigger any sort of scan on are:
            string[] transmuteOrLiquidateKeys = { Config.TransmuteKey, Config.LiquidateKey };

            //the key pressed is one of the mods keys.. I'm doing this so I don't fire logic for anything unless either of the mod's keys were pressed.            
            if (transmuteOrLiquidateKeys.Contains(e.KeyPressed.ToString()))
            {
                HandleEitherTransmuteEvent(e.KeyPressed);
            }
        }

        //I'm lazy
        private long GetCurrentPlayerID()
        {
            return Game1.player.uniqueMultiplayerID;
        }

        //sets up the basic structure of either transmute event, since they have some common ground
        private void HandleEitherTransmuteEvent(Keys keyPressed)
        {

            bool bothModifierKeysPressed = IsShiftKeyPressed() && IsControlKeyPressed();

            //ctrl + shift = 16, ctrl = 9, shift = 4, default is 1
            int amount = (bothModifierKeysPressed ? 16 : (IsControlKeyPressed() ? 9 : (IsShiftKeyPressed() ? 4 : 1)));

            //the stamina cost is only exacted from the player this many times.
            //the number of chances of a rebound is only rolled this many times.
            //this incentivizes larger batches [greatly]
            int costMultiplier = (int)Math.Sqrt(amount);

            // save is loaded
            if (Context.IsWorldReady)
            {
                //per the advice of Ento, abort if the player is in an event
                if (Game1.CurrentEvent != null)
                    return;

                //used to figure out which player we need to load, in this pretend-multiplayer setup we've got so far.
                long playerID = GetCurrentPlayerID();

                //linq seek the player in question, get the player's save data. We're gonna pass this AlchemistFarmer around.
                AlchemistFarmer player = playerList.Where(p => p.uniqueMultiplayerID == playerID).FirstOrDefault();

                //something may have gone wrong if this is null, maybe there's no save data?
                if (player != null) {
                    //get the player's current item
                    Item heldItem = player.CurrentItem;

                    //player is holding item
                    if (heldItem != null)
                    {                        
                        //get the item's ID
                        int heldItemID = heldItem.parentSheetIndex;

                        //abort any transmutation event for blacklisted items or items that for whatever reason can't exist in world.
                        if (blackListedItemIDs.Contains(heldItemID) || !heldItem.canBeDropped())
                        {
                            return;
                        }

                        //get the transmutation value, it's based on what it's worth to the player, including profession bonuses. This affects both cost and value.
                        int actualValue = ((StardewValley.Object)heldItem).sellToStorePrice();

                        //try to transmute [copy] the item
                        if (keyPressed.ToString() == Config.TransmuteKey)
                        {
                            HandleTransmuteEvent(player, heldItem, amount, actualValue, costMultiplier);
                        }

                        //try to liquidate the item [sell for gold]
                        if (keyPressed.ToString() == Config.LiquidateKey)
                        {
                            HandleLiquidateEvent(player, heldItem, amount, actualValue, costMultiplier);
                        }
                    }
                }
            }
        }

        public void HandleLiquidateEvent(AlchemistFarmer player, Item heldItem, int attemptedAmount, int actualValue, int costMultiplier)
        {            
            //reduce the amount if the player's modifying the count and it is greater than what the stack holds.
            //for player convenience, the mod won't let you liquidate the last item
            attemptedAmount = Math.Min(attemptedAmount, heldItem.getStack() - 1);

            //placeholder for determining if the transmute occurs, so it knows to play a sound.
            bool didTransmuteOccur = false;

            //placeholder for determining if the transmute rebounds, so it knows to play a different sound.
            bool didTransmuteFail = false;

            while (attemptedAmount > 0)
            {
                float staminaCost = player.GetStaminaCostForTransmutation(actualValue);

                //if the player lacks the stamina to execute a transmute, abort
                if (player.Stamina <= staminaCost)
                {
                    attemptedAmount = 0;
                    continue;
                }

                int amount = Math.Min(attemptedAmount, costMultiplier);

                //we decrement the attempted amount before the rebound occurs to mark that the attempt occurred.
                attemptedAmount -= amount;

                //if we fail this check, it's because a rebound would kill the player.
                //if the rebound chance is zero, this check will automatically pass.
                if (!player.CanSurviveRebound(actualValue))
                    continue;

                //if we fail this check, transmutation will fail this cycle.
                //this is our "rebound check"
                if (player.DidPlayerFailReboundCheck()) {
                    player.TakeDamageFromRebound(actualValue);
                    didTransmuteFail = true;
                    //the conduit profession makes it so that the transmutation succeeds anyway, after taking damage.
                    if (!player.playerSaveData.hasConduitProfession)
                        continue;
                }

                //if we reached this point transmutation will succeed
                didTransmuteOccur = true;

                //a rebound obviates a lucky transmute, but a profession trait obviates stamina drain when you rebound.
                if (!didTransmuteFail && !player.playerSaveData.hasConduitProfession)
                    player.HandleStaminaDeduction(staminaCost);

                //we floor the math here because we don't want weirdly divergent values based on stack count - the rate is fixed regardless of quantity
                //this occurs at the expense of rounding - liquidation is lossy.
                int liquidationValue = (int)Math.Floor(player.GetLiquidationValuePercentage() * actualValue);

                int totalValue = liquidationValue * amount;

                player.Money += totalValue;

                ReduceActiveItemByAmount(Game1.player, amount);             
            }

            //a transmute (at least one) happened, play the cash money sound
            if (didTransmuteOccur && !didTransmuteFail)
                PlayMoneySound();

            //a rebound occurred, so play the ouchy sound.
            if (didTransmuteFail)
                PlayReboundSound();
        }

        public void HandleTransmuteEvent (AlchemistFarmer player, Item heldItem, int attemptedAmount, int actualValue, int costMultiplier)
        {
            //cost of a single item, multiplied by the cost multiplier below
            int transmutationCost = (int)Math.Ceiling(player.GetTransmutationMarkupPercentage() * actualValue);

            //unlike liquidations, amount shouldn't have to change in the loop
            int amount = costMultiplier;

            //nor should totalCost of a single cycle
            int totalCost = transmutationCost * amount;            

            //placeholder for determining if the transmute occurs, so it knows to play a sound.
            bool didTransmuteOccur = false;

            //placeholder for determining if the transmute rebounds, so it knows to play a different sound.
            bool didTransmuteFail = false;

            //loop for each transmute-cycle attempt
            while (player.money >= totalCost && attemptedAmount > 0)
            {
                float staminaCost = player.GetStaminaCostForTransmutation(actualValue);
                //if the player lacks the stamina to execute a transmute, abort
                if (player.Stamina <= staminaCost)
                {
                    attemptedAmount = 0;
                    continue;
                }

                //if we fail this check, it's because a rebound would kill the player.
                //if the rebound chance is zero, this check will automatically pass.
                if (!player.CanSurviveRebound(actualValue))
                    continue;

                //if we fail this check, transmutation will fail this cycle.
                //this is our "rebound check"
                if (player.DidPlayerFailReboundCheck())
                {
                    player.TakeDamageFromRebound(actualValue);
                    didTransmuteFail = true;
                    //the conduit profession makes it so that the transmutation succeeds anyway, after taking damage.
                    if (!player.playerSaveData.hasConduitProfession)
                        continue;
                }

                didTransmuteOccur = true;

                //a rebound obviates a lucky transmute, but a profession trait obviates stamina drain when you rebound.
                if (!didTransmuteFail && !player.playerSaveData.hasConduitProfession)
                    player.HandleStaminaDeduction(staminaCost);
                
                player.Money -= totalCost;

                Item spawnedItem = heldItem.getOne();

                //since we have at least 1 already, add amount - 1 to the stack. if amount is 1, we're adding nothing.
                if (amount > 1)
                {
                    spawnedItem.addToStack(amount - 1);
                }

                Game1.createItemDebris(spawnedItem, player.getStandingPosition(), player.FacingDirection, (GameLocation)null);
            }

            //a transmute (at least one) happened, play the magicky sound
            if (didTransmuteOccur && !didTransmuteFail)
                PlayMagickySound();

            //a rebound occurred, so play the ouchy sound.
            if (didTransmuteFail)
                PlayReboundSound();
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