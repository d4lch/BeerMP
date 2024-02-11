using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetItemsManager : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass37_1
	{
		public FsmGameObject newWood;

		internal void _003CStart_003Eb__3()
		{
			Rigidbody component = newWood.Value.GetComponent<Rigidbody>();
			NetRigidbodyManager.AddRigidbody(component, (component.transform.GetGameobjectHashString() + NetJobManager.logsCount++).GetHashCode());
		}
	}

	private static FsmBool spannerSetOpen;

	private static FsmBool ratchetSetOpen;

	private static FsmBool gasolineCanOpen;

	private static FsmBool dieselCanOpen;

	private static FsmBool gasolineCanClose;

	private static FsmBool dieselCanClose;

	private static PlayMakerFSM spannerSetTop;

	private static PlayMakerFSM ratchetSetTop;

	private static PlayMakerFSM camshaftGear;

	private static PlayMakerFSM dieselCanLid;

	private static PlayMakerFSM gasolineCanLid;

	private static PlayMakerFSM distributorHandRotate;

	private static PlayMakerFSM alternatorHandRotate;

	private static PlayMakerFSM trailerDetach;

	private static List<NetCreateItemsManager.Item> beercases = new List<NetCreateItemsManager.Item>();

	private static FsmFloat camshaftGearAngle;

	private static FsmFloat camshaftGearRotateAmount;

	private static FsmFloat gasolineCanFluid;

	private static FsmFloat dieselCanFluid;

	private static FsmFloat distributorRotation;

	private static FsmFloat alternatorRotation;

	private static Transform camshaftGearMesh;

	private static Transform distributorRotationPivot;

	private static Dictionary<int, PlayMakerFSM> woodCarriers;

	private List<PlayMakerFSM> updatingFsms = new List<PlayMakerFSM>();

	public float jerryCanSyncTime = 31f;

	public float carFluidsSyncTime = 32f;

	internal const string spannerSetOpenEvent = "SpannerTop";

	internal const string camshaftGearEvent = "CamGear";

	internal const string beercaseDrinkEvent = "BeercaseD";

	internal const string jerryCanSyncEvent = "JerryCan";

	internal const string jerryCanLidEvent = "JCanLid";

	internal const string distributorRotateEvent = "Distributor";

	internal const string alternatorRotateEvent = "AlternatorTune";

	internal const string carFluidsSync = "CarFluids";

	internal const string trailerDetachEvent = "TrailerDetach";

	internal const string woodCarrierEvent = "WoodCar";

	private void Start()
	{
		Action action = delegate
		{
			NetEvent<NetItemsManager>.Register("SpannerTop", OnSpannerSetOpen);
			NetEvent<NetItemsManager>.Register("CamGear", OnCamshaftGearAdjust);
			NetEvent<NetItemsManager>.Register("BeercaseD", OnBeercaseSubtractBottle);
			NetEvent<NetItemsManager>.Register("JerryCan", OnJerryCanSync);
			NetEvent<NetItemsManager>.Register("JCanLid", OnJerryCanLid);
			NetEvent<NetItemsManager>.Register("Distributor", OnDistributorHandRotate);
			NetEvent<NetItemsManager>.Register("AlternatorTune", OnAlternatorHandRotate);
			NetEvent<NetItemsManager>.Register("CarFluids", FsmNetVehicle.OnCarFluidsAndFields);
			NetEvent<NetItemsManager>.Register("TrailerDetach", OnTrailerDetach);
			NetEvent<NetItemsManager>.Register("WoodCar", OnWoodCarrierSpawnLog);
			Transform obj = GameObject.Find("ITEMS").transform;
			spannerSetTop = obj.Find("spanner set(itemx)/Pivot/top").GetComponent<PlayMakerFSM>();
			InjectSpannerSetTop(spannerSetTop, delegate
			{
				SpannerSetOpen(isRatchet: false);
			}, out spannerSetOpen);
			ratchetSetTop = GetDatabaseObject("Database/DatabaseOrders/Ratchet Set").transform.Find("Hinge/Pivot/top").GetComponent<PlayMakerFSM>();
			InjectSpannerSetTop(ratchetSetTop, delegate
			{
				SpannerSetOpen(isRatchet: true);
			}, out ratchetSetOpen);
			camshaftGear = GetDatabaseObject("Database/DatabaseMotor/CamshaftGear").GetPlayMaker("BoltCheck");
			camshaftGearMesh = camshaftGear.transform.Find("camshaft_gear_mesh");
			camshaftGearAngle = camshaftGear.FsmVariables.FindFsmFloat("Angle");
			camshaftGearRotateAmount = camshaftGear.FsmVariables.FindFsmFloat("RotateAmount");
			Transform transform = obj.Find("gasoline(itemx)");
			gasolineCanFluid = transform.Find("FluidTrigger").GetPlayMaker("Data").FsmVariables.FindFsmFloat("Fluid");
			SetupJerryCanLidFsm(transform.Find("Open"), isDiesel: false);
			Transform transform2 = obj.Find("diesel(itemx)");
			dieselCanFluid = transform2.Find("FluidTrigger").GetPlayMaker("Data").FsmVariables.FindFsmFloat("Fluid");
			SetupJerryCanLidFsm(transform2.Find("Open"), isDiesel: true);
			distributorHandRotate = GetDatabaseObject("Database/DatabaseMotor/Distributor").GetPlayMaker("HandRotate");
			distributorRotation = distributorHandRotate.FsmVariables.FindFsmFloat("Rotation");
			distributorRotationPivot = distributorHandRotate.transform.Find("Pivot");
			distributorHandRotate.InsertAction("Wait", DistributorHandRotate, 0);
			alternatorHandRotate = GetDatabaseObject("Database/DatabaseMotor/Alternator").transform.Find("Pivot").GetPlayMaker("HandRotate");
			alternatorRotation = alternatorHandRotate.FsmVariables.FindFsmFloat("Rotation");
			alternatorHandRotate.InsertAction("Wait", AlternatorHandRotate, 0);
			trailerDetach = GameObject.Find("KEKMET(350-400psi)").transform.Find("Trailer/Remove").GetComponent<PlayMakerFSM>();
			FsmEvent fsmEvent = trailerDetach.AddEvent("MP_DETACH");
			trailerDetach.AddGlobalTransition(fsmEvent, "Close door");
			trailerDetach.InsertAction("Close door", SendTrailerDetached, 0);
			for (int i = 0; i < ObjectsLoader.ObjectsInGame.Length; i++)
			{
				GameObject gameObject = ObjectsLoader.ObjectsInGame[i];
				int hash = gameObject.transform.GetGameobjectHashString().GetHashCode();
				if (gameObject.name == "wood carrier(itemx)")
				{
					new _003C_003Ec__DisplayClass37_1();
					gameObject.SetActive(value: false);
				}
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

	private void OnWoodCarrierSpawnLog(ulong sender, Packet packet)
	{
		int key = packet.ReadInt();
		if (woodCarriers.ContainsKey(key))
		{
			woodCarriers[key].SendEvent("PICKWOOD");
		}
	}

	private void SendTrailerDetached()
	{
		using Packet packet = new Packet(1);
		NetEvent<NetItemsManager>.Send("TrailerDetach", packet);
	}

	private void OnTrailerDetach(ulong sender, Packet packet)
	{
		trailerDetach.Fsm.Event(trailerDetach.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "MP_DETACH"));
	}

	public static GameObject GetDatabaseObject(string databasePath)
	{
		GameObject gameObject = GameObject.Find(databasePath);
		if (gameObject == null)
		{
			Console.Log("Database '" + databasePath + "' could not be found");
			return null;
		}
		PlayMakerFSM component = gameObject.GetComponent<PlayMakerFSM>();
		if (component == null)
		{
			Console.Log("Database '" + databasePath + "' doesn't have an fsm");
			return null;
		}
		FsmGameObject fsmGameObject = component.FsmVariables.FindFsmGameObject("ThisPart");
		if (fsmGameObject == null)
		{
			Console.Log("Database '" + databasePath + "' doesn't have a this part variable");
			return null;
		}
		return fsmGameObject.Value;
	}

	private void Update()
	{
		DoRegularSync(ref jerryCanSyncTime, SyncJerryCans);
		DoRegularSync(ref carFluidsSyncTime, FsmNetVehicle.SendCarFluidsAndFields);
	}

	private void DoRegularSync(ref float time, Action doSync, float resetTime = 30f, bool onlyHost = true)
	{
		if (!onlyHost || BeerMPGlobals.IsHost)
		{
			time -= Time.deltaTime;
			if (time < 0f)
			{
				time = resetTime;
				doSync();
			}
		}
	}

	private void SpannerSetOpen(bool isRatchet)
	{
		using Packet packet = new Packet(1);
		packet.Write(isRatchet);
		packet.Write(isRatchet ? ratchetSetOpen.Value : spannerSetOpen.Value);
		NetEvent<NetItemsManager>.Send("SpannerTop", packet);
	}

	private void OnSpannerSetOpen(ulong sender, Packet packet)
	{
		bool flag;
		if (sender != BeerMPGlobals.UserID && ((flag = packet.ReadBool()) ? ratchetSetOpen : spannerSetOpen).Value == packet.ReadBool())
		{
			(flag ? ratchetSetTop : spannerSetTop).SendEvent("MP_TOGGLE");
		}
	}

	private void InjectSpannerSetTop(PlayMakerFSM fsm, Action topToggled, out FsmBool isOpen)
	{
		FsmEvent fsmEvent = fsm.AddEvent("MP_TOGGLE");
		fsm.AddGlobalTransition(fsmEvent, "Bool test");
		isOpen = fsm.FsmVariables.FindFsmBool("Open");
		fsm.InsertAction("Bool test", topToggled, 0);
	}

	internal static void CamshaftGearAdjustEvent()
	{
		using Packet packet = new Packet(1);
		packet.Write(camshaftGearAngle.Value);
		NetEvent<NetItemsManager>.Send("CamGear", packet);
	}

	private static void OnCamshaftGearAdjust(ulong sender, Packet p)
	{
		float num = p.ReadFloat();
		camshaftGearAngle.Value = num - camshaftGearRotateAmount.Value;
		camshaftGearMesh.localEulerAngles = Vector3.right * camshaftGearAngle.Value;
		camshaftGear.SendEvent("ADJUST");
	}

	public static void SetupBeercaseFSM(PlayMakerFSM fsm, NetCreateItemsManager.Item item)
	{
		int hash = item.ID.GetHashCode();
		beercases.Add(item);
		fsm.InsertAction("Remove bottle", delegate
		{
			BeercaseSubtractBottleEvent(hash);
		}, 0);
		fsm.InsertAction("NPC Drink", delegate
		{
			BeercaseSubtractBottleEvent(hash);
		}, 0);
	}

	private static void BeercaseSubtractBottleEvent(int hash)
	{
		using Packet packet = new Packet(1);
		packet.Write(hash);
		NetEvent<NetItemsManager>.Send("BeercaseD", packet);
	}

	private void OnBeercaseSubtractBottle(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		NetCreateItemsManager.Item item = beercases.FirstOrDefault((NetCreateItemsManager.Item i) => i.ID.GetHashCode() == hash);
		if (item == null)
		{
			Console.LogError($"Beercase of hash {hash} does not exist");
		}
		else
		{
			item.fsm.SendEvent("SUSKI");
		}
	}

	private void SyncJerryCans()
	{
		using Packet packet = new Packet(1);
		packet.Write(gasolineCanFluid.Value);
		packet.Write(dieselCanFluid.Value);
		NetEvent<NetItemsManager>.Send("JerryCan", packet);
	}

	private void OnJerryCanSync(ulong sender, Packet packet)
	{
		gasolineCanFluid.Value = packet.ReadFloat();
		dieselCanFluid.Value = packet.ReadFloat();
	}

	private void SetupJerryCanLidFsm(Transform lid, bool isDiesel)
	{
		PlayMakerFSM playMaker = lid.GetPlayMaker("Use");
		playMaker.Initialize();
		if (isDiesel)
		{
			dieselCanLid = playMaker;
		}
		else
		{
			gasolineCanLid = playMaker;
		}
		FsmBool fsmBool = playMaker.FsmVariables.FindFsmBool("Open");
		if (isDiesel)
		{
			dieselCanOpen = fsmBool;
		}
		else
		{
			gasolineCanOpen = fsmBool;
		}
		FsmBool fsmBool2 = playMaker.FsmVariables.FindFsmBool("Closed");
		if (isDiesel)
		{
			dieselCanClose = fsmBool2;
		}
		else
		{
			gasolineCanClose = fsmBool2;
		}
		playMaker.InsertAction("State 2", delegate
		{
			JerryCanLidToggle(isDiesel);
		}, 0);
		FsmEvent fsmEvent = playMaker.AddEvent("MP_TOGGLE");
		playMaker.AddGlobalTransition(fsmEvent, "State 2");
		playMaker.Initialize();
	}

	private void JerryCanLidToggle(bool isDiesel)
	{
		PlayMakerFSM item = (isDiesel ? dieselCanLid : gasolineCanLid);
		if (updatingFsms.Contains(item))
		{
			updatingFsms.Remove(item);
			return;
		}
		using Packet packet = new Packet(1);
		packet.Write(isDiesel);
		packet.Write(!(isDiesel ? dieselCanOpen : gasolineCanOpen).Value);
		NetEvent<NetItemsManager>.Send("JCanLid", packet);
	}

	private void OnJerryCanLid(ulong sender, Packet packet)
	{
		bool num = packet.ReadBool();
		bool flag = packet.ReadBool();
		(num ? dieselCanOpen : gasolineCanOpen).Value = !flag;
		(num ? dieselCanClose : gasolineCanClose).Value = flag;
		PlayMakerFSM playMakerFSM = (num ? dieselCanLid : gasolineCanLid);
		updatingFsms.Add(playMakerFSM);
		playMakerFSM.SendEvent("MP_TOGGLE");
	}

	private void DistributorHandRotate()
	{
		using Packet packet = new Packet(1);
		packet.Write(distributorRotationPivot.localEulerAngles.z);
		NetEvent<NetItemsManager>.Send("Distributor", packet);
	}

	private void OnDistributorHandRotate(ulong sender, Packet p)
	{
		float num = p.ReadFloat();
		distributorRotationPivot.localEulerAngles = Vector3.forward * num;
		distributorRotation.Value = num;
	}

	private void AlternatorHandRotate()
	{
		using Packet packet = new Packet(1);
		packet.Write(alternatorHandRotate.transform.localEulerAngles.x);
		NetEvent<NetItemsManager>.Send("AlternatorTune", packet);
	}

	private void OnAlternatorHandRotate(ulong sender, Packet p)
	{
		float num = p.ReadFloat();
		alternatorHandRotate.transform.localEulerAngles = Vector3.right * num;
		alternatorRotation.Value = num;
	}
}
