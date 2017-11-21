using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using EquivalentExchange.Models;
using System.IO;
using System.Reflection;
using StardewValley.Menus;

namespace EquivalentExchange
{

    /// <summary>The mod entry point.</summary>
    public class EquivalentExchange : Mod
    {
        //instantiate config
        private ConfigurationModel Config;
        
        //this instance of the mod's helper class file, intialized by Entry
        public IModHelper eeHelper;

        //the mod's "static" instance, initialized by Entry. There caN ONly bE ONe
        public static EquivalentExchange instance;

        public SaveDataModel currentPlayerData;

        //config for if the mod is allowed to play sounds
        public static bool canPlaySounds;

        //handles all the things.
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

            //set texture files in memory, they're tiny things.
            DrawingUtil.HandleTextureCaching();

            //trying something completely different from a patched event hook...
            //gonna try using this to detect the night event heuristically.
            GameEvents.UpdateTick += GameEvents_UpdateTick;

            //check for experience bars mod: if it's here we draw hud elements for the new alchemy skill
            CheckForExperienceBarsMod();
            if (hasExperienceBarsMod)
            {
                GraphicsEvents.OnPostRenderHudEvent += GraphicsEvents_OnPostRenderHudEvent;
            }

            //check for all professions mod: if it's here we run a wireup to give the player all skills professions at the right time (or after), when present.
            CheckForAllProfessionsMod();
            if (hasAllProfessionsMod)
            {
                LocationEvents.CurrentLocationChanged += LocationEvents_CurrentLocationChanged; ;
            }

            //add a debug option to give yourself experience
            Helper.ConsoleCommands.Add("player_givealchemyexp", "player_givealchemyexp <amount>", GiveAlchemyExperience);

            //post render event for skills menu
            GraphicsEvents.OnPostRenderGuiEvent += DrawAfterGUI;

            //check for chase's skills
            checkForLuck();
            checkForCooking();  
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            //unsure if this does what I think it does
            if (Game1.player.isEmoting && Game1.player.CurrentEmote == 24)
            {
                //the player has just answered "yes" to sleep? or the player passed out like a chump.
                if (Game1.currentLocation.lastQuestionKey == null || Game1.currentLocation.lastQuestionKey.Equals("Sleep"))
                    AddEndOfNightMenus();
            }
        }

        //integration considerations for chase's skills

        public static bool hasLuck = false;
        private void checkForLuck()
        {
            if (!Helper.ModRegistry.IsLoaded("spacechase0.LuckSkill"))
            {
                Log.info("Luck Skill not found");
                return;
            }
            
            hasLuck = true;
        }

        public static bool hasCooking = false;
        private void checkForCooking()
        {
            if (!Helper.ModRegistry.IsLoaded("spacechase0.CookingSkill"))
            {
                Log.info("Cooking Skill not found");
                return;
            }
            
            hasCooking = true;
        }

