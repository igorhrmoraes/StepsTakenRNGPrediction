using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StepsTakenOnScreen;

string root = Directory.GetCurrentDirectory();
if (args.Length != 1)
    throw new ArgumentException("Pass the Stardew Valley installation directory as the first argument; run from the repository root.");
string game = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    foreach (string dir in new[] { game, Path.Combine(game, "smapi-internal"), Path.Combine(game, "Mods/GenericModConfigMenu") })
    {
        string file = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(file)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file));
    }
    return null;
};
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var jsonOptions = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
string oldJson = """
{"HorizontalOffset":41,"VerticalOffset":205,"TargetLuck":0.085,"TargetDish":"Salad,Fried Egg","TargetStepsLimit":5000,"ToggleKey":"F9"}
""";
var config = JsonSerializer.Deserialize<ModConfig>(oldJson, jsonOptions);
config.Normalize();
Check(config.HorizontalOffset == 41 && config.VerticalOffset == 205 && config.TargetLuck == .085 && config.TargetDish == "Salad,Fried Egg" && config.TargetStepsLimit == 5000 && config.ToggleKey.ToString() == "F9", "Old configuration values were changed.");
Check(config.WindowWidth == 580 && config.WindowScale == 1, "Missing new fields must use defaults.");
config.HorizontalOffset = -1; config.VerticalOffset = -20; config.WindowWidth = 0; config.WindowScale = float.NaN;
config.TargetLuck = double.NaN; config.TargetDish = null; config.TargetDishAmount = 30; config.TargetStepsLimit = -10;
config.Normalize();
Check(config.HorizontalOffset == 0 && config.VerticalOffset == 0 && config.WindowWidth == 240 && config.WindowScale == 1 && config.TargetLuck == .1 && config.TargetDish == "" && config.TargetDishAmount == 13 && config.TargetStepsLimit == 100, "Invalid configuration must recover safely.");
config.TargetLuck = -1; config.Normalize();
Check(config.TargetLuck == -1, "Legacy disabled-luck sentinel must be preserved.");
Console.WriteLine("PASS: old settings preserved; new defaults and invalid-value recovery.");

Assembly gmcm = Assembly.LoadFrom(Path.Combine(game, "Mods/GenericModConfigMenu/GenericModConfigMenu.dll"));
Type api = gmcm.GetType("GenericModConfigMenu.Framework.Api", throwOnError: true);
foreach (MethodInfo method in typeof(IGenericModConfigMenuApi).GetMethods())
{
    MethodInfo actual = api.GetMethod(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray());
    Check(actual != null && actual.ReturnType == method.ReturnType, "Installed GMCM API mismatch: " + method.Name);
}
Console.WriteLine("PASS: all 7 API signatures match the installed GMCM assembly.");

// Synthetic glyph metrics exercise wrapping and sizing without opening the game or using a GPU.
var chars = Enumerable.Range(32, 352).Select(i => (char)i).ToList();
var font = new SpriteFont(null, chars.Select(_ => new Rectangle(0, 0, 9, 18)).ToList(),
    chars.Select(_ => new Rectangle(0, 0, 9, 18)).ToList(), chars, 18, 0,
    chars.Select(_ => new Vector3(0, 9, 0)).ToList(), '?');
string text = string.Join(Environment.NewLine, new[] {
    "Passos totais: 12345", "Sorte base de amanhã: 0,100", "Prato de amanhã: Berinjela à parmegiana (13)",
    "Meta: 13567 passos totais", "Critérios da busca:", "Sorte base mínima: +0,100",
    "Prato: Berinjela à parmegiana (estoque mínimo: 13)",
    "Tempestade: antes das 23h, raios noturnos podem mudar a sorte prevista."
});
int layouts = 0;
foreach (var screen in new[] { (640, 360), (1280, 720), (1920, 1080) })
foreach (int width in new[] { 240, 580, 1200 })
foreach (float scale in new[] { .5f, 1f, 2f })
foreach (Vector2 position in new[] { Vector2.Zero, new Vector2(16, 160), new Vector2(10000, 10000) })
{
    var layout = DrawHelper.GetPanelLayout(font, text, position, width, scale, screen.Item1, screen.Item2);
    Check(layout.Position.X >= 8 && layout.Position.Y >= 8, "Panel exceeded top/left margin.");
    Check(layout.Position.X + layout.Size.X <= screen.Item1 - 7 && layout.Position.Y + layout.Size.Y <= screen.Item2 - 7, "Panel exceeds viewport.");
    Vector2 measured = DrawHelper.DrawTextBlock(null, font, new IFormattedText[] { new FormattedText(text) }, Vector2.Zero, layout.TextWidth, layout.Scale, draw: false);
    Check(measured.X <= layout.TextWidth + 1 && measured.Y + layout.Padding * 2 <= layout.Size.Y + 1, "Text exceeds panel.");
    layouts++;
}
Console.WriteLine($"PASS: {layouts} combinations of resolution, width, scale and position fit the viewport.");

string package = Path.Combine(root, "StepsTakenRNGPrediction");
var en = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(package, "i18n/default.json")));
var pt = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(package, "i18n/pt.json")));
foreach (string key in en.Keys.Where(k => k.StartsWith("Menu.") || k.StartsWith("Hud.")))
{
    Check(pt.ContainsKey(key) && !string.IsNullOrWhiteSpace(pt[key]), "Missing translation: " + key);
    string[] Tokens(string value) => Regex.Matches(value, @"\{\{\w+\}\}").Select(m => m.Value).OrderBy(s => s).ToArray();
    Check(Tokens(en[key]).SequenceEqual(Tokens(pt[key])), "Translation token mismatch: " + key);
}
Console.WriteLine("PASS: menu and HUD translations contain matching substitution tokens.");
