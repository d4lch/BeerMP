using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BeerMP.Networking.Managers;

internal class FsmNetVehicle
{
	internal struct Pivot
	{
		public string path;

		public Vector3 position;

		public Vector3 eulerAngles;

		public string path_alt;

		public Vector3 position_alt;

		public Vector3 eulerAngles_alt;

		public Pivot GetAltPivot()
		{
			Pivot result = default(Pivot);
			result.path = path_alt;
			result.position = position_alt;
			result.eulerAngles = eulerAngles_alt;
			return result;
		}
	}

	public Transform transform;

	public NetVehicle netVehicle;

	public List<FsmVehicleDoor> vehicleDoors = new List<FsmVehicleDoor>();

	public List<FsmDashboardButton> dashboardButtons = new List<FsmDashboardButton>();

	public List<FsmDashboardLever> dashboardLevers = new List<FsmDashboardLever>();

	public List<FsmDashboardKnob> dashboardKnobs = new List<FsmDashboardKnob>();

	public List<FsmTurnSignals> turnSignals = new List<FsmTurnSignals>();

	internal static string[] carNames = new string[7] { "HAYOSIKO(1500kg, 250)", "GIFU(750/450psi)", "FERNDALE(1630kg)", "RCO_RUSCKO12(270)", "SATSUMA(557kg, 248)", "KEKMET(350-400psi)", "JONNEZ ES(Clone)" };

	internal static Vector3[] itemColliderCenter = new Vector3[7]
	{
		default(Vector3),
		new Vector3(0f, 2f, 3f),
		new Vector3(0f, 0f, -1f),
		new Vector3(0f, 0f, -0.3f),
		default(Vector3),
		new Vector3(0f, 1f, -2.2f),
		new Vector3(0f, 0.4f, -0.3f)
	};

	internal static float[] itemColliderRadius = new float[7] { 2.4f, 1.7f, 2f, 2f, 2f, 5.5f, 0.5f };

	internal static Vector3[][][] carPassengerSeats = new Vector3[7][][]
	{
		new Vector3[2][]
		{
			new Vector3[2]
			{
				new Vector3((float)Math.E * 35f / 218f, 0.8763284f, 0.7790703f),
				new Vector3(0.3336831f, 1.290549f, 0.7909793f)
			},
			new Vector3[2]
			{
				new Vector3(0.0364214f, 0.8763284f, 0.7790703f),
				new Vector3(0.3336831f, 1.290549f, 0.7909793f)
			}
		},
		new Vector3[1][] { new Vector3[2]
		{
			new Vector3(0.696f, 1.759f, 2.831f),
			new Vector3(0.6999686f, 1.290549f, 0.7909793f)
		} },
		new Vector3[4][]
		{
			new Vector3[2]
			{
				new Vector3(0.513f, 0.669f, -0.259f),
				new Vector3(0.3940828f, 0.8053533f, -0.2172538f)
			},
			new Vector3[2]
			{
				new Vector3(-0.513f, 0.669f, -0.9589999f),
				new Vector3(-0.3940828f, 0.8053533f, -1.1f)
			},
			new Vector3[2]
			{
				new Vector3(0f, 0.669f, -0.9589999f),
				new Vector3(0f, 0.8053533f, -1.1f)
			},
			new Vector3[2]
			{
				new Vector3(0.513f, 0.669f, -0.9589999f),
				new Vector3(0.3940828f, 0.8053533f, -1.1f)
			}
		},
		new Vector3[1][] { new Vector3[2]
		{
			new Vector3(0.26f, 0.325f, -0.087f),
			new Vector3(0.2239742f, 0.8545685f, -0.3627806f)
		} },
		new Vector3[3][]
		{
			new Vector3[2]
			{
				new Vector3(0.282f, 0.30727938f, -0.0671216f),
				new Vector3(0.3000017f, 0.5315849f, 0.01975246f)
			},
			new Vector3[2]
			{
				new Vector3(-0.282f, 0.30727938f, -0.5671216f),
				new Vector3(-0.3000017f, 0.5315849f, -0.48024753f)
			},
			new Vector3[2]
			{
				new Vector3(0.282f, 0.30727938f, -0.5671216f),
				new Vector3(0.3000017f, 0.5315849f, -0.48024753f)
			}
		},
		new Vector3[0][],
		new Vector3[0][]
	};

