using Steamworks;

namespace BeerMP.Networking
{

	internal struct LobbyID
	{
		public CSteamID m_SteamID;

		public string m_LobbyCode;

		public LobbyID(CSteamID cSteamID, string lobbyCode)
		{
			m_SteamID = cSteamID;
			m_LobbyCode = lobbyCode;
		}

		public LobbyID(CSteamID cSteamID)
		{
			m_SteamID = cSteamID;
			m_LobbyCode = LobbyCodeParser.GetString(cSteamID.m_SteamID);
		}

		public LobbyID(string lobbyCode)
		{
			m_SteamID = new CSteamID(LobbyCodeParser.GetUlong(lobbyCode));
			m_LobbyCode = lobbyCode;
		}

		public static explicit operator LobbyID(string lobbyCode)
		{
			return new LobbyID(lobbyCode);
		}

		public static explicit operator LobbyID(CSteamID cSteamID)
		{
			return new LobbyID(cSteamID);
		}

		public static implicit operator CSteamID(LobbyID lobbyID)
		{
			return lobbyID.m_SteamID;
		}

		public static implicit operator string(LobbyID lobbyID)
		{
			return lobbyID.m_LobbyCode;
		}

		public override string ToString()
		{
			return m_LobbyCode;
		}

		public override int GetHashCode()
		{
			return m_SteamID.m_SteamID.GetHashCode();
		}
	}
}