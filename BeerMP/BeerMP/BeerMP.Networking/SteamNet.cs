using System;
using System.Collections.Generic;
using BeerMP.Helpers;
using BeerMP.Networking.Managers;
using Discord;
using Steamworks;

namespace BeerMP.Networking;

internal class SteamNet
{
	public static LobbyID currentLobby;

	private static List<CSteamID> p2pConnections;

	private static Callback<P2PSessionConnectFail_t> onP2PSessionConnectFail;

	private static Callback<P2PSessionRequest_t> onP2PSessionRequest;

	private static Callback<GameLobbyJoinRequested_t> onGameLobbyJoinRequested;

	private static Callback<LobbyCreated_t> onLobbyCreated;

	private static Callback<LobbyEnter_t> onLobbyEnter;

	private static Callback<LobbyChatUpdate_t> onLobbyChatUpdate;

	private static Callback<LobbyDataUpdate_t> onLobbyDataUpdate;

	private static Activity activity;

	private static bool joiningLobby;

	public static CSteamID[] GetMembers()
	{
		LobbyID lobbyID = currentLobby;
		int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
		CSteamID[] array = new CSteamID[numLobbyMembers];
		for (int i = 0; i < numLobbyMembers; i++)
		{
			array[i] = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
		}
		return array;
	}

	public static void CloseConnections()
	{
		for (int i = 0; i < p2pConnections.Count; i++)
		{
			CloseConnection(p2pConnections[i]);
			p2pConnections.Remove(p2pConnections[i]);
		}
	}

	public static void CloseConnection(CSteamID user)
	{
		SteamNetworking.CloseP2PSessionWithUser(user);
		p2pConnections.Remove(user);
	}

	private static void OnP2PSessionConnectFail(P2PSessionConnectFail_t cb)
	{
		Console.LogError($"P2P Connection failed {(EP2PSessionError)cb.m_eP2PSessionError}");
	}

	private static void OnP2PSessionRequest(P2PSessionRequest_t cb)
	{
		Console.Log($"P2P Session requested by {cb.m_steamIDRemote}");
		if (SteamNetworking.AcceptP2PSessionWithUser(cb.m_steamIDRemote))
		{
			NetManager.OnMemberConnect(cb.m_steamIDRemote, GetMembers());
			if (!p2pConnections.Contains(cb.m_steamIDRemote))
			{
				p2pConnections.Add(cb.m_steamIDRemote);
			}
			Console.Log($"Accepting P2P Session with {cb.m_steamIDRemote.m_SteamID} successful.");
			SetActivity();
		}
		else
		{
			Console.LogError($"Accepting P2P Session with {cb.m_steamIDRemote.m_SteamID} failed.");
		}
	}

