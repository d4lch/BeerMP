using System.Reflection;
using UnityEngine;

namespace BeerMP.Helpers;

internal class Clipboard
{
	private static PropertyInfo cp = typeof(GUIUtility).GetProperty("systemCopyBuffer", BindingFlags.Static | BindingFlags.NonPublic);

	public static string text
	{
		get
		{
			return cp.GetValue(null, null).ToString();
		}
		set
		{
			cp.SetValue(null, value, null);
		}
	}
}
