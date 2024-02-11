using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using BeerMP.Networking.PlayerManagers;
using Steamworks;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
public class NetVehicleManager : MonoBehaviour
{
	internal static List<NetVehicle> vehicles = new List<NetVehicle>();

	internal static List<FsmNetVehicle> vanillaVehicles = new List<FsmNetVehicle>();

	internal static NetEvent<NetVehicleManager> InitialSyncEvent;

	internal static NetEvent<NetVehicleManager> SoundUpdateEvent;

	internal static NetEvent<NetVehicleManager> DrivingModeEvent;

	internal static NetEvent<NetVehicleManager> PassengerModeEvent;

	internal static NetEvent<NetVehicleManager> FlatbedPassengerModeEvent;

	internal static NetEvent<NetVehicleManager> InputUpdateEvent;

	internal static NetEvent<NetVehicleManager> DrivetrainUpdateEvent;

	private Transform flatbed;

	internal int flatbedHash;

	public static void RegisterNetVehicle(NetVehicle netVeh)
	{
		vehicles.Add(netVeh);
		Console.Log($"registered NetVehicle with hash {netVeh.hash} ({netVeh.transform.name})", show: false);
	}

	private void Start()
	{
		vehicles.Clear();
		Action action = delegate
		{
			InitialSyncEvent = NetEvent<NetVehicleManager>.Register("InitialSync", OnInitialSync);
			SoundUpdateEvent = NetEvent<NetVehicleManager>.Register("SoundUpdate", OnSoundUpdate);
			DrivingModeEvent = NetEvent<NetVehicleManager>.Register("DrivingMode", OnDrivingMode);
			PassengerModeEvent = NetEvent<NetVehicleManager>.Register("PassengerMode", OnPassengerMode);
			FlatbedPassengerModeEvent = NetEvent<NetVehicleManager>.Register("FlatbedPassengerMode", OnFlatbedPassengerMode);
			InputUpdateEvent = NetEvent<NetVehicleManager>.Register("InputUpdate", OnInputUpdate);
			DrivetrainUpdateEvent = NetEvent<NetVehicleManager>.Register("DrivetrainUpdate", OnDrivetrainUpdate);
			PlayMakerFSM[] array = (from x in Resources.FindObjectsOfTypeAll<PlayMakerFSM>()
				where x.FsmName == "GearIndicator"
				select x).ToArray();
			int num = 0;
			while (true)
			{
				if (num >= array.Length)
				{
					FsmNetVehicle.DoFlatbedPassengerSeats(out flatbed, out flatbedHash, EnterFlatbedPassenger);
					BeerMPGlobals.OnMemberReady += new Action<ulong>(OnMemberReady);
					BeerMPGlobals.OnMemberExit += new Action<ulong>(OnMemberExit);
					break;
				}
				if (array[num].transform.GetComponent<Drivetrain>() == null)
				{
					break;
				}
				FsmNetVehicle item = new FsmNetVehicle(array[num].transform);
				vanillaVehicles.Add(item);
				num++;
			}
		};
		if (ObjectsLoader.IsGameLoaded)
		{
			action();
		}
		else
		{
			ObjectsLoader.gameLoaded += action;
		}
	}

	private void OnFlatbedPassengerMode(ulong sender, Packet packet)
	{
		bool enter = packet.ReadBool();
		NetManager.GetPlayerComponentById<NetPlayer>((CSteamID)sender).SetPassengerMode(enter, flatbed, applySitAnim: false);
	}

	private void EnterFlatbedPassenger(bool enter)
	{
		using Packet packet = new Packet(1);
		packet.Write(enter);
		LocalNetPlayer.Instance.inCar = enter;
		FlatbedPassengerModeEvent.Send(packet);
	}

	private void OnInitialSync(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		NetVehicle netVehicle = vehicles.FirstOrDefault((NetVehicle x) => x.hash == hash);
		if (netVehicle == null)
		{
			Console.LogError($"InitialSync: vehicle with hash {hash} not found.", show: true);
		}
		else
		{
			netVehicle.OnInitialSync(packet);
		}
	}

