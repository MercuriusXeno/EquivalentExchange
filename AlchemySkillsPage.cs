// Decompiled with JetBrains decompiler
// Type: StardewValley.Menus.SkillsPage
// Assembly: Stardew Valley, Version=1.2.6400.27469, Culture=neutral, PublicKeyToken=null
// MVID: 77B7094A-F6F0-4ACC-91F4-E335E2733EDB
// Assembly location: D:\Steam\steamapps\common\Stardew Valley\Stardew Valley.exe

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using EquivalentExchange;
using StardewValley.Menus;
using StardewValley;

namespace EquivalentExchange
{
    public class AlchemySkillsPage : IClickableMenu
    {
        public List<ClickableTextureComponent> skillBars = new List<ClickableTextureComponent>();
        public List<ClickableTextureComponent> skillAreas = new List<ClickableTextureComponent>();
        private string hoverText = "";
        private string hoverTitle = "";
        private int skillOrderIndex = -1;
        private Texture2D professionImage = null;
        private int[] playerPanelFrames = new int[4]
        {
      0,
      1,
      0,
      2
        };
        public const int region_special1 = 10201;
        public const int region_special2 = 10202;
        public const int region_special3 = 10203;
        public const int region_special4 = 10204;
        public const int region_special5 = 10205;
        public const int region_special6 = 10206;
        public const int region_special7 = 10207;
        public const int region_skillArea1 = 0;
        public const int region_skillArea2 = 1;
        public const int region_skillArea3 = 2;
        public const int region_skillArea4 = 3;
        public const int region_skillArea5 = 4;
        private int playerPanelIndex;
        private int playerPanelTimer;
        private Rectangle playerPanel;

        public AlchemySkillsPage(int x, int y, int width, int height, int skillOrderIndex)
          : base(x, y, width, height, false)
        {
            this.skillOrderIndex = skillOrderIndex;
            int x1 = this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + Game1.tileSize * 5 / 4;
            int y1 = this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (int)((double)height / 2.0) + Game1.tileSize * 5 / 4;
            this.playerPanel = new Rectangle(this.xPositionOnScreen + Game1.tileSize, this.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder, Game1.tileSize * 2, Game1.tileSize * 3);
            
            int num5 = 0;
            int xStart = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru ? this.xPositionOnScreen + width - Game1.tileSize * 7 - Game1.tileSize * 3 / 4 + Game1.pixelZoom : this.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 4 * Game1.tileSize - Game1.pixelZoom;
            int yStart = this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - Game1.pixelZoom * 3;
            int skillIndex = 4;
            while (skillIndex < 10)
            {
                int whichProfession = Professions.getProfessionForSkill(skillIndex + 1);
                string professionBlurb = Professions.GetProfessionTitleFromNumber(whichProfession);
                string professionTitle = Professions.GetProfessionDescription(whichProfession);
                bool flag = EquivalentExchange.instance.currentPlayerData.AlchemyLevel > skillIndex;
                
                if (flag && (skillIndex + 1) % 5 == 0)
                {
                    List<ClickableTextureComponent> skillBars = this.skillBars;
                    ClickableTextureComponent skillTextureComponent = new ClickableTextureComponent(string.Concat(whichProfession), new Rectangle(num5 + xStart - Game1.pixelZoom + skillIndex * (Game1.tileSize / 2 + Game1.pixelZoom), yStart + skillOrderIndex * (Game1.tileSize / 2 + Game1.pixelZoom * 6), 14 * Game1.pixelZoom, 9 * Game1.pixelZoom), (string)null, professionBlurb, Game1.mouseCursors, new Rectangle(159, 338, 14, 9), (float)Game1.pixelZoom, true);
                    skillBars.Add(skillTextureComponent);
                }
                skillIndex += 5;
            }
            num5 += Game1.pixelZoom * 6;
            
            if (this.skillBars.Count > 1 && this.skillBars.Last<ClickableTextureComponent>().myID >= 200 && this.skillBars[this.skillBars.Count - 2].myID >= 200)
                this.skillBars.Last<ClickableTextureComponent>().upNeighborID = this.skillBars[this.skillBars.Count - 2].myID;
           
            //dead code for vanilla hover over, we can just write a custom one.
            //string hoverText = "";
            //List<ClickableTextureComponent> skillAreas = this.skillAreas;
            //ClickableTextureComponent textureComponent = new ClickableTextureComponent("Alchemy", new Rectangle(xStart - Game1.tileSize * 2 - Game1.tileSize * 3 / 4, yStart + skillOrderIndex * (Game1.tileSize / 2 + Game1.pixelZoom * 6), Game1.tileSize * 2 + Game1.pixelZoom * 5, 9 * Game1.pixelZoom), string.Concat((object)skillOrderIndex), hoverText, (Texture2D)null, Rectangle.Empty, 1f, false);
            //int num2 = skillOrderIndex;
            //textureComponent.myID = num2;
            //int num3 = skillOrderIndex < 4 ? skillOrderIndex + 1 : 10201;
            //textureComponent.downNeighborID = num3;
            //int num4 = skillOrderIndex > 0 ? skillOrderIndex - 1 : 12341;
            //textureComponent.upNeighborID = num4;
            //int num9 = 100 + skillOrderIndex;
            //textureComponent.rightNeighborID = num9;
            //skillAreas.Add(textureComponent);
            
        }

