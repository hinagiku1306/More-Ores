using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Locations;

namespace MoreOres.Patches;

/// <summary>
/// Harmony postfix on MineShaft.adjustLevelChances to scale stoneChance by config multiplier.
/// </summary>
internal static class AdjustLevelChancesPatch
{
    private static IMonitor Monitor = null!;

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        Monitor = monitor;

        harmony.Patch(
            original: AccessTools.Method(typeof(MineShaft), "adjustLevelChances",
                new[] { typeof(double).MakeByRefType(), typeof(double).MakeByRefType(),
                        typeof(double).MakeByRefType(), typeof(double).MakeByRefType() }),
            postfix: new HarmonyMethod(typeof(AdjustLevelChancesPatch), nameof(Postfix))
        );
    }

    public static void Postfix(ref double stoneChance)
    {
        try
        {
            stoneChance *= ModEntry.Config.StoneChanceMultiplier;
        }
        catch (Exception ex)
        {
            Monitor.Log($"AdjustLevelChances postfix failed: {ex}", LogLevel.Error);
        }
    }
}
