﻿using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquivalentExchange
{
    // Algorithm for calculating "distance" from an alchemy offset "node", a location where the alchemy "power level" is a constant.
    // Something something leylines. Written by Entoarox
    public static class DistanceCalculator
    {
        // We remember the calculations for all static maps, since they *shouldnt* change during gameplay
        private static Dictionary<string, double> _Cache = new Dictionary<string, double>();
        // The extra "distance" added for every floor down into the skull cave (Always 121 or more floors)
        private const double SkullDepthPenalty = 0.2;
        // The extra "distance" added for every floor down into mines (Never more then 120 floors)
        private const double MineDepthPenalty = 0.1;
        // The extra "distance" added for every warp that has to be used
        private const double DistancePenalty = 1;
        public static double GetPathDistance(GameLocation location, double dist = 0, List<string> memory = null)
        {
            // We only calculate path distance if we havent done so already for this location
            // We recalculate for the mineshaft at all times because it is a leveled location
            if (!_Cache.ContainsKey(location.Name))
            {
                // Check if this is a leveled location
                if (location is MineShaft)
                {
                    var shaft = location as MineShaft;
                    if (shaft.mineLevel > 120) // SkullCave
                        return GetPathDistance(Game1.getLocationFromName("SkullCave"), dist + DistancePenalty + (shaft.mineLevel * SkullDepthPenalty));
                    else // Mines
                        return GetPathDistance(Game1.getLocationFromName("Mine"), dist + DistancePenalty + (shaft.mineLevel * MineDepthPenalty));
                }
                if (memory == null)
                    memory = new List<string>();
                memory.Add(location.Name);
                // We assume to begin with that we are insanely far away (No real situation should ever have -this- high a value, so it also makes it possible to detect a location that is not connected at all)
                double mdist = double.MaxValue;
                // AlchemyOffset, used to create path distance end points that can have a default penalty or have it as 0 for no default penalty
                if (location.map.Properties.ContainsKey("AlchemyOffset"))
                    mdist = Convert.ToDouble((string)location.map.Properties["AlchemyOffet"]);
                else // The hard offset of a alchemyOffset point overrides any distance based cost
                {
                    // Check through all warps in the location
                    foreach (Warp warp in location.warps)
                    {
                        if (memory.Contains(warp.TargetName))
                            continue;
                        // We get the path distance for the found warp, if it hasnt gotten one calculated yet then we will also be doing so
                        double vdist0 = GetPathDistance(Game1.getLocationFromName(warp.TargetName), dist + DistancePenalty, memory);
                        // We check if the path distance for this location is less then the one we currently have, and if so, hold onto it
                        if (vdist0 < mdist)
                            mdist = vdist0;
                    }
                    // We loop through all Buildings tiles on the map to look for certain tile actions
                    for (int x = 0; x < location.map.Layers[0].TileSize.Width; x++)
                        for (int y = 0; y < location.map.Layers[0].TileSize.Height; y++)
                        {
                            // We check if it has a Action property, otherwise we can just ignore it
                            string prop = location.doesTileHaveProperty(x, y, "Action", "Buildings");
                            if (prop == null)
                                continue;
                            // We check if the property is a certain type
                            switch (prop)
                            {
                                // Locations with special warps are handled here
                                case "WarpCommunityCenter":
                                case "WarpGreenhouse":
                                    string targetName = prop.Substring(4);
                                    if (memory.Contains(targetName))
                                        break;
                                    // We get the path distance for the found Action warp, if it hasnt gotten one calculated yet then we will also be doing so
                                    double vdist1 = GetPathDistance(Game1.getLocationFromName(targetName), dist + DistancePenalty, memory);
                                    // We check if the path distance for this location is less then the one we currently have, and if so, hold onto it
                                    if (vdist1 < mdist)
                                        mdist = vdist1;
                                    break;
                                default:
                                    // Locations that are normal or locked-door warps are handled here
                                    var props = prop.Split(' ');
                                    if ((props[0].Equals("Warp") || props[0].Equals("LockedDoorWarp")) && !memory.Contains(props[3]) && Game1.getLocationFromName(props[3]) != null)
                                    {
                                        // We get the path distance for the found Action warp, if it hasnt gotten one calculated yet then we will also be doing so
                                        double vdist2 = GetPathDistance(Game1.getLocationFromName(props[3]), dist + DistancePenalty, memory);
                                        // We check if the path distance for this location is less then the one we currently have, and if so, hold onto it
                                        if (vdist2 < mdist)
                                            mdist = vdist2;
                                    }
                                    break;
                            }

                        }
                    // We add the result for this location to the cache
                    _Cache.Add(location.Name, mdist);
                }
            }
            // We return the offset (Distance of the parent) to our own, and return it
            return dist + _Cache[location.Name];
        }
    }
}
