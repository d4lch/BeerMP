using System;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetFloorJackManager : MonoBehaviour
{
	private FsmFloat y;

	private PlayMakerFSM usageFsm;

	private bool receivedJackEvent;

	private void Start()
	{
		NetEvent<NetFloorJackManager>.Register("Move", OnMove);
		Transform transform = GameObject.Find("ITEMS").transform.Find("floor jack(itemx)");
		usageFsm = transform.Find("Trigger").GetPlayMaker("Use");
		y = usageFsm.FsmVariables.FindFsmFloat("Y");
		Action<bool> move = delegate(bool isUp)
		{
			if (receivedJackEvent)
			{
				receivedJackEvent = false;
				return;
			}
			using Packet packet2 = new Packet(1);
			packet2.Write(isUp);
			if (isUp)
			{
				packet2.Write(y.Value);
			}
			NetEvent<NetFloorJackManager>.Send("Move", packet2);
		};
		usageFsm.InsertAction("Up", delegate
		{
			move(obj: true);
		}, 0);
		usageFsm.InsertAction("Down", delegate
		{
			move(obj: false);
		}, 0);
		usageFsm.AddGlobalTransition(usageFsm.FsmEvents.First((FsmEvent e) => e.Name == "LIFT UP"), "Up");
		usageFsm.AddGlobalTransition(usageFsm.FsmEvents.First((FsmEvent e) => e.Name == "LIFT DOWN"), "Down");
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			if (y.Value != 0f)
			{
				using (Packet packet = new Packet(1))
				{
					packet.Write(_value: true);
					packet.Write(y.Value);
					NetEvent<NetFloorJackManager>.Send("Move", packet, user);
				}
			}
		};
	}

	private void OnMove(ulong sender, Packet packet)
	{
		receivedJackEvent = true;
		bool flag;
		if (flag = packet.ReadBool())
		{
			y.Value = packet.ReadFloat();
		}
		usageFsm.SendEvent("LIFT " + (flag ? "UP" : "DOWN"));
	}
}
