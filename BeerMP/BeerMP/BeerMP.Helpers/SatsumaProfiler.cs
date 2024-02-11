using System.Collections.Generic;
using System.IO;
using BeerMP.Networking.Managers;
using Steamworks;
using UnityEngine;

namespace BeerMP.Helpers;

internal class SatsumaProfiler
{
	internal static SatsumaProfiler Instance;

	internal List<string> attached = new List<string>();

	internal List<string> detached = new List<string>();

	private Rigidbody satsuma;

	private string[] logs = new string[Mathf.RoundToInt(10f * (1f / Time.fixedDeltaTime))];

	private int currentPosition;

	internal SatsumaProfiler(Rigidbody satuma)
	{
		satsuma = satuma;
		Instance = this;
		Console.Log("Satsuma profiler initialized");
	}

	internal void Update(bool receivedRBupdate, ulong owner)
	{
		logs[currentPosition] = $"[{Time.timeSinceLevelLoad}] Velocity: {satsuma.velocity} ({satsuma.velocity.magnitude}), received update: {receivedRBupdate}, owner: {NetManager.playerNames[(CSteamID)owner]}";
		if (attached.Count > 0)
		{
			string text = "\n Attached: ";
			for (int i = 0; i < attached.Count; i++)
			{
				if (i > 0)
				{
					text += ", ";
				}
				text += attached[i];
			}
			logs[currentPosition] += text;
		}
		if (detached.Count > 0)
		{
			string text2 = "\n Detached: ";
			for (int j = 0; j < detached.Count; j++)
			{
				if (j > 0)
				{
					text2 += ", ";
				}
				text2 += detached[j];
			}
			logs[currentPosition] += text2;
		}
		attached.Clear();
		detached.Clear();
		currentPosition++;
		if (currentPosition >= logs.Length)
		{
			currentPosition = 0;
		}
	}

	internal void PrintToFile()
	{
		string text = "";
		int num = currentPosition;
		int num2 = currentPosition;
		do
		{
			text = text + logs[num2] + "\n";
			num2++;
			if (num2 == logs.Length)
			{
				num2 = 0;
			}
		}
		while (num2 != num);
		File.WriteAllText("satsuma_profiler.txt", text);
	}
}
