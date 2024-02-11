using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Harmony;
using UnityEngine;

namespace BeerMP.Helpers;

public class BProfiler
{
	private class Patch
	{
		private static bool Prefix(out long __state)
		{
			__state = 0L;
			if (!doProfiling)
			{
				return true;
			}
			__state = Stopwatch.GetTimestamp();
			return true;
		}

		private static void Postfix(MethodBase __originalMethod, long __state)
		{
			if (doProfiling)
			{
				long timestamp = Stopwatch.GetTimestamp();
				if (!execTimes.ContainsKey(__originalMethod))
				{
					execTimes.Add(__originalMethod, new Queue<long>());
				}
				if (__state < timestamp)
				{
					execTimes[__originalMethod].Enqueue(timestamp - __state);
				}
				if (execTimes[__originalMethod].Count > maxSamples)
				{
					execTimes[__originalMethod].Dequeue();
				}
			}
		}
	}

	private static TraceSource ts;

	private static TextWriterTraceListener tw;

	private static HarmonyInstance instance;

	private static Dictionary<MethodBase, Queue<long>> execTimes = new Dictionary<MethodBase, Queue<long>>();

	public static int maxSamples = 10;

	private static bool doProfiling = false;

	private static List<MethodBase> patched = new List<MethodBase>();

	private static Vector2 scrollRect;

	internal static void Start()
	{
		Console.Log("Started Profiling".Color("lime"));
		doProfiling = true;
	}

	internal static void Stop(string filePath = "")
	{
		if (doProfiling)
		{
			Console.Log("Stopped Profiling".Color("red"));
		}
		else
		{
			execTimes.Clear();
			Console.Log("Cleared execution times".Color("teal"));
		}
		if (filePath != "" && doProfiling)
		{
			WriteToFile(filePath);
		}
		doProfiling = false;
	}

	public static void Attach(Type targetType, bool attachNestedTypes = true)
	{
		try
		{
			if (instance == null)
			{
				instance = HarmonyInstance.Create("BeerMP.Profiler");
			}
			if (ts == null)
			{
				if (File.Exists("BeerMP_Profiler_output_log.txt"))
				{
					File.Delete("BeerMP_Profiler_output_log.txt");
				}
				tw = new TextWriterTraceListener("BeerMP_Profiler_output_log.txt");
				ts = new TraceSource("BeerMP_Profiler");
				ts.Switch.Level = SourceLevels.All;
				ts.Listeners.Add(tw);
			}
			if (targetType == typeof(BProfiler) || targetType.IsGenericType)
			{
				return;
			}
			Type[] nestedTypes = targetType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			MethodInfo[] methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			for (int i = 0; i < methods.Length; i++)
			{
				if (methods[i].DeclaringType != targetType || patched.Contains(methods[i]))
				{
					continue;
				}
				if (methods[i].IsGenericMethod)
				{
					tw.WriteLine("Warning: " + methods[i].FullDescription() + " is a generic method, not supported!");
					tw.Flush();
					continue;
				}
				if (methods[i].GetMethodBody() == null)
				{
					tw.WriteLine("Warning: " + methods[i].FullDescription() + " has no body");
					tw.Flush();
					continue;
				}
				DynamicMethod dynamicMethod = instance.Patch(methods[i], new HarmonyMethod(typeof(Patch), "Prefix"), new HarmonyMethod(typeof(Patch), "Postfix"));
				tw.WriteLine("Info: attached to " + methods[i].FullDescription());
				tw.Flush();
				if (dynamicMethod != null)
				{
					patched.Add(methods[i]);
				}
			}
			if (attachNestedTypes)
			{
				for (int j = 0; j < nestedTypes.Length; j++)
				{
					Attach(nestedTypes[j]);
				}
			}
		}
		catch (Exception arg)
		{
			tw.WriteLine($"Error: {arg}");
			tw.Flush();
		}
	}

	internal static void Reset()
	{
		doProfiling = false;
		Stop();
		if (instance != null)
		{
			instance.UnpatchAll();
		}
		patched = new List<MethodBase>();
	}

	internal static void DoGUI()
	{
		string text = "";
		GUILayout.BeginArea(new Rect(0f, 0f, Screen.width, Screen.height));
		GUILayout.BeginHorizontal(GUILayout.ExpandWidth(expand: true));
		GUILayout.FlexibleSpace();
		GUILayout.BeginVertical($"<color=white>[BeerMP Profiler] max samples: {maxSamples}</color>", "", GUILayout.MaxHeight(Screen.height), GUILayout.ExpandWidth(expand: true));
		GUILayout.Space(10f);
		scrollRect = GUILayout.BeginScrollView(scrollRect, "", "");
		foreach (KeyValuePair<MethodBase, Queue<long>> item in execTimes.OrderBy((KeyValuePair<MethodBase, Queue<long>> x) => x.Key.DeclaringType.FullName))
		{
			string fullName = item.Key.DeclaringType.FullName;
			string name = item.Key.Name;
			if (text != fullName)
			{
				GUILayout.Label("<color=white>" + fullName + ":</color>");
				text = fullName;
			}
			GUILayout.Label($"<color=white>    {name}: {((item.Value.Count > 0) ? avg(execTimes[item.Key], milliseconds: true) : 0.0)}ms  {((item.Value.Count > 0) ? avg(execTimes[item.Key], milliseconds: false) : 0.0)}ticks</color>");
		}
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}

	internal static void WriteToFile(string fileName)
	{
		string text = "";
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<MethodBase, Queue<long>> item in execTimes.OrderBy((KeyValuePair<MethodBase, Queue<long>> x) => x.Key.DeclaringType.FullName))
		{
			string fullName = item.Key.DeclaringType.FullName;
			string name = item.Key.Name;
			if (text != fullName)
			{
				if (stringBuilder.Length <= 0)
				{
					stringBuilder.AppendLine($"[BeerMP Profiler] max samples: {maxSamples}");
				}
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(fullName + ":");
				text = fullName;
			}
			stringBuilder.AppendLine("\n    [" + name + "]" + $"\n     average execution time: {((item.Value.Count > 0) ? avg(execTimes[item.Key], milliseconds: true) : 0.0)}ms  {((item.Value.Count > 0) ? avg(execTimes[item.Key], milliseconds: false) : 0.0)}ticks");
		}
		File.WriteAllText(fileName, stringBuilder.ToString());
		Console.Log("Saved Profiling results to '" + fileName + "' in Game Folder");
	}

	private static double avg(Queue<long> times, bool milliseconds)
	{
		float num = 0f;
		double num2 = 0.0;
		List<long> list = times.ToList();
		if (times.Count > 0)
		{
			for (int i = 0; i < list.Count; i++)
			{
				num += (float)list[i];
			}
			num2 = num / (float)list.Count;
		}
		return num2 / (double)(milliseconds ? 10000L : 1L);
	}
}
