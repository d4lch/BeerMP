using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BeerMP.Helpers;
using BeerMP.Networking.PlayerManagers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;

namespace BeerMP.Networking.Managers;

internal class NetManager : MonoBehaviour
{
	private static Dictionary<CSteamID, List<Component>> userObjects = new Dictionary<CSteamID, List<Component>>();

	public static AssetBundle assetBundle;

	public static GameObject SystemContainer;

	public static GameScene currentScene = GameScene.Unknown;

	public static Action<GameScene> sceneLoaded;

	public static int maxPlayers = 2;

	public static bool IsConnected;

	public static bool IsHost;

	public static CSteamID HostID;

	public static CSteamID UserID;

	private static bool init = false;

	private static bool savegameRecieved;

	private static bool hadSavegame;

	internal static List<CSteamID> connectedPlayers = new List<CSteamID>();

	internal static List<CSteamID> loadingPlayers = new List<CSteamID>();

	private static readonly FieldInfo f_MscloaderLoadAssetsAssetNames = ((!BeerMPGlobals.ModLoaderInstalled) ? null : BeerMPGlobals.mscloader.GetType("MSCLoader.LoadAssets").GetField("assetNames", BindingFlags.Static | BindingFlags.NonPublic));

	private int showingLobbyDialog;

	private bool doMenuReset;

	private PlayMakerFSM[] menuButtons;

	private Rect windowRect = new Rect((float)Screen.width / 2f - 150f, (float)Screen.height / 2f - 32.5f, 300f, 65f);

	public static Dictionary<CSteamID, string> playerNames = new Dictionary<CSteamID, string>();

	private string lobbyCode = "";

	private Camera cam;

	private static List<string> MscloaderLoadAssetsAssetNames => f_MscloaderLoadAssetsAssetNames?.GetValue(null) as List<string>;

	public static T GetPlayerComponentById<T>(ulong id) where T : class
	{
		return GetPlayerComponentById<T>((CSteamID)id);
	}

	public static T GetPlayerComponentById<T>(CSteamID id) where T : class
	{
		return userObjects[id].FirstOrDefault((Component x) => x.GetType() == typeof(T)) as T;
	}

	public static void AddBeerMPSyncs()
	{
		SystemContainer = new GameObject("BeerMP_NetManagers");
		try
		{
			Type[] types = typeof(NetManager).Assembly.GetTypes();
			types = (from x in types
				where x.GetCustomAttributes(typeof(ManagerCreate), inherit: true).Length != 0 && typeof(MonoBehaviour).IsAssignableFrom(x)
				orderby x.GetCustomAttributes(typeof(ManagerCreate), inherit: true).Sum((object x) => ((ManagerCreate)x).priority) descending
				select x).ToArray();
			for (int i = 0; i < types.Length; i++)
			{
				Console.Log("created '" + types[i].Name + "'.", show: false);
				SystemContainer.AddComponent(types[i]);
			}
		}
		catch (Exception message)
		{
			Console.LogError(message);
		}
	}

	internal static void SendSavegame(CSteamID player)
	{
		using Packet packet = new Packet(1, GameScene.MainMenu);
		string path = Application.persistentDataPath + "/defaultES2File.txt";
		string path2 = Application.persistentDataPath + "/items.txt";
		bool flag = File.Exists(path) && File.Exists(path2);
		packet.Write(flag);
		if (flag)
		{
			byte[] array = File.ReadAllBytes(path);
			packet.Write(array.Length);
			packet.Write(array);
			array = File.ReadAllBytes(path2);
			packet.Write(array.Length);
			packet.Write(array);
		}
		NetEvent<NetManager>.Send("Savegame", packet, player.m_SteamID);
	}

	internal static void RecieveSavegame(ulong userId, Packet packet)
	{
		if (!savegameRecieved)
		{
			savegameRecieved = true;
			string text = Application.persistentDataPath + "/defaultES2File.txt";
			string text2 = Application.persistentDataPath + "/items.txt";
			bool num = (hadSavegame = File.Exists(text) && File.Exists(text2));
			bool flag = packet.ReadBool();
			if (num)
			{
				File.Copy(text, Application.persistentDataPath + "/defaultES2File.bak", overwrite: true);
				File.Copy(text2, Application.persistentDataPath + "/items.bak", overwrite: true);
			}
			if (flag)
			{
				File.WriteAllBytes(text, packet.ReadBytes(packet.ReadInt()));
				File.WriteAllBytes(text2, packet.ReadBytes(packet.ReadInt()));
			}
			else
			{
				File.Delete(text);
				File.Delete(text2);
			}
			SceneLoader.LoadScene(GameScene.GAME);
		}
	}