	private static void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t cb)
	{
		SteamMatchmaking.JoinLobby(cb.m_steamIDLobby);
	}

	private static void OnLobbyCreated(LobbyCreated_t cb)
	{
		p2pConnections = new List<CSteamID>();
		ulong ulSteamIDLobby = cb.m_ulSteamIDLobby;
		SteamMatchmaking.SetLobbyData((CSteamID)ulSteamIDLobby, "ver", "v0.1.15");
		SteamMatchmaking.SetLobbyData((CSteamID)ulSteamIDLobby, "gver", SteamApps.GetAppBuildId().ToString());
		currentLobby = new LobbyID(LobbyCodeParser.GetString(ulSteamIDLobby));
		if (cb.m_eResult == EResult.k_EResultOK)
		{
			Console.Log($"Created Lobby {currentLobby}");
			Clipboard.text = currentLobby.ToString();
			Console.Log("Copied Lobby ID to Clipboard.");
			NetManager.OnLobbyCreate(GetMembers(), SteamUser.GetSteamID());
		}
		else
		{
			Console.LogError($"Creating Lobby failed. {cb.m_eResult}");
		}
	}

	private static void OnLobbyEnter(LobbyEnter_t cb)
	{
		p2pConnections = new List<CSteamID>();
		if (cb.m_EChatRoomEnterResponse == 1)
		{
			currentLobby = new LobbyID(LobbyCodeParser.GetString(cb.m_ulSteamIDLobby));
			NetManager.OnConnect(GetMembers(), SteamUser.GetSteamID());
			Console.Log($"Joined Lobby {currentLobby}");
			NetManager.maxPlayers = SteamMatchmaking.GetLobbyMemberLimit((CSteamID)cb.m_ulSteamIDLobby);
			using (Packet packet = new Packet())
			{
				packet.InsertString("BEERMP_IGNORE");
				NetManager.SendReliable(packet);
			}
			SetActivity();
		}
		else
		{
			Console.LogError($"Joining Lobby failed. {(EChatRoomEnterResponse)cb.m_EChatRoomEnterResponse}");
		}
	}

	public static void Start()
	{
		SteamAPI.Init();
		onP2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);
		onP2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
		onGameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
		onLobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
		onLobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
		onLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyMemberStateUpdate);
		onLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
	}

	private static void OnLobbyDataUpdate(LobbyDataUpdate_t param)
	{
		if (joiningLobby)
		{
			joiningLobby = false;
			CSteamID steamIDLobby = (CSteamID)param.m_ulSteamIDLobby;
			string @string = LobbyCodeParser.GetString(param.m_ulSteamIDLobby);
			string lobbyData = SteamMatchmaking.GetLobbyData(steamIDLobby, "gver");
			string lobbyData2 = SteamMatchmaking.GetLobbyData(steamIDLobby, "ver");
			string text = SteamApps.GetAppBuildId().ToString();
			if (string.IsNullOrEmpty(lobbyData))
			{
				Console.LogError("Can't join lobby " + @string + " because it's still loading (ERR: gver null)", show: true);
			}
			else if (lobbyData != text)
			{
				Console.LogError("Can't join lobby " + @string + " because it's targetting game version " + lobbyData + " (current version is " + text + ")", show: true);
			}
			else if (string.IsNullOrEmpty(lobbyData2))
			{
				Console.LogError("Can't join lobby " + @string + " because it's still loading (ERR: ver null)", show: true);
			}
			else if (lobbyData2 != "v0.1.15")
			{
				Console.LogError("Can't join lobby " + @string + " because it's targetting BeerMP version " + lobbyData2 + " (current version is v0.1.15)", show: true);
			}
			else
			{
				SteamMatchmaking.JoinLobby(steamIDLobby);
				Console.Log("Trying to Join Lobby (" + @string + ")...");
			}
		}
	}

	private static void OnLobbyMemberStateUpdate(LobbyChatUpdate_t param)
	{
		CSteamID cSteamID = (CSteamID)param.m_ulSteamIDUserChanged;
		if (param.m_rgfChatMemberStateChange == 2)
		{
			if (param.m_ulSteamIDUserChanged == BeerMPGlobals.HostID)
			{
				NetManager.Disconnect();
				Console.Log("Session closed. Host disconnected.");
			}
			else
			{
				CloseConnection(cSteamID);
				NetManager.OnMemberDisconnect(cSteamID);
			}
		}
	}

	public static void JoinLobby(string steamIDLobby)
	{
		if (!SteamMatchmaking.RequestLobbyData((CSteamID)LobbyCodeParser.GetUlong(steamIDLobby)))
		{
			Console.LogError("Can't join lobby " + steamIDLobby + " failed to request lobby data", show: true);
			return;
		}
		joiningLobby = true;
		Console.Log("Requesting Lobby data ...");
	}

	public static void CreateLobby(CSteamID owner, int maxPlayers, ELobbyType lobbyType)
	{
		NetManager.connectedPlayers = new List<CSteamID>();
		SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
	}

	public static void GetNetworkData()
	{
		uint pcubMsgSize;
		while (SteamNetworking.IsP2PPacketAvailable(out pcubMsgSize))
		{
			byte[] array = new byte[pcubMsgSize];
			uint pcubMsgSize2 = 0u;
			if (SteamNetworking.ReadP2PPacket(array, pcubMsgSize, out pcubMsgSize2, out var psteamIDRemote) && psteamIDRemote != SteamUser.GetSteamID())
			{
				NetManager.OnReceive(psteamIDRemote.m_SteamID, array);
			}
		}
	}

	public static void CheckConnections()
	{
		foreach (CSteamID p2pConnection in p2pConnections)
		{
			SteamNetworking.GetP2PSessionState(p2pConnection, out var pConnectionState);
			bool num = Convert.ToBoolean(pConnectionState.m_bConnecting);
			bool flag = Convert.ToBoolean(pConnectionState.m_bConnectionActive);
			if (!num && !flag)
			{
				if (p2pConnections.Contains(p2pConnection))
				{
					p2pConnections.Remove(p2pConnection);
				}
				NetManager.OnMemberDisconnect(p2pConnection);
				SetActivity();
			}
		}
	}

	private static void SetActivity()
	{
		Activity activity = default(Activity);
		activity.State = $"Playing Online ({NetManager.connectedPlayers.Count} of {NetManager.maxPlayers})";
		activity.Details = "Roaming the roads of Alivieska";
		activity.Type = ActivityType.Playing;
		activity.Timestamps = new ActivityTimestamps
		{
			Start = DateTime.Now.ToUnixTimestamp()
		};
		activity.Instance = true;
		SteamNet.activity = activity;
		BeerMP.UpdateActivity(SteamNet.activity);
	}
}
