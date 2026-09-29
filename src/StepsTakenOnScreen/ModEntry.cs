using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.Objects;

namespace StepsTakenOnScreen;

public partial class ModEntry : Mod
{
	private ModConfig Config;

	private double dailyLuck;

	private string dishOfTheDay;

	private int dishOfTheDayAmount;

	private string mailPerson;

	private int lastStepsTakenCalculation;

	private int daysPlayedCalculation;

	private int targetStepsCalculation;

	private int targetDay;

	private string labelStepsTaken;

	private string labelDailyLuck;

	private string labelDish;

	private string labelGift;

	private string labelSearch;

	private string[] dishValues;

	private string[] giftValues;

	private bool locationsChecked;

	private int extraCalls;

	private bool targetFound;

	private bool displayVisible = true;
	private double? expectedLuck;
	private string expectedDish;
	private int expectedAmount;

	public override void Entry(IModHelper helper)
	{
		helper.Events.Input.ButtonPressed += OnButtonPressed;
		helper.Events.Input.ButtonReleased += OnButtonReleased;
		helper.Events.Display.RenderedHud += OnRenderedHud;
		helper.Events.GameLoop.DayStarted += OnDayStarted;
		helper.Events.GameLoop.DayEnding += OnDayEnding;
		helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		helper.Events.GameLoop.OneSecondUpdateTicked += (_, _) =>
		{
			locationsChecked = false;
			daysPlayedCalculation = -1;
			targetDay = -1;
		};
		Config = base.Helper.ReadConfig<ModConfig>();
		labelStepsTaken = base.Helper.Translation.Get("DisplaySteps");
		labelDailyLuck = base.Helper.Translation.Get("DisplayLuck");
		labelGift = base.Helper.Translation.Get("DisplayGift");
		labelDish = base.Helper.Translation.Get("DisplayDish");
		labelSearch = base.Helper.Translation.Get("DisplaySearch");
		ApplyConfig();
	}

	private void OnDayStarted(object sender, DayStartedEventArgs e)
	{
		locationsChecked = false;
		if (expectedLuck.HasValue)
		{
			double actual = Game1.player.team.sharedDailyLuck.Value;
			bool matches = Math.Abs(actual - expectedLuck.Value) < 0.0000001
				&& Game1.dishOfTheDay?.ItemId == expectedDish && Game1.dishOfTheDay.Stack == expectedAmount;
			Monitor.Log($"Night check {(matches ? "PASS" : "MISMATCH")}: raw luck {expectedLuck.Value:F3}/{actual:F3}; dish {expectedDish}/{Game1.dishOfTheDay?.ItemId}; amount {expectedAmount}/{Game1.dishOfTheDay?.Stack}", matches ? LogLevel.Info : LogLevel.Warn);
			expectedLuck = null;
		}
	}

	private void OnDayEnding(object sender, DayEndingEventArgs e)
	{
		if (!Context.IsWorldReady || Context.IsMultiplayer) return;
		locationsChecked = false;
		CalculatePredictions((int)Game1.stats.StepsTaken, out var luck, out expectedDish, out expectedAmount, out _);
		expectedLuck = luck;
	}

	private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
	{
		if (Context.IsWorldReady)
		{
			if (e.Button == Config.ToggleKey)
			{
				displayVisible = !displayVisible;
				base.Monitor.Log("Toggled display visibility: " + displayVisible, LogLevel.Info);
			}
			if (e.Button == SButton.F5)
			{
				Config = base.Helper.ReadConfig<ModConfig>();
				base.Monitor.Log("Config reloaded", LogLevel.Info);
				ApplyConfig();
			}
		}
	}

	private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
	{
		if (Context.IsWorldReady && (e.Button.IsActionButton() || e.Button.IsUseToolButton()))
		{
			Vector2 grabTile = e.Cursor.GrabTile;
			StardewValley.Object objectAtTile = Game1.currentLocation.getObjectAtTile((int)grabTile.X, (int)grabTile.Y);
			if (objectAtTile != null && objectAtTile.MinutesUntilReady > 0)
			{
				locationsChecked = false;
			}
		}
	}

