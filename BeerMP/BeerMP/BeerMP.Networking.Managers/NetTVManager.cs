using System;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetTVManager : MonoBehaviour
{
	private PlayMakerFSM TVswitch;

	private FsmBool isOn;

	private void Start()
	{
		NetEvent<NetTVManager>.Register("On", OnTVToggle);
		TVswitch = GameObject.Find("YARD").transform.Find("Building/LIVINGROOM/TV/Switch").GetPlayMaker("Use");
		isOn = TVswitch.FsmVariables.FindFsmBool("Open");
		FsmEvent fsmEvent = TVswitch.AddEvent("MP_OPEN");
		Action<ulong, bool> a = delegate(ulong target, bool init)
		{
			using Packet packet = new Packet(1);
			packet.Write(init ? isOn.Value : (!isOn.Value));
			if (target == 0L)
			{
				NetEvent<NetTVManager>.Send("On", packet);
			}
			else
			{
				NetEvent<NetTVManager>.Send("On", packet, target);
			}
		};
		TVswitch.InsertAction("Switch", delegate
		{
			a(0uL, arg2: false);
		}, 0);
		TVswitch.AddGlobalTransition(fsmEvent, "State 5");
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			a(user, arg2: true);
		};
	}

	private void OnTVToggle(ulong sender, Packet packet)
	{
		bool flag = packet.ReadBool();
		TVswitch.SendEvent(flag ? "MP_OPEN" : "GLOBALEVENT");
	}
}
