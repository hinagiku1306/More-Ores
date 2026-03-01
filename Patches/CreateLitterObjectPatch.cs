using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace MoreOres.Patches;

/// <summary>
/// Harmony prefix replacement for MineShaft.createLitterObject.
/// Reimplements vanilla logic (SDV 1.6.15) with configurable ore spawn multipliers.
/// </summary>
internal static class CreateLitterObjectPatch
{
    private static IMonitor Monitor = null!;
    private static FieldInfo NetIsTreasureRoomField = null!;
    private static FieldInfo BrownSpotsField = null!;

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        Monitor = monitor;
        NetIsTreasureRoomField = AccessTools.Field(typeof(MineShaft), "netIsTreasureRoom");
        BrownSpotsField = AccessTools.Field(typeof(MineShaft), "brownSpots");

        harmony.Patch(
            original: AccessTools.Method(typeof(MineShaft), "createLitterObject",
                new[] { typeof(double), typeof(double), typeof(double), typeof(Vector2) }),
            prefix: new HarmonyMethod(typeof(CreateLitterObjectPatch), nameof(Prefix))
        );
    }

    private static bool GetIsTreasureRoom(MineShaft mine)
    {
        var netBool = (Netcode.NetBool)NetIsTreasureRoomField.GetValue(mine)!;
        return netBool.Value;
    }

    private static List<Vector2> GetBrownSpots(MineShaft mine)
    {
        return (List<Vector2>)BrownSpotsField.GetValue(mine)!;
    }

    /// <summary>
    /// Replaces MineShaft.createLitterObject with configurable multipliers on ore probability checks.
    /// Returns false to skip the original method.
    /// </summary>
    public static bool Prefix(
        MineShaft __instance,
        double chanceForPurpleStone,
        double chanceForMysticStone,
        double gemStoneChance,
        Vector2 tile,
        ref Object? __result)
    {
        try
        {
            __result = CreateLitterObject(__instance, chanceForPurpleStone, chanceForMysticStone, gemStoneChance, tile);
            return false;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Patch failed, falling back to vanilla: {ex}", LogLevel.Error);
            return true;
        }
    }

    private static Object? CreateLitterObject(
        MineShaft mine,
        double chanceForPurpleStone,
        double chanceForMysticStone,
        double gemStoneChance,
        Vector2 tile)
    {
        var config = ModEntry.Config;
        var mineRandom = mine.mineRandom;
        int mineLevel = mine.mineLevel;

        Color stoneColor = Color.White;
        int stoneHealth = 1;

        // --- Radioactive ore (danger mines only) ---
        if (mine.GetAdditionalDifficulty() > 0 && mineLevel % 5 != 0
            && mineRandom.NextDouble() < ((double)mine.GetAdditionalDifficulty() * 0.001
                + (double)((float)mineLevel / 100000f)
                + Game1.player.team.AverageDailyLuck(mine) / 13.0
                + Game1.player.team.AverageLuckLevel(mine) * 0.0001500000071246177)
                * config.RadioactiveOreMultiplier)
        {
            return new Object("95", 1) { MinutesUntilReady = 25 };
        }

        int whichStone;

        // ===== MINE AREA 0/10 (Floors 1-40) =====
        if (mine.getMineArea() == 0 || mine.getMineArea() == 10)
        {
            whichStone = mineRandom.Next(31, 42);
            if (mineLevel % 40 < 30 && whichStone >= 33 && whichStone < 38)
                whichStone = mineRandom.Choose(32, 38);
            else if (mineLevel % 40 >= 30)
                whichStone = mineRandom.Choose(34, 36);

            if (mine.GetAdditionalDifficulty() > 0)
            {
                whichStone = mineRandom.Next(33, 37);
                stoneHealth = 5;
                if (Game1.random.NextDouble() < 0.33)
                    whichStone = 846;
                else
                    stoneColor = new Color(Game1.random.Next(60, 90), Game1.random.Next(150, 200), Game1.random.Next(190, 240));

                if (mine.isDarkArea())
                {
                    whichStone = mineRandom.Next(32, 39);
                    int tone = Game1.random.Next(130, 160);
                    stoneColor = new Color(tone, tone, tone);
                }

                // Enhanced copper ore (849) in danger mines
                if (mineLevel != 1 && mineLevel % 5 != 0
                    && mineRandom.NextDouble() < 0.029 * config.CopperOreMultiplier)
                {
                    return new Object("849", 1) { MinutesUntilReady = 6 };
                }

                if (stoneColor.Equals(Color.White))
                    return new Object(whichStone.ToString(), 1) { MinutesUntilReady = stoneHealth };
            }
            else if (mineLevel != 1 && mineLevel % 5 != 0
                     && mineRandom.NextDouble() < 0.029 * config.CopperOreMultiplier)
            {
                // Copper ore
                return new Object("751", 1) { MinutesUntilReady = 3 };
            }
        }
        // ===== MINE AREA 40 (Floors 41-80) =====
        else if (mine.getMineArea() == 40)
        {
            whichStone = mineRandom.Next(47, 54);
            stoneHealth = 3;

            if (mine.GetAdditionalDifficulty() > 0 && mineLevel % 40 < 30)
            {
                whichStone = mineRandom.Next(39, 42);
                stoneHealth = 5;
                stoneColor = new Color(170, 255, 160);
                if (mine.isDarkArea())
                {
                    whichStone = mineRandom.Next(32, 39);
                    int tone = Game1.random.Next(130, 160);
                    stoneColor = new Color(tone, tone, tone);
                }

                if (mineRandom.NextDouble() < 0.15)
                {
                    return new ColoredObject((294 + mineRandom.Choose(1, 0)).ToString(), 1, new Color(170, 140, 155))
                    {
                        MinutesUntilReady = 6,
                        CanBeSetDown = true,
                        Flipped = mineRandom.NextBool()
                    };
                }

                // Enhanced iron ore in danger mines
                if (mineLevel != 1 && mineLevel % 5 != 0
                    && mineRandom.NextDouble() < 0.029 * config.IronOreMultiplier)
                {
                    return new ColoredObject("290", 1, new Color(150, 225, 160))
                    {
                        MinutesUntilReady = 6,
                        CanBeSetDown = true,
                        Flipped = mineRandom.NextBool()
                    };
                }

                if (stoneColor.Equals(Color.White))
                    return new Object(whichStone.ToString(), 1) { MinutesUntilReady = stoneHealth };
            }
            else if (mineLevel % 5 != 0
                     && mineRandom.NextDouble() < 0.029 * config.IronOreMultiplier)
            {
                // Iron ore
                return new Object("290", 1) { MinutesUntilReady = 4 };
            }
        }
        // ===== MINE AREA 80 (Floors 81-120) =====
        else if (mine.getMineArea() == 80)
        {
            stoneHealth = 4;
            whichStone = ((mineRandom.NextDouble() < 0.3 && !mine.isDarkArea())
                ? ((!mineRandom.NextBool()) ? 32 : 38)
                : ((mineRandom.NextDouble() < 0.3)
                    ? mineRandom.Next(55, 58)
                    : ((!mineRandom.NextBool()) ? 762 : 760)));

            if (mine.GetAdditionalDifficulty() > 0)
            {
                whichStone = (!mineRandom.NextBool()) ? 32 : 38;
                stoneHealth = 5;
                stoneColor = new Color(Game1.random.Next(140, 190), Game1.random.Next(90, 120), Game1.random.Next(210, 255));
                if (mine.isDarkArea())
                {
                    whichStone = mineRandom.Next(32, 39);
                    int tone = Game1.random.Next(130, 160);
                    stoneColor = new Color(tone, tone, tone);
                }

                // Gold ore in danger mines
                if (mineLevel != 1 && mineLevel % 5 != 0
                    && mineRandom.NextDouble() < 0.029 * config.GoldOreMultiplier)
                {
                    return new Object("764", 1) { MinutesUntilReady = 7 };
                }

                if (stoneColor.Equals(Color.White))
                    return new Object(whichStone.ToString(), 1) { MinutesUntilReady = stoneHealth };
            }
            else if (mineLevel % 5 != 0
                     && mineRandom.NextDouble() < 0.029 * config.GoldOreMultiplier)
            {
                // Gold ore
                return new Object("764", 1) { MinutesUntilReady = 8 };
            }
        }
        // ===== QUARRY MINE (77377) + SKULL CAVERN (121+) =====
        else
        {
            if (mine.getMineArea() == 77377)
            {
                stoneHealth = 5;
                bool foundSomething = false;
                foreach (Vector2 v in Utility.getAdjacentTileLocations(tile))
                {
                    if (mine.objects.ContainsKey(v))
                    {
                        foundSomething = true;
                        break;
                    }
                }

                if (!foundSomething && mineRandom.NextDouble() < 0.45)
                    return null;

                var brownSpots = GetBrownSpots(mine);
                bool brownSpot = false;
                for (int i = 0; i < brownSpots.Count; i++)
                {
                    if (Vector2.Distance(tile, brownSpots[i]) < 4f)
                    {
                        brownSpot = true;
                        break;
                    }
                    if (Vector2.Distance(tile, brownSpots[i]) < 6f)
                        return null;
                }

                if (tile.X > 50f)
                {
                    whichStone = Game1.random.Choose(668, 670);
                    if (mineRandom.NextDouble() < (0.09 + Game1.player.team.AverageDailyLuck(mine) / 2.0)
                        * config.CoalNodeMultiplier)
                    {
                        return new Object(Game1.random.Choose("BasicCoalNode0", "BasicCoalNode1"), 1)
                        {
                            MinutesUntilReady = 5
                        };
                    }
                    if (mineRandom.NextDouble() < 0.25)
                        return null;
                }
                else if (brownSpot)
                {
                    whichStone = mineRandom.Choose(32, 38);
                    // Quarry copper
                    if (mineRandom.NextDouble() < 0.01 * config.CopperOreMultiplier)
                        return new Object("751", 1) { MinutesUntilReady = 3 };
                }
                else
                {
                    whichStone = mineRandom.Choose(34, 36);
                    // Quarry iron
                    if (mineRandom.NextDouble() < 0.01 * config.IronOreMultiplier)
                        return new Object("290", 1) { MinutesUntilReady = 3 };
                }

                return new Object(whichStone.ToString(), 1) { MinutesUntilReady = stoneHealth };
            }

            // ===== SKULL CAVERN =====
            stoneHealth = 5;
            whichStone = mineRandom.NextBool()
                ? ((!mineRandom.NextBool()) ? 32 : 38)
                : ((!mineRandom.NextBool()) ? 42 : 40);

            int skullCavernMineLevel = mineLevel - 120;
            double chanceForOre = 0.02 + (double)skullCavernMineLevel * 0.0005;
            if (mineLevel >= 130)
                chanceForOre += 0.01 * (double)((float)(Math.Min(100, skullCavernMineLevel) - 10) / 10f);

            double iridiumBoost = 0.0;
            if (mineLevel >= 130)
                iridiumBoost += 0.001 * (double)((float)(skullCavernMineLevel - 10) / 10f);
            iridiumBoost = Math.Min(iridiumBoost, 0.004);
            if (skullCavernMineLevel > 100)
                iridiumBoost += (double)skullCavernMineLevel / 1000000.0;

            if (!GetIsTreasureRoom(mine) && mineRandom.NextDouble() < chanceForOre * config.SkullCavernOreChanceMultiplier)
            {
                double chanceForIridium = (double)Math.Min(100, skullCavernMineLevel) * (0.0003 + iridiumBoost);
                double chanceForGold = 0.01 + (double)(mineLevel - Math.Min(150, skullCavernMineLevel)) * 0.0005;
                double chanceForIron = Math.Min(0.5, 0.1 + (double)(mineLevel - Math.Min(200, skullCavernMineLevel)) * 0.005);

                // Desert Festival calico egg stone
                if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0
                    && mineRandom.NextBool(0.13 + (double)((float)(Game1.player.team.calicoEggSkullCavernRating.Value * 5) / 1000f)))
                {
                    return new Object("CalicoEggStone_" + mineRandom.Next(3), 1)
                    {
                        MinutesUntilReady = 8
                    };
                }

                // Iridium ore
                if (mineRandom.NextDouble() < chanceForIridium * config.IridiumOreMultiplier)
                    return new Object("765", 1) { MinutesUntilReady = 16 };

                // Gold ore
                if (mineRandom.NextDouble() < chanceForGold * config.GoldOreMultiplier)
                    return new Object("764", 1) { MinutesUntilReady = 8 };

                // Iron ore
                if (mineRandom.NextDouble() < chanceForIron * config.IronOreMultiplier)
                    return new Object("290", 1) { MinutesUntilReady = 4 };

                // Copper ore (fallback)
                return new Object("751", 1) { MinutesUntilReady = 2 };
            }
        }

        // ===== GLOBAL CHECKS (all areas) =====
        double averageDailyLuck = Game1.player.team.AverageDailyLuck(mine);
        double averageMiningLevel = Game1.player.team.AverageSkillLevel(3, Game1.currentLocation);
        double chanceModifier = averageDailyLuck + averageMiningLevel * 0.005;

        // Diamond node
        if (mineLevel > 50
            && mineRandom.NextDouble() < (0.00025 + (double)mineLevel / 120000.0 + 0.0005 * chanceModifier / 2.0)
                * config.DiamondNodeMultiplier)
        {
            whichStone = 2;
            stoneHealth = 10;
        }
        // Gem-rich stones
        else if (gemStoneChance != 0.0
                 && mineRandom.NextDouble() < (gemStoneChance + gemStoneChance * chanceModifier + (double)mineLevel / 24000.0)
                     * config.GemStoneMultiplier)
        {
            return new Object(mine.getRandomGemRichStoneForThisLevel(mineLevel), 1)
            {
                MinutesUntilReady = 5
            };
        }

        // Gem node (purple stone 44)
        if (mineRandom.NextDouble() < (chanceForPurpleStone / 2.0
                + chanceForPurpleStone * averageMiningLevel * 0.008
                + chanceForPurpleStone * (averageDailyLuck / 2.0))
                * config.GemNodeMultiplier)
        {
            whichStone = 44;
        }

        // Mystic stone (46)
        if (mineLevel > 100
            && mineRandom.NextDouble() < (chanceForMysticStone
                + chanceForMysticStone * averageMiningLevel * 0.008
                + chanceForMysticStone * (averageDailyLuck / 2.0))
                * config.MysticStoneMultiplier)
        {
            whichStone = 46;
        }

        whichStone += whichStone % 2;

        // 10% chance for basic stone (668/670)
        if (mineRandom.NextDouble() < 0.1 && mine.getMineArea() != 40)
        {
            if (!stoneColor.Equals(Color.White))
            {
                return new ColoredObject(mineRandom.Choose("668", "670"), 1, stoneColor)
                {
                    MinutesUntilReady = 2,
                    Flipped = mineRandom.NextBool()
                };
            }
            return new Object(mineRandom.Choose("668", "670"), 1)
            {
                MinutesUntilReady = 2,
                Flipped = mineRandom.NextBool()
            };
        }

        if (!stoneColor.Equals(Color.White))
        {
            return new ColoredObject(whichStone.ToString(), 1, stoneColor)
            {
                MinutesUntilReady = stoneHealth,
                Flipped = mineRandom.NextBool()
            };
        }

        return new Object(whichStone.ToString(), 1)
        {
            MinutesUntilReady = stoneHealth
        };
    }
}
