using UnityEngine;

namespace BeerMP;

internal class BeerMPEntry
{
	internal static BeerMP system;

	internal static void Start()
	{
		if (system == null)
		{
			system = new GameObject("BeerMP").AddComponent<BeerMP>();
		}
	}
}
