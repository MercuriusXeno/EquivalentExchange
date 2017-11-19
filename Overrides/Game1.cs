using Harmony;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EquivalentExchange.Events;
using StardewValley;

namespace EquivalentExchange.Overrides
{

    [HarmonyPatch(typeof(Game1), "showEndOfNightStuff")]
    public class ShowOvernightEventHook
    {
        public static void ShowOvernightEvent()
        {
            var ev = new EventArgsOvernightEvent();
            OvernightEvent.InvokeShowNightEndMenus(ev);
        }

        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, MethodBase original, IEnumerable<CodeInstruction> instructions)
        {
            // TODO: Learn how to use ILGenerator. This is spacechase0's comment, but it might as well be mine too. I don't know what this does or why.

            var newInstructions = new List<CodeInstruction>();
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && (string)instruction.operand == "newRecord")
                {
                    newInstructions.Insert(newInstructions.Count - 2, new CodeInstruction(OpCodes.Call, typeof(ShowOvernightEventHook).GetMethod("ShowOvernightEvent")));
                }
                newInstructions.Add(instruction);
            }

            return newInstructions;
        }
    }
}
