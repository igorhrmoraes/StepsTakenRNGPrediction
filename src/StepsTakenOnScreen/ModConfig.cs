using StardewModdingAPI;

namespace StepsTakenOnScreen;

internal class ModConfig
{
	public bool DisplaySteps { get; set; }

	public bool DisplayLuck { get; set; }

	public bool DisplayGift { get; set; }

	public bool DisplayDish { get; set; }

	public int HorizontalOffset { get; set; }

	public int VerticalOffset { get; set; }

	public int WindowWidth { get; set; } = 580;

	public float WindowScale { get; set; } = 1f;

	public double TargetLuck { get; set; }

	public string TargetGifter { get; set; }

	public string TargetDish { get; set; }

	public int TargetDishAmount { get; set; }

	public int TargetStepsLimit { get; set; }

	public SButton ToggleKey { get; set; } = SButton.F8;

	public ModConfig()
	{
		DisplaySteps = true;
		DisplayLuck = true;
		DisplayGift = false;
		DisplayDish = true;
		HorizontalOffset = 16;
		VerticalOffset = 160;
		TargetLuck = 0.1;
		TargetGifter = "";
		TargetDish = "";
		TargetStepsLimit = 1000;
		TargetDishAmount = 1;
	}

	public void Normalize()
	{
		HorizontalOffset = System.Math.Max(0, HorizontalOffset);
		VerticalOffset = System.Math.Max(0, VerticalOffset);
		WindowWidth = System.Math.Clamp(WindowWidth, 240, 1200);
		WindowScale = float.IsFinite(WindowScale) ? System.Math.Clamp(WindowScale, 0.5f, 2f) : 1f;
		TargetLuck = double.IsFinite(TargetLuck) ? (TargetLuck == -1 ? -1 : System.Math.Round(System.Math.Clamp(TargetLuck, -0.1, 0.1), 3)) : 0.1;
		TargetDish = (TargetDish ?? "").Trim();
		TargetGifter = (TargetGifter ?? "").Trim();
		TargetDishAmount = System.Math.Clamp(TargetDishAmount, 1, 13);
		TargetStepsLimit = System.Math.Clamp(TargetStepsLimit, 100, 20000);
	}
}
