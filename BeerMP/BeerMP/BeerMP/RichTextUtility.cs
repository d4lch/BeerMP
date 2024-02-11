using UnityEngine;

namespace BeerMP;

internal static class RichTextUtility
{
	private static string Hexify(this Color color)
	{
		Color white = UnityEngine.Color.white;
		int num = (int)color.a;
		int num2 = (int)((float)(num / 255) * color.r + (float)(1 - num / 255) * white.r);
		int num3 = (int)((float)(num / 255) * color.g + (float)(1 - num / 255) * white.g);
		int num4 = (int)((float)(num / 255) * color.b + (float)(1 - num / 255) * white.b);
		return "#" + num2.ToString("X2") + num3.ToString("X2") + num4.ToString("X2");
	}

	public static string Bold(this string text)
	{
		return "<b>" + text + "</b>";
	}

	public static string Italic(this string text)
	{
		return "<i>" + text + "</i>";
	}

	public static string Size(this string text, int size)
	{
		return $"<size={size}>{text}</size>";
	}

	public static string Color(this string text, Color color)
	{
		return "<color=" + color.Hexify() + ">" + text + "</color>";
	}

	public static string Color(this string text, string colorText)
	{
		return "<color=" + colorText + ">" + text + "</color>";
	}

	public static string ClearColor(this string text)
	{
		string text2 = "";
		bool flag = false;
		int num = text.Length;
		if (text.EndsWith("</color>"))
		{
			num -= "</color>".Length;
		}
		for (int i = 0; i < num; i++)
		{
			if (i + 7 < num && text.Substring(i, 7) == "<color=")
			{
				flag = true;
				i += 6;
				continue;
			}
			if (i + 8 < num && text.Substring(i, 8) == "</color>")
			{
				i += 7;
				continue;
			}
			if (!flag)
			{
				text2 += text[i];
			}
			if (flag && text[i] == '>')
			{
				flag = false;
			}
		}
		return text2;
	}
}
