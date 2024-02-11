using System;
using System.Collections.Generic;
using BeerMP.Helpers;
using BeerMP.Networking.Managers;

namespace BeerMP.Networking;

internal static class NetEventWorker
{
	internal static Dictionary<string, Dictionary<string, NetEventHandler>> eventHandlers = new Dictionary<string, Dictionary<string, NetEventHandler>>();

	internal static void HandlePacket(ulong sender, Packet packet)
	{
		try
		{
			string text = packet.ReadString();
			if (text == "BEERMP_IGNORE")
			{
				return;
			}
			string text2 = packet.ReadString();
			int num = packet.ReadInt();
			GameScene gameScene = (GameScene)packet.ReadInt();
			if (num != 1)
			{
				Console.Log($"received Packet for NetEvent {text2} of {text}\nTarget Scene: {gameScene}");
			}
			if (NetManager.currentScene != gameScene)
			{
				return;
			}
			switch (gameScene)
			{
			case GameScene.GAME:
				if (!ObjectsLoader.IsGameLoaded)
				{
					break;
				}
				goto default;
			default:
				if (!eventHandlers.ContainsKey(text))
				{
					Console.LogWarning("missing Event Handler Dictionary for " + text);
				}
				else if (!eventHandlers[text].ContainsKey(text2))
				{
					Console.LogWarning("missing Event Handler for " + text2 + " (parent: " + text + ")");
				}
				else
				{
					eventHandlers[text][text2]?.Invoke(sender, packet);
				}
				break;
			case GameScene.Unknown:
				break;
			}
		}
		catch (Exception message)
		{
			Console.LogError(message);
		}
	}
}
