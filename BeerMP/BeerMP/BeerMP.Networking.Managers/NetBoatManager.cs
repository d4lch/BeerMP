using System;
using System.Linq;
using BeerMP.Helpers;
using BeerMP.Networking.PlayerManagers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetBoatManager : MonoBehaviour
{
	internal static NetBoatManager instance;

	internal SphereCollider itemCollider;

	internal Rigidbody boat;

	private Rigidbody[] allRBS;

	private ConfigurableJoint[] passengerModes;

	private NetEvent<NetBoatManager> drivingModeEvent;

	private NetEvent<NetBoatManager> passengerModeEvent;

	public ulong driver;

	public ulong owner = BeerMPGlobals.HostID;

	private void Start()
	{
		instance = this;
		ObjectsLoader.gameLoaded += (Action)delegate
		{
			boat = GameObject.Find("BOAT").GetComponent<Rigidbody>();
			allRBS = boat.GetComponentsInChildren<Rigidbody>(includeInactive: true);
			PlayMakerFSM[] componentsInChildren = boat.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
			RemoveDeath(componentsInChildren);
			DoItemCollider();
			PlayMakerFSM drivingMode = componentsInChildren.FirstOrDefault(delegate(PlayMakerFSM fsm)
			{
				if (!(fsm.FsmName != "PlayerTrigger") && !(fsm.gameObject.name != "DriveTrigger"))
				{
					fsm.Initialize();
					FsmState state = fsm.GetState("Press return");
					if (state == null)
					{
						return false;
					}
					if (!(state.Actions.FirstOrDefault((FsmStateAction a) => a is SetStringValue) is SetStringValue setStringValue))
					{
						return false;
					}
					return setStringValue.stringValue.Value.Contains("DRIVING");
				}
				return false;
			});
			if (drivingMode != null)
			{
				drivingMode.Initialize();
				drivingMode.InsertAction("Press return", delegate
				{
					if (driver != 0L)
					{
						drivingMode.SendEvent("FINISHED");
					}
				}, 0);
				drivingMode.InsertAction("Player in car", SendEnterDrivingMode);
				drivingMode.InsertAction("Create player", SendExitDrivingMode);
				Console.Log("Init driving mode for BOAT", show: false);
			}
			BoxCollider component = boat.transform.Find("GFX/Triggers/PlayerTrigger").GetComponent<BoxCollider>();
			component.center = Vector3.forward * -1.38f;
			component.size = new Vector3(0.8f, 0.4f, 3.7f);
			AddPassengerSeat(boat, boat.transform, new Vector3(0f, 0f, 0.1f), new Vector3(1f, 0f, 0f));
			AddPassengerSeat(boat, boat.transform, new Vector3(0f, 0f, -1.1f), new Vector3(1f, 0f, 0f));
			drivingModeEvent = NetEvent<NetBoatManager>.Register("DrivingMode", delegate(ulong s, Packet p)
			{
				bool enter2 = p.ReadBool();
				DrivingMode(s, enter2);
			});
			passengerModeEvent = NetEvent<NetBoatManager>.Register("PassengerMode", delegate(ulong s, Packet p)
			{
				bool enter = p.ReadBool();
				NetManager.GetPlayerComponentById<NetPlayer>((CSteamID)s).SetPassengerMode(enter, boat.transform, applySitAnim: false);
			});
		};
	}

	private void DoItemCollider()
	{
		GameObject obj = new GameObject("ItemCollider");
		obj.transform.parent = boat.transform;
		obj.transform.localPosition = default(Vector3);
		SphereCollider sphereCollider = obj.AddComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
		sphereCollider.radius = 3f;
		itemCollider = sphereCollider;
	}

	private void RemoveDeath(PlayMakerFSM[] fsms)
	{
		int num = 0;
		while (true)
		{
			if (num < fsms.Length)
			{
				if (fsms[num].FsmName == "Death" && fsms[num].gameObject.name == "DriverHeadPivot")
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		Transform obj = fsms[num].transform;
		obj.GetComponent<Rigidbody>().isKinematic = true;
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localEulerAngles = Vector3.zero;
		UnityEngine.Object.Destroy(fsms[num]);
		UnityEngine.Object.Destroy(obj.parent.GetComponentInChildren<ConfigurableJoint>());
		Console.Log("Successfully removed death fsm from driving mode of " + base.transform.name, show: false);
	}

	public void SendEnterDrivingMode()
	{
		using Packet packet = new Packet(1);
		packet.Write(_value: true);
		driver = (owner = BeerMPGlobals.UserID);
		LocalNetPlayer.Instance.inCar = true;
		drivingModeEvent.Send(packet);
		NetRigidbodyManager.RequestOwnership(boat);
		for (int i = 0; i < allRBS.Length; i++)
		{
			NetRigidbodyManager.RequestOwnership(allRBS[i]);
		}
	}

	public void SendExitDrivingMode()
	{
		using Packet packet = new Packet(1);
		packet.Write(_value: false);
		driver = 0uL;
		LocalNetPlayer.Instance.inCar = false;
		drivingModeEvent.Send(packet);
	}

	internal void DrivingMode(ulong player, bool enter)
	{
		driver = (enter ? player : 0uL);
		if (enter)
		{
			owner = player;
		}
		NetManager.GetPlayerComponentById<NetPlayer>((CSteamID)player).SetPassengerMode(enter, boat.transform, applySitAnim: false);
	}

	private void AddPassengerSeat(Rigidbody rb, Transform parent, Vector3 triggerOffset, Vector3 headPivotOffset)
	{
		GameObject obj = UnityEngine.Object.Instantiate(GameObject.Find("NPC_CARS").transform.Find("Amikset/KYLAJANI/LOD/PlayerFunctions").gameObject);
		obj.name = $"MPPlayerFunctions_{0}";
		Transform obj2 = obj.transform.Find("DriverHeadPivot");
		obj2.GetComponent<Rigidbody>().isKinematic = true;
		obj2.transform.localPosition = Vector3.zero;
		UnityEngine.Object.Destroy(obj2.GetPlayMaker("Death"));
		Transform child = obj.transform.GetChild(1);
		child.gameObject.SetActive(value: false);
		child.transform.localPosition = headPivotOffset;
		UnityEngine.Object.Destroy(child.GetComponent<ConfigurableJoint>());
		child.gameObject.SetActive(value: true);
		UnityEngine.Object.Destroy(obj.transform.GetChild(0).gameObject);
		obj.transform.SetParent(parent, worldPositionStays: false);
		obj.transform.Find("PlayerTrigger/DriveTrigger").localPosition = triggerOffset;
		Transform obj3 = obj.transform.Find("PlayerTrigger");
		obj3.localPosition = Vector3.zero;
		UnityEngine.Object.Destroy(obj3.GetComponent<PlayMakerFSM>());
		UnityEngine.Object.Destroy(obj3.GetComponent<BoxCollider>());
		PlayMakerFSM component = obj.transform.Find("PlayerTrigger/DriveTrigger").GetComponent<PlayMakerFSM>();
		component.name = "PassengerTrigger";
		component.transform.parent.name = "PlayerOffset";
		component.Initialize();
		component.GetComponent<CapsuleCollider>().radius = 0.2f;
		component.InsertAction("Reset view", delegate
		{
			using Packet packet2 = new Packet(1);
			packet2.Write(_value: true);
			LocalNetPlayer.Instance.inCar = true;
			passengerModeEvent.Send(packet2);
		});
		component.InsertAction("Create player", delegate
		{
			using Packet packet = new Packet(1);
			packet.Write(_value: false);
			LocalNetPlayer.Instance.inCar = false;
			passengerModeEvent.Send(packet);
		});
		(component.FsmStates.First((FsmState s) => s.Name == "Check speed").Actions[0] as GetVelocity).gameObject = new FsmOwnerDefault
		{
			GameObject = rb.transform.gameObject,
			OwnerOption = OwnerDefaultOption.SpecifyGameObject
		};
		(component.FsmStates.First((FsmState s) => s.Name == "Player in car").Actions[3] as SetStringValue).stringValue = "Passenger_" + rb.transform.name;
		obj.SetActive(value: true);
		obj.transform.GetChild(1).gameObject.SetActive(value: true);
		component.gameObject.SetActive(value: true);
		_ = obj.transform;
	}

	private void Update()
	{
	}
}
