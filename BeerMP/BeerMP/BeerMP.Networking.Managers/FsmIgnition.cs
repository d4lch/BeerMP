using System;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using Steamworks;

namespace BeerMP.Networking.Managers;

internal class FsmIgnition
{
	public FsmNetVehicle fsmVeh;

	public PlayMakerFSM fsm;

	public FsmStarter starter;

	internal ulong user;

	private bool doSync = true;

	private NetEvent<FsmIgnition> Sound;

	private NetEvent<FsmIgnition> AccOn;

	private NetEvent<FsmIgnition> Shutoff;

	private NetEvent<FsmIgnition> MotorOFF;

	private NetEvent<FsmIgnition> Motorstarting;

	private FsmEvent sound;

	private FsmEvent accon;

	private FsmEvent shutoff;

	private FsmEvent motoroff;

	private FsmEvent motorstarting;

	private FsmEvent waitbutton;

	private FsmEvent current;

	public FsmIgnition(FsmNetVehicle fsmVeh, PlayMakerFSM fsm)
	{
		FsmIgnition fsmIgnition = this;
		this.fsmVeh = fsmVeh;
		this.fsm = fsm;
		if (fsmVeh.starter != null)
		{
			starter = fsmVeh.starter;
		}
		fsm.Initialize();
		sound = fsm.AddEvent("MP_PUTKEYIN");
		fsm.AddGlobalTransition(sound, "Sound");
		fsm.InsertAction("Sound", delegate
		{
			fsmIgnition.current = fsmIgnition.sound;
			if (fsmIgnition.doSync)
			{
				using (Packet packet5 = new Packet(1))
				{
					if (fsmIgnition.user == 0L)
					{
						fsmIgnition.user = BeerMPGlobals.UserID;
						if (fsmIgnition.starter != null)
						{
							fsmIgnition.starter.user = BeerMPGlobals.UserID;
						}
					}
					fsmIgnition.Sound.Send(packet5);
				}
			}
		}, 0);
		accon = fsm.AddEvent("MP_ACCON");
		fsm.AddGlobalTransition(accon, "ACC on");
		FsmState state = fsm.GetState("ACC on");
		fsm.InsertAction("ACC on", delegate
		{
			fsmIgnition.current = fsmIgnition.accon;
			if (fsmIgnition.doSync && fsmIgnition.user == BeerMPGlobals.UserID)
			{
				using Packet packet4 = new Packet(1);
				fsmIgnition.AccOn.Send(packet4);
			}
			else
			{
				fsm.SendEvent("FINISHED");
			}
			fsmIgnition.doSync = true;
		}, state.Actions.Length - 1);
		shutoff = fsm.AddEvent("MP_SHUTOFF");
		fsm.AddGlobalTransition(shutoff, "Shut off");
		fsm.InsertAction("Shut off", delegate
		{
			fsmIgnition.current = fsmIgnition.shutoff;
			if (fsmIgnition.doSync && fsmIgnition.user == BeerMPGlobals.UserID)
			{
				using Packet packet3 = new Packet(1);
				fsmIgnition.Shutoff.Send(packet3);
			}
			fsmIgnition.doSync = true;
		}, 0);
		motoroff = fsm.AddEvent("MP_MOTOROFF");
		fsm.AddGlobalTransition(motoroff, "Motor OFF");
		fsm.InsertAction("Motor OFF", delegate
		{
			fsmIgnition.current = fsmIgnition.motoroff;
			if (fsmIgnition.doSync)
			{
				using Packet packet2 = new Packet(1);
				fsmIgnition.MotorOFF.Send(packet2);
			}
			fsmIgnition.user = 0uL;
			fsmIgnition.doSync = true;
		}, 0);
		motorstarting = fsm.AddEvent("MP_MOTORSTARTING");
		fsm.AddGlobalTransition(motorstarting, "Motor starting");
		FsmState state2 = fsm.GetState("Motor starting");
		fsm.InsertAction("Motor starting", delegate
		{
			fsmIgnition.current = fsmIgnition.motorstarting;
			if (fsmIgnition.doSync && fsmIgnition.user == BeerMPGlobals.UserID)
			{
				using Packet packet = new Packet(1);
				fsmIgnition.Motorstarting.Send(packet);
			}
			else
			{
				fsm.SendEvent(fsmIgnition.waitbutton.Name);
			}
			fsmIgnition.doSync = true;
		}, state2.Actions.Length - 1);
		waitbutton = fsm.AddEvent("MP_WAITBUTTON");
		fsm.AddGlobalTransition(waitbutton, "Wait button");
		Sound = NetEvent<FsmIgnition>.Register($"Sound{fsmVeh.netVehicle.hash}", OnSound);
		AccOn = NetEvent<FsmIgnition>.Register($"AccOn{fsmVeh.netVehicle.hash}", OnAccOn);
		Shutoff = NetEvent<FsmIgnition>.Register($"Shutoff{fsmVeh.netVehicle.hash}", OnShutoff);
		MotorOFF = NetEvent<FsmIgnition>.Register($"MotorOFF{fsmVeh.netVehicle.hash}", OnMotorOFF);
		Motorstarting = NetEvent<FsmIgnition>.Register($"Motorstarting{fsmVeh.netVehicle.hash}", OnMotorstarting);
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate
		{
			if (fsmIgnition.current != null)
			{
				fsmIgnition.doSync = true;
				fsm.SendEvent(fsmIgnition.current.Name);
			}
		};
	}

	private void OnMotorstarting(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			if (starter != null && starter.user == 0L)
			{
				starter.user = sender;
			}
			doSync = false;
			fsm.SendEvent(motorstarting.Name);
		}
	}

	private void OnMotorOFF(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			user = 0uL;
			doSync = false;
			fsm.SendEvent(motoroff.Name);
		}
	}

	private void OnShutoff(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			fsm.SendEvent(shutoff.Name);
		}
	}

	private void OnAccOn(ulong sender, Packet packet)
	{
		if (sender != BeerMPGlobals.UserID)
		{
			doSync = false;
			fsm.SendEvent(accon.Name);
		}
	}

	private void OnSound(ulong sender, Packet packet)
	{
		if (sender == BeerMPGlobals.UserID)
		{
			return;
		}
		if (user == 0L)
		{
			Console.Log("FsmIgnition & FsmStarter: " + SteamFriends.GetFriendPersonaName((CSteamID)sender) + " is new User!");
			user = sender;
			if (starter != null)
			{
				starter.user = sender;
			}
		}
		doSync = false;
		fsm.SendEvent(sound.Name);
	}
}
