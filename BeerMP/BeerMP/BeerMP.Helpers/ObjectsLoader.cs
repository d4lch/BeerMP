using System;
using System.Reflection;
using BeerMP.Networking;
using BeerMP.Networking.Managers;
using UnityEngine;

namespace BeerMP.Helpers;

[ManagerCreate(10000)]
public class ObjectsLoader : MonoBehaviour
{
	private static GameObject[] objectsInGame;

	public static ActionContainer gameLoaded = new ActionContainer();

	private static bool isGameLoaded = false;

	private FieldInfo allModsLoaded;

	private object modloaderInstance;

	public static GameObject[] ObjectsInGame => objectsInGame;

	public static bool IsGameLoaded => isGameLoaded;

	public ObjectsLoader()
	{
		if (BeerMPGlobals.ModLoaderInstalled)
		{
			Type type = BeerMPGlobals.mscloader.GetType("MSCLoader.ModLoader");
			Console.Log($"modloader null {type == null}");
			modloaderInstance = type.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			if (modloaderInstance == null)
			{
				Console.LogError("ModLoader instance is null but it is present");
			}
			else
			{
				allModsLoaded = type.GetField("allModsLoaded", BindingFlags.Instance | BindingFlags.NonPublic);
			}
		}
	}

	private void Start()
	{
	}

	private void Update()
	{
		if (isGameLoaded || !(GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera") != null) || (BeerMPGlobals.ModLoaderInstalled && !(bool)allModsLoaded.GetValue(modloaderInstance)))
		{
			return;
		}
		isGameLoaded = true;
		objectsInGame = Resources.FindObjectsOfTypeAll<GameObject>();
		gameLoaded?.Invoke();
		if (!BeerMPGlobals.IsHost)
		{
			using Packet packet = new Packet();
			NetEvent<NetManager>.Send("PlayerLoaded", packet);
		}
		Console.Log("Game loaded!");
	}

	private void OnDestroy()
	{
		objectsInGame = null;
		gameLoaded = null;
		isGameLoaded = false;
	}
}
