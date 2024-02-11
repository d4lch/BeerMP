using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Helpers;

internal static class PlayMakerUtilities
{
	internal class PM_Hook : FsmStateAction
	{
		public Action action;

		public bool everyFrame;

		public PM_Hook(Action action, bool everyFrame = false)
		{
			this.action = action;
			this.everyFrame = everyFrame;
		}

		public override void OnEnter()
		{
			action?.Invoke();
			if (!everyFrame)
			{
				Finish();
			}
		}

		public override void OnUpdate()
		{
			if (everyFrame)
			{
				action?.Invoke();
			}
		}
	}

	public static PlayMakerFSM GetPlayMaker(this GameObject go, string fsmName)
	{
		return go.GetComponentsInChildren<PlayMakerFSM>(includeInactive: true).FirstOrDefault((PlayMakerFSM x) => x.FsmName == fsmName && x.transform == go.transform);
	}

	public static PlayMakerFSM GetPlayMaker(this Transform tf, string fsmName)
	{
		return tf.gameObject.GetPlayMaker(fsmName);
	}

	public static void Initialize(this PlayMakerFSM fsm)
	{
		fsm.Fsm.InitData();
	}

	public static FsmEvent AddEvent(this PlayMakerFSM fsm, string eventName)
	{
		fsm.Fsm.InitData();
		FsmEvent fsmEvent = FsmEvent.GetFsmEvent(eventName);
		if (fsmEvent == null)
		{
			fsmEvent = new FsmEvent(eventName);
		}
		List<FsmEvent> list = fsm.FsmEvents.ToList();
		list.Add(fsmEvent);
		fsm.Fsm.Events = list.ToArray();
		fsm.Fsm.InitData();
		return fsmEvent;
	}

	public static void AddGlobalTransition(this PlayMakerFSM pm, FsmEvent fsmEvent, string stateName)
	{
		pm.Fsm.InitData();
		List<FsmTransition> list = pm.Fsm.GlobalTransitions.ToList();
		list.Add(new FsmTransition
		{
			FsmEvent = fsmEvent,
			ToState = stateName
		});
		pm.Fsm.GlobalTransitions = list.ToArray();
		pm.Fsm.InitData();
	}

	public static bool HasState(this PlayMakerFSM pm, string stateName)
	{
		return pm.FsmStates.Any((FsmState x) => x.Name == stateName);
	}

	public static FsmState GetState(this PlayMakerFSM pm, string stateName)
	{
		return pm.FsmStates.FirstOrDefault((FsmState x) => x.Name == stateName);
	}

	public static void InsertAction<T>(this PlayMakerFSM pm, string stateName, T action, int index = -1) where T : FsmStateAction
	{
		FsmState state = pm.GetState(stateName);
		if (state == null)
		{
			Console.Log("InsertAction(FSM, string, T, int): The state of name " + stateName + " is null on fsm " + pm.FsmName + " on object " + pm.gameObject.name);
		}
		else
		{
			List<FsmStateAction> list = state.Actions.ToList();
			if (index == -1)
			{
				list.Add(action);
			}
			else
			{
				list.Insert(index, action);
			}
			state.Actions = list.ToArray();
		}
	}

	public static void InsertAction(this PlayMakerFSM pm, string stateName, Action action, int index = -1)
	{
		pm.InsertAction(stateName, new PM_Hook(action), index);
	}
}