	private void OnDrivetrainUpdate(ulong sender, Packet packet)
	{
		int hash;
		while (true)
		{
			if (packet.UnreadLength() > 0)
			{
				hash = packet.ReadInt();
				float rpm = packet.ReadFloat();
				int gear = packet.ReadInt();
				NetVehicle netVehicle = vehicles.FirstOrDefault((NetVehicle x) => x.hash == hash);
				if (netVehicle == null)
				{
					break;
				}
				if (netVehicle.owner == sender && netVehicle.acc != null)
				{
					netVehicle.SetDrivetrain(rpm, gear);
				}
				continue;
			}
			return;
		}
		Console.LogError($"Received input update packet for unknown vehicle with hash {hash}");
	}

	private void OnMemberExit(ulong player)
	{
		for (int i = 0; i < vehicles.Count; i++)
		{
			NetVehicle netVehicle = vehicles[i];
			if (netVehicle != null && netVehicle.driver == player)
			{
				netVehicle.driver = 0uL;
				netVehicle.owner = BeerMPGlobals.HostID;
				if (BeerMPGlobals.IsHost)
				{
					NetRigidbodyManager.RequestOwnership(netVehicle.rigidbody);
				}
			}
			for (int j = 0; j < netVehicle?.seatsUsed.Count; j++)
			{
				if (netVehicle.seatsUsed[j] == player)
				{
					netVehicle.seatsUsed[j] = 0uL;
				}
			}
		}
	}

	private void OnMemberReady(ulong player)
	{
		using Packet packet = new Packet(1);
		for (int i = 0; i < vehicles.Count; i++)
		{
			NetVehicle netVehicle = vehicles[i];
			if (netVehicle?.owner == BeerMPGlobals.UserID)
			{
				if (netVehicle.audioController != null)
				{
					netVehicle.audioController.WriteUpdate(packet, netVehicle.hash, initSync: true);
				}
				netVehicle.SendInitialSync(player);
			}
		}
		SoundUpdateEvent.Send(packet, player);
	}

	private void OnInputUpdate(ulong sender, Packet packet)
	{
		int hash;
		while (true)
		{
			if (packet.UnreadLength() > 0)
			{
				hash = packet.ReadInt();
				float brake = packet.ReadFloat();
				float throttle = packet.ReadFloat();
				float steering = packet.ReadFloat();
				float handbrake = packet.ReadFloat();
				float clutch = packet.ReadFloat();
				NetVehicle netVehicle = vehicles.FirstOrDefault((NetVehicle x) => x.hash == hash);
				if (netVehicle == null)
				{
					break;
				}
				if (netVehicle.owner == sender && netVehicle.acc != null)
				{
					netVehicle.SetAxisController(brake, throttle, steering, handbrake, clutch);
				}
				continue;
			}
			return;
		}
		Console.LogError($"Received input update packet for unknown vehicle with hash {hash}");
	}

	private void LateUpdate()
	{
		using Packet packet = new Packet(1);
		using Packet packet2 = new Packet(1);
		using Packet packet3 = new Packet(1);
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		for (int i = 0; i < vehicles.Count; i++)
		{
			NetVehicle netVehicle = vehicles[i];
			netVehicle.Update();
			if (netVehicle.audioController != null)
			{
				netVehicle.audioController.Update();
			}
			if (netVehicle.owner == BeerMPGlobals.UserID)
			{
				if (netVehicle.audioController != null)
				{
					flag |= netVehicle.audioController.WriteUpdate(packet, netVehicle.hash);
				}
				if (netVehicle.acc != null)
				{
					flag2 = true;
					netVehicle.WriteAxisControllerUpdate(packet2);
				}
				if (netVehicle.drivetrain != null)
				{
					flag3 = true;
					netVehicle.WriteDrivetrainUpdate(packet3);
				}
			}
		}
		if (flag)
		{
			SoundUpdateEvent.Send(packet);
		}
		if (flag2)
		{
			InputUpdateEvent.Send(packet2);
		}
		if (flag3)
		{
			DrivetrainUpdateEvent.Send(packet3);
		}
		for (int j = 0; j < vanillaVehicles.Count; j++)
		{
			for (int k = 0; k < vanillaVehicles[j].dashboardLevers.Count; k++)
			{
				vanillaVehicles[j].dashboardLevers[k].Update();
			}
		}
	}