	private void OnRenderedHud(object sender, RenderedHudEventArgs e)
	{
		if (!displayVisible || !Context.IsWorldReady || Context.IsMultiplayer)
		{
			return;
		}
		SpriteBatch spriteBatch = Game1.spriteBatch;
		bool flag = false;
		if (!locationsChecked)
		{
			int num = extraCalls;
			CheckLocations();
			flag = extraCalls != num;
		}
		if ((Config.DisplayDish || Config.DisplayGift || Config.DisplayLuck) && (flag || lastStepsTakenCalculation != Game1.stats.StepsTaken || daysPlayedCalculation != Game1.stats.DaysPlayed))
		{
			lastStepsTakenCalculation = (int)Game1.stats.StepsTaken;
			daysPlayedCalculation = (int)Game1.stats.DaysPlayed;
			CalculatePredictions(lastStepsTakenCalculation, out dailyLuck, out dishOfTheDay, out dishOfTheDayAmount, out mailPerson);
		}
		string text = "";
		if (Config.DisplaySteps)
		{
			text = InsertLine(text, GetStepsTaken());
		}
		if (Config.DisplayLuck)
		{
			text = InsertLine(text, GetLuck());
		}
		if (Config.DisplayDish)
		{
			text = InsertLine(text, GetDishOfTheDay());
		}
		if (Config.DisplayGift)
		{
			text = InsertLine(text, GetMailPerson());
		}
		if (Config.TargetLuck != -1.0 || Config.TargetGifter != "" || Config.TargetDish != "")
		{
			if (flag || (ulong)Game1.stats.Get("stepsTaken") > (ulong)targetStepsCalculation || Game1.stats.Get("daysPlayed") != targetDay)
			{
				targetFound = false;
				targetDay = (int)Game1.stats.Get("daysPlayed");
				for (int i = 0; i < Config.TargetStepsLimit; i++)
				{
					targetStepsCalculation = i + (int)Game1.stats.Get("stepsTaken");
					CalculatePredictions(targetStepsCalculation, out var num2, out var dish, out var num3, out var value);
					if ((Config.TargetLuck == -1.0 || num2 >= Config.TargetLuck) && (Config.TargetDish == "" || (dishValues.Contains(GetDishOfTheDayValue(dish), StringComparer.OrdinalIgnoreCase) && num3 >= Config.TargetDishAmount)) && (Config.TargetGifter == "" || giftValues.Contains(value)))
					{
						targetFound = true;
						break;
					}
				}
			}
			text = InsertLine(text, "");
			text = InsertLine(text, targetFound ? Helper.Translation.Get("Hud.TargetFound", new { steps = targetStepsCalculation }) : Helper.Translation.Get("Hud.TargetMissing", new { steps = targetStepsCalculation }));
			text = InsertLine(text, Helper.Translation.Get("Hud.Criteria"));
			if (Config.TargetLuck != -1.0)
			{
				text = InsertLine(text, Helper.Translation.Get("Hud.MinimumLuck", new { luck = Config.TargetLuck.ToString("+0.000;-0.000;0.000") }));
			}
			if (Config.TargetDish != "")
			{
				text = InsertLine(text, Helper.Translation.Get("Hud.TargetDish", new { dish = FormatDishChoice(Config.TargetDish), amount = Config.TargetDishAmount }));
			}
			if (Config.TargetGifter != "")
			{
				text = InsertLine(text, Helper.Translation.Get("Hud.Gifter", new { name = Config.TargetGifter }));
			}
		}
		if (text != "")
		{
			if (Game1.isLightning && Game1.timeOfDay < 2300)
				text = InsertLine(text, Helper.Translation.Get("Hud.Lightning"));
			string label = text;
			DrawHelper.DrawHoverBox(spriteBatch, label, new Vector2(Config.HorizontalOffset, Config.VerticalOffset), Config.WindowWidth, Config.WindowScale);
		}
	}

	private string GetStepsTaken()
	{
		return labelStepsTaken + ": " + Game1.stats.Get("stepsTaken");
	}

	private string GetLuck()
	{
		return labelDailyLuck + ": " + dailyLuck;
	}

	private string GetDishOfTheDay()
	{
		return labelDish + ": " + ItemRegistry.GetDataOrErrorItem("(O)" + dishOfTheDay).DisplayName + " (" + dishOfTheDayAmount + ")";
	}

