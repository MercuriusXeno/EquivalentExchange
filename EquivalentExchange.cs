using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using EquivalentExchange.Models;
using System.IO;
using Netcode;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Tools;
using StardewValley.Network;
using SpaceCore.Events;
using SpaceCore;

namespace EquivalentExchange
{

    /// <summary>The mod entry point.</summary>
    public class EquivalentExchange : Mod
    {
        //instantiate config
        private ConfigurationModel Config;

        //the mod's "static" instance, initialized by Entry. There caN ONly bE ONe
        public static EquivalentExchange instance;

        // holds the player data for all active players, then uses statics to expose this player's data.
        public SaveDataModel currentPlayerData = new SaveDataModel();

        //config for if the mod is allowed to play sounds
        public static bool canPlaySounds;

        public const string MSG_DATA = "EquivalentExchange.AlchemySkill.Data";
        public const string MSG_EXPERIENCE = "EquivalentExchange.AlchemySkill.Experience";
        public const string MSG_LEVEL = "EquivalentExchange.AlchemySkill.Level";
        public const string MSG_CURRENT_ENERGY = "EquivalentExchange.AlchemySkill.CurrentEnergy";
        public const string MSG_MAX_ENERGY = "EquivalentExchange.AlchemySkill.MaxEnergy";
        public const string MSG_TOTAL_VALUE_TRANSMUTED = "EquivalentExchange.AlchemySkill.TotalValueTransumted";
        public const string MSG_REGEN_TICK = "EquivalentExchange.AlchemySkill.RegenTick";

        //handles all the things.
        public override void Entry(IModHelper helper)
        {
            //set the static instance variable. is this an oxymoron?
            instance = this;            

            //read the config file, poached from horse whistles, get the configured keys and settings
            Config = helper.ReadConfig<ConfigurationModel>();

            //add handler for the "transmute/copy" button.
            ControlEvents.KeyPressed += ControlEvents_KeyPressed;

            //exclusively to figure out if ctrl or shift have been let go of.
            ControlEvents.KeyReleased += ControlEvents_KeyReleased;

            //wire up the library scraping function to occur on save-loading to defer recipe scraping until all mods are loaded, optimistically.
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;

            //we need this to save our alchemists['] data
            SaveEvents.BeforeSave += SaveEvents_BeforeSave;

            //set texture files in memory, they're tiny things.
            DrawingUtil.HandleTextureCaching();

            //handles high resolution update ticks, like regeneration and held keys.
            GameEvents.UpdateTick += GameEvents_UpdateTick;

            //wire up the PreRenderHUD event so I can display info bubbles when needed
            GraphicsEvents.OnPreRenderHudEvent += GraphicsEvents_OnPreRenderHudEvent;

            //check for all professions mod: if it's here we run a wireup to give the player all skills professions at the right time (or after), when present.
            CheckForAllProfessionsMod();

            //location wireup to detect displacement from leylines for debug reasons
            LocationEvents.LocationsChanged += LocationEvents_LocationsChanged; ;

            //check for experience bars mod: if it's here we draw hud elements for the new alchemy skill
            CheckForExperienceBarsMod();
            if (hasExperienceBarsMod)
            {
                GraphicsEvents.OnPostRenderHudEvent += GraphicsEvents_OnPostRenderHudEvent;
            }

            //post render event for skills menu
            GraphicsEvents.OnPostRenderGuiEvent += DrawAfterGUI;

            // handles end of night event requirements like alchemy energy being restored and level ups.
            SpaceEvents.ShowNightEndMenus += SpaceEvents_ShowNightEndMenus;

            // stuff we have to do for multiplayer now, handles client join events to cascade data to the non-hosts.
            SpaceEvents.ServerGotClient += SpaceEvents_ServerGotClient;

            Networking.RegisterMessageHandler(MSG_DATA, OnDataMessage);
            Networking.RegisterMessageHandler(MSG_EXPERIENCE, OnExpMessage);
            Networking.RegisterMessageHandler(MSG_LEVEL, OnLevelMessage);
            Networking.RegisterMessageHandler(MSG_CURRENT_ENERGY, OnCurrentEnergyMessage);
            Networking.RegisterMessageHandler(MSG_MAX_ENERGY, OnMaxEnergyMessage);
            Networking.RegisterMessageHandler(MSG_TOTAL_VALUE_TRANSMUTED, OnTransmutedValueMessage);
            Networking.RegisterMessageHandler(MSG_REGEN_TICK, OnRegenTick);

            //check for chase's skills
            CheckForLuck();
            CheckForCooking();
        }

        private void SpaceEvents_ShowNightEndMenus(object sender, EventArgsShowNightEndMenus e)
        {
            //the new day hook seems to be inconsistent, so this is a full restore at the end of the night.
            Alchemy.RestoreAlkahestryEnergyForNewDay();
            AddEndOfNightMenus();
        }

