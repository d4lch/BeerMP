using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetPartManager : MonoBehaviour
{
	internal class Bolt
	{
		internal PlayMakerFSM screw;

		internal FsmInt stage;

		internal FsmFloat floatstage;

		internal int hash;

		private bool isPin;

		private bool isTuneBolt;

		private bool isScrewableLid;

		private bool isCamshaftGear;

		private Action<int, byte> onTightnessChange;

		internal FsmFloat alignment;

		internal FsmFloat rot;

		private float maxAlignment;

		private float rotAmount;

		private byte lastScrewSyncStage;

		private bool doSync = true;

		private static FsmGameObject raycastBolt;

		public byte ScrewSyncStage
		{
			get
			{
				if (isTuneBolt)
				{
					return (byte)Mathf.RoundToInt(alignment.Value / maxAlignment * 255f);
				}
				if (isScrewableLid)
				{
					return (byte)Mathf.RoundToInt(rot.Value / 360f * 255f);
				}
				if (stage == null)
				{
					return (byte)floatstage.Value;
				}
				return (byte)stage.Value;
			}
		}

		public Bolt(PlayMakerFSM screw, int hash, Action<int, byte> onTightnessChange)
		{
			if (raycastBolt == null)
			{
				raycastBolt = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/2Spanner/Raycast").GetPlayMaker("Raycast").FsmVariables.FindFsmGameObject("Bolt");
			}
			screw.Initialize();
			this.screw = screw;
			stage = screw.FsmVariables.IntVariables.FirstOrDefault((FsmInt i) => i.Name == "Stage");
			if (stage == null)
			{
				floatstage = screw.FsmVariables.FindFsmFloat("Tightness");
			}
			this.hash = hash;
			this.onTightnessChange = onTightnessChange;
			if (screw.HasState("Wait") && !screw.HasState("Wait 2") && !screw.HasState("Wait 3") && !screw.HasState("Wait 4"))
			{
				rotAmount = screw.FsmVariables.FindFsmFloat("ScrewAmount").Value;
				rot = screw.FsmVariables.FindFsmFloat("Rot");
				FsmEvent fsmEvent = screw.AddEvent("MP_UNSCREW");
				FsmEvent fsmEvent2 = screw.AddEvent("MP_SCREW");
				screw.AddGlobalTransition(fsmEvent, "Unscrew");
				screw.AddGlobalTransition(fsmEvent2, "Screw");
				screw.InsertAction("Wait", OnTightness, 0);
				isScrewableLid = true;
			}
			else if (!screw.HasState("Wait") && !screw.HasState("Wait 2") && !screw.HasState("Wait 3") && !screw.HasState("Wait 4"))
			{
				if (screw.HasState("Setup"))
				{
					alignment = screw.FsmVariables.FindFsmFloat("Alignment");
					maxAlignment = screw.FsmVariables.FindFsmFloat("Max").Value;
					screw.InsertAction("Setup", OnTightness, 0);
					FsmEvent fsmEvent3 = screw.AddEvent("MP_SETUP");
					screw.AddGlobalTransition(fsmEvent3, "Setup");
					isTuneBolt = true;
				}
			}
			else
			{
				if (screw.HasState("Wait 3") || screw.HasState("Wait"))
				{
					screw.InsertAction(screw.HasState("Wait 3") ? "Wait 3" : "Wait", OnTightness, 0);
				}
				screw.InsertAction(screw.HasState("Wait 4") ? "Wait 4" : "Wait 2", OnTightness, 0);
			}
			if (screw.gameObject.name.StartsWith("oil filter"))
			{
				FsmEvent fsmEvent4 = screw.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "TIGHTEN");
				FsmEvent fsmEvent5 = screw.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "UNTIGHTEN");
				if (fsmEvent4 == null || fsmEvent5 == null)
				{
					Console.LogError($"Init bolt with name oil filter but occured null: {fsmEvent4 == null} {fsmEvent5 == null}");
					return;
				}
				screw.AddGlobalTransition(fsmEvent4, "Screw");
				screw.AddGlobalTransition(fsmEvent5, "Unscrew");
			}
			if (screw.gameObject.name == "Pin")
			{
				isPin = true;
				FsmEvent fsmEvent6 = screw.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "TIGHTEN");
				FsmEvent fsmEvent7 = screw.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "UNTIGHTEN");
				if (fsmEvent6 == null || fsmEvent7 == null)
				{
					Console.LogError($"Init bolt with name Pin but occured null: {fsmEvent6 == null} {fsmEvent7 == null}");
					return;
				}
				screw.AddGlobalTransition(fsmEvent6, "1");
				screw.AddGlobalTransition(fsmEvent7, "0");
			}
			if (screw.transform.parent != null && screw.transform.parent.name == "MaskedCamshaftGear")
			{
				isCamshaftGear = true;
				screw.InsertAction("Rotate", NetItemsManager.CamshaftGearAdjustEvent);
			}
			if (screw.transform.name.Contains("oil filter"))
			{
				Console.Log("Oil filter bolt, " + screw.transform.name + ", " + Environment.StackTrace);
			}
			screw.Initialize();
		}

		private void OnTightness()
		{
			if (doSync && ((raycastBolt != null && raycastBolt.Value == screw.gameObject) || isPin || (!screw.HasState("Wait 3") && !screw.HasState("Wait 4"))) && ScrewSyncStage != lastScrewSyncStage)
			{
				lastScrewSyncStage = ScrewSyncStage;
				onTightnessChange(hash, ScrewSyncStage);
			}
			doSync = true;
		}

		public void SetTightness(byte stage)
		{
			if (this.stage != null && stage == this.stage.Value)
			{
				return;
			}
			doSync = false;
			if (isTuneBolt)
			{
				float value = (float)(int)stage / 255f * maxAlignment;
				alignment.Value = value;
				screw.SendEvent("MP_SETUP");
			}
			else if (isScrewableLid)
			{
				float num = (float)(int)stage / 255f * 360f;
				bool flag = num > rot.Value;
				rot.Value = (flag ? (num - rotAmount) : (num + rotAmount));
				if (!flag && num < 5f)
				{
					rot.Value = rotAmount;
				}
				screw.SendEvent(flag ? "MP_SCREW" : "MP_UNSCREW");
			}
			else
			{
				bool flag2 = stage > this.stage.Value;
				this.stage.Value = (flag2 ? (stage - 1) : (stage + 1));
				screw.SendEvent(flag2 ? "TIGHTEN" : "UNTIGHTEN");
			}
		}
	}

	internal static List<int> assemblesHashes = new List<int>();

	internal static List<int> removesHashes = new List<int>();

	internal static List<PlayMakerFSM> removes = new List<PlayMakerFSM>();

	internal static List<PlayMakerFSM> assembles = new List<PlayMakerFSM>();

	internal static List<PlayMakerFSM> updatingFsms = new List<PlayMakerFSM>();

	internal static List<Bolt> bolts = new List<Bolt>();

	internal static Transform wiringMess;

	private const string assembleEvent = "PartAssemble";

	private const string removeEvent = "PartRemove";

	private const string screwEvent = "Screw";

	private const string camshaftGearEvent = "CamGear";

	private void Start()
	{
		NetEvent<NetPartManager>.Register("PartAssemble", OnAssemble);
		NetEvent<NetPartManager>.Register("PartRemove", OnRemove);
		NetEvent<NetPartManager>.Register("InitAssemble", OnInitSyncAssemble);
		NetEvent<NetPartManager>.Register("Screw", OnTightnessChange);
		NetEvent<NetPartManager>.Register("InitScrew", OnInitBolts);
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			InitSyncAssemble(user);
			InitSyncBolts(user);
		};
	}

	internal static void AddAssembleFsm(int hash, PlayMakerFSM fsm)
	{
		if (assemblesHashes.Contains(hash))
		{
			Console.LogError($"<b>FATAL ERROR!</b> FSM Hash {hash} of FSM '{fsm.FsmName}' on path '{fsm.transform.GetPath()}' already exists!");
			return;
		}
		assembles.Add(fsm);
		assemblesHashes.Add(hash);
	}

	internal static bool AddBolt(PlayMakerFSM fsm, int hash)
	{
		Bolt bolt = new Bolt(fsm, hash, TightnessChangeEvent);
		if (bolt.stage == null && bolt.alignment == null && bolt.rot == null)
		{
			return false;
		}
		bolts.Add(bolt);
		return true;
	}

	private void OnInitSyncAssemble(ulong sender, Packet packet)
	{
		while (packet.UnreadLength() > 0)
		{
			int num = packet.ReadInt();
			int num2 = packet.ReadInt();
			int num3 = NetRigidbodyManager.rigidbodyHashes.IndexOf(num);
			if (num3 == -1)
			{
				Console.LogError($"NetRigidbodyManager.OnInitSyncAssemble(ulong sender, Packet packet): the item hash {num} does not exist");
				continue;
			}
			NetRigidbodyManager.OwnedRigidbody ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num3];
			PlayMakerFSM playMakerFSM = null;
			if (num2 != 0)
			{
				int num4 = assemblesHashes.IndexOf(num2);
				if (num4 == -1)
				{
					Console.LogError($"NetRigidbodyManager.OnInitSyncAssemble(ulong sender, Packet packet): the assemble hash {num2} does not exist");
					continue;
				}
				playMakerFSM = assembles[num4];
			}
			Console.Log("InitAssemble: " + ((playMakerFSM == null) ? "null" : playMakerFSM.transform.name) + ", " + ((ownedRigidbody.assemble == null) ? "null" : ownedRigidbody.assemble.transform.name) + ", " + ownedRigidbody.transform.name, show: false);
			if (playMakerFSM != ownedRigidbody.assemble)
			{
				if (playMakerFSM == null)
				{
					Remove(num);
				}
				else
				{
					Assemble(num2, num);
				}
			}
		}
	}

	private void InitSyncAssemble(ulong user)
	{
		if (!BeerMPGlobals.IsHost)
		{
			return;
		}
		using Packet packet = new Packet(1);
		for (int i = 0; i < NetRigidbodyManager.ownedRigidbodies.Count; i++)
		{
			Console.Log("Init assemble send: " + NetRigidbodyManager.ownedRigidbodies[i].transform.name, show: false);
			packet.Write(NetRigidbodyManager.rigidbodyHashes[i]);
			int value = 0;
			if (NetRigidbodyManager.ownedRigidbodies[i].assemble != null)
			{
				value = assemblesHashes[assembles.IndexOf(NetRigidbodyManager.ownedRigidbodies[i].assemble)];
			}
			packet.Write(value);
		}
		NetEvent<NetPartManager>.Send("InitAssemble", packet, user);
	}

	private void OnDestroy()
	{
		assemblesHashes.Clear();
		assembles.Clear();
		bolts.Clear();
	}

	private void OnAssemble(ulong user, Packet p)
	{
		int fsmHash = p.ReadInt();
		int partHash = p.ReadInt();
		Assemble(fsmHash, partHash);
	}

	private void OnRemove(ulong user, Packet p)
	{
		int fsmHash = p.ReadInt();
		Remove(fsmHash);
	}

	private static void SendAssembleEvent(int fsmHash, FsmGameObject part)
	{
		using Packet packet = new Packet(1);
		packet.Write(fsmHash);
		if (part != null)
		{
			if (part.Value == null)
			{
				Console.LogError("NetRigidbodyManager.SendAssembeEvent: Attached gameobject is null!");
				return;
			}
			int num = NetRigidbodyManager.ownedRigidbodies.FindIndex((NetRigidbodyManager.OwnedRigidbody r) => r.Rigidbody == part.Value.GetComponent<Rigidbody>());
			if (num == -1)
			{
				Console.LogError("NetRigidbodyManager.SendAssembeEvent: Attached gameobject '" + part.Value.transform.GetPath() + "' does not have a hash!");
				return;
			}
			NetRigidbodyManager.ownedRigidbodies[num].assemble = assembles[assemblesHashes.IndexOf(fsmHash)];
			packet.Write(NetRigidbodyManager.rigidbodyHashes[num]);
		}
		else
		{
			packet.Write(0);
		}
		NetEvent<NetPartManager>.Send("PartAssemble", packet);
	}

	private static void SendRemoveEvent(int fsmHash)
	{
		using Packet packet = new Packet(1);
		packet.Write(fsmHash);
		NetEvent<NetPartManager>.Send("PartRemove", packet);
	}

	internal static void SetupRemovalPlaymaker(PlayMakerFSM fsm, int hash)
	{
		try
		{
			fsm.Initialize();
			removesHashes.Add(hash);
			removes.Add(fsm);
			FsmTransition fsmTransition = fsm.FsmStates[0].Transitions.FirstOrDefault((FsmTransition t) => t.EventName.Contains("REMOV"));
			string text = "";
			text = ((fsmTransition != null) ? fsmTransition.ToState : "Remove part");
			FsmEvent fsmEvent = fsm.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "MP_REMOVE");
			if (fsmEvent == null)
			{
				FsmEvent[] array = new FsmEvent[fsm.FsmEvents.Length + 1];
				fsm.FsmEvents.CopyTo(array, 0);
				fsmEvent = new FsmEvent("MP_REMOVE");
				FsmEvent.AddFsmEvent(fsmEvent);
				array[array.Length - 1] = fsmEvent;
				fsm.Fsm.Events = array;
			}
			FsmTransition[] array2 = new FsmTransition[fsm.FsmGlobalTransitions.Length + 1];
			fsm.FsmGlobalTransitions.CopyTo(array2, 0);
			array2[array2.Length - 1] = new FsmTransition
			{
				FsmEvent = fsmEvent,
				ToState = text
			};
			fsm.Fsm.GlobalTransitions = array2;
			fsm.InsertAction(text, delegate
			{
				RemoveEvent(hash);
			}, fsm.GetState(text).Actions.Length - 1);
			fsm.Initialize();
		}
		catch (Exception ex)
		{
			Console.LogError($"NetAttachmentManager.SetupPlaymaker(PlaymakerFSM): fsm {fsm.FsmName} with hash {hash} ({fsm.transform.name}) failed with exception {ex}");
		}
	}

	internal static void SetupAssemblePlaymaker(PlayMakerFSM fsm, int hash)
	{
		try
		{
			fsm.Initialize();
			string fsmName = fsm.FsmName;
			FsmTransition[] array2;
			if (!(fsmName == "Assembly"))
			{
				if (fsmName == "Assemble")
				{
					goto IL_01b0;
				}
			}
			else if (fsm.transform.name == "Insert")
			{
				FsmEvent[] array = new FsmEvent[fsm.FsmEvents.Length + 1];
				fsm.FsmEvents.CopyTo(array, 0);
				array[array.Length - 1] = new FsmEvent("MP_ASSEMBLE");
				FsmEvent.AddFsmEvent(array[array.Length - 1]);
				fsm.Fsm.Events = array;
				array2 = new FsmTransition[fsm.FsmGlobalTransitions.Length + 1];
				fsm.FsmGlobalTransitions.CopyTo(array2, 0);
				array2[array2.Length - 1] = new FsmTransition
				{
					FsmEvent = fsm.FsmEvents.First((FsmEvent e) => e.Name == "MP_ASSEMBLE"),
					ToState = "Add battery"
				};
				fsm.Fsm.GlobalTransitions = array2;
				fsm.InsertAction("Add battery", delegate
				{
					BatteryToRadioOrFlashlightEvent(hash, fsm.FsmVariables.FindFsmGameObject("Part"));
				});
			}
			else
			{
				if (!(fsm.transform.name == "TriggerCharger"))
				{
					goto IL_01b0;
				}
				fsm.InsertAction("Init", delegate
				{
					BatteryOnChargerEvent(hash, fsm.FsmVariables.FindFsmGameObject("Battery"));
				});
			}
			goto IL_0441;
			IL_01b0:
			FsmTransition fsmTransition = fsm.FsmStates[0].Transitions.FirstOrDefault((FsmTransition t) => t.EventName.Contains("ASSEMBL"));
			string text = "";
			bool flag;
			if (flag = fsm.FsmGlobalTransitions.Any((FsmTransition t) => t.EventName == "RESETWIRING"))
			{
				text = "Sound";
				FixWiringFsm(fsm);
			}
			else if (fsmTransition == null)
			{
				FsmState fsmState = fsm.FsmStates.FirstOrDefault((FsmState s) => s.Name.ToLower().Contains("assemble"));
				if (fsmState == null)
				{
					Console.LogError($"NetAttachmentManager.SetupPlaymaker(PlaymakerFSM): fsm {fsm.FsmName} with hash {hash} ({fsm.transform.name}) failed because there was no state 'assemble' nor event 'ASSEMBLE'");
					return;
				}
				text = fsmState.Name;
			}
			else
			{
				text = fsmTransition.ToState;
			}
			FsmEvent fsmEvent = fsm.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "MP_ASSEMBLE");
			if (fsmEvent == null)
			{
				FsmEvent[] array3 = new FsmEvent[fsm.FsmEvents.Length + 1];
				fsm.FsmEvents.CopyTo(array3, 0);
				fsmEvent = new FsmEvent("MP_ASSEMBLE");
				FsmEvent.AddFsmEvent(fsmEvent);
				array3[array3.Length - 1] = fsmEvent;
				fsm.Fsm.Events = array3;
			}
			array2 = new FsmTransition[fsm.FsmGlobalTransitions.Length + 1];
			fsm.FsmGlobalTransitions.CopyTo(array2, 0);
			array2[array2.Length - 1] = new FsmTransition
			{
				FsmEvent = fsmEvent,
				ToState = text
			};
			fsm.Fsm.GlobalTransitions = array2;
			FsmState[] array4 = fsm.FsmStates.Where((FsmState s) => s.Name.ToLower().Contains("assemble")).ToArray();
			if (!flag && array4.Length != 0)
			{
				for (int i = 0; i < array4.Length; i++)
				{
					fsm.InsertAction(array4[i].Name, delegate
					{
						AssembleEvent(hash);
					});
				}
			}
			else
			{
				fsm.InsertAction(text, delegate
				{
					AssembleEvent(hash);
				});
			}
			goto IL_0441;
			IL_0441:
			fsm.Initialize();
		}
		catch (Exception ex)
		{
			Console.LogError($"NetAttachmentManager.SetupPlaymaker(PlaymakerFSM): fsm {fsm.FsmName} with hash {hash} ({fsm.transform.name}) failed with exception {ex}");
		}
	}

	private static void FixWiringFsm(PlayMakerFSM fsm)
	{
		FsmFloat dist = fsm.FsmVariables.FindFsmFloat("Distance");
		FsmFloat tol = fsm.FsmVariables.FindFsmFloat("Tolerance");
		FsmState state = fsm.GetState("State 1");
		state.Actions[state.Actions.ToList().FindIndex((FsmStateAction a) => a is FloatCompare)] = new PlayMakerUtilities.PM_Hook(delegate
		{
			if (dist.Value < tol.Value && wiringMess.transform.root.name == "PLAYER")
			{
				fsm.SendEvent("ASSEMBLE");
			}
		}, everyFrame: true);
	}

	private static void AssembleEvent(int fsmHash)
	{
		try
		{
			PlayMakerFSM playMakerFSM = assembles[assemblesHashes.IndexOf(fsmHash)];
			Console.Log($"Attach event triggered: {fsmHash}, {playMakerFSM.transform.name}", show: false);
			SatsumaProfiler.Instance.attached.Add(playMakerFSM.transform.name);
			FsmGameObject part = playMakerFSM.FsmVariables.FindFsmGameObject("Part");
			if (updatingFsms.Contains(playMakerFSM))
			{
				updatingFsms.Remove(playMakerFSM);
			}
			else
			{
				SendAssembleEvent(fsmHash, part);
			}
		}
		catch (Exception ex)
		{
			Console.LogError($"Error in AssembleEvent: {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
		}
	}

	private static void RemoveEvent(int fsmHash)
	{
		try
		{
			int num = NetRigidbodyManager.rigidbodyHashes.IndexOf(fsmHash);
			NetRigidbodyManager.OwnedRigidbody ownedRigidbody;
			if (num == -1)
			{
				int index = removesHashes.IndexOf(fsmHash);
				PlayMakerFSM playMakerFSM = removes[index];
				FsmObject fsmObject = playMakerFSM.FsmVariables.FindFsmObject("Rigidbody");
				ownedRigidbody = NetRigidbodyManager.AddRigidbody(fsmObject.Value as Rigidbody, fsmHash);
				ownedRigidbody.remove = playMakerFSM;
				ownedRigidbody.Removal_Rigidbody = fsmObject;
				num = NetRigidbodyManager.rigidbodyHashes.IndexOf(fsmHash);
			}
			else
			{
				ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num];
			}
			NetRigidbodyManager.RequestOwnership(ownedRigidbody);
			ownedRigidbody.assemble = null;
			PlayMakerFSM remove = ownedRigidbody.remove;
			SatsumaProfiler.Instance.detached.Add(remove.transform.name);
			if (updatingFsms.Contains(remove))
			{
				updatingFsms.Remove(remove);
			}
			else
			{
				SendRemoveEvent(fsmHash);
			}
		}
		catch (Exception ex)
		{
			Console.LogError($"Error in RemoveEvent: {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
		}
	}

	private static void BatteryOnChargerEvent(int fsmHash, FsmGameObject battery)
	{
		try
		{
			Console.Log($"Battery attached to charger: {battery.Value.transform.GetPath().GetHashCode()}", show: false);
			PlayMakerFSM item = assembles[assemblesHashes.IndexOf(fsmHash)];
			if (updatingFsms.Contains(item))
			{
				updatingFsms.Remove(item);
			}
			else
			{
				SendAssembleEvent(fsmHash, battery);
			}
		}
		catch (Exception ex)
		{
			Console.LogError($"Error in AssembleEvent: {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
		}
	}

	private static void BatteryToRadioOrFlashlightEvent(int fsmHash, FsmGameObject battery)
	{
		try
		{
			PlayMakerFSM playMakerFSM = assembles[assemblesHashes.IndexOf(fsmHash)];
			Console.Log($"Battery added to radio or flashlight: {fsmHash}, {playMakerFSM.transform.name}, batt: {battery.Value.transform.GetPath().GetHashCode()}", show: false);
			if (updatingFsms.Contains(playMakerFSM))
			{
				updatingFsms.Remove(playMakerFSM);
			}
			else
			{
				SendAssembleEvent(fsmHash, battery);
			}
		}
		catch (Exception ex)
		{
			Console.LogError($"Error in AssembleEvent: {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
		}
	}

	private void Assemble(int fsmHash, int partHash)
	{
		int num = assemblesHashes.IndexOf(fsmHash);
		if (num == -1)
		{
			return;
		}
		PlayMakerFSM playMakerFSM = assembles[num];
		updatingFsms.Add(playMakerFSM);
		if (partHash == 0)
		{
			if (!playMakerFSM.FsmGlobalTransitions.Any((FsmTransition t) => t.EventName == "RESETWIRING"))
			{
				Console.LogError($"Received assemble event for fsm {fsmHash} ({playMakerFSM.transform.name}) but the part hash is 0 and the fsm doesn't look like a wiring fsm");
			}
			playMakerFSM.Fsm.Event(playMakerFSM.FsmEvents.First((FsmEvent e) => e.Name == "MP_ASSEMBLE"));
			return;
		}
		num = NetRigidbodyManager.rigidbodyHashes.IndexOf(partHash);
		if (num == -1)
		{
			return;
		}
		NetRigidbodyManager.OwnedRigidbody ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num];
		ownedRigidbody.assemble = playMakerFSM;
		GameObject value = ownedRigidbody.Rigidbody.gameObject;
		if (playMakerFSM.transform.name == "TriggerCharger")
		{
			playMakerFSM.FsmVariables.FindFsmGameObject("Battery").Value = value;
			playMakerFSM.Fsm.Event(playMakerFSM.FsmEvents.First((FsmEvent e) => e.Name == "PLACEBATT"));
		}
		else
		{
			playMakerFSM.FsmVariables.FindFsmGameObject("Part").Value = value;
			playMakerFSM.Fsm.Event(playMakerFSM.FsmEvents.First((FsmEvent e) => e.Name == "MP_ASSEMBLE"));
		}
	}

	private void Remove(int fsmHash)
	{
		int num = NetRigidbodyManager.rigidbodyHashes.IndexOf(fsmHash);
		NetRigidbodyManager.OwnedRigidbody ownedRigidbody;
		if (num == -1)
		{
			int index = removesHashes.IndexOf(fsmHash);
			PlayMakerFSM playMakerFSM = removes[index];
			ownedRigidbody = NetRigidbodyManager.AddRigidbody(playMakerFSM.GetComponent<Rigidbody>(), fsmHash);
			ownedRigidbody.remove = playMakerFSM;
			ownedRigidbody.Removal_Rigidbody = playMakerFSM.FsmVariables.FindFsmObject("Rigidbody");
			num = NetRigidbodyManager.rigidbodyHashes.IndexOf(fsmHash);
		}
		else
		{
			ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num];
		}
		PlayMakerFSM remove = ownedRigidbody.remove;
		remove.Fsm.Event(remove.FsmEvents.First((FsmEvent e) => e.Name == "MP_REMOVE"));
		if (num != -1)
		{
			NetRigidbodyManager.RequestOwnership(ownedRigidbody);
			ownedRigidbody.assemble = null;
			updatingFsms.Add(remove);
		}
	}

	private static void TightnessChangeEvent(int boltHash, byte stage)
	{
		using Packet packet = new Packet(1);
		packet.Write(boltHash);
		packet.Write(stage);
		NetEvent<NetPartManager>.Send("Screw", packet);
	}

	private void OnTightnessChange(ulong sender, Packet packet)
	{
		int boltHash = packet.ReadInt();
		byte tightness = packet.ReadByte();
		Bolt bolt = bolts.FirstOrDefault((Bolt b) => b.hash == boltHash);
		if (bolt == null)
		{
			Console.LogError($"The bolt with hash {boltHash} could not be found");
		}
		else
		{
			bolt.SetTightness(tightness);
		}
	}

	private void InitSyncBolts(ulong target)
	{
		if (!BeerMPGlobals.IsHost)
		{
			return;
		}
		using Packet packet = new Packet(1);
		for (int i = 0; i < bolts.Count; i++)
		{
			packet.Write(bolts[i].hash);
			packet.Write(bolts[i].ScrewSyncStage);
		}
		NetEvent<NetPartManager>.Send("InitScrew", packet, target);
	}

	private void OnInitBolts(ulong sender, Packet p)
	{
		while (p.UnreadLength() > 0)
		{
			int boltHash = p.ReadInt();
			byte tightness = p.ReadByte();
			Bolt bolt = bolts.FirstOrDefault((Bolt b) => b.hash == boltHash);
			if (bolt == null)
			{
				Console.LogError($"The bolt with hash {boltHash} could not be found");
			}
			else
			{
				bolt.SetTightness(tightness);
			}
		}
	}
}
