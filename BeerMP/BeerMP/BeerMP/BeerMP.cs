using System;
using BeerMP.Helpers;
using BeerMP.Networking.Managers;
using BeerMP.Properties;
using Discord;
using Steamworks;
using UnityEngine;

namespace BeerMP;

internal class BeerMP : MonoBehaviour
{
	public static BeerMP instance;

	internal NetManager netman;

	private bool init;

	public static bool debug;

	private static Activity activity;

	private static ActivityAssets activityAssets;

	private static ActivityManager activityManager;

	private const string _version = "v0.1.15";

	public const string version = "v0.1.15";

	public global::Discord.Discord discord { get; internal set; }

	private static long ApplicationID { get; } = long.Parse(global::BeerMP.Properties.Resources.clientID);


	private void Awake()
	{
		SteamAPI.Init();
		if (SteamApps.GetAppBuildId() < 100)
		{
			Debug.Log($"BEERMP CAN'T LOAD: APP BUILD ID = {SteamApps.GetAppBuildId()}, PLEASE UPDATE YOUR GAME");
			Application.Quit();
			return;
		}
		instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		netman = base.gameObject.AddComponent<NetManager>();
		Environment.SetEnvironmentVariable("BeerMP-Present", "fuck MSCO lmao - brenn 2024");
		try
		{
			discord = new global::Discord.Discord(ApplicationID, 1uL);
			if (discord != null)
			{
				activityManager = discord.GetActivityManager();
			}
		}
		catch (Exception ex)
		{
			discord = null;
			Debug.LogError(ex.Message);
		}
	}

	private void OnLevelWasLoaded(int levelId)
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	private void OnGUI()
	{
		Console.DrawGUI();
		Watermark();
	}

	private void Watermark()
	{
		GUILayout.BeginArea(new Rect(0f, 0f, Screen.width, Screen.height));
		GUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(("BeerMP remastered".Color("yellow") + " | " + "v0.1.15".Color("yellow")).Size(16));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(("THIS MOD IS IN ACTIVE DEVELOPMENT - ALL ELEMENTS AND ASPECTS ARE SUBJECT TO CHANGE!".Color("yellow") ?? "").Size(12).Italic());
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	private void Update()
	{
		if (discord != null)
		{
			discord.RunCallbacks();
		}
		SteamAPI.RunCallbacks();
		if (Application.loadedLevelName == "MainMenu" && !init)
		{
			Debug.Log("[BeerMP Init]");
			Console.Init();
			netman.Init();
			init = true;
			ResetActivity();
		}
		if (Input.GetKey(KeyCode.Alpha0))
		{
			Console.Log(Camera.main.transform.GetGameobjectHashString());
		}
		Console.UpdateLogDeleteTime();
	}

	public static void ResetActivity()
	{
		if (instance.discord != null)
		{
			Activity activity = default(Activity);
			activity.State = "Idling";
			activity.Timestamps = new ActivityTimestamps
			{
				Start = DateTime.Now.ToUnixTimestamp()
			};
			BeerMP.activity = activity;
			UpdateActivity(BeerMP.activity);
		}
	}

	public static void UpdateActivity(Activity activity)
	{
		if (instance.discord == null)
		{
			return;
		}
		activity.ApplicationId = ApplicationID;
		activity.Assets = new ActivityAssets
		{
			LargeImage = "beermp_logo",
			LargeText = "No alcohol is no solution."
		};
		activityManager.UpdateActivity(activity, delegate(Result res)
		{
			if (res == Result.Ok)
			{
				Console.Log("Discord: Status Updated");
			}
			else
			{
				Console.LogError($"Discord: Status Update failed! {res}");
			}
		});
	}

	private void OnApplicationQuit()
	{
		Environment.SetEnvironmentVariable("BeerMP-Present", null);
		SteamAPI.Shutdown();
	}
}