        private void SpaceEvents_ServerGotClient(object sender, EventArgsServerGotClient e)
        {
            // first thing we need to do is check to see if this player exists in player data. If they don't, let's make them a profile.
            var farmerId = e.FarmerID;
            if (!PlayerData.AlchemyLevel.ContainsKey(farmerId))
                PlayerData.AlchemyLevel[farmerId] = 0;
            if (!PlayerData.AlchemyExperience.ContainsKey(farmerId))
                PlayerData.AlchemyExperience[farmerId] = 0;
            if (!PlayerData.AlkahestryCurrentEnergy.ContainsKey(farmerId))
                PlayerData.AlkahestryCurrentEnergy[farmerId] = 0F;
            if (!PlayerData.AlkahestryMaxEnergy.ContainsKey(farmerId))
                PlayerData.AlkahestryMaxEnergy[farmerId] = 0F;
            if (!PlayerData.TotalValueTransmuted.ContainsKey(farmerId))
                PlayerData.TotalValueTransmuted[farmerId] = 0;

            // Log.debug($"Adding player {farmerId.ToString()} to registry. Keys currently: { PlayerData.AlchemyLevel.Count }");

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // arbitrarily using the first property as the index master, it should be irrelevant, as each should have the same # of keys.
                writer.Write(PlayerData.AlchemyLevel.Count);
                foreach (var lvl in PlayerData.AlchemyLevel)
                {
                    writer.Write(lvl.Key);
                    writer.Write(lvl.Value);
                }
                // we don't need the key beyond this point.
                foreach (var exp in PlayerData.AlchemyExperience)
                {
                    writer.Write(exp.Key);
                    writer.Write(exp.Value);
                }
                foreach (var maxEnergy in PlayerData.AlkahestryMaxEnergy)
                {
                    writer.Write(maxEnergy.Key);
                    writer.Write(maxEnergy.Value);
                }
                foreach (var currentEnergy in PlayerData.AlkahestryCurrentEnergy)
                {
                    writer.Write(currentEnergy.Key);
                    writer.Write(currentEnergy.Value);
                }
                foreach (var totalValue in PlayerData.TotalValueTransmuted)
                {
                    writer.Write(totalValue.Key);
                    writer.Write(totalValue.Value);
                }


                Log.info($"Player data being broadcasted to { e.FarmerID }.");
                Log.info($"Total objects { PlayerData.AlchemyLevel.Count }");
                Networking.ServerSendTo(e.FarmerID, MSG_DATA, stream.ToArray());
            }
        }

        private void OnTransmutedValueMessage(IncomingMessage msg)
        {
            PlayerData.TotalValueTransmuted[msg.FarmerID] = msg.Reader.ReadInt32();
        }

        private void OnMaxEnergyMessage(IncomingMessage msg)
        {
            PlayerData.AlkahestryMaxEnergy[msg.FarmerID] = msg.Reader.ReadSingle();
        }

        private void OnCurrentEnergyMessage(IncomingMessage msg)
        {
            PlayerData.AlkahestryCurrentEnergy[msg.FarmerID] = msg.Reader.ReadSingle();
        }

        private void OnLevelMessage(IncomingMessage msg)
        {
            PlayerData.AlchemyLevel[msg.FarmerID] = msg.Reader.ReadInt32();
        }

        private void OnExpMessage(IncomingMessage msg)
        {
            PlayerData.AlchemyExperience[msg.FarmerID] = msg.Reader.ReadInt32();
        }

        // unabashedly stolen from spacechase, like all things.
        private void OnDataMessage(IncomingMessage msg)
        {
            Log.info("Receiving player data from server.");
            // Log.debug("Receiving updated data from server.");
            int count = msg.Reader.ReadInt32();

            Log.info($"Player data objects found: { count }.");

            // Log.debug($"Count of { count }");

            for (int i = 0; i < count; ++i)
            {
                long id = msg.Reader.ReadInt64();
                int level = msg.Reader.ReadInt32();
                PlayerData.AlchemyLevel[id] = level;
            }

            for (int i = 0; i < count; ++i)
            {
                long id = msg.Reader.ReadInt64();
                int experience = msg.Reader.ReadInt32();
                PlayerData.AlchemyExperience[id] = experience;
            }

            for (int i = 0; i < count; ++i)
            {
                long id = msg.Reader.ReadInt64();
                float maxEnergy = msg.Reader.ReadSingle();
                PlayerData.AlkahestryMaxEnergy[id] = maxEnergy;
            }

            for (int i = 0; i < count; ++i)
            {
                long id = msg.Reader.ReadInt64();
                float currentEnergy = msg.Reader.ReadSingle();
                PlayerData.AlkahestryCurrentEnergy[id] = currentEnergy;
            }

            for (int i = 0; i < count; ++i)
            {
                long id = msg.Reader.ReadInt64();
                int totalValueTransmuted = msg.Reader.ReadInt32();
                PlayerData.TotalValueTransmuted[id] = totalValueTransmuted;
            }
        }

