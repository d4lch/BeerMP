using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;

namespace BeerMP.Networking.Managers;

internal class FsmTurnSignals
{
	private PlayMakerFSM fsm;

	private int hash;

	private int updating = -1;

	private int current;

	private static readonly string[] eventNames = new string[3] { "MP_OFF", "MP_LEFT", "MP_RIGHT" };

	private static NetEvent<FsmTurnSignals> updateEvent;

	private static List<FsmTurnSignals> turnSignals = new List<FsmTurnSignals>();

	private static bool initSyncLoaded = false;

	public FsmTurnSignals(PlayMakerFSM fsm)
	{
		hash = fsm.transform.GetGameobjectHashString().GetHashCode();
		this.fsm = fsm;
		SetupFSM();
		if (updateEvent == null)
		{
			updateEvent = NetEvent<FsmTurnSignals>.Register("Update", OnUpdate);
		}
		if (!initSyncLoaded)
		{
			initSyncLoaded = true;
			BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong u)
			{
				if (BeerMPGlobals.IsHost)
				{
					for (int i = 0; i < turnSignals.Count; i++)
					{
						turnSignals[i].SendUpdate(turnSignals[i].current, u);
					}
				}
			};
		}
		turnSignals.Add(this);
		NetManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(NetManager.sceneLoaded, (Action<GameScene>)delegate
		{
			if (turnSignals.Contains(this))
			{
				turnSignals.Remove(this);
			}
		});
	}

	private void SetupFSM()
	{
		FsmState state = fsm.GetState("State 2");
		string toState = state.Transitions.First((FsmTransition t) => t.EventName == "LEFT").ToState;
		string toState2 = state.Transitions.First((FsmTransition t) => t.EventName == "RIGHT").ToState;
		fsm.InsertAction("State 3", delegate
		{
			SendUpdate(0);
		}, 0);
		fsm.AddGlobalTransition(fsm.AddEvent(eventNames[0]), "State 3");
		fsm.InsertAction(toState, delegate
		{
			SendUpdate(1);
		}, 0);
		fsm.AddGlobalTransition(fsm.AddEvent(eventNames[1]), toState);
		fsm.InsertAction(toState2, delegate
		{
			SendUpdate(2);
		}, 0);
		fsm.AddGlobalTransition(fsm.AddEvent(eventNames[2]), toState2);
	}

	private void SendUpdate(int i, ulong target = 0uL)
	{
		if (updating == i)
		{
			updating = -1;
			return;
		}
		using Packet packet = new Packet(1);
		packet.Write(hash);
		packet.Write((byte)i);
		current = i;
		if (target == 0L)
		{
			updateEvent.Send(packet);
		}
		else
		{
			updateEvent.Send(packet, target);
		}
	}

	private static void OnUpdate(ulong sender, Packet p)
	{
		int hash = p.ReadInt();
		FsmTurnSignals fsmTurnSignals = turnSignals.FirstOrDefault((FsmTurnSignals ts) => ts.hash == hash);
		if (fsmTurnSignals == null)
		{
			Console.LogError($"Received turn signal of hash {hash} update, but it does not exist");
			return;
		}
		int num = p.ReadByte();
		fsmTurnSignals.updating = (fsmTurnSignals.current = num);
		fsmTurnSignals.fsm.SendEvent(eventNames[num]);
	}
}
