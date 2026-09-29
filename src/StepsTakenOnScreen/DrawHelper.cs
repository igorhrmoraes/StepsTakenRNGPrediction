using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace StepsTakenOnScreen;

internal static class DrawHelper
{
	public static float GetSpaceWidth(SpriteFont font)
	{
		return font.MeasureString("A B").X - font.MeasureString("AB").X;
	}

	public static Vector2 DrawHoverBox(SpriteBatch spriteBatch, string label, in Vector2 position, float wrapWidth, float scale = 1f)
	{
		var layout = GetPanelLayout(Game1.smallFont, label, position, wrapWidth, scale, Game1.uiViewport.Width, Game1.uiViewport.Height);
		IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), (int)layout.Position.X, (int)layout.Position.Y, (int)layout.Size.X, (int)layout.Size.Y, Color.White);
		spriteBatch.DrawTextBlock(Game1.smallFont, label, layout.Position + new Vector2(layout.Padding), layout.TextWidth, scale: layout.Scale);
		return layout.Size;
	}

	internal readonly record struct PanelLayout(Vector2 Position, Vector2 Size, float TextWidth, float Padding, float Scale);

	internal static PanelLayout GetPanelLayout(SpriteFont font, string text, Vector2 position, float width, float scale, int screenWidth, int screenHeight)
	{
		float maxWidth = Math.Max(80, screenWidth - 16);
		float maxHeight = Math.Max(80, screenHeight - 16);
		float panelWidth = 0, panelHeight = 0, padding = 0, textWidth = 0;
		for (int attempt = 0; attempt < 10; attempt++)
		{
			panelWidth = Math.Clamp(width * scale, 80, maxWidth);
			padding = Math.Max(12, 20 * scale);
			textWidth = Math.Max(20, panelWidth - padding * 2);
			Vector2 size = DrawTextBlock(null, font, new IFormattedText[] { new FormattedText(text) }, Vector2.Zero, textWidth, scale, draw: false);
			panelHeight = (float)Math.Ceiling(size.Y + padding * 2);
			if (panelHeight <= maxHeight) break;
			scale *= Math.Max(0.1f, (maxHeight - 24) / panelHeight) * 0.95f;
		}
		position.X = Math.Clamp(position.X, 8, Math.Max(8, screenWidth - panelWidth - 8));
		position.Y = Math.Clamp(position.Y, 8, Math.Max(8, screenHeight - panelHeight - 8));
		return new PanelLayout(position, new Vector2((float)Math.Ceiling(panelWidth), panelHeight), textWidth, padding, scale);
	}

	public static Vector2 DrawTextBlock(this SpriteBatch batch, SpriteFont font, string text, Vector2 position, float wrapWidth, Color? color = null, bool bold = false, float scale = 1f)
	{
		return batch.DrawTextBlock(font, new IFormattedText[1]
		{
			new FormattedText(text, color, bold)
		}, position, wrapWidth, scale);
	}

	public static Vector2 DrawTextBlock(this SpriteBatch batch, SpriteFont font, IEnumerable<IFormattedText> text, Vector2 position, float wrapWidth, float scale = 1f, bool draw = true)
	{
		if (text == null)
		{
			return new Vector2(0f, 0f);
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = font.MeasureString("ABC").Y * scale;
		float num4 = GetSpaceWidth(font) * scale;
		float num5 = 0f;
		float num6 = num3;
		foreach (IFormattedText item in text)
		{
			if (item == null || item.Text == null)
			{
				continue;
			}
			bool flag = item.Text.StartsWith(" ");
			bool flag2 = item.Text.EndsWith(" ");
			IList<string> list = new List<string>();
			string[] array = item.Text.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			int i = 0;
			for (int num7 = array.Length - 1; i <= num7; i++)
			{
				string text2 = array[i];
				if (flag && i == 0)
				{
					text2 = " " + text2;
				}
				if (flag2 && i == num7)
				{
					text2 += " ";
				}
				string text3 = text2;
				int num8;
				while ((num8 = text3.IndexOf(Environment.NewLine, StringComparison.InvariantCulture)) >= 0)
				{
					if (num8 == 0)
					{
						list.Add(Environment.NewLine);
						text3 = text3.Substring(Environment.NewLine.Length);
					}
					else if (num8 > 0)
					{
						list.Add(text3.Substring(0, num8));
						list.Add(Environment.NewLine);
						text3 = text3.Substring(num8 + Environment.NewLine.Length);
					}
				}
				if (text3.Length > 0)
				{
					list.Add(text3);
				}
			}
			bool flag3 = true;
			foreach (string item2 in list)
			{
				float num9 = font.MeasureString(item2).X * scale;
				float num10 = (flag3 ? 0f : num4);
				if (item2 == Environment.NewLine || (num9 + num + num10 > wrapWidth && (int)num != 0))
				{
					num = 0f;
					num2 += num3;
					num6 += num3;
					flag3 = true;
					num10 = 0;
				}
				if (!(item2 == Environment.NewLine))
				{
					Vector2 position2 = new Vector2(position.X + num + num10, position.Y + num2);
					if (draw && item.Bold)
					{
						Utility.drawBoldText(batch, item2, font, position2, item.Color ?? Color.Black, scale);
					}
					else if (draw)
					{
						batch.DrawString(font, item2, position2, item.Color ?? Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
					}
					if (num + num9 + num10 > num5)
					{
						num5 = num + num9 + num10;
					}
					num += num9 + num10;
					flag3 = false;
				}
			}
		}
		return new Vector2(num5, num6);
	}
}