        static int lastTickTime = 0;  // The time at the last tick processed.
        public static int CurrentDefaultTickInterval => 7000 + (Game1.currentLocation?.getExtraMillisecondsPerInGameMinuteForThisLocation() ?? 0);
        public static int CurrentRegenResolution => CurrentDefaultTickInterval / 100;
        private static void RegenerateAlchemyBar()
        {
            //Log.debug("Regen debug out:");
            //Log.debug($"Game1.menuUp || Game1.paused || Game1.dialogueUp || Game1.activeClickableMenu != null || !Game1.shouldTimePass()");
            //Log.debug($"{Game1.menuUp}    {Game1.paused}    {Game1.dialogueUp}    {Game1.activeClickableMenu != null}    {!Game1.shouldTimePass() }");
            //checking for paused or menuUp doesn't return true for some reason, but this is
            //a reliable way to check to see if the player is in a menu to prevent regen.
            if (!Game1.shouldTimePass() || Game1.HostPaused)
                return;
            // Log.debug($"Game tick interval: {Game1.gameTimeInterval}");

            // it's important to point out that only the master game will ever have a gameTimeInterval > 0
            // this *never fires* for clients, which is why the server has to cascade a broadcast message to clients to DoRegenTick();
            int currentTime = Game1.gameTimeInterval;

            if (currentTime - lastTickTime < 0)
                lastTickTime = 0;
            int timeElapsed = currentTime - lastTickTime;
            if (timeElapsed > CurrentRegenResolution)
            {
                DoRegenTick();
                BroadcastRegenTick();
                lastTickTime = currentTime;
            }
        }

        private static void BroadcastRegenTick()
        {
            foreach (var farmer in Game1.otherFarmers)
            {
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                {
                    // arbitrary bool
                    writer.Write(true);
                    Networking.ServerSendTo(farmer.Key, MSG_REGEN_TICK, stream.ToArray());
                }
            }
        }

        private static void OnRegenTick(IncomingMessage msg)
        {
            var arbitraryBool = msg.Reader.ReadBoolean();
            DoRegenTick();
        }

        private static void DoRegenTick()
        {
            // handles this player's regen
            double regenAlchemyBar = Math.Sqrt(AlchemyLevel + 1) / 10D;
            regenAlchemyBar *= MaxEnergy / 100D;
            CurrentEnergy = (float)Math.Min(CurrentEnergy + Math.Max(0.05D, regenAlchemyBar), MaxEnergy);
        }

        public static SaveDataModel PlayerData
        {
            get { return EquivalentExchange.instance.currentPlayerData; }
            set { EquivalentExchange.instance.currentPlayerData = value; }
        }

