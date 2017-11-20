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

            num = skills.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 4 * Game1.tileSize - 8;
            num2 = skills.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - Game1.pixelZoom * 2;

            int num3 = 0;
            for (int i = 0; i < 10; i++)
            {
                bool flag = false;
                bool flag2 = false;
                string text = "";
                int num4 = 0;
                Rectangle empty = Rectangle.Empty;

                flag = (level > i);
                if (i == 0)
                {
                    text = "Alchemy";
                }
                num4 = level;
                flag2 = false;
                empty = new Rectangle(140, 512, 13, 16);

                //first draw the skill name, happens when i == 0
                if (!text.Equals(""))
                {
                    b.DrawString(Game1.smallFont, text, new Vector2((float)num - Game1.smallFont.MeasureString(text).X - (float)(Game1.pixelZoom * 4) - (float)Game1.tileSize, (float)(num2 + Game1.pixelZoom + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), Game1.textColor);
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num - Game1.pixelZoom * 16), (float)(num2 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(empty), Color.Black * 0.3f, 0f, Vector2.Zero, (float)Game1.pixelZoom * 0.75f, SpriteEffects.None, 0.85f);
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num - Game1.pixelZoom * 15), (float)(num2 - Game1.pixelZoom + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(empty), Color.White, 0f, Vector2.Zero, (float)Game1.pixelZoom * 0.75f, SpriteEffects.None, 0.87f);
                }

                //player has doesn't have the skill yet and the next square is a profession rank
                if (!flag && (i + 1) % 5 == 0)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num3 + num - Game1.pixelZoom + i * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num2 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(145, 338, 14, 9)), Color.Black * 0.35f, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num3 + num + i * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num2 - Game1.pixelZoom + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(145 + (flag ? 14 : 0), 338, 14, 9)), Color.White * (flag ? 1f : 0.65f), 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
                }
                //the next square NOT a profession rank
                else if ((i + 1) % 5 != 0)
                {
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num3 + num - Game1.pixelZoom + i * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num2 + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(129, 338, 8, 9)), Color.Black * 0.35f, 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.85f);
                    b.Draw(Game1.mouseCursors, new Vector2((float)(num3 + num + i * (Game1.tileSize / 2 + Game1.pixelZoom)), (float)(num2 - Game1.pixelZoom + j * (Game1.tileSize / 2 + Game1.pixelZoom * 6))), new Rectangle?(new Rectangle(129 + (flag ? 8 : 0), 338, 8, 9)), Color.White * (flag ? 1f : 0.65f), 0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None, 0.87f);
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
    }
}