	private string GetDishOfTheDayValue(string dish)
	{
		return Game1.objectData[dish].Name;
	}

	private string GetMailPerson()
	{
		return labelGift + ": " + mailPerson;
	}

	private void CheckLocations()
	{
		if (locationsChecked)
		{
			return;
		}
		locationsChecked = true;
		extraCalls = 0;
		int num = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay);
		Utility.ForEachLocation(location =>
		{
			foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
			{
				StardewValley.Object value = pair.Value;
				if (value.Location == null || value.heldObject.Value == null || value.QualifiedItemId == "(BC)165" || value.IsSprinkler() || value.readyForHarvest.Value)
					continue;
				var machine = value.GetMachineData();
				int remaining = value.MinutesUntilReady - (machine == null || value.ShouldTimePassForMachine() ? num : 0);
				if (remaining > 0 && (machine == null || machine.WorkingEffects != null))
				{
					extraCalls++;
				}
			}
			return true;
		});
	}

	private void OvernightLightning(Random random)
	{
		int num = (2300 - Game1.timeOfDay) / 100;
		for (int i = 1; i <= num; i++)
		{
		}
	}

	private void PerformLightningUpdate(int time_of_day, Random game1random)
	{
	}

	private void CalculatePredictions(int steps, out double dailyLuck, out string dishOfTheDay, out int dishOfTheDayAmount, out string mailPerson)
	{
		CheckLocations();
		int num = 1;
		switch (Game1.currentSeason)
		{
		case "winter":
			num = 4;
			break;
		case "fall":
			num = 3;
			break;
		case "summer":
			num = 2;
			break;
		case "spring":
			num = 1;
			break;
		}
		int num2 = Game1.dayOfMonth + 1;
		if (num2 == 29)
		{
			num2 = 1;
			num++;
		}
		int num3 = (int)(Game1.stats.DaysPlayed + 1);
		// Match Game1._newDayAfterFade: hash separate inputs, then hash the resulting seed.
		int overnightSeed = Utility.CreateRandomSeed(Game1.uniqueIDForThisGame / 100, num3 * 10 + 1, steps);
		Random random = Utility.CreateRandom(overnightSeed);
		for (int i = 0; i < num2; i++)
		{
			random.Next();
		}
		do
		{
			dishOfTheDay = random.Next(194, 240).ToString();
		}
		while (Utility.IsForbiddenDishOfTheDay(dishOfTheDay));
		dishOfTheDayAmount = random.Next(1, 4 + ((random.NextDouble() < 0.08) ? 10 : 0));
		random.NextDouble();
		for (int j = 0; j < extraCalls; j++)
		{
			random.Next();
		}
		mailPerson = "";
		if (Utility.TryGetRandom(Game1.player.friendshipData, out var key, out var value, random) && random.NextBool((double)(value.Points / 250) * 0.1) && Game1.player.spouse != key && DataLoader.Mail(Game1.content).ContainsKey(key))
		{
			mailPerson = key;
		}
		random.NextDouble();
		if (Game1.player.shirtItem.Value != null && Game1.player.pantsItem.Value != null
			&& (Game1.currentLocation is StardewValley.Locations.FarmHouse || Game1.currentLocation is StardewValley.Locations.IslandFarmHouse || Game1.currentLocation is StardewValley.Shed))
		foreach (StardewValley.Object value3 in Game1.currentLocation.netObjects.Values)
		{
			if (value3 is Mannequin mannequin)
			{
				MannequinData value2 = null;
				if (value2 == null && !DataLoader.Mannequins(Game1.content).TryGetValue(mannequin.ItemId, out value2))
				{
					value2 = null;
				}
				if (value2 != null && value2.Cursed && random.NextDouble() < 0.005)
				{
					_ = mannequin.swappedWithFarmerTonight.Value;
				}
			}
		}
		dailyLuck = (double)random.Next(-100, 101) / 1000.0;
	}

	private string InsertLine(string str, string newStr)
	{
		if (str == "")
		{
			return newStr;
		}
		return str + "\r\n" + newStr;
	}

	public ModEntry()
	{
		lastStepsTakenCalculation = -1;
		daysPlayedCalculation = -1;
		targetStepsCalculation = -1;
		targetDay = -1;
	}
}
