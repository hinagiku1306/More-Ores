using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Locations;

namespace MoreOres.Patches;

/// <summary>
/// Harmony postfix on VolcanoDungeon.adjustLevelChances to scale stoneChance and itemChance.
/// </summary>
internal static class VolcanoAdjustLevelChancesPatch
{
    private static IMonitor Monitor = null!;

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        Monitor = monitor;

        harmony.Patch(
            original: AccessTools.Method(typeof(VolcanoDungeon), "adjustLevelChances",
                new[] { typeof(double).MakeByRefType(), typeof(double).MakeByRefType(),
                        typeof(double).MakeByRefType(), typeof(double).MakeByRefType() }),
            postfix: new HarmonyMethod(typeof(VolcanoAdjustLevelChancesPatch), nameof(Postfix))
        );
    }

    public static void Postfix(ref double stoneChance, ref double itemChance)
    {
        try
        {
            stoneChance *= ModEntry.Config.VolcanoStoneDensityMultiplier;
            itemChance *= ModEntry.Config.VolcanoMagmaCapMultiplier;
        }
        catch (Exception ex)
        {
            Monitor.Log($"VolcanoAdjustLevelChances postfix failed: {ex}", LogLevel.Error);
        }
    }
}
