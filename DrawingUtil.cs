using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    class DrawingUtil
    {
        public static void DoPostRenderHudEvent()
        {

            if (Game1.activeClickableMenu != null)
                return;

            Type t = Type.GetType("ExperienceBars.Mod, ExperienceBars");

            int currentAlchemyLevel = EquivalentExchange.instance.currentPlayerData.AlchemyLevel;
            int currentAlchemyExperience = EquivalentExchange.instance.currentPlayerData.AlchemyExperience;
            int x = 10;
            int y = (int)Util.GetStaticField(t, "expBottom");

            int previousExperienceRequired = 0, nextExperienceRequired = 1;
            if (currentAlchemyLevel == 0)
            {
                nextExperienceRequired = Alchemy.GetAlchemyExperienceNeededForNextLevel();
            }
            else if (currentAlchemyLevel != 10)
            {
                previousExperienceRequired = Alchemy.GetAlchemyExperienceNeededForLevel(currentAlchemyLevel - 1);
                nextExperienceRequired = Alchemy.GetAlchemyExperienceNeededForLevel(currentAlchemyLevel);
            }

            int progressTowardCurrentLevel = currentAlchemyExperience - previousExperienceRequired;
            int experienceGapForCurrentLevel = nextExperienceRequired - previousExperienceRequired;
            float progressBarPercentage = (float)progressTowardCurrentLevel / experienceGapForCurrentLevel;
            if (currentAlchemyLevel == 10)
            {
                progressBarPercentage = -1;
            }

            object[] args = new object[]
            {
                x, y,
                alchemySkillIcon, new Rectangle( 0, 0, 16, 16 ),
                currentAlchemyLevel, progressBarPercentage,
                new Color( 196, 79, 255 ),
            };
            Util.CallStaticMethod(t, "renderSkillBar", args);

            Util.SetStaticField(t, "expBottom", y + 40);
        }

        public struct Icons
        {
            public static string SkillIcon = $"alchemySkillIconDeeper.png";
            public static string SkillIconBordered = $"alchemySkillIconBorderedNewColorExtended.png";
            public static string ShaperIcon = $"shaperProfessionIcon_Hybrid.png";
            public static string TransmuterIcon = $"transmuterProfessionIconV2.png";
            public static string AdeptIcon = $"adeptProfessionIcon.png";
            public static string SageIcon = $"sageProfessionIcon.png";
            public static string AurumancerIcon = $"aurumancerProfessionIcon.png";
            public static string ConduitIcon = $"conduitProfessionIcon.png";
        }

        public static Texture2D alchemySkillIcon;
        public static Texture2D alchemySkillIconBordered;
        public static Texture2D alchemyShaperIcon;
        public static Texture2D alchemyTransmuterIcon;
        public static Texture2D alchemyAdeptIcon;
        public static Texture2D alchemySageIcon;
        public static Texture2D alchemyAurumancerIcon;
        public static Texture2D alchemyConduitIcon;

        public static string assetPrefix = "assets\\";

        //handle capturing icons/textures for the mod's texture needs.
        public static void HandleTextureCaching()
        {
            alchemySkillIcon = EquivalentExchange.instance.eeHelper.Content.Load<Texture2D>($"{assetPrefix}{Icons.SkillIcon}");
            alchemySkillIconBordered = EquivalentExchange.instance.eeHelper.Content.Load<Texture2D>($"{assetPrefix}{Icons.SkillIconBordered}");
            alchemyShaperIcon = EquivalentExchange.instance.eeHelper.Content.Load<Texture2D>($"{assetPrefix}{Icons.ShaperIcon}");
            alchemyTransmuterIcon = EquivalentExchange.instance.eeHelper.Content.Load<Texture2D>($"{assetPrefix}{Icons.TransmuterIcon}");
            alchemyAdeptIcon = EquivalentExchange.instance.eeHelper.Content.Load<Texture2D>($"{assetPrefix}{Icons.AdeptIcon}");
            alchemySageIcon = EquivalentExchange.instance.eeHelper.Content.Load<Texture2D>($"{assetPrefix}{Icons.SageIcon}");
            alchemyAurumancerIcon = EquivalentExchange.instance.eeHelper.Content.Load<Texture2D>($"{assetPrefix}{Icons.AurumancerIcon}");
            alchemyConduitIcon = EquivalentExchange.instance.eeHelper.Content.Load<Texture2D>($"{assetPrefix}{Icons.ConduitIcon}");
        }

        internal static Texture2D GetProfessionTexture(int profession)
        {
            switch (profession)
            {
                case (int)Professions.Shaper:
                    return alchemyShaperIcon;
                case (int)Professions.Sage:
                    return alchemySageIcon;
                case (int)Professions.Transmuter:
                    return alchemyTransmuterIcon;
                case (int)Professions.Adept:
                    return alchemyAdeptIcon;
                case (int)Professions.Aurumancer:
                    return alchemyAurumancerIcon;
                case (int)Professions.Conduit:
                    return alchemyConduitIcon;
            }
            return alchemySkillIconBordered;
        }
    }
}
