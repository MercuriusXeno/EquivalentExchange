using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using EquivalentExchange;

namespace EquivalentExchange
{
    public class AlchemistFarmer : StardewValley.Farmer
    {
        //default experience progression values that I'm gonna try to balance around, somehow.
        public static readonly int[] alchemyExperienceNeededPerLevel = new int[] { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

        //save data for the mod's alchemy skill, in list form for per-player lookups.
        public SaveDataModel playerSaveData = new SaveDataModel();

        //constructor for keeping the player's data in this class.
        public AlchemistFarmer (SaveDataModel playerData)
        {
            if (playerData == null)
                playerData = new SaveDataModel();
            this.playerSaveData = playerData;
        }

        public int GetAlchemyLevel()
        {
            return playerSaveData.alchemyLevel;
        }

        public int GetAlchemyExperience()
        {
            return playerSaveData.alchemyExp;
        }

        public void AddAlchemyExperience(int exp)
        {
            playerSaveData.alchemyExp += exp;

            while (GetAlchemyExperience() >= GetAlchemySkillForNextLevel())
            {
                playerSaveData.alchemyLevel++;
            }
        }

        public int GetAlchemySkillForNextLevel()
        {
            int level = GetAlchemyLevel();
            return alchemyExperienceNeededPerLevel[level];
        }

        public float GetAlchemyStaminaCostSkillMultiplier()
        {
            return 1 - (GetAlchemyLevel() * 0.075F) - (playerSaveData.hasSageProfession ? 0.15F : 0.0F);
        }

        //professions:
        //Shaper(Rank 5): Daily Luck(0 - 20%) is twice as effective.
        //Transmuter(Rank 10): Coefficient Cost -= 1 [Big Bonus]
        //Adept(Rank 10): Proximity to Wizard Tower increases your chance of Free Transmutes.
        //Sage(Rank 5): PlayerSkillMult -= 0.15 [Stamina Cost max 75% reduction becomes 90%]
        //Aurumancer(Rank 10): Coefficient Value += 0.25 [Big Bonus]
        //Conduit(Rank 10): Rebounds now count as a free transmute, but you still take health damage.
    }
}
