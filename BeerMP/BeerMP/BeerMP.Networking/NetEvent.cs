using System;
using System.Collections.Generic;
using BeerMP.Networking.Managers;
using Steamworks;

namespace BeerMP.Networking;

public class NetEvent<T> : IDisposable
{
	internal string typeFullname;

	internal static List<NetEvent<T>> instances = new List<NetEvent<T>>();

	public string name { get; internal set; }

	internal NetEvent(string name, NetEventHandler handler)
	{
		this.name = name;
		typeFullname = typeof(T).FullName;
		instances.Add(this);
		if (!NetEventWorker.eventHandlers.ContainsKey(typeFullname))
		{
			NetEventWorker.eventHandlers.Add(typeFullname, new Dictionary<string, NetEventHandler>());
		}
		Dictionary<string, NetEventHandler> dictionary = NetEventWorker.eventHandlers[typeFullname];
		if (!dictionary.ContainsKey(name))
		{
			dictionary.Add(name, handler);
		}
	}

	public static NetEvent<T> Register(string name, NetEventHandler handler)
	{
		return new NetEvent<T>(name, handler);
	}

	public void Send(Packet packet, bool sendReliable = true)
	{
		Send(name, packet, sendReliable);
	}

	public static void Send(string name, Packet packet, bool sendReliable = true)
	{
		packet.Write(packet.scene, 0);
		packet.Write(packet.id, 0);
		packet.InsertString(name);
		packet.InsertString(typeof(T).FullName);
		if (sendReliable)
		{
			NetManager.SendReliable(packet);
		}
		else
		{
			NetManager.SendUnreliable(packet);
		}
	}

	public void Send(Packet packet, ulong target, bool sendReliable = true)
	{
		Send(name, packet, target, sendReliable);
	}

	public static void Send(string name, Packet packet, ulong target, bool sendReliable = true)
	{
		packet.Write(packet.scene, 0);
		packet.Write(packet.id, 0);
		packet.InsertString(name);
		packet.InsertString(typeof(T).FullName);
		if (sendReliable)
		{
			NetManager.SendReliable(packet, (CSteamID)target);
		}
		else
		{
			NetManager.SendUnreliable(packet, (CSteamID)target);
		}
	}

	public void Unregister()
	{
		instances.Remove(this);
		NetEventWorker.eventHandlers[typeFullname].Remove(name);
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Unregister();
	}
}
