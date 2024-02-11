using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BeerMP.Networking.Managers;

internal class FsmVehicleDoor
{
	private PlayMakerFSM fsm;

	private FsmBool doorOpen;

	private FsmGameObject door;

	private int hash;

	private int axis = -1;

	private ulong owner;

	private bool updatingFsm;

	private const string openDoorFsmEvent = "MP_OPEN";

	private const string closeDoorFsmEvent = "MP_CLOSE";

	private static NetEvent<FsmVehicleDoor> doorRotationEvent;

	private static NetEvent<FsmVehicleDoor> doorToggleEvent;

	private static NetEvent<FsmVehicleDoor> requestOwnershipEvent;

	private static NetEvent<FsmVehicleDoor> initSync;

	private static List<FsmVehicleDoor> doors = new List<FsmVehicleDoor>();

	public FsmVehicleDoor(PlayMakerFSM fsm, bool isHayosikoSidedoor = false)
	{
		this.fsm = fsm;
		hash = fsm.transform.GetGameobjectHashString().GetHashCode();
		owner = BeerMPGlobals.HostID;
		fsm.Initialize();
		if (isHayosikoSidedoor)
		{
			SetupFSMSidedoor();
		}
		else
		{
			SetupFSM();
		}
		if (doorRotationEvent == null)
		{
			doorRotationEvent = NetEvent<FsmVehicleDoor>.Register("Rot", OnDoorRotation);
		}
		if (doorToggleEvent == null)
		{
			doorToggleEvent = NetEvent<FsmVehicleDoor>.Register("Toggle", OnDoorToggle);
		}
		if (requestOwnershipEvent == null)
		{
			requestOwnershipEvent = NetEvent<FsmVehicleDoor>.Register("SetOwner", OnSetOwner);
		}
		if (initSync == null)
		{
			int hash;
			initSync = NetEvent<FsmVehicleDoor>.Register("Init", delegate(ulong s, Packet p)
			{
				while (true)
				{
					if (p.UnreadLength() <= 0)
					{
						return;
					}
					hash = p.ReadInt();
					bool flag = p.ReadBool();
					FsmVehicleDoor fsmVehicleDoor = doors.FirstOrDefault((FsmVehicleDoor d) => d.hash == hash);
					if (fsmVehicleDoor == null)
					{
						break;
					}
					fsmVehicleDoor.owner = s;
					if (flag)
					{
						fsmVehicleDoor.updatingFsm = true;
						fsmVehicleDoor.fsm.SendEvent("MP_OPEN");
					}
				}
				Console.LogError($"Failed to init sync fsm car door: the hash {hash} was not found");
			});
			BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
			{
				using Packet packet = new Packet(0);
				for (int i = 0; i < doors.Count; i++)
				{
					if (doors[i] == null)
					{
						doors.RemoveAt(i);
						i--;
					}
					else if (doors[i].owner == BeerMPGlobals.UserID && doors[i].doorOpen != null)
					{
						packet.Write(doors[i].hash);
						packet.Write(doors[i].doorOpen.Value);
					}
				}
				initSync.Send(packet, user);
			};
		}
		doors.Add(this);
		NetManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(NetManager.sceneLoaded, (Action<GameScene>)delegate
		{
			if (doors.Contains(this))
			{
				doors.Remove(this);
			}
		});
	}

	private static void OnDoorRotation(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		FsmVehicleDoor fsmVehicleDoor = doors.FirstOrDefault((FsmVehicleDoor d) => d.hash == hash);
		if (fsmVehicleDoor == null)
		{
			Console.LogError($"Failed to rotate fsm car door: the hash {hash} was not found");
		}
		else if (fsmVehicleDoor.doorOpen.Value)
		{
			float value = packet.ReadFloat();
			Vector3 localEulerAngles = fsmVehicleDoor.door.Value.transform.localEulerAngles;
			localEulerAngles[fsmVehicleDoor.axis] = value;
			fsmVehicleDoor.door.Value.transform.localEulerAngles = localEulerAngles;
		}
	}

	private static void OnDoorToggle(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		FsmVehicleDoor fsmVehicleDoor = doors.FirstOrDefault((FsmVehicleDoor d) => d.hash == hash);
		if (fsmVehicleDoor == null)
		{
			Console.LogError($"Failed to toggle fsm car door: the hash {hash} was not found");
			return;
		}
		fsmVehicleDoor.updatingFsm = true;
		bool flag = packet.ReadBool();
		fsmVehicleDoor.fsm.SendEvent(flag ? "MP_OPEN" : "MP_CLOSE");
	}

