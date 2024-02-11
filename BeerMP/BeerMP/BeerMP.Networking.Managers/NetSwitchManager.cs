using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetSwitchManager : MonoBehaviour
{
	public class Switch
	{
		public Vector3 switchPos;

		public PlayMakerFSM fsm;

		public FsmBool switchOn;

		public FsmEvent fsmEvent;

		public bool doSync = true;

		public static void Create(GameObject switchObject)
		{
			if (!switchObject.name.StartsWith("switch"))
			{
				return;
			}
			PlayMakerFSM playMaker = switchObject.GetPlayMaker("Use");
			if (playMaker == null)
			{
				return;
			}
			playMaker.Initialize();
			if (playMaker.FsmStates.Any((FsmState x) => x.Name == "Switch") && playMaker.FsmStates.Any((FsmState x) => x.Name == "Position"))
			{
				Switch @switch = new Switch();
				@switch.switchPos = switchObject.transform.position;
				@switch.fsm = playMaker;
				FsmEvent fsmEvent = (@switch.fsmEvent = playMaker.AddEvent("MP_UpdateSwitch"));
				if (!playMaker.FsmStates.Any((FsmState x) => x.Name == "Switch"))
				{
					@switch.switchOn = playMaker.FsmVariables.FindFsmBool("SwitchOn");
					playMaker.AddGlobalTransition(fsmEvent, "Position");
					playMaker.InsertAction("Position", new SwitchAction
					{
						sw = @switch
					}, 0);
				}
				else
				{
					@switch.switchOn = playMaker.FsmVariables.FindFsmBool("Switch");
					playMaker.AddGlobalTransition(fsmEvent, "Switch");
					playMaker.InsertAction("Switch", new SwitchAction
					{
						sw = @switch
					}, 0);
				}
				switches.Add(@switch);
			}
		}

		public void Toggle(bool on)
		{
			if (switchOn.Value != on)
			{
				doSync = false;
				fsm.Fsm.Event(fsmEvent);
			}
		}

		public void SyncStateInitial(ulong userId)
		{
			using Packet packet = new Packet(1);
			packet.Write(switchPos);
			packet.Write(switchOn.Value);
			NetEvent<NetSwitchManager>.Send("ToggleSwitch", packet, userId);
		}
	}

	public class SwitchAction : FsmStateAction
	{
		public Switch sw;

		public override void OnEnter()
		{
			using (Packet packet = new Packet(1))
			{
				packet.Write(sw.switchPos);
				packet.Write(!sw.switchOn.Value);
				if (sw.doSync)
				{
					NetEvent<NetSwitchManager>.Send("ToggleSwitch", packet);
				}
			}
			sw.doSync = true;
			Finish();
		}
	}

	public static NetSwitchManager Instance;

	internal static List<Switch> switches = new List<Switch>();

	private NetEvent<NetSwitchManager> ToggleSwitch;

	private void Start()
	{
		Instance = this;
		GameObject[] array = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
			where x.name.StartsWith("switch")
			select x).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			Switch.Create(array[i]);
		}
		ToggleSwitch = NetEvent<NetSwitchManager>.Register("ToggleSwitch", OnToggleSwitch);
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong sender)
		{
			if (BeerMPGlobals.IsHost)
			{
				foreach (Switch @switch in switches)
				{
					@switch.SyncStateInitial(sender);
				}
			}
		};
	}

	private void OnToggleSwitch(ulong userId, Packet packet)
	{
		if (userId != BeerMPGlobals.UserID)
		{
			Vector3 switchPos = packet.ReadVector3();
			bool on = packet.ReadBool();
			switches.First((Switch x) => x.switchPos == switchPos).Toggle(on);
		}
	}
}