	internal static void OnLobbyCreate(CSteamID[] players, CSteamID userId)
	{
		IsHost = true;
		IsConnected = true;
		HostID = SteamMatchmaking.GetLobbyOwner(SteamNet.currentLobby);
		UserID = SteamUser.GetSteamID();
		connectedPlayers = new List<CSteamID>();
		for (int i = 0; i < players.Length; i++)
		{
			connectedPlayers.Add(players[i]);
		}
		SceneLoader.LoadScene(GameScene.GAME);
	}

	internal static void OnMemberConnect(CSteamID userId, params CSteamID[] players)
	{
		connectedPlayers = new List<CSteamID>();
		for (int i = 0; i < players.Length; i++)
		{
			connectedPlayers.Add(players[i]);
		}
		loadingPlayers.Add(userId);
		CreateUserObjects();
		if (IsHost)
		{
			SendSavegame(userId);
		}
	}

	internal static void OnMemberDisconnect(CSteamID userId)
	{
		DeleteUserObjects(userId);
		connectedPlayers.Remove(userId);
		Console.Log(SteamFriends.GetFriendPersonaName(userId) + " left.");
		BeerMPGlobals.OnMemberExit?.Invoke(userId.m_SteamID);
	}

	internal static void OnConnect(CSteamID[] players, CSteamID userId)
	{
		IsConnected = true;
		connectedPlayers = new List<CSteamID>();
		for (int i = 0; i < players.Length; i++)
		{
			connectedPlayers.Add(players[i]);
		}
		UserID = SteamUser.GetSteamID();
		HostID = SteamMatchmaking.GetLobbyOwner(SteamNet.currentLobby);
	}

	internal static void OnReceive(ulong sender, byte[] data)
	{
		using Packet packet = new Packet(data);
		NetEventWorker.HandlePacket(sender, packet);
	}

	private void OnLevelWasLoaded(int levelId)
	{
		string loadedLevelName = Application.loadedLevelName;
		if (!(loadedLevelName == "MainMenu"))
		{
			if (loadedLevelName == "GAME")
			{
				currentScene = GameScene.GAME;
				if (IsConnected && !init)
				{
					init = true;
					CreateUserObjects();
					AddBeerMPSyncs();
					if (!BeerMPGlobals.IsHost)
					{
						DisableSaveFSM();
					}
					SystemContainer.AddComponent<LocalNetPlayer>();
				}
			}
		}
		else
		{
			currentScene = GameScene.MainMenu;
			init = false;
			StartMenu();
		}
		sceneLoaded?.Invoke(currentScene);
	}

	private static void DisableSaveFSM()
	{
		ObjectsLoader.gameLoaded += (Action)delegate
		{
			for (int i = 0; i < ObjectsLoader.ObjectsInGame.Length; i++)
			{
				if (!(ObjectsLoader.ObjectsInGame[i].name != "SAVEGAME"))
				{
					PlayMakerFSM component = ObjectsLoader.ObjectsInGame[i].GetComponent<PlayMakerFSM>();
					if (!(component == null) && !(component.FsmName != "Button"))
					{
						component.Initialize();
						FsmState state = component.GetState("Wait for click");
						if (state == null)
						{
							Console.LogError("Wait for click is null on " + component.transform.GetGameobjectHashString());
						}
						else if (!(state.Actions.First((FsmStateAction a) => a is SetStringValue) is SetStringValue setStringValue))
						{
							Console.LogError("Wait for click string is null on " + component.transform.GetGameobjectHashString());
						}
						else
						{
							setStringValue.stringValue = "DISCONNECT";
							FsmState state2 = component.GetState("Mute audio");
							if (state2 == null)
							{
								Console.LogError("Mute audio is null on " + component.transform.GetGameobjectHashString());
							}
							else if (!(state2.Actions.First((FsmStateAction a) => a is SetStringValue) is SetStringValue setStringValue2))
							{
								Console.LogError("Mute audio string is null on " + component.transform.GetGameobjectHashString());
							}
							else
							{
								setStringValue2.stringValue = "DISCONNECTING...";
								state2.Transitions[0].ToState = "Load menu";
								component.Initialize();
							}
						}
					}
				}
			}
		};
	}