        private bool didInitSkills = false;
        private void DrawAfterGUI(object sender, EventArgs args)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                GameMenu menu = Game1.activeClickableMenu as GameMenu;
                if (menu.currentTab == GameMenu.skillsTab)
                {
                    var tabs = (List<IClickableMenu>)Util.GetInstanceField(typeof(GameMenu), menu, "pages");
                    var skills = (SkillsPage)tabs[GameMenu.skillsTab];

                    if (!didInitSkills)
                    {
                        DrawingUtil.InitAlchemySkill(skills);
                        didInitSkills = true;
                    }
                    DrawingUtil.DrawAlchemySkill(skills);
                }
            }
            else didInitSkills = false;
        }        

        //command to give yourself experience for debug purposes primarily

        private void GiveAlchemyExperience(object sender, string[] args)
        {
            if (args.Length != 1)
            {
                Log.info("Command format: giveAlchemyExp <amount>");
                return;
            }

            int amt = 0;
            try
            {
                amt = Convert.ToInt32(args[0]);
            }
            catch (Exception e)
            {
                Log.error("Bad experience amount.");
                return;
            }

            Alchemy.AddAlchemyExperience(amt);
            Log.info("Added " + amt + " alchemy experience.");
        }

        public List<int> showLevelUpMenusByRank = new List<int>();

        internal void AddSkillUpMenuAppearance(int alchemyLevel)
        {
            showLevelUpMenusByRank.Add(alchemyLevel);
        }

        //show the level up menus at night when you hit a profession breakpoint.
        private void AddEndOfNightMenus()
        {
            if (!Context.IsWorldReady)
                return;
            //generally wait for the game fade to reach a pretty high number, this is a total hack to delay the menus until approximately the right time.
            if (Game1.fadeToBlackAlpha < 0.7D)
                return;
            bool playerNeedsLevelFiveProfession = currentPlayerData.AlchemyLevel >= 5 && !Game1.player.professions.Contains((int)Professions.Shaper) && !Game1.player.professions.Contains((int)Professions.Sage);
            bool playerNeedsLevelTenProfession = currentPlayerData.AlchemyLevel >= 10 && !Game1.player.professions.Contains((int)Professions.Transmuter) && !Game1.player.professions.Contains((int)Professions.Adept) && !Game1.player.professions.Contains((int)Professions.Aurumancer) && !Game1.player.professions.Contains((int)Professions.Conduit);            
            bool playerGainedALevel = showLevelUpMenusByRank.Count() > 0 ;

            //nothing requires our intervention, bypass this method as it is heavy on logic and predecate searches, and we don't want to fire those every #&!$ing tick
            if (!playerGainedALevel && !playerNeedsLevelFiveProfession && !playerNeedsLevelTenProfession)
                return;

            //the save menus follow a last in-first out rule. If any levels are going to be added by the routine, this gets handled in vanilla code
            //but if *only* the alchemy skill has gained a level, there's no game code to handle an auto-save, so we force one.
            bool needsSave = playerGainedALevel || playerNeedsLevelFiveProfession || playerNeedsLevelTenProfession;

            //this list will hold any existing calls to save the game if one exists. If one doesn't add it.
            if (needsSave)
            {
                //try to figure out if the game would have saved anyway...
                bool wouldHaveSavedAnyway = (Game1.player.newLevels.Count > 0 || Game1.getFarm().shippingBin.Count > 0);
                if (!wouldHaveSavedAnyway)
                {                
                    List<IClickableMenu> existingSaveGameCalls = Game1.endOfNightMenus.Where(x => x.GetType().Equals(typeof(SaveGameMenu))).ToList();
                    bool nightMenusAlreadyHasSave = existingSaveGameCalls.Count > 0;

                    if (!nightMenusAlreadyHasSave)
                        Game1.endOfNightMenus.Push(new SaveGameMenu());
                }
            }

            if (playerGainedALevel)
            {
                for (int i = showLevelUpMenusByRank.Count() - 1; i >= 0; --i)
                {
                    int level = showLevelUpMenusByRank[i];
                    //search for existing levelups already injected into the night menu routine.
                    List<IClickableMenu> existingLevelUps = Game1.endOfNightMenus.Where(x => x.GetType().Equals(typeof(AlchemyLevelUpMenu)) && ((AlchemyLevelUpMenu)x).GetLevel() == level).ToList();
                    //excuse the plural, this check is testing for *this level* specifically.
                    if (existingLevelUps.Count == 0)
                    {
                        Game1.endOfNightMenus.Push(new AlchemyLevelUpMenu(Game1.player, level));
                    }
                }
                //presume we've added all the levels we need, wipe this thing.
                showLevelUpMenusByRank.Clear();
            }
            else if (playerNeedsLevelFiveProfession)
            {
                List<IClickableMenu> existingLevelUps = Game1.endOfNightMenus.Where(x => x.GetType().Equals(typeof(AlchemyLevelUpMenu)) && ((AlchemyLevelUpMenu)x).GetLevel() == 5).ToList();
                if (existingLevelUps.Count == 0)                    
                    Game1.endOfNightMenus.Push(new AlchemyLevelUpMenu(Game1.player, 5));
            }
            else if (playerNeedsLevelTenProfession)
            {
                List<IClickableMenu> existingLevelUps = Game1.endOfNightMenus.Where(x => x.GetType().Equals(typeof(AlchemyLevelUpMenu)) && ((AlchemyLevelUpMenu)x).GetLevel() == 10).ToList();
                if (existingLevelUps.Count == 0)
                    Game1.endOfNightMenus.Push(new AlchemyLevelUpMenu(Game1.player, 10));
            }
        }

        //misleading event wireup is actually for the has-all-professions mod, which enables all professions at the appropriate level.
        private void LocationEvents_CurrentLocationChanged(object sender, EventArgsCurrentLocationChanged e)
        {
            if (hasAllProfessionsMod)
            {
                List<int> professions = Game1.player.professions;
                List<List<int>> list = new List<List<int>> { Professions.firstRankProfessions, Professions.secondRankProfessions };
                foreach (List<int> current in list)
                {
                    bool flag = professions.Intersect(current).Any<int>();
                    if (flag)
                    {
                        foreach (int current2 in current)
                        {
                            bool flag2 = !professions.Contains(current2);
                            if (flag2)
                            {
                                professions.Add(current2);
                            }
                        }
                    }
                }
            }
        }

        //hooked for drawing the experience bar on screen when experience bars mod is present.
        private void GraphicsEvents_OnPostRenderHudEvent(object sender, EventArgs e)
        {
            DrawingUtil.DoPostRenderHudEvent();
        }


        //ensures that the wizard tower and witch hut are leylines for the mod by default.
        private static string[] VANILLA_LEYLINE_LOCATIONS = new string[]{ "WizardHouse", "WitchHut", "Desert" };

        private void InitializeVanillaLeylines()
        {
            foreach (string leyline in VANILLA_LEYLINE_LOCATIONS)
            {
                if (Game1.getLocationFromName(leyline) == null)
                    Log.error($"{leyline} is missing, there is a very bad problem and you will not be going to space today.");
                else
                    Game1.getLocationFromName(leyline)?.map.Properties.Add(Alchemy.LEYLINE_PROPERTY_INDICATOR, 0F);
            }
        }

        //fires when loading a save, initializes the item blacklist and loads player save data.
        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            InitializePlayerData();
            InitializeVanillaLeylines();
            PopulateItemLibrary();
        }
                
        //handles reading current player json file and loading them into memory
        private void InitializePlayerData()
        {
            // save is loaded
            if (Context.IsWorldReady)
            {
                //fetch the alchemy save for this game file.
                instance.currentPlayerData = instance.eeHelper.ReadJsonFile<SaveDataModel>(Path.Combine(Constants.CurrentSavePath, $"{Game1.uniqueIDForThisGame.ToString()}.json"));                

                //we want to generate the save data model, but we don't save it until we're supposed to, to prevent data from saving prematurely (thus generating a new multiplayer ID)
                if (instance.currentPlayerData == null)
                {                                        
                    instance.currentPlayerData = new SaveDataModel(Game1.uniqueIDForThisGame);
                }
            }
        }

        //handles writing "each" player's json save to the appropriate file.
        private void SaveEvents_BeforeSave(object sender, EventArgs e)
        {
            SavePlayerData();
        }

        private void SavePlayerData()
        {
            instance.eeHelper.WriteJsonFile<SaveDataModel>(Path.Combine(Constants.CurrentSavePath, $"{ Game1.uniqueIDForThisGame.ToString()}.json"), instance.currentPlayerData);
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
            //debug REMOVE ME
            if (e.KeyPressed == Keys.F5)
                Alchemy.AddAlchemyExperience(5000);
            
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
                
                //something may have gone wrong if this is null, maybe there's no save data?
                if (Game1.player != null) {
                    //get the player's current item
                    Item heldItem = Game1.player.CurrentItem;

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
                            HandleTransmuteEvent(heldItem, amount, actualValue, costMultiplier);
                        }

                        //try to liquidate the item [sell for gold]
                        if (keyPressed.ToString() == Config.LiquidateKey)
                        {
                            HandleLiquidateEvent(heldItem, amount, actualValue, costMultiplier);
                        }
                    }
                }
            }
        }

        public void HandleLiquidateEvent(Item heldItem, int attemptedAmount, int actualValue, int costMultiplier)
        {            
            //reduce the amount if the player's modifying the count and it is greater than what the stack holds.
            //for player convenience, the mod won't let you liquidate the last item
            attemptedAmount = Math.Min(attemptedAmount, heldItem.getStack() - 1);

            //placeholder for determining if the transmute occurs, so it knows to play a sound.
            bool didTransmuteOccur = false;

            //placeholder for determining if the transmute rebounds, so it knows to play a different sound.
            bool didTransmuteFail = false;

            //if the transmute did fail, this preserves the damage so we can apply it in one cycle, otherwise batches look weird af
            int reboundDamageTaken = 0;

            while (attemptedAmount > 0)
            {
                int amount = Math.Min(attemptedAmount, costMultiplier);

                //we decrement the attempted amount before the anything occurs to prevent loop problems.
                attemptedAmount -= amount;

                double staminaCost = Alchemy.GetStaminaCostForTransmutation(actualValue);

                //if the player lacks the stamina to execute a transmute, abort
                if (Game1.player.Stamina <= staminaCost)
                {
                    continue;
                }

                //if we fail this check, it's because a rebound would kill the player.
                //if the rebound chance is zero, this check will automatically pass.
                if (!Alchemy.CanSurviveRebound(actualValue))
                {
                    continue;
                }

                //if we fail this check, transmutation will fail this cycle.
                //this is our "rebound check"
                if (Alchemy.DidPlayerFailReboundCheck()) {
                    reboundDamageTaken += actualValue;
                    didTransmuteFail = true;
                    //the conduit profession makes it so that the transmutation succeeds anyway, after taking damage.
                    if (!Game1.player.professions.Contains((int)Professions.Conduit))
                        continue;
                }

                //if we reached this point transmutation will succeed
                didTransmuteOccur = true;

                //a rebound obviates a lucky transmute, but a profession trait obviates stamina drain when you rebound.
                if (!didTransmuteFail && !Game1.player.professions.Contains((int)Professions.Conduit))
                    Alchemy.HandleStaminaDeduction(staminaCost);

                //we floor the math here because we don't want weirdly divergent values based on stack count - the rate is fixed regardless of quantity
                //this occurs at the expense of rounding - liquidation is lossy.
                int liquidationValue = (int)Math.Floor(Alchemy.GetLiquidationValuePercentage() * actualValue);

                int totalValue = liquidationValue * amount;

                Game1.player.Money += totalValue;

                ReduceActiveItemByAmount(Game1.player, amount);

                //for right now, use the cost multiplier (number of cycles cognate) as the experience gained.
                int experienceValue = costMultiplier;

                Alchemy.AddAlchemyExperience(experienceValue);
            }

            //a transmute (at least one) happened, play the cash money sound
            if (didTransmuteOccur && !didTransmuteFail)
                PlayMoneySound();

            //a rebound occurred, apply the damage and also play the ouchy sound.
            if (didTransmuteFail)
            {

                Alchemy.TakeDamageFromRebound(reboundDamageTaken);
                PlayReboundSound();
            }
        }

        public void HandleTransmuteEvent (Item heldItem, int attemptedAmount, int actualValue, int costMultiplier)
        {
            //cost of a single item, multiplied by the cost multiplier below
            int transmutationCost = (int)Math.Ceiling(Alchemy.GetTransmutationMarkupPercentage() * actualValue);

            //unlike liquidations, amount shouldn't have to change in the loop
            int amount = costMultiplier;

            //nor should totalCost of a single cycle
            int totalCost = transmutationCost * amount;            

            //placeholder for determining if the transmute occurs, so it knows to play a sound.
            bool didTransmuteOccur = false;

            //placeholder for determining if the transmute rebounds, so it knows to play a different sound.
            bool didTransmuteFail = false;

            //if the transmute did fail, this preserves the damage so we can apply it in one cycle, otherwise batches look weird af
            int reboundDamageTaken = 0;

            //loop for each transmute-cycle attempt
            while (Game1.player.money >= totalCost && attemptedAmount > 0)
            {
                attemptedAmount -= amount;

                double staminaCost = Alchemy.GetStaminaCostForTransmutation(actualValue);
                //if the player lacks the stamina to execute a transmute, abort
                if (Game1.player.Stamina <= staminaCost)
                {
                    continue;
                }

                //if we fail this check, it's because a rebound would kill the player.
                //if the rebound chance is zero, this check will automatically pass.
                if (!Alchemy.CanSurviveRebound(actualValue))
                {
                    continue;
                }

                //if we fail this check, transmutation will fail this cycle.
                //this is our "rebound check"
                if (Alchemy.DidPlayerFailReboundCheck())
                {
                    reboundDamageTaken += actualValue;
                    didTransmuteFail = true;
                    //the conduit profession makes it so that the transmutation succeeds anyway, after taking damage.
                    if (!Game1.player.professions.Contains((int)Professions.Conduit))
                        continue;
                }

                didTransmuteOccur = true;

                //a rebound obviates a lucky transmute, but a profession trait obviates stamina drain when you rebound.
                if (!didTransmuteFail && !Game1.player.professions.Contains((int)Professions.Conduit))
                    Alchemy.HandleStaminaDeduction(staminaCost);
                
                Game1.player.Money -= totalCost;

                Item spawnedItem = heldItem.getOne();

                //since we have at least 1 already, add amount - 1 to the stack. if amount is 1, we're adding nothing.
                if (amount > 1)
                {
                    spawnedItem.addToStack(amount - 1);
                }

                Game1.createItemDebris(spawnedItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection, (GameLocation)null);

                //for right now, use the cost multiplier (number of cycles cognate) as the experience gained.
                int experienceValue = costMultiplier;

                Alchemy.AddAlchemyExperience(experienceValue);
            }

            //a transmute (at least one) happened, play the magicky sound
            if (didTransmuteOccur && !didTransmuteFail)
                PlayMagickySound();

            //a rebound occurred, apply the damage and also play the ouchy sound.
            if (didTransmuteFail)
            {

                Alchemy.TakeDamageFromRebound(reboundDamageTaken);
                PlayReboundSound();
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

        //hopefully the stuff needed to support spacechase0's show-experience-bars mod can start here

        private static bool hasExperienceBarsMod = false;

        private void CheckForExperienceBarsMod()
        {
            if (!Helper.ModRegistry.IsLoaded("spacechase0.ExperienceBars"))
            {
                Log.info("Experience Bars not found");
                return;
            }

            hasAllProfessionsMod = true;

            Log.info("Experience Bars mod found, adding alchemy experience bar renderer.");       
        }

        private static bool hasAllProfessionsMod = false;
        private void CheckForAllProfessionsMod()
        {
            if (!eeHelper.ModRegistry.IsLoaded("community.AllProfessions"))
            {
                Log.info("All Professions not found.");
                return;
            }

            Log.info("All Professions mod found. You will get every alchemy profession for your level.");
            hasAllProfessionsMod = true;
        }
    }
}