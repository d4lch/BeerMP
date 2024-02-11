using System;
using System.Collections.Generic;
using BeerMP.Helpers;
using BeerMP.Networking;
using BeerMP.Networking.Managers;
using BeerMP.Networking.PlayerManagers;
using Steamworks;
using UnityEngine;

namespace BeerMP;

internal static class CommandHandler
{
	internal struct Command
	{
		public string name;

		public string usage;

		public bool isHostOnly;

		public int argCountMin;

		public int argCountMax;

		public Action<string[]> handler;

		public Command(string name, string usage, int argCount, bool isHostOnly, Action<string[]> handler)
		{
			this.name = name;
			this.usage = usage;
			argCountMin = (argCountMax = argCount);
			this.isHostOnly = isHostOnly;
			this.handler = handler;
		}

		public Command(string name, string usage, int argCountMin, int argCountMax, bool isHostOnly, Action<string[]> handler)
		{
			this.name = name;
			this.usage = usage;
			this.argCountMin = argCountMin;
			this.argCountMax = argCountMax;
			this.isHostOnly = isHostOnly;
			this.handler = handler;
		}
	}

	private static Command[] commands = new Command[9]
	{
		new Command("/tp", "/tp <player>", 1, isHostOnly: false, delegate(string[] args)
		{
			CSteamID id2 = default(CSteamID);
			bool flag2 = false;
			foreach (KeyValuePair<CSteamID, string> playerName in NetManager.playerNames)
			{
				if (playerName.Value.ToLower() == args[0].ToLower())
				{
					id2 = playerName.Key;
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				Console.Log("Player " + args[0] + " can't be found");
			}
			else
			{
				Transform transform = NetManager.GetPlayerComponentById<NetPlayer>(id2).player.transform;
				LocalNetPlayer.Instance.player.Value.transform.position = transform.position;
				Console.Log("Teleported to " + args[0]);
			}
		}),
		new Command("/rbhash", "/rbhash <hash>", 1, isHostOnly: false, delegate(string[] args)
		{
			if (int.TryParse(args[0], out var result3))
			{
				NetRigidbodyManager.OwnedRigidbody ownedRigidbody = NetRigidbodyManager.GetOwnedRigidbody(result3);
				Console.Log($"The object of hash {result3} is {ownedRigidbody.transform.name}, full path: {ownedRigidbody.transform.GetGameobjectHashString()}. DEBUG: {ownedRigidbody.remove == null}, {ownedRigidbody.Removal_Part == null}, {ownedRigidbody.Removal_Rigidbody == null}, {ownedRigidbody.rigidbody == null}, {ownedRigidbody.rigidbodyPart == null}, {ownedRigidbody.transform == null}");
			}
			else
			{
				Console.LogError("The argument is not a number!", show: true);
			}
		}),
		new Command("/sethand", "/sethand <player> <left = true|right = false> <true|false>", 3, isHostOnly: false, delegate(string[] args)
		{
			if (!bool.TryParse(args[1], out var result))
			{
				Console.LogError("The hand type argument is not a bool!", show: true);
			}
			if (!bool.TryParse(args[2], out var result2))
			{
				Console.LogError("The value argument is not a bool!", show: true);
			}
			CSteamID id = default(CSteamID);
			bool flag = false;
			foreach (KeyValuePair<CSteamID, string> playerName2 in NetManager.playerNames)
			{
				if (playerName2.Value.ToLower() == args[0].ToLower())
				{
					id = playerName2.Key;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Console.Log("Player " + args[0] + " can't be found");
			}
			else
			{
				PlayerAnimationManager playerAnimationManager = NetManager.GetPlayerComponentById<NetPlayer>(id).playerAnimationManager;
				if (result)
				{
					playerAnimationManager.leftHandOn = result2;
				}
				else
				{
					playerAnimationManager.rightHandOn = result2;
				}
				Console.Log(string.Format("Set {0} hand of {1} to {2}", result ? "left" : "right", args[0], result2));
			}
		}),
		new Command("/resetgrab", "/resetgrab", 0, isHostOnly: false, delegate
		{
			PlayerGrabbingManager.handFSM.SendEvent("FINISHED");
			Console.Log("Successfully attempted to reset grabbing fsm");
		}),
		new Command("/satprof", "/satprof", 0, isHostOnly: false, delegate
		{
			SatsumaProfiler.Instance.PrintToFile();
			Console.Log("Saved last 10 seconds of satsuma behaviour to satsuma_profiler.txt");
		}),
		new Command("/bprofiler", "/bprofiler <start|stop>", 1, isHostOnly: false, delegate(string[] args)
		{
			string text = args[0].ToLower();
			if (!(text == "start"))
			{
				if (!(text == "stop"))
				{
					Console.LogError("Invalid argument, please use either start or stop", show: true);
				}
				else
				{
					BProfiler.Stop("BeerMP_Profiler_results.txt");
				}
			}
			else
			{
				BProfiler.Start();
			}
		}),
		new Command("/pvc", "/pvc <enable true/false|pushtotalk true/false|changekey|mastervolume 0-10>", 1, 2, isHostOnly: false, delegate(string[] args)
		{
			if (args[0].ToLower() != "changekey" && args.Length < 2)
			{
				Console.LogError("Invalid syntax. Please use: /pvc <enable true/false|pushtotalk true/false|changekey|mastervolume 0-10>", show: true);
			}
			else
			{
				switch (args[0].ToLower())
				{
				case "mastervolume":
				{
					float? num2 = float.Parse(args[1]);
					if (num2.HasValue)
					{
						ProximityVoiceChat.SetMasterVolume(num2.Value);
						Console.Log($"Changed voice chat volume to {num2.Value}");
					}
					else
					{
						Console.LogError("The value " + args[1] + " is not a number!", show: true);
					}
					break;
				}
				case "changekey":
					ProximityVoiceChat.ChangePTT_Keybing();
					Console.Log("Press the key you want to assign Push to talk to");
					break;
				case "pushtotalk":
				{
					bool num3 = bool.Parse(args[1]);
					ProximityVoiceChat.SetPushToTalk(num3);
					Console.Log((num3 ? "Enabled" : "Disabled") + " push to talk");
					break;
				}
				case "enable":
				{
					bool num = bool.Parse(args[1]);
					ProximityVoiceChat.SetEnabled(num);
					Console.Log((num ? "Enabled" : "Disabled") + " voice chat");
					break;
				}
				}
			}
		}),
		new Command("/vehlist", "/vehlist", 0, isHostOnly: false, delegate
		{
			for (int i = 0; i < NetVehicleManager.vehicles.Count; i++)
			{
				Console.Log($"{i} = {NetVehicleManager.vehicles[i].transform.name}");
			}
		}),
		new Command("/resetdm", "/resetdm <vehicle index>", 1, isHostOnly: false, delegate(string[] args)
		{
			int index = int.Parse(args[0]);
			NetVehicleManager.vehicles[index].driver = 0uL;
			Console.Log("Successfully resetted " + NetVehicleManager.vehicles[index].transform.name + " driving mode");
		})
	};

	public static void Execute(string command, params string[] args)
	{
		int num = -1;
		for (int i = 0; i < commands.Length; i++)
		{
			if (commands[i].name == command)
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			Console.LogError("Unknown command '" + command + "'", show: true);
		}
		else if (args.Length >= commands[num].argCountMin && args.Length <= commands[num].argCountMax)
		{
			if (commands[num].isHostOnly && !BeerMPGlobals.IsHost)
			{
				Console.LogError("This command can only be executed by the lobby host!", show: true);
			}
			else
			{
				commands[num].handler?.Invoke(args);
			}
		}
		else
		{
			Console.LogError("Invalid syntax. Please use: " + commands[num].usage, show: true);
		}
	}
}