	private static void CreateUserObjects()
	{
		for (int i = 0; i < connectedPlayers.Count; i++)
		{
			if (connectedPlayers[i] == SteamUser.GetSteamID() || userObjects.ContainsKey(connectedPlayers[i]))
			{
				continue;
			}
			userObjects.Add(connectedPlayers[i], new List<Component>());
			if (!userObjects[connectedPlayers[i]].Any((Component x) => x.GetType() != typeof(NetPlayer)))
			{
				NetPlayer netPlayer = BeerMP.instance.gameObject.AddComponent<NetPlayer>();
				netPlayer.steamID = connectedPlayers[i];
				if (!loadingPlayers.Contains(connectedPlayers[i]))
				{
					netPlayer.disableModel = false;
				}
				userObjects[connectedPlayers[i]].Add(netPlayer);
			}
		}
	}

	private static void DeleteUserObjects(CSteamID steamID)
	{
		if (userObjects.ContainsKey(steamID))
		{
			foreach (Component item in userObjects[steamID])
			{
				UnityEngine.Object.Destroy(item);
			}
		}
		userObjects.Remove(steamID);
	}

	public static void SendReliable(Packet packet, CSteamID user = default(CSteamID))
	{
		if (!IsConnected)
		{
			return;
		}
		byte[] array = packet.ToArray();
		if (user == default(CSteamID))
		{
			for (int i = 0; i < connectedPlayers.Count; i++)
			{
				if (connectedPlayers[i].m_SteamID != BeerMPGlobals.UserID)
				{
					SteamNetworking.SendP2PPacket(connectedPlayers[i], array, (uint)array.Length, EP2PSend.k_EP2PSendReliable);
				}
			}
		}
		else
		{
			SteamNetworking.SendP2PPacket(user, array, (uint)array.Length, EP2PSend.k_EP2PSendReliable);
		}
	}

	public static void SendUnreliable(Packet packet, CSteamID user = default(CSteamID))
	{
		if (!IsConnected)
		{
			return;
		}
		byte[] array = packet.ToArray();
		if (user == default(CSteamID))
		{
			for (int i = 0; i < connectedPlayers.Count; i++)
			{
				if (connectedPlayers[i].m_SteamID != BeerMPGlobals.UserID)
				{
					SteamNetworking.SendP2PPacket(connectedPlayers[i], array, (uint)array.Length, EP2PSend.k_EP2PSendUnreliable);
				}
			}
		}
		else
		{
			SteamNetworking.SendP2PPacket(user, array, (uint)array.Length, EP2PSend.k_EP2PSendUnreliable);
		}
	}

	private void StartMenu()
	{
		Console.Log("StartMenu");
		GameObject gameObject = GameObject.Find("Interface/Buttons");
		if (gameObject != null)
		{
			gameObject.GetComponent<PlayMakerFSM>().enabled = false;
			menuButtons = new PlayMakerFSM[2]
			{
				InitMenuButton(gameObject.transform.Find("ButtonContinue"), "OPEN LOBBY", delegate
				{
					showingLobbyDialog = 1;
				}),
				InitMenuButton(gameObject.transform.Find("ButtonNewgame"), "JOIN LOBBY", delegate
				{
					showingLobbyDialog = 2;
				})
			};
		}
	}

	private PlayMakerFSM InitMenuButton(Transform parent, string name, Action click)
	{
		PlayMakerFSM component = parent.GetComponent<PlayMakerFSM>();
		FsmState fsmState = component.FsmStates.First((FsmState s) => s.Name == "Action");
		string clickStateName = fsmState.Transitions.First((FsmTransition t) => t.EventName == "DOWN").ToState;
		FsmState fsmState2 = component.FsmStates.First((FsmState s) => s.Name == clickStateName);
		fsmState2.Transitions = new FsmTransition[1]
		{
			new FsmTransition
			{
				FsmEvent = component.FsmEvents.First((FsmEvent e) => e.Name == "FINISHED"),
				ToState = component.FsmStates[0].Name
			}
		};
		fsmState2.Actions = new FsmStateAction[2]
		{
			new PlayMakerUtilities.PM_Hook(click),
			new Wait
			{
				time = 0.2f
			}
		};
		TextMesh component2 = parent.GetChild(0).GetComponent<TextMesh>();
		TextMesh component3 = component2.transform.GetChild(0).GetComponent<TextMesh>();
		string text2 = (component3.text = name);
		component2.text = text2;
		return component;
	}