        private void parseProfessionDescription(ref string professionBlurb, ref string professionTitle, List<string> professionDescription)
        {
            if (professionDescription.Count <= 0)
                return;
            professionTitle = professionDescription[0];
            for (int index = 1; index < professionDescription.Count; ++index)
            {
                professionBlurb = professionBlurb + professionDescription[index];
                if (index < professionDescription.Count - 1)
                    professionBlurb = professionBlurb + Environment.NewLine;
            }
        }

        public override void snapToDefaultClickableComponent()
        {
            this.currentlySnappedComponent = this.skillAreas.Count > 0 ? this.getComponentWithID(0) : (ClickableComponent)null;
            if (this.currentlySnappedComponent == null || !Game1.options.snappyMenus || !Game1.options.gamepadControls)
                return;
            this.currentlySnappedComponent.snapMouseCursorToCenter();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        //called by the renderer during draw to simulate the vanilla perform hover method
        //needed for profession hover over description/icon as well as skill bonus descriptions.
        public override void performHoverAction(int x, int y)
        {
            this.hoverText = "";
            this.hoverTitle = "";
            this.professionImage = null;
            foreach (ClickableTextureComponent skillBar in this.skillBars)
            {
                skillBar.scale = (float)Game1.pixelZoom;
                if (skillBar.containsPoint(x, y) && skillBar.hoverText.Length > 0 && !skillBar.name.Equals("-1"))
                {
                    this.hoverText = Professions.GetProfessionDescription(Convert.ToInt32(skillBar.name));
                    this.hoverTitle = Professions.GetProfessionTitleFromNumber(Convert.ToInt32(skillBar.name));
                    this.professionImage = DrawingUtil.GetProfessionTexture(Convert.ToInt32(skillBar.name));
                    skillBar.scale = 0.0f;
                }
            }
            foreach (ClickableTextureComponent skillArea in this.skillAreas)
            {
                if (skillArea.containsPoint(x, y) && skillArea.hoverText.Length > 0)
                {
                    this.hoverText = skillArea.hoverText;
                    this.hoverTitle = skillArea.name;
                    break;
                }
            }
            if (this.playerPanel.Contains(x, y))
            {
                this.playerPanelTimer = this.playerPanelTimer - Game1.currentGameTime.ElapsedGameTime.Milliseconds;
                if (this.playerPanelTimer > 0)
                    return;
                this.playerPanelIndex = (this.playerPanelIndex + 1) % 4;
                this.playerPanelTimer = 150;
            }
            else
                this.playerPanelIndex = 0;
        }

        public override void draw(SpriteBatch b)
        {
            this.performHoverAction(Game1.getMouseX(), Game1.getMouseY());
            int num3 = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru ? this.xPositionOnScreen + this.width - Game1.tileSize * 7 - Game1.tileSize * 3 / 4 : this.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 4 * Game1.tileSize - 8;
            int num4 = this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - Game1.pixelZoom * 2;
            int num5 = 0;
            //foreach skill rank...
            for (int index1 = 0; index1 < 10; ++index1)
            {
                //there's only one skill to draw here.
                int index2 = this.skillOrderIndex;
                bool flag1 = EquivalentExchange.instance.currentPlayerData.AlchemyLevel > index1;
                bool flag2 = EquivalentExchange.instance.showLevelUpMenusByRank.Count > 0;
                string text = (index1 == 0 ? "Alchemy" : "");
                int number = EquivalentExchange.instance.currentPlayerData.AlchemyLevel;
                Rectangle rectangle = Rectangle.Empty;
                //alchemy skill text             
                if (!text.Equals(""))
                {
                    b.DrawString(Game1.smallFont, text, new Vector2((float)num3 - Game1.smallFont.MeasureString(text).X + (float)Game1.pixelZoom - (float)Game1.tileSize, (float)(num4 + Game1.pixelZoom + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), Game1.textColor);
                    //I think this is the drop shadow for the skill icon
                    b.Draw(DrawingUtil.alchemySkillIcon, new Vector2((float)(num3 - Game1.pixelZoom * 14), (float)(num4 + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle(0, 0, 10, 10), Color.Black * 0.3f, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.85f);
                    //I think this is the actual skill icon.
                    b.Draw(DrawingUtil.alchemySkillIcon, new Vector2((float)(num3 - Game1.pixelZoom * 13), (float)(num4 - Game1.pixelZoom + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle(0, 0, 10, 10), Color.White, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                }
                //player has doesn't have the skill yet and the next square is a profession rank
                if (!flag1 && (index1 + 1) % 5 == 0)
                {
                    //this is the drop shadow for profession rank 5/10
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num5 + num3 - Game1.pixelZoom + index1 * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num4 + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(145, 338, 14, 9)), Color.Black * 0.35f, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                    //this is the icon for profession rank 5/10
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num5 + num3 + index1 * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num4 - Game1.pixelZoom + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(145 + (flag1 ? 14 : 0), 338, 14, 9)), Color.White * (flag1 ? 1f : 0.65f), 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                }
                //the next square NOT a profession rank
                else if ((index1 + 1) % 5 != 0)
                {
                    //I think this is the drop shadow for the level markers
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num5 + num3 - Game1.pixelZoom + index1 * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num4 + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(129, 338, 8, 9)), Color.Black * 0.35f, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.85f);
                    //I think this is the golden bit that covers the drop shadow.
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num5 + num3 + index1 * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num4 - Game1.pixelZoom + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(129 + (flag1 ? 8 : 0), 338, 8, 9)), Color.White * (flag1 ? 1f : 0.65f), 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                }
                //i == 9; (i + 1) == 10, not sure what this is doing.
                if (index1 == 9)
                {
                    NumberSprite.draw(number, b, new Vector2((float)(num5 + num3 + (index1 + 2) * (Game1.tileSize / 2 + Game1.pixelZoom) + Game1.pixelZoom * 3 + (number >= 10 ? Game1.pixelZoom * 3 : 0)), (float)(num4 + Game1.pixelZoom * 4 + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), Color.Black * 0.35f, 1f, 0.85f, 1f, 0, 0);
                    NumberSprite.draw(number, b, new Vector2((float)(num5 + num3 + (index1 + 2) * (Game1.tileSize / 2 + Game1.pixelZoom) + Game1.pixelZoom * 4 + (number >= 10 ? Game1.pixelZoom * 3 : 0)), (float)(num4 + Game1.pixelZoom * 3 + index2 * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), (flag2 ? Color.LightGreen : Color.SandyBrown) * (number == 0 ? 0.75f : 1f), 1f, 0.87f, 1f, 0, 0);
                }
                //makes the next square a fatty, I think.     
                if ((index1 + 1) % 5 == 0)
                    num5 += Game1.pixelZoom * 6;
            }
            foreach (ClickableTextureComponent skillBar in this.skillBars)
                skillBar.draw(b);
            foreach (ClickableTextureComponent skillBar in this.skillBars)
            {
                if ((double)skillBar.scale == 0.0)
                {
                    IClickableMenu.drawTextureBox(b, skillBar.bounds.X - Game1.tileSize / 4 - Game1.pixelZoom * 2, skillBar.bounds.Y - Game1.tileSize / 4 - Game1.pixelZoom * 4, Game1.tileSize * 5 / 4 + Game1.pixelZoom * 4, Game1.tileSize * 5 / 4 + Game1.pixelZoom * 4, Color.White);
                    //I think this is the hover over profession icon.
                    b.Draw(professionImage, new Vector2((float)(skillBar.bounds.X - Game1.pixelZoom * 2), (float)(skillBar.bounds.Y - Game1.tileSize / 2 + Game1.tileSize / 4)), new Rectangle(0, 0, 16, 16), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                }
            }
            Game1.drawDialogueBox(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + Game1.tileSize / 2, this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (int)((double)this.height / 2.0) - Game1.tileSize / 2, this.width - Game1.tileSize - IClickableMenu.spaceToClearSideBorder * 2, this.height / 4 + Game1.tileSize, false, true, (string)null, false);
            this.drawBorderLabel(b, Game1.content.LoadString("Strings\\StringsFromCSFiles:SkillsPage.cs.11610"), Game1.smallFont, this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + Game1.tileSize * 3 / 2, this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + (int)((double)this.height / 2.0) - Game1.tileSize / 2);
            
            if (this.hoverText.Length <= 0)
                return;
            IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont, 0, 0, -1, this.hoverTitle.Length > 0 ? this.hoverTitle : (string)null, -1, (string[])null, (Item)null, 0, -1, -1, -1, -1, 1f, (CraftingRecipe)null);
        }
    }
}
