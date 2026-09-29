using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StepsTakenOnScreen;

public partial class ModEntry
{
    private readonly Dictionary<string, string> dishIdsByName = new(StringComparer.OrdinalIgnoreCase);

    private void ApplyConfig()
    {
        Config.Normalize();
        dishValues = Config.TargetDish.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        giftValues = Config.TargetGifter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        locationsChecked = false;
        lastStepsTakenCalculation = daysPlayedCalculation = targetDay = targetStepsCalculation = -1;
        targetFound = false;
    }

    private string FormatDishChoice(string name)
    {
        if (string.IsNullOrEmpty(name)) return Helper.Translation.Get("Menu.AnyDish");
        if (dishIdsByName.TryGetValue(name, out string id))
            return ItemRegistry.GetDataOrErrorItem("(O)" + id).DisplayName;
        return name; // Preserve manually configured comma-separated targets.
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api == null) return;

        // Only expose objects the game's UpdateDishOfTheDay can actually roll.
        // Reading item data is side-effect-free: never instantiate items to get display names.
        for (int id = 194; id < 240; id++)
        {
            string key = id.ToString();
            if (!Utility.IsForbiddenDishOfTheDay(key) && Game1.objectData.TryGetValue(key, out var data))
                dishIdsByName[data.Name] = key;
        }
        var dishChoices = new[] { "" }.Concat(dishIdsByName.Keys.OrderBy(FormatDishChoice, StringComparer.CurrentCultureIgnoreCase)).ToList();
        if (!dishChoices.Contains(Config.TargetDish)) dishChoices.Add(Config.TargetDish);

        Func<string> Label(string key) => () => Helper.Translation.Get("Menu." + key);
        void Number(string key, Func<int> get, Action<int> set, int? min, int? max, int? step = null, Func<int, string> format = null)
            => api.AddNumberOption(ModManifest, get, set, Label(key), Label(key + ".Help"), min, max, step, format, key);

        api.Register(ModManifest,
            reset: () => { Config = new ModConfig(); ApplyConfig(); },
            save: () => { ApplyConfig(); Helper.WriteConfig(Config); },
            titleScreenOnly: false);

        api.AddSectionTitle(ModManifest, Label("Targets"));
        // Integer thousandths avoid float rounding turning +0.100 into an unreachable threshold.
        Number("MinimumLuck", () => Config.TargetLuck == -1 ? -100 : (int)Math.Round(Config.TargetLuck * 1000),
            value => Config.TargetLuck = value / 1000d, -100, 100, 1,
            value => (value / 1000d).ToString("+0.000;-0.000;0.000"));
        api.AddTextOption(ModManifest, () => Config.TargetDish, value => Config.TargetDish = value,
            Label("Dish"), Label("Dish.Help"), dishChoices.ToArray(), FormatDishChoice, "Dish");
        Number("DishAmount", () => Config.TargetDishAmount, value => Config.TargetDishAmount = value, 1, 13, 1);
        Number("SearchSteps", () => Config.TargetStepsLimit, value => Config.TargetStepsLimit = value, 100, 20000, 100);

        api.AddSectionTitle(ModManifest, Label("Panel"));
        Number("PositionX", () => Config.HorizontalOffset, value => Config.HorizontalOffset = value, 0, null);
        Number("PositionY", () => Config.VerticalOffset, value => Config.VerticalOffset = value, 0, null);
        Number("Width", () => Config.WindowWidth, value => Config.WindowWidth = value, 240, 1200, 20);
        Number("Scale", () => (int)Math.Round(Config.WindowScale * 100), value => Config.WindowScale = value / 100f, 50, 200, 5, value => value + "%");
        api.AddParagraph(ModManifest, Label("Panel.Help"));
        api.AddBoolOption(ModManifest, () => Config.DisplaySteps, value => Config.DisplaySteps = value, Label("ShowSteps"));
        api.AddBoolOption(ModManifest, () => Config.DisplayLuck, value => Config.DisplayLuck = value, Label("ShowLuck"));
        api.AddBoolOption(ModManifest, () => Config.DisplayDish, value => Config.DisplayDish = value, Label("ShowDish"));
        api.AddBoolOption(ModManifest, () => Config.DisplayGift, value => Config.DisplayGift = value, Label("ShowGift"));
        api.AddKeybind(ModManifest, () => Config.ToggleKey, value => Config.ToggleKey = value, Label("ToggleKey"));
        Monitor.Log("Generic Mod Config Menu integration registered.", LogLevel.Debug);
    }
}
