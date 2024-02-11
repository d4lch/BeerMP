using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using BeerMP.Networking.PlayerManagers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;

namespace BeerMP.Networking.Managers;

public class NetVehicle
{
	private SphereCollider _itemCollider;

	private ulong _owner;

	public NetVehicleDriverPivots driverPivots;

	public static Rigidbody FLATBED;

	private Rigidbody[] allRBS;

	private float brakeInput;

	private float throttleInput;

	private float steerInput;

	private float handbrakeInput;

	private float clutchInput;

	internal NetVehicleAudio audioController;

	private int seatCount;

	internal List<ulong> seatsUsed = new List<ulong>();

	private float lastIdleThrottle;

	private float lastIdleThrottleUpdate;

	private bool updatingLastIdleThrottle;

	private static NetEvent<NetVehicle> updateIdleThrottle;

	internal List<ConfigurableJoint> headJoints = new List<ConfigurableJoint>();

	private static ConfigurableJoint flatbedPassengerSeat;

	private int itemMask = LayerMask.GetMask("Parts");

	public int hash { get; internal set; }

	public Transform transform { get; set; }

	public Rigidbody rigidbody { get; set; }

	public AxisCarController acc { get; set; }

	public Drivetrain drivetrain { get; set; }

	public SphereCollider itemCollider
	{
		get
		{
			return _itemCollider;
		}
		set
		{
			_itemCollider = value;
			value.enabled = false;
		}
	}

	public ulong owner
	{
		get
		{
			return _owner;
		}
		internal set
		{
			_owner = value;
			if (audioController != null)
			{
				audioController.IsDrivenBySoundController = value == BeerMPGlobals.UserID;
			}
		}
	}

	public ulong driver { get; internal set; }

	public bool driverSeatTaken => driver > 0L;

	public NetVehicle(Transform transform)
	{
		rigidbody = transform.GetComponent<Rigidbody>();
		allRBS = transform.GetComponentsInChildren<Rigidbody>(includeInactive: true);
		this.transform = transform;
		hash = transform.GetGameobjectHashString().GetHashCode();
		acc = transform.GetComponent<AxisCarController>();
		drivetrain = transform.GetComponent<Drivetrain>();
		SoundController component = transform.GetComponent<SoundController>();
		if (component != null)
		{
			audioController = new NetVehicleAudio(transform, component);
		}
		owner = BeerMPGlobals.HostID;
		NetVehicleManager.RegisterNetVehicle(this);
		if (updateIdleThrottle == null)
		{
			updateIdleThrottle = NetEvent<NetVehicle>.Register("IThrottle", OnIdleThrottleUpdate);
		}
		ConfigurableJoint componentInChildren = transform.GetComponentInChildren<ConfigurableJoint>();
		headJoints.Add(componentInChildren);
		NetRigidbodyManager.AddRigidbody(rigidbody, hash);
		FindFlatbed();
	}

	private static void FindFlatbed()
	{
		if (FLATBED == null)
		{
			FLATBED = GameObject.Find("FLATBED").GetComponent<Rigidbody>();
		}
	}

	private static void OnIdleThrottleUpdate(ulong sender, Packet p)
	{
		int hash = p.ReadInt();
		NetVehicle netVehicle = NetVehicleManager.vehicles.FirstOrDefault((NetVehicle v) => v.hash == hash);
		if (netVehicle == null)
		{
			Console.LogError($"Received idle throttle update for vehicle of hash {hash} but it doesn't exist");
			return;
		}
		netVehicle.updatingLastIdleThrottle = true;
		netVehicle.drivetrain.idlethrottle = p.ReadFloat();
	}

