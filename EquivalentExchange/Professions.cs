using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    class Professions
    {
        public const int Shaper = 61;
        public const int Sage = 62;
        public const int Transmuter = 63;
        public const int Adept = 64;
        public const int Aurumancer = 65;
        public const int Conduit = 66;

    //return first match on professions for levels 5 and 10
        public static List<int> GetProfessionsForSkillLevel(int whichLevel)
        {
            List<int> obtainedProfessionsAtLevel = new List<int>();
            switch (whichLevel)
            {
                case 5:
                    if (Game1.player.professions.Contains(Professions.Sage))
                        obtainedProfessionsAtLevel.Add(Professions.Sage);
                    if (Game1.player.professions.Contains(Professions.Shaper))
                        obtainedProfessionsAtLevel.Add(Professions.Shaper);
                    break;
                default:
                    if (Game1.player.professions.Contains(Professions.Transmuter))
                        obtainedProfessionsAtLevel.Add(Professions.Transmuter);
                    if (Game1.player.professions.Contains(Professions.Adept))
                        obtainedProfessionsAtLevel.Add(Professions.Adept);
                    if (Game1.player.professions.Contains(Professions.Aurumancer))
                        obtainedProfessionsAtLevel.Add(Professions.Aurumancer);
                    if (Game1.player.professions.Contains(Professions.Conduit))
                        obtainedProfessionsAtLevel.Add(Professions.Conduit);
                    break;
            }
            return obtainedProfessionsAtLevel;
        }


        public static List<int> firstRankProfessions = new List<int> { Professions.Shaper, Professions.Sage };
        public static List<int> secondRankProfessions = new List<int> { Professions.Transmuter, Professions.Adept, Professions.Aurumancer, Professions.Conduit };

        public static int getProfessionForSkill(int level)
        {
            if (level != 5 && level != 10)
                return -1;

            List<int> list = (level == 5 ? firstRankProfessions : secondRankProfessions);
            foreach (int prof in list)
            {
                if (Game1.player.professions.Contains(prof))
                    return prof;
            }

            return -1;
        }

        //called when selecting a profession from the level up menu, sets the chosen profession for icon purposes.
        internal static void EnableAlchemistProfession(int profession)
        {
            switch (profession)
            {
                case Professions.Shaper:
                    Game1.player.professions.Add(Professions.Shaper);
                    break;
                case Professions.Sage:
                    Game1.player.professions.Add(Professions.Sage);
                    break;
                case Professions.Transmuter:
                    Game1.player.professions.Add(Professions.Transmuter);
                    break;
                case Professions.Adept:
                    Game1.player.professions.Add(Professions.Adept);
                    break;
                case Professions.Aurumancer:
                    Game1.player.professions.Add(Professions.Aurumancer);
                    break;
                case Professions.Conduit:
                    Game1.player.professions.Add(Professions.Conduit);
                    break;
            }
        }

        public static string GetProfessionTitleFromNumber (int whichProfession)
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
                default:
                    return null;
            }
        }
    }
}
