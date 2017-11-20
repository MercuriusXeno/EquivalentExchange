using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using System.Reflection.Emit;
using System.Reflection;

namespace EquivalentExchange.Overrides
{
    //getProfessionName
    [HarmonyPatch(typeof(StardewValley.Menus.LevelUpMenu))]
    [HarmonyPatch("getProfessionName")]
    [HarmonyPatch(new Type[] { typeof(int)})]
    public class LevelUpMenuHook
    {
        static bool Prefix(string __result, ref int whichProfession)
        {
            if (AlchemyLevelUpMenu.GetProfessionName(whichProfession) != null)
            {
                return false;
            }
            return true;
        }

        
        static void Postfix (string __result, int whichProfession)
        {            
            if (AlchemyLevelUpMenu.GetProfessionName(whichProfession) != null)
            {
                __result = AlchemyLevelUpMenu.GetProfessionName(whichProfession);
            }
        }
    }
}
