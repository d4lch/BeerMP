using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;

namespace BeerMP.Networking.Managers;

internal class FsmDashboardKnob
{
	private PlayMakerFSM fsm;

	private FsmFloat targetValue;

	private FsmEvent update;

	private float add;

	private int hash;

	private bool updating;

	private static NetEvent<FsmDashboardKnob> updateEvent;

	private static List<FsmDashboardKnob> knobs = new List<FsmDashboardKnob>();

	private static bool initSyncLoaded = false;

	public FsmDashboardKnob(PlayMakerFSM fsm)
	{
		this.fsm = fsm;
		hash = fsm.transform.GetGameobjectHashString().GetHashCode();
		SetupFSM();
		if (updateEvent == null)
		{
			updateEvent = NetEvent<FsmDashboardKnob>.Register("Twist", OnKnobUpdate);
		}
		if (!initSyncLoaded)
		{
			initSyncLoaded = true;
			BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate
			{
				if (BeerMPGlobals.IsHost)
				{
					for (int i = 0; i < knobs.Count; i++)
					{
						knobs[i].SendKnobUpdate();
					}
				}
			};
		}
		knobs.Add(this);
		NetManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(NetManager.sceneLoaded, (Action<GameScene>)delegate
		{
			if (knobs.Contains(this))
			{
				knobs.Remove(this);
			}
		});
	}

	private void SetupFSM()
	{
		fsm.Initialize();
		try
		{
			FsmState state = fsm.GetState("Increase");
			string toState = state.Transitions[0].ToState;
			for (int i = 0; i < state.Actions.Length; i++)
			{
				if (state.Actions[i] is FloatAdd floatAdd)
				{
					targetValue = floatAdd.floatVariable;
					add = floatAdd.add.Value;
					break;
				}
			}
			update = fsm.AddEvent("MP_UPDATE");
			fsm.AddGlobalTransition(update, "Increase");
			fsm.InsertAction(toState, SendKnobUpdate, 0);
		}
		catch (Exception ex)
		{
			Console.LogError($"Failed to setup dashboard knob {hash} ({fsm.transform.GetGameobjectHashString()}): {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
		}
	}

	private static void OnKnobUpdate(ulong sender, Packet packet)
	{
		if (NetRadioManager.radioLoaded)
		{
			int hash = packet.ReadInt();
			float num = packet.ReadFloat();
			FsmDashboardKnob fsmDashboardKnob = knobs.FirstOrDefault((FsmDashboardKnob b) => b.hash == hash);
			if (fsmDashboardKnob == null)
			{
				Console.LogError($"Received dashboard knob triggered action from {NetManager.playerNames[(CSteamID)sender]} but the hash {hash} cannot be found");
				return;
			}
			fsmDashboardKnob.updating = true;
			fsmDashboardKnob.targetValue.Value = num - fsmDashboardKnob.add;
			fsmDashboardKnob.fsm.Fsm.Event(fsmDashboardKnob.update);
		}
	}

	private void SendKnobUpdate()
	{
		if (!NetRadioManager.radioLoaded)
		{
			return;
		}
		if (updating)
		{
			updating = false;
			return;
		}
		using Packet packet = new Packet(1);
		packet.Write(hash);
		packet.Write(targetValue.Value);
		updateEvent.Send(packet);
	}
}
