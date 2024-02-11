using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace BeerMP;

public static class Console
{
	internal class LogData
	{
		public string log;

		public string og;

		public float time = 5f;

		internal LogData(string message, string og, bool show)
		{
			log = message.ToString();
			this.og = og.ClearColor();
			tw.WriteLine(og);
			tw.Flush();
			if (show)
			{
				logs.Insert(0, this);
			}
		}
	}

	private static TraceSource ts = new TraceSource("BeerMP-Console");

	private static TextWriterTraceListener tw = new TextWriterTraceListener("BeerMP_output_log.txt");

	internal static List<LogData> logs = new List<LogData>();

	internal static void UpdateLogDeleteTime()
	{
		for (int i = 0; i < logs.Count; i++)
		{
			logs[i].time -= Time.deltaTime;
			if (logs[i].time <= 0f)
			{
				logs.RemoveAt(i);
				i--;
			}
		}
	}

	internal static void Init()
	{
		ts.Switch.Level = SourceLevels.All;
		ts.Listeners.Add(tw);
	}

	internal static void DrawGUI()
	{
		float num = 1f;
		DrawGUIText(num, num, isBlack: true);
		DrawGUIText(-1f, num, isBlack: true);
		DrawGUIText(num, -1f, isBlack: true);
		DrawGUIText(-1f, -1f, isBlack: true);
		DrawGUIText(0f, 0f, isBlack: false);
	}

	private static void DrawGUIText(float xOffset, float yOffset, bool isBlack)
	{
		GUILayout.BeginArea(new Rect(10f + xOffset, 10f + yOffset, (float)Screen.width - 20f, (float)Screen.height - 20f));
		for (int i = 0; i < logs.Count; i++)
		{
			GUILayout.Label(isBlack ? (logs[i].og ?? "").Size(20).Bold().Color("black") : (logs[i].log ?? "").Size(20).Bold());
		}
		GUILayout.EndArea();
	}

	public static void Log(object message, bool show = true)
	{
		new LogData("[" + "INFO".Color("lime") + "] [" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss").Color("yellow") + "]: " + message.ToString().Color("yellow"), string.Format("[INFO] [{0}]: {1}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), message), show);
	}

	public static void LogWarning(object message, bool show = true)
	{
		new LogData("[" + "WARNING".Color("orange") + "] [" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss").Color("yellow") + "]: " + message.ToString().Color("yellow"), string.Format("[WARNING] [{0}]: {1}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), message), show);
	}

	public static void LogError(object message, bool show = false)
	{
		new LogData("[" + "ERROR".Color("red") + "] [" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss").Color("yellow") + "]: " + message.ToString().Color("yellow"), string.Format("[ERROR] [{0}]: {1}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), message), show);
	}
}
