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

        public static void InitAlchemySkill(SkillsPage skills)
        {
            int alchemyLevel = EquivalentExchange.instance.currentPlayerData.AlchemyLevel;

            // Bunch of stuff from the constructor
            int num2 = 0;
            int num3 = skills.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 4 * Game1.tileSize - Game1.pixelZoom;
            int num4 = skills.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - Game1.pixelZoom * 3;
            for (int i = 4; i < 10; i += 5)
            {
                int j = 5;
                if (EquivalentExchange.hasLuck)
                    j++;
                if (EquivalentExchange.hasCooking)
                    j++;

                string text = "";
                string text2 = "";
                bool flag = false;

                flag = (alchemyLevel > i);

                //that said, the chosen profession is needed for.. something?
                int chosenProfessionNumber = Professions.getProfessionForSkill(i + 1);

                object[] args = new object[] { text, text2, AlchemyLevelUpMenu.getProfessionDescription(chosenProfessionNumber) };
                Util.CallInstanceMethod(typeof(SkillsPage), skills, "parseProfessionDescription", args);
                text = (string)args[0];
                text2 = (string)args[1];

                if (flag && (i + 1) % 5 == 0)
                {
                    var skillBars = (List<ClickableTextureComponent>)Util.GetInstanceField(typeof(AlchemySkillPage), skills, "skillBars");
                    skillBars.Add(new ClickableTextureComponent(string.Concat(chosenProfessionNumber), new Rectangle(num2 + num3 - Game1.pixelZoom + i * (Game1.tileSize / 2 + Game1.pixelZoom), num4 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6), 14 * Game1.pixelZoom, 9 * Game1.pixelZoom), null, text, Game1.mouseCursors, new Rectangle(159, 338, 14, 9), (float)Game1.pixelZoom, true));
                }
                num2 += Game1.pixelZoom * 6;
            }
            int k = 5;
            if (EquivalentExchange.hasLuck)
                k++;
            if (EquivalentExchange.hasCooking)
                k++;
            int num6 = k;
            string text3 = "";
            var skillAreas = (List<ClickableTextureComponent>)Util.GetInstanceField(typeof(AlchemySkillPage), skills, "skillAreas");
            skillAreas.Add(new ClickableTextureComponent(string.Concat(num6), new Rectangle(num3 - Game1.tileSize * 2 - Game1.tileSize * 3 / 4, num4 + k * (Game1.tileSize / 2 + Game1.pixelZoom * 6), Game1.tileSize * 2 + Game1.pixelZoom * 5, 9 * Game1.pixelZoom), string.Concat(num6), text3, null, Rectangle.Empty, 1f, false));
        }

        public static void DrawAlchemySkill(SkillsPage skills)
        {
            int level = EquivalentExchange.instance.currentPlayerData.AlchemyLevel;

            SpriteBatch b = Game1.spriteBatch;
            int j = 5;
            if (EquivalentExchange.hasLuck)
                j++;
            if (EquivalentExchange.hasCooking)
                j++;

            int num;
            int num2;

            //experiment here for the skill icon x position?
            num = skills.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 4 * Game1.tileSize - 8;
            //experiment here for the skill icon y position?
            num2 = skills.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - Game1.pixelZoom * 2;

            int num3 = 0;
            for (int i = 0; i < 10; i++)
            {
                bool playerHasRank = false;
                bool flag2 = false;
                string text = "";
                int num4 = 0;
                Rectangle empty = Rectangle.Empty;

                playerHasRank = (level > i);
                if (i == 0)
                {
                    text = "Alchemy";
                }
                num4 = level;
                flag2 = false;
                empty = new Rectangle(0, 0, 10, 10);

                //first draw the skill name, happens when i == 0
                if (!text.Equals(""))
                {
                    //alchemy skill text
                    b.DrawString(Game1.smallFont, text, new Vector2((float)num + 20 - Game1.smallFont.MeasureString(text).X - (float)(Game1.pixelZoom * 4) - (float)Game1.tileSize, (float)(num2 + Game1.pixelZoom + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), Game1.textColor);
                    //I think this is the drop shadow for the skill icon
                    b.Draw(DrawingUtil.alchemySkillIcon, new Vector2((float)(num + 12 - Game1.pixelZoom * 16), (float)(num2 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(empty), Color.Black * 0.3f, 0f, Vector2.Zero, (float)Game1.pixelZoom * 0.75f, SpriteEffects.None, 0.85f);
                    //I think this is the actual skill icon.
                    b.Draw(DrawingUtil.alchemySkillIcon, new Vector2((float)(num + 12 - Game1.pixelZoom * 15), (float)(num2 - Game1.pixelZoom + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(empty), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom * 0.75f, SpriteEffects.None, 0.87f);
                }

                //player has doesn't have the skill yet and the next square is a profession rank
                if (!playerHasRank && (i + 1) % 5 == 0)
                {
                    //this is the drop shadow for profession rank 5
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num3 + num - Game1.pixelZoom + i * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num2 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(145, 338, 14, 9)), Color.Black * 0.35f, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                    //this is the icon for profession rank 5
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num3 + num + i * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num2 - Game1.pixelZoom + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(145, 338, 14, 9)), Color.White * 0.65f, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                }
                //the next square NOT a profession rank
                else if ((i + 1) % 5 != 0)
                {
                    //I think this is the drop shadow for the level markers
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num3 + num - Game1.pixelZoom + i * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num2 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(129, 338, 8, 9)), Color.Black * 0.35f, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.85f);
                    //I think this is the golden bit that covers the drop shadow.
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num3 + num + i * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num2 - Game1.pixelZoom + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(129 + (playerHasRank ? 8 : 0), 338, 8, 9)), Color.White * (playerHasRank ? 1f : 0.65f), 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                }

                //i == 9; (i + 1) == 10, not sure what this is doing.
                if (i == 9)
                {
                    NumberSprite.draw(num4, b, new Vector2((float)(num3 + num + (i + 2) * (Game1.tileSize / 2 + Game1.pixelZoom) + Game1.pixelZoom * 3 + ((num4 >= 10) ? (Game1.pixelZoom * 3) : 0)), (float)(num2 + Game1.pixelZoom * 4 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), Color.Black * 0.35f, 1f, 0.85f, 1f, 0, 0);
                    NumberSprite.draw(num4, b, new Vector2((float)(num3 + num + (i + 2) * (Game1.tileSize / 2 + Game1.pixelZoom) + Game1.pixelZoom * 4 + ((num4 >= 10) ? (Game1.pixelZoom * 3) : 0)), (float)(num2 + Game1.pixelZoom * 3 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), (flag2 ? Color.LightGreen : Color.SandyBrown) * ((num4 == 0) ? 0.75f : 1f), 1f, 0.87f, 1f, 0, 0);
                }

                //makes the next square a fatty, I think.
                if ((i + 1) % 5 == 0)
                {
                    num3 += Game1.pixelZoom * 6;
                }
            }
        }

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
            public static string SkillIcon = $"alchemySkillIcon.png";
            public static string SkillIconBordered = $"alchemySkillIconBordered.png";
            public static string ShaperIcon = $"shaperProfessionIcon_Hybrid.png";
            public static string TransmuterIcon = $"transmuterProfessionIcon.png";
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