	private static void OnSetOwner(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		FsmVehicleDoor fsmVehicleDoor = doors.FirstOrDefault((FsmVehicleDoor d) => d.hash == hash);
		if (fsmVehicleDoor == null)
		{
			Console.LogError($"Failed to set fsm car door ownership: the hash {hash} was not found");
		}
		else
		{
			fsmVehicleDoor.owner = sender;
		}
	}

	public void FixedUpdate()
	{
		if (doorOpen != null && owner == BeerMPGlobals.UserID && doorOpen.Value)
		{
			using (Packet packet = new Packet(1))
			{
				packet.Write(hash);
				packet.Write(door.Value.transform.localEulerAngles[axis]);
				doorRotationEvent.Send(packet);
			}
		}
	}

	private void SetupFSMSidedoor()
	{
		string stateName = "Open door";
		string stateName2 = "Close door";
		FsmEvent fsmEvent = fsm.AddEvent("MP_OPEN");
		fsm.AddGlobalTransition(fsmEvent, stateName);
		FsmEvent fsmEvent2 = fsm.AddEvent("MP_CLOSE");
		fsm.AddGlobalTransition(fsmEvent2, stateName2);
		fsm.InsertAction(stateName, OpenDoor, 0);
		fsm.InsertAction(stateName2, CloseDoor, 0);
	}

	private void SetupFSM()
	{
		try
		{
			bool flag = false;
			doorOpen = fsm.FsmVariables.FindFsmBool("Open");
			door = fsm.FsmVariables.FindFsmGameObject("Door");
			if (door == null)
			{
				flag = true;
				door = fsm.FsmVariables.FindFsmGameObject("Bootlid");
				if (door == null)
				{
					door = fsm.gameObject;
					flag = false;
				}
			}
			string stateName = (flag ? "Open hood" : "Open door");
			string stateName2 = ((!flag) ? "Sound" : (fsm.HasState("Drop") ? "Drop" : "State 2"));
			GetRotation getRotation = fsm.GetState(stateName).Actions.First((FsmStateAction a) => a is GetRotation) as GetRotation;
			if (getRotation.xAngle != null && !getRotation.xAngle.IsNone)
			{
				axis = 0;
			}
			else if (getRotation.yAngle != null && !getRotation.yAngle.IsNone)
			{
				axis = 1;
			}
			else if (getRotation.zAngle != null && !getRotation.zAngle.IsNone)
			{
				axis = 2;
			}
			FsmEvent fsmEvent = fsm.AddEvent("MP_OPEN");
			fsm.AddGlobalTransition(fsmEvent, stateName);
			FsmEvent fsmEvent2 = fsm.AddEvent("MP_CLOSE");
			fsm.AddGlobalTransition(fsmEvent2, stateName2);
			fsm.InsertAction(stateName, OpenDoor, 0);
			if (!flag)
			{
				fsm.InsertAction(fsm.HasState("Open door 2") ? "Open door 2" : "Open door 3", RequestOwnership, 0);
			}
			fsm.InsertAction(stateName2, CloseDoor, 0);
		}
		catch (Exception ex)
		{
			Console.LogError($"Failed to setup door {hash} ({fsm.transform.GetGameobjectHashString()}): {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
		}
	}

	private void RequestOwnership()
	{
		if (BeerMPGlobals.UserID == owner)
		{
			return;
		}
		owner = BeerMPGlobals.UserID;
		using Packet packet = new Packet(1);
		packet.Write(hash);
		requestOwnershipEvent.Send(packet);
	}

	private void CloseDoor()
	{
		if (updatingFsm)
		{
			updatingFsm = false;
			return;
		}
		RequestOwnership();
		SendDoorToggleEvent(open: false);
	}

	private void OpenDoor()
	{
		if (updatingFsm)
		{
			updatingFsm = false;
			return;
		}
		RequestOwnership();
		SendDoorToggleEvent(open: true);
	}

	private void SendDoorToggleEvent(bool open)
	{
		using Packet packet = new Packet(1);
		packet.Write(hash);
		packet.Write(open);
		doorToggleEvent.Send(packet);
	}
}