	public void SendEnterDrivingMode()
	{
		using Packet packet = new Packet(1);
		packet.Write(hash);
		packet.Write(_value: true);
		ulong num = (owner = BeerMPGlobals.UserID);
		driver = num;
		LocalNetPlayer.Instance.inCar = true;
		NetVehicleManager.DrivingModeEvent.Send(packet);
		NetRigidbodyManager.RequestOwnership(rigidbody);
		for (int i = 0; i < allRBS.Length; i++)
		{
			NetRigidbodyManager.RequestOwnership(allRBS[i]);
		}
		if (itemCollider != null)
		{
			Collider[] array = Physics.OverlapSphere(itemCollider.transform.position, itemCollider.radius, itemMask);
			for (int j = 0; j < array.Length; j++)
			{
				if (!(array[j].transform.parent != null))
				{
					Rigidbody component = array[j].GetComponent<Rigidbody>();
					if (!NetPlayer.grabbedItems.Contains(component))
					{
						NetRigidbodyManager.RequestOwnership(component);
					}
				}
			}
		}
		if (transform.name == "KEKMET(350-400psi)")
		{
			FindFlatbed();
			NetRigidbodyManager.RequestOwnership(FLATBED);
		}
	}

	public void SendExitDrivingMode()
	{
		using Packet packet = new Packet(1);
		packet.Write(hash);
		packet.Write(_value: false);
		driver = 0uL;
		LocalNetPlayer.Instance.inCar = false;
		NetVehicleManager.DrivingModeEvent.Send(packet);
	}

	internal void DrivingMode(ulong player, bool enter)
	{
		Console.Log(SteamFriends.GetFriendPersonaName((CSteamID)player) + " " + (enter ? "entered" : "exited") + " " + transform.name + " driving mode", show: false);
		driver = (enter ? player : 0uL);
		if (enter)
		{
			owner = player;
		}
		NetManager.GetPlayerComponentById<NetPlayer>((CSteamID)player).SetInCar(enter, this);
	}

	internal void WriteAxisControllerUpdate(Packet p)
	{
		p.Write(hash);
		p.Write(acc.brakeInput);
		p.Write(acc.throttleInput);
		p.Write(acc.steerInput);
		p.Write(acc.handbrakeInput);
		p.Write(acc.clutchInput);
	}

	internal void SetAxisController(float brake, float throttle, float steering, float handbrake, float clutch)
	{
		brakeInput = brake;
		throttleInput = throttle;
		steerInput = steering;
		handbrakeInput = handbrake;
		clutchInput = clutch;
	}

	internal void WriteDrivetrainUpdate(Packet p)
	{
		p.Write(hash);
		p.Write(drivetrain.rpm);
		p.Write(drivetrain.gear);
	}

	internal void SetDrivetrain(float rpm, int gear)
	{
		drivetrain.gear = gear;
	}

	internal void SendInitialSync(ulong target)
	{
		if (driver == BeerMPGlobals.UserID)
		{
			SendEnterDrivingMode();
		}
		using Packet packet = new Packet(1);
		packet.Write(hash);
		packet.Write(transform.position);
		packet.Write(transform.rotation);
	}

	internal void OnInitialSync(Packet packet)
	{
		Vector3 position = packet.ReadVector3();
		Quaternion rotation = packet.ReadQuaternion();
		transform.position = position;
		transform.rotation = rotation;
	}

	internal void Update()
	{
		if (owner != BeerMPGlobals.UserID && owner != 0L && acc != null)
		{
			acc.brakeInput = brakeInput;
			acc.throttleInput = throttleInput;
			acc.steerInput = steerInput;
			acc.handbrakeInput = handbrakeInput;
			acc.clutchInput = clutchInput;
		}
		for (int i = 0; i < headJoints.Count; i++)
		{
			if (!(headJoints[i] == null))
			{
				headJoints[i].breakForce = float.PositiveInfinity;
				headJoints[i].breakTorque = float.PositiveInfinity;
			}
		}
		if (flatbedPassengerSeat != null)
		{
			flatbedPassengerSeat.breakForce = float.PositiveInfinity;
			flatbedPassengerSeat.breakTorque = float.PositiveInfinity;
		}
	}

