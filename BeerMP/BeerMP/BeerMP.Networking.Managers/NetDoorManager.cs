using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(20000)]
internal class NetDoorManager : MonoBehaviour
{
	public class Door
	{
		public Vector3 doorPos;

		public PlayMakerFSM fsm;

		public FsmBool doorOpen;

		public FsmEvent fsmEvent;

		internal bool doSync = true;

		public static void Create(GameObject doorObject)
		{
			Transform transform = doorObject.transform.Find("Pivot/Handle");
			if (!transform)
			{
				transform = doorObject.transform.Find("Handle");
			}
			if (!transform)
			{
				transform = doorObject.transform;
			}
			if (transform.name != "Handle")
			{
				return;
			}
			Rigidbody[] componentsInChildren = doorObject.GetComponentsInChildren<Rigidbody>();
			if (componentsInChildren.Length != 0)
			{
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					UnityEngine.Object.Destroy(componentsInChildren[i]);
				}
			}
			PlayMakerFSM playMaker = transform.gameObject.GetPlayMaker("Use");
			if (!(playMaker == null))
			{
				playMaker.Initialize();
				if (playMaker.FsmEvents.Any((FsmEvent x) => x.Name == "OPENDOOR"))
				{
					Door door = new Door();
					FsmEvent fsmEvent = playMaker.AddEvent("MP_TOGGLEDOOR");
					playMaker.AddGlobalTransition(fsmEvent, "Check position");
					DoorAction doorAction = new DoorAction();
					doorAction.door = door;
					playMaker.InsertAction("Check position", doorAction, 0);
					Instance.doors.Add(door);
					door.doorPos = doorObject.transform.position;
					door.fsm = playMaker;
					door.fsmEvent = fsmEvent;
					door.doorOpen = playMaker.FsmVariables.GetFsmBool("DoorOpen");
				}
			}
		}

		public void Toggle(bool open = false)
		{
			if (doorOpen.Value == open)
			{
				doSync = false;
				fsm.Fsm.Event(fsmEvent);
			}
		}

		public void SyncStateInitial(ulong userId)
		{
			using Packet packet = new Packet(1);
			packet.Write(doorPos);
			packet.Write(!doorOpen.Value);
			NetEvent<NetDoorManager>.Send("ToggleDoor", packet, userId);
		}
	}

	public class DoorAction : FsmStateAction
	{
		public Door door;

		public override void OnEnter()
		{
			if (door.doSync)
			{
				using Packet packet = new Packet(1);
				packet.Write(door.doorPos);
				packet.Write(door.doorOpen.Value);
				NetEvent<NetDoorManager>.Send("ToggleDoor", packet);
			}
			door.doSync = true;
			Finish();
		}
	}

	public static NetDoorManager Instance;

	public List<Door> doors = new List<Door>();

	private void Start()
	{
		Instance = this;
		GameObject[] array = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
			where x.name.StartsWith("Door")
			select x).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			Door.Create(array[i]);
		}
		Door.Create(GameObject.Find("YARD").transform.Find("Building/KITCHEN/Fridge/Pivot/Handle").gameObject);
		NetEvent<NetDoorManager>.Register("ToggleDoor", OnToggleDoor);
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong sender)
		{
			if (BeerMPGlobals.IsHost)
			{
				foreach (Door door in doors)
				{
					door.SyncStateInitial(sender);
				}
			}
		};
	}

	private void OnToggleDoor(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			Vector3 doorPos = packet.ReadVector3();
			bool open = packet.ReadBool();
			doors.First((Door x) => x.doorPos == doorPos).Toggle(open);
		}
	}
}
