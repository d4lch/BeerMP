using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using Steamworks;

namespace BeerMP.Networking.Managers;

internal class FsmDashboardButton
{
	private PlayMakerFSM fsm;

	private string[] actionEvents;

	private int hash;

	private int updatingAction = -1;

	private int currentAction = 255;

	private static NetEvent<FsmDashboardButton> knobToggleEvent;

	private static List<FsmDashboardButton> dashboardButtons = new List<FsmDashboardButton>();

	private static bool initSyncLoaded = false;

	public FsmDashboardButton(PlayMakerFSM fsm)
	{
		this.fsm = fsm;
		hash = fsm.transform.GetGameobjectHashString().GetHashCode();
		SetupFSM();
		if (knobToggleEvent == null)
		{
			knobToggleEvent = NetEvent<FsmDashboardButton>.Register("Toggle", OnTriggeredAction);
		}
		if (!initSyncLoaded)
		{
			initSyncLoaded = true;
			BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong u)
			{
				if (BeerMPGlobals.IsHost)
				{
					for (int i = 0; i < dashboardButtons.Count; i++)
					{
						int index = dashboardButtons[i].currentAction;
						dashboardButtons[i].TriggeredAction(index, u);
					}
				}
			};
		}
		dashboardButtons.Add(this);
		NetManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(NetManager.sceneLoaded, (Action<GameScene>)delegate
		{
			if (dashboardButtons.Contains(this))
			{
				dashboardButtons.Remove(this);
			}
		});
	}

	private void SetupFSM()
	{
		FsmState fsmState = fsm.FsmStates.FirstOrDefault((FsmState s) => s.Name.Contains("Test"));
		actionEvents = new string[fsmState.Transitions.Length];
		for (int i = 0; i < actionEvents.Length; i++)
		{
			string text = "MP_" + fsmState.Transitions[i].EventName;
			actionEvents[i] = text;
			FsmEvent fsmEvent = fsm.AddEvent(text);
			fsm.AddGlobalTransition(fsmEvent, fsmState.Transitions[i].ToState);
			int index = i;
			fsm.InsertAction(fsmState.Transitions[i].ToState, delegate
			{
				TriggeredAction(index);
			}, 0);
		}
	}

	private void TriggeredAction(int index, ulong target = 0uL)
	{
		if (updatingAction == index)
		{
			updatingAction = -1;
			return;
		}
		using Packet packet = new Packet(1);
		packet.Write(hash);
		packet.Write((byte)index);
		currentAction = index;
		if (target == 0L)
		{
			knobToggleEvent.Send(packet);
		}
		else
		{
			knobToggleEvent.Send(packet, target);
		}
	}

	private static void OnTriggeredAction(ulong sender, Packet p)
	{
		int hash = p.ReadInt();
		FsmDashboardButton fsmDashboardButton = dashboardButtons.FirstOrDefault((FsmDashboardButton b) => b.hash == hash);
		if (fsmDashboardButton == null)
		{
			Console.LogError($"Received dashboard button triggered action from {NetManager.playerNames[(CSteamID)sender]} but the hash {hash} cannot be found");
			return;
		}
		int num = p.ReadByte();
		if (num != 255)
		{
			fsmDashboardButton.updatingAction = (fsmDashboardButton.currentAction = num);
			fsmDashboardButton.fsm.SendEvent(fsmDashboardButton.actionEvents[num]);
		}
	}
}