	public Transform AddPassengerSeat(Vector3 triggerOffset, Vector3 headPivotOffset)
	{
		return AddPassengerSeat(this, rigidbody, transform, triggerOffset, headPivotOffset);
	}

	public static Transform AddPassengerSeat(NetVehicle self, Rigidbody rb, Transform parent, Vector3 triggerOffset, Vector3 headPivotOffset)
	{
		GameObject gameObject = GameObject.Find("NPC_CARS").transform.Find("Amikset/KYLAJANI/LOD/PlayerFunctions").gameObject;
		int seatIndex = 0;
		GameObject gameObject2 = Object.Instantiate(gameObject);
		if (self != null)
		{
			seatIndex = self.seatCount;
			self.seatsUsed.Add(0uL);
		}
		gameObject2.name = $"MPPlayerFunctions_{seatIndex}";
		Transform obj = gameObject2.transform.Find("DriverHeadPivot");
		obj.GetComponent<Rigidbody>().isKinematic = true;
		obj.transform.localPosition = Vector3.zero;
		Object.Destroy(obj.GetPlayMaker("Death"));
		Transform child = gameObject2.transform.GetChild(1);
		child.gameObject.SetActive(value: false);
		child.transform.localPosition = headPivotOffset;
		Object.Destroy(child.GetComponent<ConfigurableJoint>());
		child.gameObject.SetActive(value: true);
		Object.Destroy(gameObject2.transform.GetChild(0).gameObject);
		gameObject2.transform.SetParent(parent, worldPositionStays: false);
		gameObject2.transform.Find("PlayerTrigger/DriveTrigger").localPosition = triggerOffset;
		Transform obj2 = gameObject2.transform.Find("PlayerTrigger");
		obj2.localPosition = Vector3.zero;
		Object.Destroy(obj2.GetComponent<PlayMakerFSM>());
		Object.Destroy(obj2.GetComponent<BoxCollider>());
		PlayMakerFSM dtFsm = gameObject2.transform.Find("PlayerTrigger/DriveTrigger").GetComponent<PlayMakerFSM>();
		dtFsm.name = "PassengerTrigger";
		dtFsm.transform.parent.name = "PlayerOffset";
		dtFsm.Initialize();
		dtFsm.GetComponent<CapsuleCollider>().radius = 0.2f;
		int hash = parent.GetGameobjectHashString().GetHashCode();
		if (self != null)
		{
			dtFsm.InsertAction("Press return", delegate
			{
				if (self.seatsUsed[seatIndex] != 0L)
				{
					dtFsm.SendEvent("FINISHED");
				}
			}, 0);
		}
		if (self != null)
		{
			dtFsm.InsertAction("Reset view", delegate
			{
				using Packet packet2 = new Packet(1);
				packet2.Write(hash);
				packet2.Write(seatIndex);
				packet2.Write(_value: true);
				LocalNetPlayer.Instance.inCar = true;
				NetVehicleManager.PassengerModeEvent.Send(packet2);
			});
			dtFsm.InsertAction("Create player", delegate
			{
				using Packet packet = new Packet(1);
				packet.Write(hash);
				packet.Write(seatIndex);
				packet.Write(_value: false);
				LocalNetPlayer.Instance.inCar = false;
				NetVehicleManager.PassengerModeEvent.Send(packet);
			});
		}
		(dtFsm.FsmStates.First((FsmState s) => s.Name == "Check speed").Actions[0] as GetVelocity).gameObject = new FsmOwnerDefault
		{
			GameObject = rb.transform.gameObject,
			OwnerOption = OwnerDefaultOption.SpecifyGameObject
		};
		(dtFsm.FsmStates.First((FsmState s) => s.Name == "Player in car").Actions[3] as SetStringValue).stringValue = "Passenger_" + rb.transform.name;
		gameObject2.SetActive(value: true);
		gameObject2.transform.GetChild(1).gameObject.SetActive(value: true);
		dtFsm.gameObject.SetActive(value: true);
		if (self != null)
		{
			self.seatCount++;
		}
		return gameObject2.transform;
	}
}