        public static int AlchemyExperience
        {
            get {
                if (!PlayerData.AlchemyExperience.ContainsKey(PlayerId))
                    return 0;
                // Log.debug($"Current alchemy experience is {PlayerData.AlchemyExperience[PlayerId]}");
                return PlayerData.AlchemyExperience[PlayerId];
            }
            set
            {
                if (!PlayerData.AlchemyExperience.ContainsKey(PlayerId) || PlayerData.AlchemyExperience[PlayerId] != value)
                {
                    PlayerData.AlchemyExperience[PlayerId] = value;
                    using (var stream = new MemoryStream())
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(value);
                        Networking.BroadcastMessage(MSG_EXPERIENCE, stream.ToArray());
                    }
                }
            }
        }

        public static int AlchemyLevel
        {
            get
            {
                if (!PlayerData.AlchemyLevel.ContainsKey(PlayerId))
                    return 0;
                // Log.debug($"Current alchemy level is {PlayerData.AlchemyLevel[PlayerId]}");
                return PlayerData.AlchemyLevel[PlayerId];
            }
            set
            {
                if (!PlayerData.AlchemyLevel.ContainsKey(PlayerId) || PlayerData.AlchemyLevel[PlayerId] != value)
                {
                    PlayerData.AlchemyLevel[PlayerId] = value;
                    using (var stream = new MemoryStream())
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(value);
                        Networking.BroadcastMessage(MSG_LEVEL, stream.ToArray());
                    }
                }
            }
        }

        public static float CurrentEnergy
        {
            get
            {
                if (!PlayerData.AlkahestryCurrentEnergy.ContainsKey(PlayerId))
                    return 0F;
                // Log.debug($"Current energy is {PlayerData.AlkahestryCurrentEnergy[PlayerId]}");
                return PlayerData.AlkahestryCurrentEnergy[PlayerId];
            }
            set
            {
                if (!PlayerData.AlkahestryCurrentEnergy.ContainsKey(PlayerId) || PlayerData.AlkahestryCurrentEnergy[PlayerId] != value)
                {
                    PlayerData.AlkahestryCurrentEnergy[PlayerId] = value;
                    using (var stream = new MemoryStream())
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(value);
                        Networking.BroadcastMessage(MSG_CURRENT_ENERGY, stream.ToArray());
                    }
                }
            }
        }

        public static float MaxEnergy
        {
            get
            {
                if (!PlayerData.AlkahestryMaxEnergy.ContainsKey(PlayerId))
                    return 0F;
                // Log.debug($"Current alchemy max energy is {PlayerData.AlkahestryMaxEnergy[PlayerId]}");
                return PlayerData.AlkahestryMaxEnergy[PlayerId];
            }
            set
            {
                if (!PlayerData.AlkahestryMaxEnergy.ContainsKey(PlayerId) || PlayerData.AlkahestryMaxEnergy[PlayerId] != value)
                {
                    PlayerData.AlkahestryMaxEnergy[PlayerId] = value;
                    using (var stream = new MemoryStream())
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(value);
                        Networking.BroadcastMessage(MSG_MAX_ENERGY, stream.ToArray());
                    }
                }
            }
        }

        public static int TotalValueTransmuted
        {
            get
            {
                if (!PlayerData.TotalValueTransmuted.ContainsKey(PlayerId))
                    return 0;
                // Log.debug($"Current value transmuted is {PlayerData.TotalValueTransmuted[PlayerId]}");
                return PlayerData.TotalValueTransmuted[PlayerId];
            }
            set
            {
                if (!PlayerData.TotalValueTransmuted.ContainsKey(PlayerId) || PlayerData.TotalValueTransmuted[PlayerId] != value)
                {
                    PlayerData.TotalValueTransmuted[PlayerId] = value;
                    using (var stream = new MemoryStream())
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(value);
                        Networking.BroadcastMessage(MSG_TOTAL_VALUE_TRANSMUTED, stream.ToArray());
                    }
                }
            }
        }

        public static long PlayerId
        {
            get { return Game1.player.uniqueMultiplayerID; }
        }

        public static void AddTotalValueTransmuted(int value)
        {
            TotalValueTransmuted += value;
            // Extremely nerfed formula for alchemy energy training.
            var updatedMaxEnergy = (int)Math.Floor(Math.Sqrt(TotalValueTransmuted / 10)) + (AlchemyLevel * 10);
            MaxEnergy = updatedMaxEnergy;
        }

        public static void AddAlchemyExperience(int exp)
        {
            AlchemyExperience += exp;
            var originalAlchemyLevel = AlchemyLevel;
            var resultAlchemyLevel = AlchemyLevel;
            while (resultAlchemyLevel < 10 && AlchemyExperience >= Alchemy.GetAlchemyExperienceNeededForNextLevel(resultAlchemyLevel))
            {
                resultAlchemyLevel += 1;

                //player gained a skilllevel, flag the night time skill up to appear.
                EquivalentExchange.instance.AddSkillUpMenuAppearance(resultAlchemyLevel);
            }

            if (originalAlchemyLevel != resultAlchemyLevel)
            {
                AlchemyLevel = resultAlchemyLevel;
            }
        }

        //integration considerations for chase's skills

        public static bool hasLuck = false;

        private void CheckForLuck()
        {
            if (!Helper.ModRegistry.IsLoaded("spacechase0.LuckSkill"))
            {
                //Log.info($"{LocalizationStrings.Get(LocalizationStrings.LuckSkillNotFound)}");
                return;
            }

            hasLuck = true;
        }

        public static bool hasCooking = false;
        private void CheckForCooking()
        {
            if (!Helper.ModRegistry.IsLoaded("spacechase0.CookingSkill"))
            {
                //Log.info($"{LocalizationStrings.Get(LocalizationStrings.CookingSkillNotFound)}");
                return;
            }

            hasCooking = true;
        }

        //hooked to show the alchemy skill/bar/description/professions on the Skills Page
        private void DrawAfterGUI(object sender, EventArgs args)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                GameMenu menu = Game1.activeClickableMenu as GameMenu;
                if (menu.currentTab == GameMenu.skillsTab)
                {
                    var tabs = (List<IClickableMenu>)Util.GetInstanceField(typeof(GameMenu), menu, "pages");
                    var skills = (SkillsPage)tabs[GameMenu.skillsTab];
                    var alchemySkills = new AlchemySkillsPage(skills.xPositionOnScreen, skills.yPositionOnScreen, skills.width, skills.height, 5 + (hasLuck ? 1 : 0) + (hasCooking ? 1 : 0));
                    alchemySkills.draw(Game1.spriteBatch);
                }
            }
        }
        
        public List<int> showLevelUpMenusByRank = new List<int>();

        internal void AddSkillUpMenuAppearance(int alchemyLevel)
        {
            showLevelUpMenusByRank.Add(alchemyLevel);
        }

        //internal default value for the repeat rate starting point of the auto-fire functionality of transmute/liquidate when the buttons are held.
        private const int AUTO_REPEAT_UPDATE_RATE_REFRESH = 20;

        int heldCounter = 1;
        int updateTickCount = AUTO_REPEAT_UPDATE_RATE_REFRESH;

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            // Log.debug($"Update tick firing. Context isWorldReady returns { Context.IsWorldReady.ToString() }");
            if (!Context.IsWorldReady)
                return;
            RegenerateAlchemyBar();
            HandleHeldTransmuteKeysUpdateTick();

            // Detect (heuristically) whether the player gave the wizard a slime ball. This is the gateway for the rest of the mod.
            HandleWizardSlimeListener();
        }


        // there are two states we need to preserve. 
        // The first is whether the player gave the wizard a slime.        
        // The second is whether the player has unlocked their alchemy ability.
        // In between those, we can infer that the player hasn't seen the dialog.
        private void HandleWizardSlimeListener()
        {

        }

        private void HandleHeldTransmuteKeysUpdateTick()
        {
            if (transmuteKeyHeld)
            {
                heldCounter++;
                if (heldCounter % updateTickCount == 0)
                {
                    HandleEitherTransmuteEvent(Config.TransmuteKey.ToString());
                    updateTickCount = (int)Math.Floor(Math.Max(1, updateTickCount * 0.9F));
                }
            }
        }

        //show the level up menus at night when you hit a profession breakpoint.
        private void AddEndOfNightMenus()
        {
            // hack to fix.. something. Recommended by space to avoid errors.
            if (Game1.endOfNightMenus.Count == 0)
                Game1.endOfNightMenus.Push(new SaveGameMenu());

            bool playerNeedsLevelFiveProfession = AlchemyLevel >= 5 && !Game1.player.professions.Contains((int)Professions.Shaper) && !Game1.player.professions.Contains((int)Professions.Sage);
            bool playerNeedsLevelTenProfession = AlchemyLevel >= 10 && !Game1.player.professions.Contains((int)Professions.Transmuter) && !Game1.player.professions.Contains((int)Professions.Adept) && !Game1.player.professions.Contains((int)Professions.Aurumancer) && !Game1.player.professions.Contains((int)Professions.Conduit);
            bool playerGainedALevel = showLevelUpMenusByRank.Count() > 0;

            //nothing requires our intervention, bypass this method
            if (!playerGainedALevel && !playerNeedsLevelFiveProfession && !playerNeedsLevelTenProfession)
                return;

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
                        Game1.endOfNightMenus.Push(new AlchemyLevelUpMenu(level));
                    }
                }
                //presume we've added all the levels we need, wipe this thing.
                showLevelUpMenusByRank.Clear();
            }
            else if (playerNeedsLevelFiveProfession)
            {
                List<IClickableMenu> existingLevelUps = Game1.endOfNightMenus.Where(x => x.GetType().Equals(typeof(AlchemyLevelUpMenu)) && ((AlchemyLevelUpMenu)x).GetLevel() == 5).ToList();
                if (existingLevelUps.Count == 0)
                    Game1.endOfNightMenus.Push(new AlchemyLevelUpMenu(5));
            }
            else if (playerNeedsLevelTenProfession)
            {
                List<IClickableMenu> existingLevelUps = Game1.endOfNightMenus.Where(x => x.GetType().Equals(typeof(AlchemyLevelUpMenu)) && ((AlchemyLevelUpMenu)x).GetLevel() == 10).ToList();
                if (existingLevelUps.Count == 0)
                    Game1.endOfNightMenus.Push(new AlchemyLevelUpMenu(10));
            }
        }

        //misleading event wireup is actually for the has-all-professions mod, which enables all professions at the appropriate level.
        private void LocationEvents_LocationsChanged(object sender, EventArgsLocationsChanged e)
        {
            if (hasAllProfessionsMod)
            {
                NetList<int, NetInt> professions = Game1.player.professions;
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

        //fires when loading a save, initializes the item blacklist and loads player save data.
        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            InitializePlayerData();
        }

        // the order of this list is not arbitrary - it starts at one end of the the transmutation map
        // and works its way to the other. Certain professions allow alternative inputs/outputs
        // or increase the number of "steps" away from a target you can get inputs.
        public static List<int> transmutationSteps = new List<int>
        {
            Reference.Items.IridiumOre,
            Reference.Items.GoldOre,
            Reference.Items.IronOre,
            Reference.Items.CopperOre,
            Reference.Items.Stone,
            Reference.Items.Clay,
            Reference.Items.Coal,
            Reference.Items.Sap,
            Reference.Items.Fiber,
            Reference.Items.Wood,
            Reference.Items.Hardwood
        };

        public static List<AlchemyTransmutationRecipe> GetTransmutationFormulas()
        {
            var recipes = new List<AlchemyTransmutationRecipe>();

            // iterate over each step in the transmutation "map"; by default, you can transmute into any object from 1 step away.
            foreach(var step in transmutationSteps)
            {
                var index = transmutationSteps.IndexOf(step);
                if (index > 0)
                {
                    recipes.AddRecipeLink(transmutationSteps[index - 1], step);
                }
                if (index < transmutationSteps.Count - 1)
                {
                    recipes.AddRecipeLink(transmutationSteps[index + 1], step);
                }

                // if the player has the "Sage" profession, they can traverse up to 2 steps away.
                if (index > 1 && HasProfession(Professions.Sage))
                {
                    recipes.AddRecipeLink(transmutationSteps[index - 2], step);
                }
                if (index < transmutationSteps.Count - 2 && HasProfession(Professions.Sage))
                {
                    recipes.AddRecipeLink(transmutationSteps[index + 2], step);
                }
                
                // if the player has the adept profession, you can transmute this thing into slimes no matter what, but transmutations cost double.
                if (HasProfession(Professions.Adept))
                {
                    recipes.AddRecipeLink(step, Reference.Items.Slime, 2);
                }

                // if the player has the conduit profession, you can create this thing from slimes no matter what, but transmutations cost double.
                if (HasProfession(Professions.Conduit))
                {
                    recipes.AddRecipeLink(Reference.Items.Slime, step, 2);
                }

                // if the player has the shaper profession you can create stone and clay from slime and vice versa.
                if (HasProfession(Professions.Shaper) && (step == Reference.Items.Stone || step == Reference.Items.Clay))
                {
                    recipes.AddRecipeLink(Reference.Items.Slime, step);
                    recipes.AddRecipeLink(step, Reference.Items.Slime);
                }

                // if the player has the transmuter profession you can create wood from slime and vice versa.
                if (HasProfession(Professions.Transmuter) && step == Reference.Items.Wood)
                {
                    recipes.AddRecipeLink(Reference.Items.Slime, step);
                    recipes.AddRecipeLink(step, Reference.Items.Slime);
                }

                // if the player has the aurumancer profession, you can create gold from slime and vice versa.
                if (HasProfession(Professions.Aurumancer) && step == Reference.Items.GoldOre)
                {
                    recipes.AddRecipeLink(Reference.Items.Slime, step);
                    recipes.AddRecipeLink(step, Reference.Items.Slime);
                }
            }

            foreach(var recipe in recipes)
            {
                var inputName = Util.GetItemName(recipe.InputId);
                var inputValue = Util.GetItemValue(recipe.InputId);
                var outputName = Util.GetItemName(recipe.OutputId);
                var outputValue = Util.GetItemValue(recipe.OutputId);
                //Log.debug($"Transmute: {recipe.GetInputCost()} {inputName} ({inputValue}) into {recipe.GetOutputQuantity()} {outputName} ({outputValue}), costs {recipe.Cost}");
            }
            return recipes;
        }       

        public static bool HasProfession(int profession)
        {
            return Game1.player.professions.Contains(profession);
        }

        //handles reading current player json file and loading them into memory
        private void InitializePlayerData()
        {
            // save is loaded
            if (Context.IsWorldReady)
            {
                //fetch the alchemy save for this game file.
                if (!Game1.IsMultiplayer || Game1.IsMasterGame)
                    PlayerData = Helper.ReadJsonFile<SaveDataModel>(Path.Combine(Constants.CurrentSavePath, $"{Game1.uniqueIDForThisGame.ToString()}.json")) ?? new SaveDataModel();

                // if we are the player/host and we don't have a profile, let's make one for ourselves.
                var farmerId = Game1.player.uniqueMultiplayerID;
                if (!PlayerData.AlchemyLevel.ContainsKey(farmerId))
                    PlayerData.AlchemyLevel[farmerId] = 0;
                if (!PlayerData.AlchemyExperience.ContainsKey(farmerId))
                    PlayerData.AlchemyExperience[farmerId] = 0;
                if (!PlayerData.AlkahestryCurrentEnergy.ContainsKey(farmerId))
                    PlayerData.AlkahestryCurrentEnergy[farmerId] = 0F;
                if (!PlayerData.AlkahestryMaxEnergy.ContainsKey(farmerId))
                    PlayerData.AlkahestryMaxEnergy[farmerId] = 0F;
                if (!PlayerData.TotalValueTransmuted.ContainsKey(farmerId))
                    PlayerData.TotalValueTransmuted[farmerId] = 0;
            }
            Log.info("Player data loaded.");
        }

        //handles writing "each" player's json save to the appropriate file.
        private void SaveEvents_BeforeSave(object sender, EventArgs e)
        {
            SavePlayerData();
        }

        private void SavePlayerData()
        {
            Log.info("Saving player data.");
            if (!Game1.IsMultiplayer || Game1.IsMasterGame)
                Helper.WriteJsonFile<SaveDataModel>(Path.Combine(Constants.CurrentSavePath, $"{ Game1.uniqueIDForThisGame.ToString()}.json"), PlayerData);
        }

        /// <summary>Update the mod's config.json file from the current <see cref="Config"/>.</summary>
        internal void SaveConfig()
        {
            Helper.WriteConfig(Config);
        }
        
        private static void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            //one of these counts as true whenever the player is in the bus stop...
            if (Game1.eventUp)
                return;

            //per the advice of Ento, abort if the player is in an event
            if (Game1.CurrentEvent != null)
                return;

            RenderAlchemyBarToHUD();
        }
        
        public static int GetSlimeValue()
        {
            return Util.GetItemValue(Reference.Items.Slime);
        }

        public static void RenderAlchemyBarToHUD()
        {
            int scale = 4;
            int alchemyBarWidth = DrawingUtil.alchemyBarSprite.Width * scale;
            int alchemyBarHeight = DrawingUtil.alchemyBarSprite.Height * scale;

            //special consideration for maps that are smaller than your display viewport (horizontally, this happens at the bus stop)
            int tileSizeWidth = Game1.player.currentLocation.Map.DisplayWidth;

            //apply special constraints in the event of small-ish maps here
            bool isPlayerOutdoors = Game1.player.currentLocation.IsOutdoors;

            //borders from the screen are viewport width / 2 - tileSizeWidth / 2
            int viewportBorderWidth = Math.Max(0, Game1.viewport.Width / 2 - tileSizeWidth / 2);

            int alchemyBarPositionX = Game1.viewport.Width - (isPlayerOutdoors ? viewportBorderWidth : 0) - alchemyBarWidth - 120;
            int alchemyBarPositionY = Game1.viewport.Height - alchemyBarHeight - 16;

            Vector2 alchemyBarPosition = new Vector2(alchemyBarPositionX, alchemyBarPositionY);

            Game1.spriteBatch.Draw(DrawingUtil.alchemyBarSprite, alchemyBarPosition, new Rectangle(0, 0, DrawingUtil.alchemyBarSprite.Width, DrawingUtil.alchemyBarSprite.Height), Color.White, 0, new Vector2(), scale, SpriteEffects.None, 1);
            if (CurrentEnergy > 0)
            {
                Rectangle targetArea = new Rectangle(3, 13, 6, 41);
                float perc = CurrentEnergy / MaxEnergy;
                int h = (int)(targetArea.Height * perc);
                targetArea.Y += targetArea.Height - h;
                targetArea.Height = h;

                targetArea.X *= 4;
                targetArea.Y *= 4;
                targetArea.Width *= 4;
                targetArea.Height *= 4;
                targetArea.X += (int)alchemyBarPosition.X;
                targetArea.Y += (int)alchemyBarPosition.Y;
                Game1.spriteBatch.Draw(DrawingUtil.alchemyBarFillSprite, targetArea, new Rectangle(0, 0, 1, 1), Color.White);

                int alchemyBarMaxX = alchemyBarPositionX + alchemyBarWidth;
                int alchemyBarMaxY = alchemyBarPositionY + alchemyBarHeight;
                //perform hover over manually
                if (Game1.getMouseX() >= alchemyBarPositionX && Game1.getMouseX() <= alchemyBarMaxX && Game1.getMouseY() >= alchemyBarPositionY && Game1.getMouseY() <= alchemyBarMaxY)
                {
                    string alkahestryEnergyString = $"{ ((int)Math.Floor(CurrentEnergy)).ToString()}/{ MaxEnergy.ToString()}";
                    float stringWidth = Game1.dialogueFont.MeasureString(alkahestryEnergyString).X;
                    Vector2 alkahestryEnergyStringPosition = new Vector2(alchemyBarPosition.X - stringWidth - 32, alchemyBarPosition.Y + 64);
                    Game1.spriteBatch.DrawString(Game1.dialogueFont, alkahestryEnergyString, alkahestryEnergyStringPosition, Color.White);
                }
            }
        }

        private static bool brokeRepeaterDueToNoEnergy = false;

        //handles the release key event for figuring out if control or shift is let go of
        public static void ControlEvents_KeyReleased(object sender, EventArgsKeyPressed e)
        {
            //let the app know the shift key is released
            if (e.KeyPressed == leftShiftKey || e.KeyPressed == rightShiftKey)
                SetModifyingControlKeyState(e.KeyPressed, false);

            //the key for transmuting is pressed, fire once and then initiate the callback routine to auto-fire.
            if (instance.Config.TransmuteKey.Equals(e.KeyPressed.ToString()))
            {
                brokeRepeaterDueToNoEnergy = false;
                transmuteKeyHeld = false;
                instance.heldCounter = 1;
                instance.updateTickCount = AUTO_REPEAT_UPDATE_RATE_REFRESH;
            }
        }

        //remembers the state of the mod control keys so we can do some fancy stuff.
        public static bool transmuteKeyHeld = false;

        //handles the key press event for figuring out if control or shift is held down, or either of the mod's major transmutation actions is being attempted.
        public static void ControlEvents_KeyPressed(object sender, EventArgsKeyPressed e)
        {
            //let the app know the shift key is held
            if (e.KeyPressed == leftShiftKey || e.KeyPressed == rightShiftKey)
                SetModifyingControlKeyState(e.KeyPressed, true);

            //the key for transmuting is pressed, fire once and then initiate the callback routine to auto-fire.
            if (instance.Config.TransmuteKey.Equals(e.KeyPressed.ToString()))
            {
                transmuteKeyHeld = true;
                HandleEitherTransmuteEvent(e.KeyPressed.ToString());
            }

            //the key pressed is one of the mods keys.. I'm doing this so I don't fire logic for anything unless either of the mod's keys were pressed.            
            if (instance.Config.NormalizeKey.Equals(e.KeyPressed.ToString()))
            {
                HandleEitherTransmuteEvent(e.KeyPressed.ToString());
            }
        }

        //sets up the basic structure of either transmute event, since they have some common ground
        private static void HandleEitherTransmuteEvent(string keyPressed)
        {
            // save is loaded
            if (Context.IsWorldReady)
            {
                //per the advice of Ento, abort if the player is in an event
                if (Game1.CurrentEvent != null)
                    return;

                //something may have gone wrong if this is null, maybe there's no save data?
                if (Game1.player != null)
                {
                    //get the player's current item
                    Item heldItem = Game1.player.CurrentItem;

                    //player is holding item
                    if (heldItem != null)
                    {
                        //get the item's ID
                        int heldItemID = heldItem.parentSheetIndex;

                        //alchemy energy can be used to execute a complex tool action if a tool is in hand.
                        if (heldItem is StardewValley.Tool && keyPressed.ToString() == instance.Config.TransmuteKey)
                        {
                            Tool itemTool = heldItem as Tool;

                            bool isScythe = itemTool is MeleeWeapon && itemTool.Name.ToLower().Contains("scythe");
                            bool isAxe = itemTool is Axe;
                            bool isPickaxe = itemTool is Pickaxe;
                            bool isHoe = itemTool is Hoe;
                            bool isWateringCan = itemTool is WateringCan;

                            bool canDoToolAlchemy = isScythe || isAxe || isPickaxe || isHoe || isWateringCan;

                            if (canDoToolAlchemy)
                            {
                                Alchemy.HandleToolTransmute(itemTool);
                            }
                        }

                        //abort any transmutation event for blacklisted items or items that for whatever reason can't exist in world.
                        if (!GetTransmutationFormulas().HasItem(heldItemID) || !heldItem.canBeDropped())
                        {
                            return;
                        }

                        //get the transmutation value, it's based on what it's worth to the player, including profession bonuses. This affects both cost and value.
                        int actualValue = ((StardewValley.Object)heldItem).sellToStorePrice();

                        //try to transmute [copy] the item
                        if (keyPressed.ToString() == instance.Config.TransmuteKey)
                        {
                            var shouldBreakOutOfRepeater = Alchemy.HandleTransmuteEvent(heldItem, actualValue);
                            if (shouldBreakOutOfRepeater && !brokeRepeaterDueToNoEnergy)
                            {
                                brokeRepeaterDueToNoEnergy = true;
                                instance.heldCounter = 1;
                                instance.updateTickCount = AUTO_REPEAT_UPDATE_RATE_REFRESH * 2;
                            }
                        }

                        //try to normalize the item [make all items of a different quality one quality and exchange any remainder for gold]
                        if (keyPressed.ToString() == instance.Config.NormalizeKey)
                        {
                            Alchemy.HandleNormalizeEvent(heldItem, actualValue);
                        }
                    }
                }
            }
        }

        //control key modifiers [shift and ctrl], I include both for a more robust "is either pressed" mechanic.
        public static bool leftShiftKeyPressed = false;
        public static bool rightShiftKeyPressed = false;

        //simple consts to keep code clean, both shift keys, both control keys.
        public const Keys leftShiftKey = Keys.LeftShift;
        public const Keys rightShiftKey = Keys.RightShift;

        //convenience methods for detecting when either keys are pressed to modify amount desired from liquidation/transmutes.
        public static bool IsShiftKeyPressed()
        {
            return leftShiftKeyPressed || rightShiftKeyPressed;
        }

        //handler for which flag to set when X key is pressed/released
        public static void SetModifyingControlKeyState(Keys keyChanged, bool isPressed)
        {
            switch (keyChanged)
            {
                case leftShiftKey:
                    leftShiftKeyPressed = isPressed;
                    break;
                case rightShiftKey:
                    rightShiftKeyPressed = isPressed;
                    break;
                default:
                    break;
            }
        }

        //// holds a list of transmutation recipes used by the mod.
        //public static Dictionary<int> transmutationFormulas = new Dictionary<int>({
        //        Game1.object
        //    });
    
        //hopefully the stuff needed to support spacechase0's show-experience-bars mod can start here

        public static bool hasExperienceBarsMod = false;

        public void CheckForExperienceBarsMod()
        {
            if (!Helper.ModRegistry.IsLoaded("spacechase0.ExperienceBars"))
            {
                //Log.info($"{LocalizationStrings.Get(LocalizationStrings.ExperienceBarsNotFound)}");
                return;
            }

            hasAllProfessionsMod = true;

            //Log.info($"{LocalizationStrings.Get(LocalizationStrings.ExperienceBarsFound)}");
        }

        public static bool hasAllProfessionsMod = false;
        public void CheckForAllProfessionsMod()
        {
            if (!Helper.ModRegistry.IsLoaded("community.AllProfessions"))
            {
                //Log.info($"{LocalizationStrings.Get(LocalizationStrings.AllProfessionsNotFound)}");
                return;
            }

            //Log.info($"{LocalizationStrings.Get(LocalizationStrings.AllProfessionsFound)}");
            hasAllProfessionsMod = true;
        }
    }
}