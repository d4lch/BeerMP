using System;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace BeerMP.Networking.Managers;

internal class FsmStarter
{
	public FsmNetVehicle fsmVeh;

	public PlayMakerFSM fsm;

	public FsmFloat PlugHeat;

	public FsmFloat StartTime;

	internal ulong user;

	private bool doSync = true;

	private NetEvent<FsmStarter> Startingengine;

	private NetEvent<FsmStarter> Startengine;

	private NetEvent<FsmStarter> Motorrunning;

	private NetEvent<FsmStarter> Wait;

	private NetEvent<FsmStarter> ACCGlowplug;

	private NetEvent<FsmStarter> Brokenstarter;

	private NetEvent<FsmStarter> Waitforstart;

	private NetEvent<FsmStarter> State1;

	private NetEvent<FsmStarter> Checkclutch;

	private FsmEvent startingengine;

	private FsmEvent startengine;

	private FsmEvent motorrunning;

	private FsmEvent wait;

	private FsmEvent accglowplug;

	private FsmEvent brokenstarter;

	private FsmEvent waitforstart;

	private FsmEvent state1;

	private FsmEvent checkclutch;

	private FsmEvent current;

	public FsmStarter(FsmNetVehicle fsmVeh, PlayMakerFSM fsm)
	{
		FsmStarter fsmStarter = this;
		this.fsmVeh = fsmVeh;
		this.fsm = fsm;
		fsm.Initialize();
		PlugHeat = fsm.FsmVariables.FindFsmFloat("PlugHeat");
		StartTime = fsm.FsmVariables.FindFsmFloat("StartTime");
		if (fsm.HasState("Starting engine"))
		{
			startingengine = fsm.AddEvent("MP_STARTINGENGINE");
			fsm.AddGlobalTransition(startingengine, "Starting engine");
			FsmState state = fsm.GetState("Starting engine");
			fsm.InsertAction("Starting engine", delegate
			{
				fsmStarter.current = fsmStarter.startingengine;
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using Packet packet9 = new Packet(1);
					packet9.Write((fsmStarter.PlugHeat != null) ? fsmStarter.PlugHeat.Value : (-1f));
					packet9.Write(fsmStarter.StartTime.Value);
					fsmStarter.Startingengine.Send(packet9);
				}
				if (fsm.HasState("ACC / Glowplug"))
				{
					fsm.GetState("ACC / Glowplug").Actions.Where((FsmStateAction x) => x.GetType() == typeof(BoolTest)).ToArray().Select(delegate(FsmStateAction x)
					{
						x.Enabled = true;
						return true;
					});
				}
				if (fsm.HasState("Check clutch"))
				{
					FsmStateAction fsmStateAction6 = fsm.GetState("Check clutch").Actions.FirstOrDefault((FsmStateAction x) => x.GetType() == typeof(SendRandomEvent));
					if (fsmStateAction6 != null)
					{
						fsmStateAction6.Enabled = true;
					}
				}
			}, state.Actions.Length - 3);
		}
		if (fsm.HasState("Start engine"))
		{
			startengine = fsm.AddEvent("MP_STARTENGINE");
			fsm.AddGlobalTransition(startengine, "Start engine");
			fsm.InsertAction("Start engine", delegate
			{
				fsmStarter.current = fsmStarter.startengine;
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using Packet packet8 = new Packet(1);
					fsmStarter.Startengine.Send(packet8);
				}
				fsmStarter.doSync = true;
			}, 0);
		}
		if (fsm.HasState("Motor running"))
		{
			motorrunning = fsm.AddEvent("MP_MOTORRUNNING");
			fsm.AddGlobalTransition(motorrunning, "Motor running");
			fsm.InsertAction("Motor running", delegate
			{
				fsmStarter.current = fsmStarter.motorrunning;
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using Packet packet7 = new Packet(1);
					fsmStarter.Motorrunning.Send(packet7);
				}
				if (fsmVeh.ignition != null)
				{
					fsmVeh.ignition.user = 0uL;
				}
				if (fsm.HasState("ACC / Glowplug"))
				{
					fsm.GetState("ACC / Glowplug").Actions.Where((FsmStateAction x) => x.GetType() == typeof(BoolTest)).ToArray().Select(delegate(FsmStateAction x)
					{
						x.Enabled = true;
						return true;
					});
				}
				if (fsm.HasState("Check clutch"))
				{
					FsmStateAction fsmStateAction5 = fsm.GetState("Check clutch").Actions.FirstOrDefault((FsmStateAction x) => x.GetType() == typeof(SendRandomEvent));
					if (fsmStateAction5 != null)
					{
						fsmStateAction5.Enabled = true;
					}
				}
				fsmStarter.doSync = true;
			}, 0);
		}
		if (fsm.HasState("Wait"))
		{
			wait = fsm.AddEvent("MP_WAIT");
			fsm.AddGlobalTransition(wait, "Wait");
			fsm.InsertAction("Wait", delegate
			{
				fsmStarter.current = fsmStarter.wait;
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using Packet packet6 = new Packet(1);
					fsmStarter.Wait.Send(packet6);
				}
				fsmStarter.doSync = true;
			}, 0);
		}
		if (fsm.HasState("ACC / Glowplug"))
		{
			accglowplug = fsm.AddEvent("MP_ACCGLOWPLUG");
			fsm.AddGlobalTransition(accglowplug, "ACC / Glowplug");
			fsm.InsertAction("ACC / Glowplug", delegate
			{
				fsmStarter.current = fsmStarter.accglowplug;
				if (fsmStarter.user == 0L)
				{
					fsmStarter.user = BeerMPGlobals.UserID;
				}
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using (Packet packet5 = new Packet(1))
					{
						packet5.Write((fsmStarter.PlugHeat != null) ? fsmStarter.PlugHeat.Value : (-1f));
						fsmStarter.ACCGlowplug.Send(packet5);
						return;
					}
				}
				fsm.GetState("ACC / Glowplug").Actions.Where((FsmStateAction x) => x.GetType() == typeof(BoolTest)).ToArray().Select(delegate(FsmStateAction x)
				{
					x.Enabled = false;
					return false;
				});
			}, 0);
		}
		if (fsm.HasState("Broken starter"))
		{
			brokenstarter = fsm.AddEvent("MP_BROKENSTARTER");
			fsm.AddGlobalTransition(startingengine, "Broken starter");
			FsmState state2 = fsm.GetState("Broken starter");
			fsm.InsertAction("Broken starter", delegate
			{
				fsmStarter.current = fsmStarter.brokenstarter;
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using Packet packet4 = new Packet(1);
					fsmStarter.Brokenstarter.Send(packet4);
				}
				if (fsm.HasState("ACC / Glowplug"))
				{
					fsm.GetState("ACC / Glowplug").Actions.Where((FsmStateAction x) => x.GetType() == typeof(BoolTest)).ToArray().Select(delegate(FsmStateAction x)
					{
						x.Enabled = true;
						return true;
					});
				}
				if (fsm.HasState("Check clutch"))
				{
					FsmStateAction fsmStateAction4 = fsm.GetState("Check clutch").Actions.FirstOrDefault((FsmStateAction x) => x.GetType() == typeof(SendRandomEvent));
					if (fsmStateAction4 != null)
					{
						fsmStateAction4.Enabled = true;
					}
				}
				fsmStarter.doSync = true;
			}, state2.Actions.Length - 1);
		}
		if (fsm.HasState("Wait for start"))
		{
			waitforstart = fsm.AddEvent("MP_WAITFORSTART");
			fsm.AddGlobalTransition(waitforstart, "Wait for start");
			fsm.InsertAction("Wait for start", delegate
			{
				fsmStarter.current = fsmStarter.waitforstart;
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using Packet packet3 = new Packet(1);
					fsmStarter.Waitforstart.Send(packet3);
				}
				if (fsm.HasState("Check clutch"))
				{
					FsmStateAction fsmStateAction3 = fsm.GetState("Check clutch").Actions.FirstOrDefault((FsmStateAction x) => x.GetType() == typeof(SendRandomEvent));
					if (fsmStateAction3 != null)
					{
						fsmStateAction3.Enabled = true;
					}
				}
				fsmStarter.user = 0uL;
				fsmStarter.doSync = true;
			});
		}
		if (fsm.HasState("State 1"))
		{
			state1 = fsm.AddEvent("MP_STATE1");
			fsm.AddGlobalTransition(state1, "State 1");
			fsm.InsertAction("State 1", delegate
			{
				fsmStarter.current = fsmStarter.state1;
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using Packet packet2 = new Packet(1);
					fsmStarter.State1.Send(packet2);
				}
				fsmStarter.doSync = true;
				if (fsm.HasState("ACC / Glowplug"))
				{
					fsm.GetState("ACC / Glowplug").Actions.Where((FsmStateAction x) => x.GetType() == typeof(BoolTest)).ToArray().Select(delegate(FsmStateAction x)
					{
						x.Enabled = true;
						return true;
					});
				}
				if (fsm.HasState("Check clutch"))
				{
					FsmStateAction fsmStateAction2 = fsm.GetState("Check clutch").Actions.FirstOrDefault((FsmStateAction x) => x.GetType() == typeof(SendRandomEvent));
					if (fsmStateAction2 != null)
					{
						fsmStateAction2.Enabled = true;
					}
				}
			}, 0);
		}
		if (fsm.HasState("Check clutch"))
		{
			checkclutch = fsm.AddEvent("MP_CHECKCLUTCH");
			fsm.AddGlobalTransition(checkclutch, "Check clutch");
			fsm.InsertAction("Check clutch", delegate
			{
				fsmStarter.current = fsmStarter.checkclutch;
				if (fsmStarter.doSync && fsmStarter.user == BeerMPGlobals.UserID)
				{
					using Packet packet = new Packet(1);
					fsmStarter.Checkclutch.Send(packet);
				}
				else
				{
					FsmStateAction fsmStateAction = fsm.GetState("Check clutch").Actions.FirstOrDefault((FsmStateAction x) => x.GetType() == typeof(SendRandomEvent));
					if (fsmStateAction != null)
					{
						fsmStateAction.Enabled = false;
					}
				}
				fsmStarter.doSync = true;
			}, 0);
		}
		Startingengine = NetEvent<FsmStarter>.Register($"Startingengine{fsmVeh.netVehicle.hash}", OnStartingengine);
		Startengine = NetEvent<FsmStarter>.Register($"Startengine{fsmVeh.netVehicle.hash}", OnStartengine);
		Motorrunning = NetEvent<FsmStarter>.Register($"Motorrunning{fsmVeh.netVehicle.hash}", OnMotorrunning);
		Wait = NetEvent<FsmStarter>.Register($"Wait{fsmVeh.netVehicle.hash}", OnWait);
		ACCGlowplug = NetEvent<FsmStarter>.Register($"ACCGlowplug{fsmVeh.netVehicle.hash}", OnACCGlowplug);
		Brokenstarter = NetEvent<FsmStarter>.Register($"Brokenstarter{fsmVeh.netVehicle.hash}", OnBrokenstarter);
		Waitforstart = NetEvent<FsmStarter>.Register($"Waitforstart{fsmVeh.netVehicle.hash}", OnWaitforstart);
		State1 = NetEvent<FsmStarter>.Register($"State1{fsmVeh.netVehicle.hash}", OnState1);
		Checkclutch = NetEvent<FsmStarter>.Register($"Checkclutch{fsmVeh.netVehicle.hash}", OnCheckclutch);
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate
		{
			if (fsmStarter.current != null)
			{
				fsmStarter.doSync = true;
				fsm.SendEvent(fsmStarter.current.Name);
			}
		};
	}

	private void OnCheckclutch(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			fsm.SendEvent(checkclutch.Name);
		}
	}

	private void OnState1(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			fsm.SendEvent(state1.Name);
		}
	}

	private void OnWaitforstart(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			fsm.SendEvent(waitforstart.Name);
		}
	}

	private void OnBrokenstarter(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			fsm.SendEvent(brokenstarter.Name);
		}
	}

	private void OnACCGlowplug(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			float num = packet.ReadFloat();
			if (num > 0f)
			{
				PlugHeat.Value = num;
			}
			fsm.SendEvent(accglowplug.Name);
		}
	}

	private void OnWait(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			fsm.SendEvent(wait.Name);
		}
	}

	private void OnMotorrunning(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			if (fsmVeh.ignition != null)
			{
				fsmVeh.ignition.user = 0uL;
			}
			user = 0uL;
			fsm.SendEvent(motorrunning.Name);
		}
	}

	private void OnStartengine(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			fsm.SendEvent(startengine.Name);
		}
	}

	private void OnStartingengine(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			float num = packet.ReadFloat();
			float value = packet.ReadFloat();
			fsm.SendEvent(startingengine.Name);
			if (num > 0f)
			{
				PlugHeat.Value = num;
			}
			StartTime.Value = value;
		}
	}
}
