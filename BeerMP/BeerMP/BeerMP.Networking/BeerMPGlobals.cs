using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BeerMP.Helpers;
using BeerMP.Networking.Managers;
using Steamworks;

namespace BeerMP.Networking;

public class BeerMPGlobals
{
	public static ActionContainer<ulong> OnMemberJoin = new ActionContainer<ulong>();

	public static ActionContainer<ulong> OnMemberReady = new ActionContainer<ulong>();

	public static ActionContainer<ulong> OnMemberExit = new ActionContainer<ulong>();

	internal static bool ModLoaderInstalled = File.Exists("mysummercar_Data\\Managed\\MSCLoader.dll") && !Environment.GetCommandLineArgs().Any((string x) => x.Contains("-mscloader-disable"));

	internal static Assembly mscloader = (ModLoaderInstalled ? Assembly.LoadFile("mysummercar_Data\\Managed\\MSCLoader.dll") : null);

	public static bool IsHost => NetManager.IsHost;

	public static ulong HostID
	{
		get
		{
			if (!(NetManager.HostID != default(CSteamID)))
			{
				return 0uL;
			}
			return NetManager.HostID.m_SteamID;
		}
	}

	public static ulong UserID
	{
		get
		{
			if (!(NetManager.UserID != default(CSteamID)))
			{
				return 0uL;
			}
			return NetManager.UserID.m_SteamID;
		}
	}
}
