﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    // Copy from LevelUpMenu
    public class AlchemyLevelUpMenu : LevelUpMenu
    {
        private int currentLevel;
        public int GetLevel()
        {
            return this.currentLevel;
        }

        private int timerBeforeStart;

        private Color leftProfessionColor = Game1.textColor;

        private Color rightProfessionColor = Game1.textColor;

        private MouseState oldMouseState;
        
        private List<CraftingRecipe> newCraftingRecipes = new List<CraftingRecipe>();

        private List<string> extraInfoForLevel = new List<string>();

        private List<string> leftProfessionDescription = new List<string>();

        private List<string> rightProfessionDescription = new List<string>();

        private Rectangle sourceRectForLevelIcon;

        private string title;

        private List<int> professionsToChoose = new List<int>();

        private List<TemporaryAnimatedSprite> littleStars = new List<TemporaryAnimatedSprite>();

        private StardewValley.Farmer player = null;

        public AlchemyLevelUpMenu()
            : base()
        {
            this.player = null;
            this.width = Game1.tileSize * 12;
            this.height = Game1.tileSize * 8;
            this.okButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width + 4, this.yPositionOnScreen + this.height - Game1.tileSize - IClickableMenu.borderWidth, Game1.tileSize, Game1.tileSize), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
        }

        public AlchemyLevelUpMenu(StardewValley.Farmer constructorPlayer,  int level)
        {
            this.player = constructorPlayer;
            this.timerBeforeStart = 250;
            this.isActive = true;
            this.width = Game1.tileSize * 12;
            this.height = Game1.tileSize * 8;
            this.okButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width + 4, this.yPositionOnScreen + this.height - Game1.tileSize - IClickableMenu.borderWidth, Game1.tileSize, Game1.tileSize), Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
            this.newCraftingRecipes.Clear();
            this.extraInfoForLevel.Clear();
            player.completelyStopAnimatingOrDoingAction();
            this.informationUp = true;
            this.isProfessionChooser = false;
            this.currentLevel = level;
            this.title = string.Concat(new object[]
            {
                "Level ",
                this.currentLevel,
                " Alchemy"
            });
            this.extraInfoForLevel = this.GetExtraInfoForLevel(this.currentLevel, constructorPlayer.luckLevel, Game1.player.professions.Contains(Professions.Aurumancer), Game1.player.professions.Contains(Professions.Transmuter));
            sourceRectForLevelIcon = new Rectangle(0, 0, 16, 16);
            if (this.currentLevel > 0 && this.currentLevel % 5 == 0)
            {
                this.professionsToChoose.Clear();
                this.isProfessionChooser = true;
                if (this.currentLevel == 5)
                {
                    this.professionsToChoose.Add(Professions.Shaper);
                    this.professionsToChoose.Add(Professions.Sage);
                }
                else if (Game1.player.professions.Contains(Professions.Shaper))
                {
                    this.professionsToChoose.Add(Professions.Transmuter);
                    this.professionsToChoose.Add(Professions.Adept);                    
                }
                else
                {
                    this.professionsToChoose.Add(Professions.Transmuter);
                    this.professionsToChoose.Add(Professions.Adept);
                }
                this.leftProfessionDescription = AlchemyLevelUpMenu.getProfessionDescription(this.professionsToChoose[0]);
                this.rightProfessionDescription = AlchemyLevelUpMenu.getProfessionDescription(this.professionsToChoose[1]);
            }
            int num = 0;
            this.height = num + Game1.tileSize * 4 + this.extraInfoForLevel.Count<string>() * Game1.tileSize * 3 / 4;
            player.freezePause = 100;
            this.gameWindowSizeChanged(Rectangle.Empty, Rectangle.Empty);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            this.xPositionOnScreen = Game1.viewport.Width / 2 - this.width / 2;
            this.yPositionOnScreen = Game1.viewport.Height / 2 - this.height / 2;
            this.okButton.bounds = new Rectangle(this.xPositionOnScreen + this.width + 4, this.yPositionOnScreen + this.height - Game1.tileSize - IClickableMenu.borderWidth, Game1.tileSize, Game1.tileSize);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
        }

        public List<string> GetExtraInfoForLevel(int whichLevel, int luckLevel, bool hasAurumancerProfession, bool hasTransmuterProfession)
        {
            double nextCoefficientCost = (Alchemy.GetTransmutationMarkupPercentage(whichLevel, hasTransmuterProfession) - Alchemy.TRANSMUTATION_BONUS_PER_LEVEL) * 100D;
            string coefficientCost = $"Transmutation Cost: {nextCoefficientCost.ToString()}%";
            double nextCoefficientValue = (Alchemy.GetLiquidationValuePercentage(whichLevel, hasAurumancerProfession) + Alchemy.LIQUIDATION_BONUS_PER_LEVEL) * 100D;
            string coefficientValue = $"Liquidation Value: {nextCoefficientValue.ToString()}%";
            double luckyTransmuteMinimum = ((Alchemy.GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses(whichLevel, luckLevel) + 0.01) * 100);
            double luckyTransmuteMaximum = ((Alchemy.GetLuckyTransmuteChanceWithoutDailyOrProfessionBonuses(whichLevel, luckLevel) + Alchemy.LUCK_NORMALIZATION_FOR_FREE_TRANSMUTES) * 100);
            string luckyTransmuteChance = $"Lucky Transmute Chance: {luckyTransmuteMinimum.ToString()}% to {luckyTransmuteMaximum.ToString()}%";
            string distanceFromTowerImpact = $"Leyline distance negated by { (whichLevel) }.";
            string staminaCostReduction = $"Stamina drain reduced by { ((1 - Alchemy.GetAlchemyStaminaCostSkillMultiplier()) * 100).ToString() }";
            List<string> extraInfoList = new List<string>();
            extraInfoList.Add(coefficientCost);
            extraInfoList.Add(coefficientValue);
            extraInfoList.Add(luckyTransmuteChance);
            extraInfoList.Add(distanceFromTowerImpact);
            extraInfoList.Add(staminaCostReduction);
            return extraInfoList;
        }

        public static void AddProfessionDescriptions(List<string> list, int whichProfession)
        {
            list.Add(GetProfessionName(whichProfession));
            switch (whichProfession)
            {
                case Professions.Shaper:                    
                    list.Add("Lucky transmutes affected by daily luck twice as much (1-25% is now 2-50%).");
                    break;
                case Professions.Sage:                    
                    list.Add($"The base stamina cost of transmutes is reduced by a flat { (Alchemy.SAGE_PROFESSION_STAMINA_DRAIN_BONUS * 100D) }%");
                    break;
                case Professions.Transmuter:
                    double nextCoefficientCost = Alchemy.GetTransmutationMarkupPercentage(10, true) * 100D;                    
                    list.Add($"Transmutation Cost reduced to { nextCoefficientCost.ToString() }%");
                    break;
                case Professions.Adept:                    
                    list.Add($"Proximity to magic increases chance for lucky transmute by up to 15%.");
                    break;
                case Professions.Aurumancer:
                    double nextCoefficientValue = (Alchemy.GetLiquidationValuePercentage(10, true) + Alchemy.AURUMANCER_LIQUIDATION_BONUS) * 100D;                    
                    list.Add($"Liquidation Value increased to { nextCoefficientValue.ToString() }%");
                    break;
                case Professions.Conduit:
                    list.Add($"Rebounds are now lucky transmutes but you still take damage.");
                    break;
            }
        }

        public static string GetProfessionName(int whichProfession)
        {
            switch (whichProfession)
            {
                case Professions.Shaper:
                    return "Shaper";
                case Professions.Sage:
                    return "Sage";
                case Professions.Transmuter:
                    return "Transmuter";
                case Professions.Adept:
                    return "Adept";
                case Professions.Aurumancer:
                    return "Aurumancer";
                case Professions.Conduit:
                    return "Conduit";
            }

            return null;
        }

        new public static List<string> getProfessionDescription(int whichProfession)
        {
            List<string> list = new List<string>();
            AlchemyLevelUpMenu.AddProfessionDescriptions(list, whichProfession);
            return list;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void performHoverAction(int x, int y)
        {
        }

        new public void getImmediateProfessionPerk(int whichProfession)
        {
        }

        public override void update(GameTime time)
        {
            if (!this.isActive)
            {
                base.exitThisMenu(true);
                return;
            }
            for (int i = this.littleStars.Count - 1; i >= 0; i--)
            {
                if (this.littleStars[i].update(time))
                {
                    this.littleStars.RemoveAt(i);
                }
            }
            if (Game1.random.NextDouble() < 0.03)
            {
                Vector2 position = new Vector2(0f, (float)(Game1.random.Next(this.yPositionOnScreen - Game1.tileSize * 2, this.yPositionOnScreen - Game1.pixelZoom) / (Game1.pixelZoom * 5) * Game1.pixelZoom * 5 + Game1.tileSize / 2));
                if (Game1.random.NextDouble() < 0.5)
                {
                    position.X = (float)Game1.random.Next(this.xPositionOnScreen + this.width / 2 - 57 * Game1.pixelZoom, this.xPositionOnScreen + this.width / 2 - 33 * Game1.pixelZoom);
                }
                else
                {
                    position.X = (float)Game1.random.Next(this.xPositionOnScreen + this.width / 2 + 29 * Game1.pixelZoom, this.xPositionOnScreen + this.width - 40 * Game1.pixelZoom);
                }
                if (position.Y < (float)(this.yPositionOnScreen - Game1.tileSize - Game1.pixelZoom * 2))
                {
                    position.X = (float)Game1.random.Next(this.xPositionOnScreen + this.width / 2 - 29 * Game1.pixelZoom, this.xPositionOnScreen + this.width / 2 + 29 * Game1.pixelZoom);
                }
                position.X = position.X / (float)(Game1.pixelZoom * 5) * (float)Game1.pixelZoom * 5f;
                this.littleStars.Add(new TemporaryAnimatedSprite(Game1.mouseCursors, new Rectangle(364, 79, 5, 5), 80f, 7, 1, position, false, false, 1f, 0f, Color.White, (float)Game1.pixelZoom, 0f, 0f, 0f, false)
                {
                    local = true
                });
            }
            if (this.timerBeforeStart > 0)
            {
                this.timerBeforeStart -= time.ElapsedGameTime.Milliseconds;
                return;
            }
            if (this.isActive && this.isProfessionChooser)
            {
                this.leftProfessionColor = Game1.textColor;
                this.rightProfessionColor = Game1.textColor;
                player.completelyStopAnimatingOrDoingAction();
                player.freezePause = 100;
                if (Game1.getMouseY() > this.yPositionOnScreen + Game1.tileSize * 3 && Game1.getMouseY() < this.yPositionOnScreen + this.height)
                {
                    if (Game1.getMouseX() > this.xPositionOnScreen && Game1.getMouseX() < this.xPositionOnScreen + this.width / 2)
                    {
                        this.leftProfessionColor = Color.Green;
                        if (((Mouse.GetState().LeftButton == ButtonState.Pressed && this.oldMouseState.LeftButton == ButtonState.Released) || (Game1.options.gamepadControls && GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.A) && !Game1.oldPadState.IsButtonDown(Buttons.A))) && this.readyToClose())
                        {
                            Professions.EnableAlchemistProfession(this.professionsToChoose[0]);
                            this.isActive = false;
                            this.informationUp = false;
                            this.isProfessionChooser = false;
                        }
                    }
                    else if (Game1.getMouseX() > this.xPositionOnScreen + this.width / 2 && Game1.getMouseX() < this.xPositionOnScreen + this.width)
                    {
                        this.rightProfessionColor = Color.Green;
                        if (((Mouse.GetState().LeftButton == ButtonState.Pressed && this.oldMouseState.LeftButton == ButtonState.Released) || (Game1.options.gamepadControls && GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.A) && !Game1.oldPadState.IsButtonDown(Buttons.A))) && this.readyToClose())
                        {
                            Professions.EnableAlchemistProfession(this.professionsToChoose[1]);
                            this.isActive = false;
                            this.informationUp = false;
                            this.isProfessionChooser = false;
                        }
                    }
                }
                this.height = Game1.tileSize * 8;
            }
            this.oldMouseState = Mouse.GetState();
            
            if (this.isActive && this.informationUp)
            {
                player.completelyStopAnimatingOrDoingAction();
                if (this.okButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) && !this.isProfessionChooser)
                {
                    this.okButton.scale = Math.Min(1.1f, this.okButton.scale + 0.05f);
                    if ((this.oldMouseState.LeftButton == ButtonState.Pressed || (Game1.options.gamepadControls && Game1.oldPadState.IsButtonDown(Buttons.A))) && this.readyToClose())
                    {
                        this.getLevelPerk(this.currentLevel);
                        this.isActive = false;
                        this.informationUp = false;
                    }
                }
                else
                {
                    this.okButton.scale = Math.Max(1f, this.okButton.scale - 0.05f);
                }
                player.freezePause = 100;
            }
        }

        public override void receiveKeyPress(Keys key)
        {
        }

        public void getLevelPerk(int level)
        {
        }

        public override void draw(SpriteBatch b)
        {
            if (this.timerBeforeStart > 0)
            {
                return;
            }
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * 0.5f);
            foreach (TemporaryAnimatedSprite current in this.littleStars)
            {
                current.draw(b, false, 0, 0);
            }
            b.Draw(Game1.mouseCursors, new Vector2((float)(this.xPositionOnScreen + this.width / 2 - 58 * Game1.pixelZoom / 2), (float)(this.yPositionOnScreen - Game1.tileSize / 2 + Game1.pixelZoom * 3)), new Rectangle?(new Rectangle(363, 87, 58, 22)), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 1f);

            if (this.informationUp)
            {
                if (this.isProfessionChooser)
                {
                    Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true, null, false);
                    base.drawHorizontalPartition(b, this.yPositionOnScreen + Game1.tileSize * 3, false);
                    base.drawVerticalIntersectingPartition(b, this.xPositionOnScreen + this.width / 2 - Game1.tileSize / 2, this.yPositionOnScreen + Game1.tileSize * 3);
                    Utility.drawWithShadow(b, DrawingUtil.alchemySkillIconBordered, new Vector2((float)(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize / 4)), this.sourceRectForLevelIcon, Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, false, 0.88f, -1, -1, 0.35f);
                    b.DrawString(Game1.dialogueFont, this.title, new Vector2((float)(this.xPositionOnScreen + this.width / 2) - Game1.dialogueFont.MeasureString(this.title).X / 2f, (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize / 4)), Game1.textColor);
                    Utility.drawWithShadow(b, DrawingUtil.alchemySkillIconBordered, new Vector2((float)(this.xPositionOnScreen + this.width - IClickableMenu.spaceToClearSideBorder - IClickableMenu.borderWidth - Game1.tileSize), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize / 4)), this.sourceRectForLevelIcon, Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, false, 0.88f, -1, -1, 0.35f);
                    b.DrawString(Game1.smallFont, "Choose a profession:", new Vector2((float)(this.xPositionOnScreen + this.width / 2) - Game1.smallFont.MeasureString("Choose a profession:").X / 2f, (float)(this.yPositionOnScreen + Game1.tileSize + IClickableMenu.spaceToClearTopBorder)), Game1.textColor);
                    b.DrawString(Game1.dialogueFont, this.leftProfessionDescription[0], new Vector2((float)(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + Game1.tileSize / 2), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize * 5 / 2)), this.leftProfessionColor);
                    Texture2D textureA = DrawingUtil.GetProfessionTexture(professionsToChoose[0]);
                    b.Draw(textureA, new Vector2((float)(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + this.width / 2 - Game1.tileSize * 2), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize * 5 / 2 - Game1.tileSize / 4)), sourceRectForLevelIcon, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                    for (int i = 1; i < this.leftProfessionDescription.Count<string>(); i++)
                    {
                        b.DrawString(Game1.smallFont, Game1.parseText(this.leftProfessionDescription[i], Game1.smallFont, this.width / 2 - 64), new Vector2((float)(-4 + this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + Game1.tileSize / 2), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize + 8 + Game1.tileSize * (i + 1))), this.leftProfessionColor);
                    }
                    b.DrawString(Game1.dialogueFont, this.rightProfessionDescription[0], new Vector2((float)(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + this.width / 2), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize * 5 / 2)), this.rightProfessionColor);
                    Texture2D textureB = DrawingUtil.GetProfessionTexture(professionsToChoose[1]);
                    b.Draw(textureB, new Vector2((float)(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + this.width - Game1.tileSize * 2), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize * 5 / 2 - Game1.tileSize / 4)), sourceRectForLevelIcon, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
                    for (int j = 1; j < this.rightProfessionDescription.Count<string>(); j++)
                    {
                        b.DrawString(Game1.smallFont, Game1.parseText(this.rightProfessionDescription[j], Game1.smallFont, this.width / 2 - 48), new Vector2((float)(-4 + this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + this.width / 2), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize + 8 + Game1.tileSize * (j + 1))), this.rightProfessionColor);
                    }
                }
                else
                {
                    Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true, null, false);
                    Utility.drawWithShadow(b, DrawingUtil.alchemySkillIconBordered, new Vector2((float)(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize / 4)), this.sourceRectForLevelIcon, Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, false, 0.88f, -1, -1, 0.35f);
                    b.DrawString(Game1.dialogueFont, this.title, new Vector2((float)(this.xPositionOnScreen + this.width / 2) - Game1.dialogueFont.MeasureString(this.title).X / 2f, (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize / 4)), Game1.textColor);
                    Utility.drawWithShadow(b, DrawingUtil.alchemySkillIconBordered, new Vector2((float)(this.xPositionOnScreen + this.width - IClickableMenu.spaceToClearSideBorder - IClickableMenu.borderWidth - Game1.tileSize), (float)(this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize / 4)), this.sourceRectForLevelIcon, Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom, false, 0.88f, -1, -1, 0.35f);
                    int num = this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + Game1.tileSize * 5 / 4;
                    foreach (string current2 in this.extraInfoForLevel)
                    {
                        b.DrawString(Game1.smallFont, current2, new Vector2((float)(this.xPositionOnScreen + this.width / 2) - Game1.smallFont.MeasureString(current2).X / 2f, (float)num), Game1.textColor);
                        num += Game1.tileSize * 3 / 4;
                    }
                    this.okButton.draw(b);
                }
                base.drawMouse(b);
            }
        }

        new static string getProfessionTitleFromNumber(int whichProfession)
        {
            string s = Professions.GetProfessionTitleFromNumber(whichProfession);
            if (s == null)
                return LevelUpMenu.getProfessionTitleFromNumber(whichProfession);
            return s;
        }
    }
}