	private void FixedUpdate()
	{
		for (int i = 0; i < vanillaVehicles.Count; i++)
		{
			for (int j = 0; j < vanillaVehicles[i].vehicleDoors.Count; j++)
			{
				vanillaVehicles[i].vehicleDoors[j].FixedUpdate();
			}
		}
	}

	private void OnPassengerMode(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		int index = packet.ReadInt();
		bool flag = packet.ReadBool();
		if (hash == flatbedHash)
		{
			NetManager.GetPlayerComponentById<NetPlayer>((CSteamID)sender).SetPassengerMode(flag, flatbed, applySitAnim: false);
			return;
		}
		NetVehicle netVehicle = vehicles.FirstOrDefault((NetVehicle x) => x.hash == hash);
		if (netVehicle == null)
		{
			Console.LogError($"Received passenger mode packet for unknown vehicle with hash {hash}");
			return;
		}
		netVehicle.seatsUsed[index] = (flag ? sender : 0uL);
		NetManager.GetPlayerComponentById<NetPlayer>((CSteamID)sender).SetPassengerMode(flag, netVehicle.transform);
	}

	private void OnDrivingMode(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		bool enter = packet.ReadBool();
		NetVehicle netVehicle = vehicles.FirstOrDefault((NetVehicle x) => x.hash == hash);
		if (netVehicle == null)
		{
			Console.LogError($"Received driving mode packet for unknown vehicle with hash {hash}");
		}
		else
		{
			netVehicle.DrivingMode(sender, enter);
		}
	}

	private void OnSoundUpdate(ulong sender, Packet packet)
	{
		int hash;
		while (true)
		{
			if (packet.UnreadLength() <= 0)
			{
				return;
			}
			if (packet.ReadByte() == 7)
			{
				hash = packet.ReadInt();
				NetVehicle netVehicle = vehicles.FirstOrDefault((NetVehicle x) => x.hash == hash);
				if (netVehicle == null)
				{
					break;
				}
				byte b = packet.ReadByte();
				while (true)
				{
					int index;
					bool? isPlaying;
					float? volume;
					float? pitch;
					float? time;
					switch (b)
					{
					case 15:
					{
						index = packet.ReadInt();
						isPlaying = null;
						volume = null;
						pitch = null;
						time = null;
						byte b2 = packet.ReadByte();
						if (b2 == 31)
						{
							isPlaying = packet.ReadBool();
							if (isPlaying.Value)
							{
								time = packet.ReadFloat();
							}
							b2 = packet.ReadByte();
						}
						if (b2 == 47)
						{
							volume = packet.ReadFloat();
							b2 = packet.ReadByte();
						}
						if (b2 == 63)
						{
							pitch = packet.ReadFloat();
							b2 = packet.ReadByte();
						}
						if (b2 == byte.MaxValue)
						{
							goto IL_00fc;
						}
						SoundUpdateReadError(1);
						return;
					}
					case 247:
						break;
					default:
						SoundUpdateReadError(2);
						return;
					}
					break;
					IL_00fc:
					b = packet.ReadByte();
					netVehicle.audioController.sources[index].OnUpdate(isPlaying, time, volume, pitch);
				}
				continue;
			}
			SoundUpdateReadError(0);
			return;
		}
		Console.LogError($"OnSoundUpdate can't find car with hash {hash}");
	}

	private void SoundUpdateReadError(int code)
	{
		Console.LogError("Error code " + code + " when reading OnSoundUpdate packet");
	}
}
