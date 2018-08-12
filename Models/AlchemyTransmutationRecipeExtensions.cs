using System;
using System.Collections.Generic;
using System.Linq;

namespace EquivalentExchange.Models
{
    public static class AlchemyTransmutationRecipeExtensions
    {
        // extension method to determine if a recipe exists
        public static bool HasItem(this List<AlchemyTransmutationRecipe> recipes, int itemId)
        {
            return recipes.Any(x => x.OutputId == itemId);
        }

        public static void AddRecipeLink(this List<AlchemyTransmutationRecipe> recipes, int input, int output, int cost = 1)
        {
            recipes.Add(new AlchemyTransmutationRecipe(input, output, cost));
        }

        public static List<AlchemyTransmutationRecipe> GetRecipesForOutput(this List<AlchemyTransmutationRecipe> recipes, int output)
        {
            var filteredRecipes = recipes.Where(x => x.OutputId == output).ToList();
            filteredRecipes.Sort((x, y) => {
                var comp = Util.GetItemValue(x.InputId) - Util.GetItemValue(y.InputId);
                // if the cost of the transmutation is higher, it's a slime recipe. Negate the value.
                if (comp < 0 && x.Cost > 1)
                {
                    comp = -comp;
                }                
                return comp;
            });
            return filteredRecipes;
        }

        public static AlchemyTransmutationRecipe FindBestRecipe(this List<AlchemyTransmutationRecipe> recipes, StardewValley.Farmer farmer)
        {            
            foreach(var recipe in recipes)
            {
                var input = recipe.InputId;
                var cost = recipe.GetInputCost();
                if (farmer.hasItemInInventory(input, recipe.GetInputCost()))
                {
                    if (EquivalentExchange.CurrentEnergy >= recipe.Cost)
                    {
                        return recipe;
                    }
                }
            }

            // the farmer isn't able to use any of the recipes, abort.
            return null;
        }
    }
}
