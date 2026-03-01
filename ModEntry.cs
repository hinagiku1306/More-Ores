using HarmonyLib;
using MoreOres.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace MoreOres;

public class ModEntry : Mod
{
    public static ModConfig Config { get; private set; } = new();

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();

        var harmony = new Harmony(ModManifest.UniqueID);
        CreateLitterObjectPatch.Apply(harmony, Monitor);
        AdjustLevelChancesPatch.Apply(harmony, Monitor);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        gmcm.Register(
            mod: ModManifest,
            reset: () => Config = new ModConfig(),
            save: () => Helper.WriteConfig(Config)
        );

        AddMultiplierOption(gmcm, nameof(ModConfig.StoneChanceMultiplier),
            () => Config.StoneChanceMultiplier, v => Config.StoneChanceMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.SkullCavernOreChanceMultiplier),
            () => Config.SkullCavernOreChanceMultiplier, v => Config.SkullCavernOreChanceMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.CopperOreMultiplier),
            () => Config.CopperOreMultiplier, v => Config.CopperOreMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.IronOreMultiplier),
            () => Config.IronOreMultiplier, v => Config.IronOreMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.GoldOreMultiplier),
            () => Config.GoldOreMultiplier, v => Config.GoldOreMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.IridiumOreMultiplier),
            () => Config.IridiumOreMultiplier, v => Config.IridiumOreMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.RadioactiveOreMultiplier),
            () => Config.RadioactiveOreMultiplier, v => Config.RadioactiveOreMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.MysticStoneMultiplier),
            () => Config.MysticStoneMultiplier, v => Config.MysticStoneMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.GemNodeMultiplier),
            () => Config.GemNodeMultiplier, v => Config.GemNodeMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.DiamondNodeMultiplier),
            () => Config.DiamondNodeMultiplier, v => Config.DiamondNodeMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.GemStoneMultiplier),
            () => Config.GemStoneMultiplier, v => Config.GemStoneMultiplier = v);
    }

    private void AddMultiplierOption(IGenericModConfigMenuApi gmcm, string fieldName,
        Func<int> getValue, Action<int> setValue)
    {
        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: getValue,
            setValue: setValue,
            name: () => Helper.Translation.Get($"config.{fieldName}.name"),
            tooltip: () => Helper.Translation.Get($"config.{fieldName}.tooltip"),
            min: 0,
            max: 100
        );
    }
}

public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue,
        Func<string> name, Func<string>? tooltip = null,
        int? min = null, int? max = null, int? interval = null,
        Func<int, string>? formatValue = null, string? fieldId = null);
}
