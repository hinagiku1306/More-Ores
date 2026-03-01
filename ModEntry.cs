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
        VolcanoAdjustLevelChancesPatch.Apply(harmony, Monitor);
        VolcanoChooseStoneTypePatch.Apply(harmony, Monitor);

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

        // ── Mines & Skull Cavern ──
        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => Helper.Translation.Get("section.MinesAndSkullCavern")
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
        AddMultiplierOption(gmcm, nameof(ModConfig.CoalNodeMultiplier),
            () => Config.CoalNodeMultiplier, v => Config.CoalNodeMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.MineForageableMultiplier),
            () => Config.MineForageableMultiplier, v => Config.MineForageableMultiplier = v);

        // ── Volcano Dungeon ──
        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => Helper.Translation.Get("section.VolcanoDungeon")
        );

        AddMultiplierOption(gmcm, nameof(ModConfig.VolcanoStoneDensityMultiplier),
            () => Config.VolcanoStoneDensityMultiplier, v => Config.VolcanoStoneDensityMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.VolcanoCinderShardMultiplier),
            () => Config.VolcanoCinderShardMultiplier, v => Config.VolcanoCinderShardMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.VolcanoGoldOreMultiplier),
            () => Config.VolcanoGoldOreMultiplier, v => Config.VolcanoGoldOreMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.VolcanoCoalNodeMultiplier),
            () => Config.VolcanoCoalNodeMultiplier, v => Config.VolcanoCoalNodeMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.VolcanoDiamondNodeMultiplier),
            () => Config.VolcanoDiamondNodeMultiplier, v => Config.VolcanoDiamondNodeMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.VolcanoGemStoneMultiplier),
            () => Config.VolcanoGemStoneMultiplier, v => Config.VolcanoGemStoneMultiplier = v);
        AddMultiplierOption(gmcm, nameof(ModConfig.VolcanoMagmaCapMultiplier),
            () => Config.VolcanoMagmaCapMultiplier, v => Config.VolcanoMagmaCapMultiplier = v);
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
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue,
        Func<string> name, Func<string>? tooltip = null,
        int? min = null, int? max = null, int? interval = null,
        Func<int, string>? formatValue = null, string? fieldId = null);
}