	private void Update()
	{
		if (currentScene == GameScene.MainMenu && BeerMPGlobals.ModLoaderInstalled)
		{
			if (doMenuReset && !Application.isLoadingLevel)
			{
				GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
				for (int i = 0; i < array.Length; i++)
				{
					if (!(array[i].name == "MSCUnloader") && !(array[i].name == "BeerMP"))
					{
						UnityEngine.Object.Destroy(array[i]);
					}
				}
				GameObject[] array2 = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
					where !x.activeInHierarchy && x.transform.parent == null
					select x).ToArray();
				List<string> mscloaderLoadAssetsAssetNames = MscloaderLoadAssetsAssetNames;
				for (int j = 0; j < array.Length; j++)
				{
					if (mscloaderLoadAssetsAssetNames.Contains(array2[j].name.ToLower()))
					{
						UnityEngine.Object.Destroy(array2[j]);
					}
				}
				PlayMakerGlobals.Instance.Variables.FindFsmBool("SongImported").Value = false;
				Application.LoadLevel(Application.loadedLevelName);
				doMenuReset = false;
			}
			if (showingLobbyDialog != 0 && Input.GetKeyDown(KeyCode.Escape))
			{
				showingLobbyDialog = 0;
			}
			for (int k = 0; k < menuButtons.Length; k++)
			{
				if (!menuButtons[k].enabled)
				{
					menuButtons[k].enabled = true;
				}
			}
		}
		if (IsConnected)
		{
			SteamNet.GetNetworkData();
		}
		if (SystemContainer != null && !SystemContainer.activeSelf)
		{
			SystemContainer.SetActive(value: true);
		}
	}

	private void FixedUpdate()
	{
		if (IsConnected)
		{
			SteamNet.CheckConnections();
		}
	}

	internal void Init()
	{
		SteamNet.Start();
		userObjects = new Dictionary<CSteamID, List<Component>>();
		NetEvent<NetManager>.Register("Savegame", RecieveSavegame);
		NetEvent<NetManager>.Register("PlayerLoaded", delegate(ulong sender, Packet _p)
		{
			loadingPlayers.Remove((CSteamID)sender);
			GetPlayerComponentById<NetPlayer>((CSteamID)sender).player.SetActive(value: true);
			BeerMPGlobals.OnMemberReady?.Invoke(sender);
		});
		if (Application.loadedLevelName == "MainMenu")
		{
			currentScene = GameScene.MainMenu;
		}
	}

	public static void Disconnect()
	{
		if (savegameRecieved && hadSavegame)
		{
			File.Copy(Application.persistentDataPath + "/defaultES2File.bak", Application.persistentDataPath + "/defaultES2File.txt", overwrite: true);
			File.Copy(Application.persistentDataPath + "/items.bak", Application.persistentDataPath + "/items.txt", overwrite: true);
		}
		IsConnected = false;
		SteamNet.CloseConnections();
		connectedPlayers = new List<CSteamID>();
		init = false;
		if (BeerMPGlobals.ModLoaderInstalled)
		{
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(BeerMPGlobals.mscloader.GetType("MSCLoader.MSCUnloader"));
			if (array.Length != 0)
			{
				GameObject obj = (array[0] as MonoBehaviour).gameObject;
				obj.SetActive(value: false);
				UnityEngine.Object.Destroy(obj);
				BeerMP.instance.netman.doMenuReset = true;
			}
		}
		SceneLoader.LoadScene(GameScene.MainMenu);
	}

	private void OnApplicationQuit()
	{
		SteamNet.CloseConnections();
		if (savegameRecieved)
		{
			File.Copy(Application.persistentDataPath + "/defaultES2File.bak", Application.persistentDataPath + "/defaultES2File.txt", overwrite: true);
			File.Copy(Application.persistentDataPath + "/items.bak", Application.persistentDataPath + "/items.txt", overwrite: true);
		}
	}

	private void OnGUI()
	{
		if (!IsConnected)
		{
			if (showingLobbyDialog <= 0)
			{
				return;
			}
			windowRect = GUI.Window(0, windowRect, delegate
			{
				GUI.DragWindow(new Rect(0f, 0f, float.PositiveInfinity, 20f));
				if (showingLobbyDialog == 1)
				{
					maxPlayers = Mathf.Clamp(int.Parse(GUILayout.TextField(maxPlayers.ToString())), 2, 250);
					if (GUILayout.Button("Open lobby"))
					{
						SteamNet.CreateLobby(SteamUser.GetSteamID(), maxPlayers, ELobbyType.k_ELobbyTypePublic);
					}
					if (!char.IsNumber(Event.current.character))
					{
						Event.current.character = '\0';
					}
				}
				else
				{
					lobbyCode = GUILayout.TextField(lobbyCode.ToString());
					GUI.enabled = false;
					if (lobbyCode != "")
					{
						GUI.enabled = true;
					}
					if (GUILayout.Button("Join lobby"))
					{
						SteamNet.JoinLobby(lobbyCode);
					}
					GUI.enabled = true;
				}
			}, (showingLobbyDialog == 1) ? "Open lobby" : "Join lobby");
			return;
		}
		GUILayout.BeginArea(new Rect(5f, 5f, Screen.width - 10, Screen.height - 10));
		CSteamID[] array = connectedPlayers.ToArray();
		if (cam == null)
		{
			FsmGameObject fsmGameObject = FsmVariables.GlobalVariables.FindFsmGameObject("POV");
			if (fsmGameObject != null)
			{
				cam = fsmGameObject.Value.GetComponent<Camera>();
			}
		}
		GUILayout.Label($"Current Lobby ID: {SteamNet.currentLobby}");
		GUILayout.Label("Connected players:");
		for (int i = 0; i < array.Length; i++)
		{
			if (!playerNames.ContainsKey(array[i]))
			{
				CSteamID steamID = SteamUser.GetSteamID();
				if (array[i] != steamID)
				{
					string friendPersonaName = SteamFriends.GetFriendPersonaName(array[i]);
					playerNames.Add(array[i], friendPersonaName);
				}
				else
				{
					playerNames.Add(array[i], SteamFriends.GetPersonaName());
				}
				continue;
			}
			GUILayout.Label(playerNames[array[i]] + " " + ((array[i] == SteamMatchmaking.GetLobbyOwner(SteamNet.currentLobby)) ? " <color=red>[Host]</color>" : " <color=lime>[Client]</color>") + " " + (loadingPlayers.Contains(array[i]) ? "Loading...".Color("orange") : ""));
			if (!userObjects.ContainsKey(array[i]) || array[i].m_SteamID == BeerMPGlobals.UserID || !(cam != null) || loadingPlayers.Contains(array[i]))
			{
				continue;
			}
			NetPlayer playerComponentById = GetPlayerComponentById<NetPlayer>(array[i]);
			if (playerComponentById != null)
			{
				Vector3 headPos = playerComponentById.HeadPos;
				Vector3 position = cam.transform.position;
				if ((cam.transform.position + cam.transform.forward - headPos).sqrMagnitude < (position - headPos).sqrMagnitude)
				{
					DrawUsername(cam, headPos, playerNames[array[i]]);
				}
			}
		}
		GUILayout.EndArea();
	}

	private void DrawUsername(Camera cam, Vector3 worldPosition, string name)
	{
		Vector3 vector = cam.WorldToScreenPoint(worldPosition + Vector3.up * 0.5f);
		float magnitude = (worldPosition - cam.transform.position).magnitude;
		GUIStyle gUIStyle = new GUIStyle
		{
			fontSize = Mathf.FloorToInt(36f / magnitude)
		};
		if (gUIStyle.fontSize != 0)
		{
			vector.x -= gUIStyle.CalcSize(new GUIContent(name)).x / 2f;
			GUI.Label(new Rect(vector.x, (float)Screen.height - vector.y, Screen.width, Screen.height), name.Color("white"), gUIStyle);
		}
	}
}
