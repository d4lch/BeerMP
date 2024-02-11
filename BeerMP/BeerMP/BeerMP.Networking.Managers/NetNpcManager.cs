using System;
using System.Collections.Generic;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetNpcManager : MonoBehaviour
{
	private static Dictionary<int, Transform> walkers = new Dictionary<int, Transform>();

	private GameObject vehiclesHighway;

	private PlayMakerFSM policeFsm;

	private PlayMakerFSM drunkHiker2Fsm;

	private FsmEvent[] policeEvents;

	private FsmEvent[] drunkHiker2CarEvents;

	private bool highwayOn;

	private int policeIndex = -1;

	private int receivedDrunkHiker2 = -1;

	private static readonly string[] jokkeHiker_eventNames = new string[4] { "MP_SATSUMA", "MP_MUSCLE", "MP_VAN", "MP_RUSCKO" };

	private static readonly string[] jokkeHiker_stateNames = new string[4] { "Satsuma", "Muscle", "Van", "Ruscko" };

	private NetEvent<NetNpcManager> walkerSync;

	private NetEvent<NetNpcManager> highwayUpdateEvent;

	private NetEvent<NetNpcManager> policeUpdateEvent;

	private NetEvent<NetNpcManager> drunkHiker2EnterCar;

	private void Start()
	{
		ObjectsLoader.gameLoaded += (Action)delegate
		{
			Transform transform = GameObject.Find("HUMANS/Randomizer/Walkers").transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				Transform child = transform.GetChild(i);
				int hashCode = child.GetGameobjectHashString().GetHashCode();
				walkers.Add(hashCode, child);
			}
			Transform transform2 = GameObject.Find("KILJUGUY").transform.Find("HikerPivot");
			walkers.Add(transform2.GetGameobjectHashString().GetHashCode(), transform2);
			drunkHiker2Fsm = transform2.Find("JokkeHiker2").GetPlayMaker("Logic");
			drunkHiker2Fsm.Initialize();
			drunkHiker2CarEvents = new FsmEvent[jokkeHiker_eventNames.Length];
			for (int j = 0; j < jokkeHiker_eventNames.Length; j++)
			{
				drunkHiker2CarEvents[j] = drunkHiker2Fsm.AddEvent(jokkeHiker_eventNames[j]);
				drunkHiker2Fsm.AddGlobalTransition(drunkHiker2CarEvents[j], jokkeHiker_stateNames[j]);
				int _i = j;
				drunkHiker2Fsm.InsertAction(jokkeHiker_stateNames[j], delegate
				{
					JokkeEnterCar(_i);
				}, 0);
			}
			drunkHiker2EnterCar = NetEvent<NetNpcManager>.Register("DH2Car", OnJokkeEnterCar);
			Transform transform3 = GameObject.Find("TRAFFIC").transform;
			vehiclesHighway = transform3.Find("VehiclesHighway").gameObject;
			policeFsm = transform3.Find("Police").GetComponent<PlayMakerFSM>();
			if (BeerMPGlobals.IsHost)
			{
				policeFsm.InsertAction("State 3", delegate
				{
					PoliceSpawn(0);
				}, 0);
				policeFsm.InsertAction("State 4", delegate
				{
					PoliceSpawn(1);
				}, 0);
				policeFsm.InsertAction("State 5", delegate
				{
					PoliceSpawn(2);
				}, 0);
				policeFsm.InsertAction("State 6", delegate
				{
					PoliceSpawn(3);
				}, 0);
			}
			else
			{
				policeEvents = new FsmEvent[4]
				{
					policeFsm.AddEvent("MP_SPAWN0"),
					policeFsm.AddEvent("MP_SPAWN1"),
					policeFsm.AddEvent("MP_SPAWN2"),
					policeFsm.AddEvent("MP_SPAWN3")
				};
				policeFsm.AddGlobalTransition(policeEvents[0], "State 3");
				policeFsm.AddGlobalTransition(policeEvents[1], "State 4");
				policeFsm.AddGlobalTransition(policeEvents[2], "State 5");
				policeFsm.AddGlobalTransition(policeEvents[3], "State 6");
				policeFsm.GetState("Cop1").Actions[2].Enabled = false;
				policeFsm.GetState("State 1").Actions[0].Enabled = false;
			}
			walkerSync = NetEvent<NetNpcManager>.Register("Walk", OnWalkerNPCSync);
			highwayUpdateEvent = NetEvent<NetNpcManager>.Register("HighwayUpdate", OnHighwayUpdate);
			policeUpdateEvent = NetEvent<NetNpcManager>.Register("PoliceUpdate", OnPoliceUpdate);
		};
	}

	private void OnJokkeEnterCar(ulong s, Packet p)
	{
		int num = (receivedDrunkHiker2 = p.ReadByte());
		drunkHiker2Fsm.Fsm.Event(drunkHiker2CarEvents[num]);
	}

	private void JokkeEnterCar(int index)
	{
		if (receivedDrunkHiker2 != -1)
		{
			receivedDrunkHiker2 = -1;
			return;
		}
		using Packet packet = new Packet(1);
		packet.Write((byte)index);
		drunkHiker2EnterCar.Send(packet);
	}

	private void OnPoliceUpdate(ulong s, Packet p)
	{
		int num = p.ReadByte();
		policeFsm.Fsm.Event(policeEvents[num]);
	}

	private void PoliceSpawn(int index)
	{
		using Packet packet = new Packet(1);
		packet.Write((byte)index);
		policeUpdateEvent.Send(packet);
	}

	private void OnHighwayUpdate(ulong s, Packet p)
	{
		bool flag = p.ReadBool();
		highwayOn = flag;
	}

	private void CheckHighway()
	{
		if (BeerMPGlobals.IsHost)
		{
			if (highwayOn != vehiclesHighway.activeSelf)
			{
				highwayOn = vehiclesHighway.activeSelf;
				using Packet packet = new Packet(1);
				packet.Write(highwayOn);
				highwayUpdateEvent.Send(packet);
			}
		}
		else if (highwayOn != vehiclesHighway.activeSelf)
		{
			vehiclesHighway.SetActive(highwayOn);
		}
	}

	private void OnWalkerNPCSync(ulong s, Packet p)
	{
		while (p.UnreadLength() > 0)
		{
			int key = p.ReadInt();
			Vector3 position = p.ReadVector3();
			Vector3 eulerAngles = p.ReadVector3();
			if (walkers.ContainsKey(key))
			{
				walkers[key].position = position;
				walkers[key].eulerAngles = eulerAngles;
			}
		}
	}

	private void UpdateWalkers()
	{
		if (!BeerMPGlobals.IsHost)
		{
			return;
		}
		using Packet packet = new Packet(1);
		foreach (KeyValuePair<int, Transform> walker in walkers)
		{
			packet.Write(walker.Key);
			packet.Write(walker.Value.position);
			packet.Write(walker.Value.eulerAngles);
		}
		walkerSync.Send(packet);
	}

	private void Update()
	{
		UpdateWalkers();
		CheckHighway();
	}
}
