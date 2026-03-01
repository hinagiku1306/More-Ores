using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using Object = StardewValley.Object;

namespace MoreOres.Patches;

/// <summary>
/// Harmony prefix replacement for VolcanoDungeon.chooseStoneTypeIndexOnly.
/// Reimplements vanilla logic (SDV 1.6.15) with configurable ore spawn multipliers.
/// </summary>
internal static class VolcanoChooseStoneTypePatch
{
    private static IMonitor Monitor = null!;

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        Monitor = monitor;

        harmony.Patch(
            original: AccessTools.Method(typeof(VolcanoDungeon), "chooseStoneTypeIndexOnly",
                new[] { typeof(Vector2) }),
            prefix: new HarmonyMethod(typeof(VolcanoChooseStoneTypePatch), nameof(Prefix))
        );
    }

    public static bool Prefix(VolcanoDungeon __instance, Vector2 tile, ref int __result)
    {
        try
        {
            __result = ChooseStoneType(__instance, tile);
            return false;
        }
        catch (Exception ex)
        {
            Monitor.Log($"VolcanoChooseStoneType patch failed, falling back to vanilla: {ex}", LogLevel.Error);
            return true;
        }
    }

    private const int CoalIndexPlaceholder = 1095382;

    private static int ChooseStoneType(VolcanoDungeon dungeon, Vector2 tile)
    {
        var config = ModEntry.Config;
        var random = dungeon.generationRandom;
        var objects = dungeon.objects;

        int whichStone = random.Next(845, 848);
        float levelMod = 1f + (float)dungeon.level.Value / 7f;
        const float MasterMultiplier = 0.8f;
        float luckMultiplier = 1f
            + (float)Game1.player.team.AverageLuckLevel() * 0.035f
            + (float)Game1.player.team.AverageDailyLuck() / 2f;

        // --- Cinder Shard nodes (843/844) ---
        double chance = 0.008 * config.VolcanoCinderShardMultiplier * levelMod * MasterMultiplier * luckMultiplier;
        foreach (Vector2 v in Utility.getAdjacentTileLocations(tile))
        {
            if (objects.TryGetValue(v, out var obj)
                && (obj.QualifiedItemId == "(O)843" || obj.QualifiedItemId == "(O)844"))
            {
                chance += 0.15;
            }
        }
        if (random.NextDouble() < chance)
        {
            whichStone = random.Next(843, 845);
        }
        else
        {
            // --- Cinder Shard item (765) ---
            chance = 0.0025 * config.VolcanoCinderShardMultiplier * levelMod * MasterMultiplier * luckMultiplier;
            foreach (Vector2 v in Utility.getAdjacentTileLocations(tile))
            {
                if (objects.TryGetValue(v, out var obj) && obj.QualifiedItemId == "(O)765")
                {
                    chance += 0.1;
                }
            }
            if (random.NextDouble() < chance)
            {
                whichStone = 765;
            }
            else
            {
                // --- Gold ore (764 → VolcanoGoldNode) ---
                chance = 0.01 * config.VolcanoGoldOreMultiplier * levelMod * MasterMultiplier;
                foreach (Vector2 v in Utility.getAdjacentTileLocations(tile))
                {
                    if (objects.TryGetValue(v, out var obj) && obj.QualifiedItemId == "(O)VolcanoGoldNode")
                    {
                        chance += 0.2;
                    }
                }
                if (random.NextDouble() < chance)
                {
                    whichStone = 764;
                }
                else
                {
                    // --- Coal node (1095382 → VolcanoCoalNode) ---
                    chance = 0.012 * config.VolcanoCoalNodeMultiplier * levelMod * MasterMultiplier;
                    foreach (Vector2 v in Utility.getAdjacentTileLocations(tile))
                    {
                        if (objects.TryGetValue(v, out var obj)
                            && obj.QualifiedItemId.StartsWith("(O)VolcanoCoalNode"))
                        {
                            chance += 0.2;
                        }
                    }
                    if (random.NextDouble() < chance)
                    {
                        whichStone = CoalIndexPlaceholder;
                    }
                    else
                    {
                        // --- Stone 850 (no multiplier, regular volcano stone) ---
                        chance = 0.015 * levelMod * MasterMultiplier;
                        foreach (Vector2 v in Utility.getAdjacentTileLocations(tile))
                        {
                            if (objects.TryGetValue(v, out var obj) && obj.QualifiedItemId == "(O)850")
                            {
                                chance += 0.25;
                            }
                        }
                        if (random.NextDouble() < chance)
                        {
                            whichStone = 850;
                        }
                        else
                        {
                            // --- Stone 849 (no multiplier, regular volcano stone) ---
                            chance = 0.018 * levelMod * MasterMultiplier;
                            foreach (Vector2 v in Utility.getAdjacentTileLocations(tile))
                            {
                                if (objects.TryGetValue(v, out var obj) && obj.QualifiedItemId == "(O)849")
                                {
                                    chance += 0.25;
                                }
                            }
                            if (random.NextDouble() < chance)
                            {
                                whichStone = 849;
                            }
                        }
                    }
                }
            }
        }

        // --- Rare gems (flat chances, independent of cascade) ---
        if (random.NextDouble() < 0.0005 * config.VolcanoDiamondNodeMultiplier)
        {
            whichStone = 819;
        }
        if (random.NextDouble() < 0.0007 * config.VolcanoGemStoneMultiplier)
        {
            whichStone = 44;
        }
        if (dungeon.level.Value > 2 && random.NextDouble() < 0.0002 * config.VolcanoGemStoneMultiplier)
        {
            whichStone = 46;
        }

        return whichStone;
    }
}