	internal static readonly Pivot[] throttlePedals = new Pivot[7]
	{
		new Pivot
		{
			path = "LOD/Dashboard/Pedals 1/throttle",
			position = new Vector3(0f, -0.2f, -0.26f),
			eulerAngles = new Vector3(310f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Pedals/Throttle",
			position = default(Vector3),
			eulerAngles = new Vector3(320f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Pedals 2/throttle",
			position = new Vector3(0f, -0.2f, 0f),
			eulerAngles = new Vector3(294f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Pedals/throttle",
			position = new Vector3(0f, 0.02f, 0.1f),
			eulerAngles = new Vector3(340f, 0f, 180f)
		},
		new Pivot
		{
			path = "Dashboard/Pedals/pedal_throttle",
			position = new Vector3(0f, -0.24f, -0.15f),
			eulerAngles = new Vector3(330f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/ThrottleFoot/Pivot/tractor_pedal_speed",
			position = new Vector3(-0.21f, -0.03f, 0.04f),
			eulerAngles = new Vector3(-45f, 90f, 90f)
		},
		new Pivot
		{
			path = "MESH",
			position = new Vector3(-0.05999999f, 0.13f, -0.18f),
			eulerAngles = new Vector3(350.0001f, 90.00019f, 100.0002f)
		}
	};

	internal static readonly Pivot[] brakePedals = new Pivot[7]
	{
		new Pivot
		{
			path = "LOD/Dashboard/Pedals 1/brake",
			position = new Vector3(0f, -0.1f, -0.26f),
			eulerAngles = new Vector3(350f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Pedals/Brake",
			position = default(Vector3),
			eulerAngles = new Vector3(330f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Pedals 2/brake",
			position = new Vector3(0f, -0.38f, -0.3f),
			eulerAngles = new Vector3(312f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Pedals/brake",
			position = new Vector3(0f, -0.05f, 0.26f),
			eulerAngles = new Vector3(340f, 0f, 180f)
		},
		new Pivot
		{
			path = "Dashboard/Pedals/pedal_brake",
			position = new Vector3(0f, -0.25f, -0.41f),
			eulerAngles = new Vector3(330f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Brake/Pivot/tractor_pedal_brake",
			position = new Vector3(-0.14f, -0.1f, -0.04f),
			eulerAngles = new Vector3(-10f, 30f, 90f)
		},
		new Pivot
		{
			path = "MESH",
			position = new Vector3(-0.05999999f, -0.13f, -0.18f),
			eulerAngles = new Vector3(350.0001f, 80.00032f, 100.0003f)
		}
	};

	internal static readonly Pivot[] clutchPedals = new Pivot[7]
	{
		new Pivot
		{
			path = "LOD/Dashboard/Pedals 1/clutch",
			position = new Vector3(0f, -0.1f, -0.26f),
			eulerAngles = new Vector3(350f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Pedals/Clutch",
			position = new Vector3(0f, -0.21f, -0.36f),
			eulerAngles = new Vector3(330f, 0f, 180f)
		},
		new Pivot
		{
			path = "",
			position = default(Vector3),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Pedals/clutch",
			position = new Vector3(0f, -0.05f, 0.15f),
			eulerAngles = new Vector3(340f, 0f, 180f)
		},
		new Pivot
		{
			path = "Dashboard/Pedals/pedal_clutch",
			position = new Vector3(0f, -0.25f, -0.41f),
			eulerAngles = new Vector3(330f, 0f, 180f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Clutch/Pivot/tractor_pedal_clutch",
			position = new Vector3(-0.17f, 0f, -0.11f),
			eulerAngles = new Vector3(0f, 30f, 90f)
		},
		new Pivot
		{
			path = "",
			position = default(Vector3),
			eulerAngles = default(Vector3)
		}
	};

	internal static readonly Pivot[] steeringWheels = new Pivot[7]
	{
		new Pivot
		{
			path = "LOD/Dashboard/Steering/VanSteeringPivot",
			position = new Vector3(0f, -0.2f, -0.09f),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Steering/TruckSteeringPivot",
			position = new Vector3(0f, 0.05f, 0.22f),
			eulerAngles = new Vector3(0f, 90f, 0f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Steering/MuscleSteeringPivot",
			position = new Vector3(0.2f, 0f, 0.05f),
			eulerAngles = new Vector3(0f, 90f, 90f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Steering/RusckoSteeringPivot",
			position = new Vector3(0f, -0.22f, -0.1f),
			eulerAngles = new Vector3(0f, 350f, 70f)
		},
		new Pivot
		{
			path = "Dashboard/Steering/CarSteeringPivot",
			position = new Vector3(0f, 0.22f, 0.82f),
			eulerAngles = new Vector3(10f, 190f, 0f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Steering/TractorSteeringPivot/valmet_steering",
			position = new Vector3(0.2f, 0f, 0f),
			eulerAngles = new Vector3(0f, 0f, 90f)
		},
		new Pivot
		{
			path = "LOD/Suspension/Steering/SteeringPivot/Column",
			position = new Vector3(0.03f, -0.3f, 0.41f),
			eulerAngles = new Vector3(3.585849E-05f, 320.0001f, 80.00005f)
		}
	};

	internal static readonly Pivot[] gearSticks = new Pivot[7]
	{
		new Pivot
		{
			path = "LOD/Dashboard/GearShifter/lever",
			position = new Vector3(-0.02f, 0.21f, -0.02f),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			path = "LOD/Dashboard/GearLever/Pivot/Lever",
			position = new Vector3(0f, -0.05f, 0.34f),
			eulerAngles = new Vector3(310f, 0f, 190f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/GearShifter/Pivot/muscle_gear_lever",
			position = new Vector3(-0.07f, 0.08f, 0.06f),
			eulerAngles = new Vector3(30f, 80f, 110f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/GearLever/Vibration/Pivot/lever",
			position = new Vector3(-0.01f, -0.14f, 0.39f),
			eulerAngles = new Vector3(280f, 0f, 180f)
		},
		new Pivot
		{
			path = "Dashboard/gear stick(xxxxx)/GearLever/Pivot/Lever/gear_stick",
			position = new Vector3(-0.05f, 0.2f, 0.2f),
			eulerAngles = new Vector3(340f, 120f, 70f),
			path_alt = "Dashboard/center console gt(xxxxx)/GearLever/Pivot/Lever/gear_stick",
			position_alt = new Vector3(-0.05f, 0.2f, 0.2f),
			eulerAngles_alt = new Vector3(340f, 120f, 70f)
		},
		new Pivot
		{
			path = "LOD/Dashboard/Gear/Lever/tractor_lever_gear",
			position = new Vector3(0.12f, 0.14f, 0.21f),
			eulerAngles = new Vector3(0f, 90f, 90f)
		},
		new Pivot
		{
			path = "LOD/Suspension/Steering/SteeringPivot/Throttle",
			position = new Vector3(0.06999999f, 0.01999998f, 0f),
			eulerAngles = new Vector3(0.0001466356f, 10.00063f, 340.0001f)
		}
	};

	internal static readonly Pivot[] drivingModes = new Pivot[7]
	{
		new Pivot
		{
			position = new Vector3(-0.4f, 0.93f, 0.99f),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			position = new Vector3(-0.75f, 1.84f, 2.74f),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			position = new Vector3(-0.4f, 0.53f, -0.05f),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			position = new Vector3(-0.29f, 0.37f, -0.08f),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			position = new Vector3(-0.25f, 0.2f, 0f),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			position = new Vector3(0f, 1.31f, -0.6f),
			eulerAngles = default(Vector3)
		},
		new Pivot
		{
			position = new Vector3(0.02f, 0.66f, -0.44f),
			eulerAngles = default(Vector3)
		}
	};

	public FsmFloat fuelLevel;

	public FsmFloat engineTemp;

	public FsmFloat oilLevel;

	public FsmFloat oilContamination;

	public FsmFloat oilGrade;

	public FsmFloat coolant1Level;

	public FsmFloat coolant2Level;

	public FsmFloat brake1Level;

	public FsmFloat brake2Level;

	public FsmFloat clutchLevel;

	public FsmIgnition ignition;

	public FsmStarter starter;

	public FsmNetVehicle(Transform transform)
	{
		this.transform = transform;
		netVehicle = new NetVehicle(transform);
		PlayMakerFSM[] componentsInChildren = transform.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
		DoItemCollider();
		DoFsms();
		DoDrivingMode(componentsInChildren);
		DoPassengerSeats();
		DoDriverPivots();
		DoFluidsAndFields(componentsInChildren);
		DoDoors(componentsInChildren);
		DoDashboard(componentsInChildren);
	}

	private void DoItemCollider()
	{
		int num = Array.IndexOf(carNames, transform.name);
		if (num == -1)
		{
			Console.LogWarning($"Car {netVehicle.hash} ({transform.name}) not in car names list", show: false);
			return;
		}
		GameObject gameObject = new GameObject("ItemCollider");
		gameObject.transform.parent = transform;
		gameObject.transform.localPosition = itemColliderCenter[num];
		SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
		sphereCollider.radius = itemColliderRadius[num];
		netVehicle.itemCollider = sphereCollider;
	}

	private void DoDrivingMode(PlayMakerFSM[] fsms)
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
		Transform transform = fsms[num].transform;
		transform.GetComponent<Rigidbody>().isKinematic = true;
		UnityEngine.Object.Destroy(fsms[num]);
		ConfigurableJoint configurableJoint = transform.GetComponent<ConfigurableJoint>();
		if (configurableJoint == null)
		{
			configurableJoint = transform.parent.GetComponentInChildren<ConfigurableJoint>();
		}
		configurableJoint.transform.localPosition = configurableJoint.connectedAnchor;
		configurableJoint.transform.localEulerAngles = Vector3.zero;
		UnityEngine.Object.Destroy(configurableJoint);
		Console.Log("Successfully removed death fsm from driving mode of " + this.transform.name, show: false);
	}

	private void DoDashboard(PlayMakerFSM[] fsms)
	{
		foreach (PlayMakerFSM playMakerFSM in fsms)
		{
			playMakerFSM.Initialize();
			if (playMakerFSM.FsmName == "Knob")
			{
				dashboardKnobs.Add(new FsmDashboardKnob(playMakerFSM));
			}
			else if (playMakerFSM.transform.name == "TurnSignals" && playMakerFSM.FsmName == "Usage")
			{
				turnSignals.Add(new FsmTurnSignals(playMakerFSM));
			}
			else
			{
				if (playMakerFSM.FsmName != "Use")
				{
					continue;
				}
				if (!playMakerFSM.HasState("Test") && !playMakerFSM.HasState("Test 2"))
				{
					if (playMakerFSM.HasState("INCREASE") && playMakerFSM.HasState("DECREASE"))
					{
						Console.Log("Added dashboard lever for car " + transform.name + ": " + playMakerFSM.transform.name, show: false);
						dashboardLevers.Add(new FsmDashboardLever(playMakerFSM));
					}
				}
				else if (!(playMakerFSM.transform.name == "Ignition"))
				{
					Console.Log("Added dashboard button for car " + transform.name + ": " + playMakerFSM.transform.name, show: false);
					dashboardButtons.Add(new FsmDashboardButton(playMakerFSM));
				}
			}
		}
	}

	private void DoDoors(PlayMakerFSM[] fsms)
	{
		if (transform.name == "SATSUMA(557kg, 248)")
		{
			vehicleDoors.Add(new FsmVehicleDoor(NetItemsManager.GetDatabaseObject("Database/DatabaseBody/Door_Left").GetPlayMaker("Use")));
			vehicleDoors.Add(new FsmVehicleDoor(NetItemsManager.GetDatabaseObject("Database/DatabaseBody/Door_Right").GetPlayMaker("Use")));
			vehicleDoors.Add(new FsmVehicleDoor(NetItemsManager.GetDatabaseObject("Database/DatabaseBody/Bootlid").transform.Find("Handles").GetPlayMaker("Use")));
			return;
		}
		foreach (PlayMakerFSM playMakerFSM in fsms)
		{
			if (!(playMakerFSM.FsmName != "Use") && (!(playMakerFSM.transform.name != "Handle") || !(playMakerFSM.transform.name != "Handles") || playMakerFSM.transform.name.ToLower().Contains("door") || playMakerFSM.transform.name.ToLower().Contains("bootlid")))
			{
				vehicleDoors.Add(new FsmVehicleDoor(playMakerFSM));
			}
		}
		if (transform.name == "HAYOSIKO(1500kg, 250)")
		{
			vehicleDoors.Add(new FsmVehicleDoor(transform.Find("SideDoor/door/Collider").GetComponent<PlayMakerFSM>(), isHayosikoSidedoor: true));
		}
	}

	private void DoFluidsAndFields(PlayMakerFSM[] fsms)
	{
		if (transform.name.ToUpper().Contains("SATSUMA"))
		{
			fuelLevel = GetDatabaseFsmFloat("Database/DatabaseMechanics/FuelTank", "FuelLevel");
			engineTemp = PlayMakerGlobals.Instance.Variables.FindFsmFloat("EngineTemp");
			oilLevel = GetDatabaseFsmFloat("Database/DatabaseMotor/Oilpan", "Oil");
			oilContamination = GetDatabaseFsmFloat("Database/DatabaseMotor/Oilpan", "OilContamination");
			oilGrade = GetDatabaseFsmFloat("Database/DatabaseMotor/Oilpan", "OilGrade");
			coolant1Level = GetDatabaseFsmFloat("Database/DatabaseMechanics/Radiator", "Water");
			coolant2Level = GetDatabaseFsmFloat("Database/DatabaseOrders/Racing Radiator", "Water");
			brake1Level = GetDatabaseFsmFloat("Database/DatabaseMechanics/BrakeMasterCylinder", "BrakeFluidF");
			brake2Level = GetDatabaseFsmFloat("Database/DatabaseMechanics/BrakeMasterCylinder", "BrakeFluidR");
			clutchLevel = GetDatabaseFsmFloat("Database/DatabaseMechanics/ClutchMasterCylinder", "ClutchFluid");
			Console.Log($"Init fluids and fields for Satsuma, {fuelLevel}, {engineTemp}, {oilLevel}, {oilContamination}, {oilGrade}, {coolant1Level}, {coolant2Level}, {brake1Level}, {brake2Level}, {clutchLevel}", show: false);
			return;
		}
		for (int i = 0; i < fsms.Length; i++)
		{
			if (fsms[i].transform.name == "FuelTank")
			{
				fuelLevel = fsms[i].FsmVariables.FindFsmFloat("FuelLevel");
				oilLevel = fsms[i].FsmVariables.FindFsmFloat("FuelOil");
			}
			else if (fsms[i].FsmName == "Cooling")
			{
				engineTemp = fsms[i].FsmVariables.FloatVariables.FirstOrDefault((FsmFloat f) => f.Name.Contains("EngineTemp"));
			}
		}
		Console.Log($"Init fluids and fields for {transform.name}, {fuelLevel}, {engineTemp}, {oilLevel}", show: false);
	}

	internal static void SendCarFluidsAndFields()
	{
		using Packet packet = new Packet(1);
		for (int i = 0; i < NetVehicleManager.vanillaVehicles.Count; i++)
		{
			FsmNetVehicle fsmNetVehicle = NetVehicleManager.vanillaVehicles[i];
			packet.Write(fsmNetVehicle.netVehicle.hash);
			WriteNullableFloat(fsmNetVehicle.fuelLevel, packet);
			WriteNullableFloat(fsmNetVehicle.oilLevel, packet);
			WriteNullableFloat(fsmNetVehicle.oilContamination, packet);
			WriteNullableFloat(fsmNetVehicle.oilGrade, packet);
			WriteNullableFloat(fsmNetVehicle.coolant1Level, packet);
			WriteNullableFloat(fsmNetVehicle.coolant2Level, packet);
			WriteNullableFloat(fsmNetVehicle.brake1Level, packet);
			WriteNullableFloat(fsmNetVehicle.brake2Level, packet);
			WriteNullableFloat(fsmNetVehicle.clutchLevel, packet);
			WriteNullableFloat(fsmNetVehicle.engineTemp, packet);
		}
		NetEvent<NetItemsManager>.Send("CarFluids", packet);
	}

	internal static void OnCarFluidsAndFields(ulong sender, Packet p)
	{
		while (p.UnreadLength() > 0)
		{
			int hash = p.ReadInt();
			FsmNetVehicle fsmNetVehicle = NetVehicleManager.vanillaVehicles.FirstOrDefault((FsmNetVehicle v) => v.netVehicle.hash == hash);
			if (fsmNetVehicle == null)
			{
				Console.LogError($"OnCarFluidsAndFields vehicle of hash {hash} cannot be found");
				for (int i = 0; i < 10; i++)
				{
					p.ReadFloat();
				}
				continue;
			}
			if (ReadNullableFloat(p, out var f))
			{
				fsmNetVehicle.fuelLevel.Value = f;
			}
			if (ReadNullableFloat(p, out var f2))
			{
				fsmNetVehicle.oilLevel.Value = f2;
			}
			if (ReadNullableFloat(p, out var f3))
			{
				fsmNetVehicle.oilContamination.Value = f3;
			}
			if (ReadNullableFloat(p, out var f4))
			{
				fsmNetVehicle.oilGrade.Value = f4;
			}
			if (ReadNullableFloat(p, out var f5))
			{
				fsmNetVehicle.coolant1Level.Value = f5;
			}
			if (ReadNullableFloat(p, out var f6))
			{
				fsmNetVehicle.coolant2Level.Value = f6;
			}
			if (ReadNullableFloat(p, out var f7))
			{
				fsmNetVehicle.brake1Level.Value = f7;
			}
			if (ReadNullableFloat(p, out var f8))
			{
				fsmNetVehicle.brake2Level.Value = f8;
			}
			if (ReadNullableFloat(p, out var f9))
			{
				fsmNetVehicle.clutchLevel.Value = f9;
			}
			if (ReadNullableFloat(p, out var f10))
			{
				fsmNetVehicle.engineTemp.Value = f10;
			}
		}
	}

	private static bool ReadNullableFloat(Packet p, out float f)
	{
		f = p.ReadFloat();
		return !float.IsNaN(f);
	}

	private static void WriteNullableFloat(FsmFloat f, Packet p)
	{
		p.Write(f?.Value ?? float.NaN);
	}

	private FsmFloat GetDatabaseFsmFloat(string databasePath, string variableName)
	{
		GameObject gameObject = GameObject.Find(databasePath);
		if (gameObject == null)
		{
			Console.Log("NV: Database '" + databasePath + "' could not be found");
			return null;
		}
		PlayMakerFSM component = gameObject.GetComponent<PlayMakerFSM>();
		if (component == null)
		{
			Console.Log("NV: Database '" + databasePath + "' doesn't have an fsm");
			return null;
		}
		FsmFloat fsmFloat = component.FsmVariables.FindFsmFloat(variableName);
		if (fsmFloat == null)
		{
			Console.Log("NV: Database '" + databasePath + "' doesn't have a " + variableName + " variable");
			return null;
		}
		return fsmFloat;
	}

	private void DoDriverPivots()
	{
		int num = Array.IndexOf(carNames, transform.name);
		if (num != -1)
		{
			netVehicle.driverPivots = new NetVehicleDriverPivots
			{
				throttlePedal = MakeDriverPivot(throttlePedals[num]),
				brakePedal = MakeDriverPivot(brakePedals[num]),
				clutchPedal = MakeDriverPivot(clutchPedals[num]),
				steeringWheel = MakeDriverPivot(steeringWheels[num]),
				gearSticks = ((num != 4) ? new Transform[1] { MakeDriverPivot(gearSticks[num]) } : new Transform[2]
				{
					MakeDriverPivot(gearSticks[num]),
					MakeDriverPivot(gearSticks[num].GetAltPivot())
				}),
				driverParent = MakeDriverPivot(drivingModes[num])
			};
		}
	}

	private Transform MakeDriverPivot(Pivot pivot)
	{
		if (pivot.path == "")
		{
			return null;
		}
		Transform transform = ((pivot.path == null) ? this.transform : this.transform.Find(pivot.path));
		if (transform == null)
		{
			return null;
		}
		Transform obj = new GameObject("BeerMP_DriverPivot").transform;
		obj.parent = transform;
		obj.localPosition = pivot.position;
		obj.localEulerAngles = pivot.eulerAngles;
		return obj;
	}

	private void DoFsms()
	{
		PlayMakerFSM[] componentsInChildren = transform.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true);
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
		if (!(drivingMode != null))
		{
			return;
		}
		drivingMode.Initialize();
		drivingMode.InsertAction("Press return", delegate
		{
			if (netVehicle.driverSeatTaken)
			{
				drivingMode.SendEvent("FINISHED");
			}
		}, 0);
		drivingMode.InsertAction("Reset view", netVehicle.SendEnterDrivingMode);
		drivingMode.InsertAction("Create player", netVehicle.SendExitDrivingMode);
		Console.Log("Init driving mode for " + transform.name, show: false);
	}

	private void DoPassengerSeats()
	{
		int num = Array.IndexOf(carNames, transform.name);
		if (num == -1)
		{
			Console.LogWarning($"no passenger seats for car with hash {netVehicle.hash} ({transform.name})", show: false);
			return;
		}
		Vector3[][] array = carPassengerSeats[num];
		if (array.Length == 0)
		{
			Console.LogWarning($"no passenger seats for car with hash {netVehicle.hash} ({transform.name})", show: false);
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			netVehicle.AddPassengerSeat(array[i][0], array[i][1]);
		}
	}

	public static void DoFlatbedPassengerSeats(out Transform _flatbed, out int _hash, Action<bool> enterPassenger)
	{
		Transform transform = (_flatbed = GameObject.Find("FLATBED").transform);
		int hashCode = transform.gameObject.name.GetHashCode();
		_hash = hashCode;
		Transform parent = transform.Find("Bed");
		Transform transform2 = NetVehicle.AddPassengerSeat(null, transform.GetComponent<Rigidbody>(), parent, new Vector3(0f, 0.5f, 2f), default(Vector3)).Find("PlayerOffset/PassengerTrigger");
		UnityEngine.Object.Destroy(transform2.GetComponent<CapsuleCollider>());
		BoxCollider boxCollider = transform2.gameObject.AddComponent<BoxCollider>();
		boxCollider.isTrigger = true;
		boxCollider.size = new Vector3(2f, 1f, 4.8f);
		Transform transform3 = UnityEngine.Object.Instantiate(GameObject.Find("RCO_RUSCKO12(270)").transform.Find("LOD/PlayerTrigger"));
		for (int i = 0; i < transform3.childCount; i++)
		{
			UnityEngine.Object.Destroy(transform3.GetChild(i));
		}
		transform3.transform.parent = transform2;
		Transform obj = transform3.transform;
		Vector3 localPosition = (transform3.transform.localEulerAngles = Vector3.zero);
		obj.localPosition = localPosition;
		transform3.GetComponent<BoxCollider>().size = new Vector3(2f, 1f, 4.8f);
		PlayMakerFSM component = transform2.GetComponent<PlayMakerFSM>();
		component.Initialize();
		component.InsertAction("Reset view", delegate
		{
			enterPassenger(obj: true);
		});
		component.InsertAction("Create player", delegate
		{
			enterPassenger(obj: false);
		});
	}
}
