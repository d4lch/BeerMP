using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetWaterSourceManager : MonoBehaviour
{
	private class WaterTap
	{
		public PlayMakerFSM fsm;

		public FsmBool tapOn;
	}

	private class Shower
	{
		public PlayMakerFSM valve;

		public PlayMakerFSM showerSwitch;

		public FsmBool tapOn;

		public FsmBool showerOn;
	}

	private class WaterWell
	{
		public PlayMakerFSM fsm;

		public bool receivedWellEvent;
	}

	private List<WaterTap> waterTaps = new List<WaterTap>();

	private List<Shower> showers = new List<Shower>();

	private List<WaterWell> wells = new List<WaterWell>();

	private void Start()
	{
		NetEvent<NetWaterSourceManager>.Register("Tap", OnWaterTap);
		NetEvent<NetWaterSourceManager>.Register("Shower", OnShower);
		NetEvent<NetWaterSourceManager>.Register("Well", OnWaterWell);
		List<PlayMakerFSM> list = (from x in Resources.FindObjectsOfTypeAll<PlayMakerFSM>()
			where x.FsmName == "Use"
			select x).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			PlayMakerFSM fsm = list[i];
			if (fsm.transform.parent == null)
			{
				continue;
			}
			if (fsm.transform.parent.name == "KitchenWaterTap")
			{
				FsmBool tapOn2 = fsm.FsmVariables.FindFsmBool("SwitchOn");
				FsmEvent fsmEvent = fsm.AddEvent("MP_ON");
				fsm.AddGlobalTransition(fsmEvent, "ON");
				FsmEvent fsmEvent2 = fsm.AddEvent("MP_OFF");
				fsm.AddGlobalTransition(fsmEvent2, "OFF");
				fsm.InsertAction("Position", delegate
				{
					using Packet packet6 = new Packet(1);
					packet6.Write(fsm.transform.position.GetHashCode());
					packet6.Write(!tapOn2.Value);
					NetEvent<NetWaterSourceManager>.Send("Tap", packet6);
				}, 0);
				waterTaps.Add(new WaterTap
				{
					fsm = fsm,
					tapOn = tapOn2
				});
			}
			else if (fsm.transform.parent.name == "Shower")
			{
				PlayMakerFSM playMaker = fsm.transform.parent.Find("Valve").GetPlayMaker("Switch");
				FsmBool showerSwitch = fsm.FsmVariables.FindFsmBool("ShowerSwitch");
				FsmEvent fsmEvent3 = fsm.AddEvent("MP_ON");
				fsm.AddGlobalTransition(fsmEvent3, "Shower");
				FsmEvent fsmEvent4 = fsm.AddEvent("MP_OFF");
				fsm.AddGlobalTransition(fsmEvent4, "State 1");
				fsm.InsertAction("Position", delegate
				{
					using Packet packet5 = new Packet(1);
					packet5.Write(fsm.transform.position.GetHashCode());
					packet5.Write(_value: true);
					packet5.Write(!showerSwitch.Value);
					NetEvent<NetWaterSourceManager>.Send("Shower", packet5);
				}, 0);
				FsmBool tapOn = playMaker.FsmVariables.FindFsmBool("Valve");
				fsmEvent3 = playMaker.AddEvent("MP_ON");
				playMaker.AddGlobalTransition(fsmEvent3, "ON");
				fsmEvent4 = playMaker.AddEvent("MP_OFF");
				playMaker.AddGlobalTransition(fsmEvent4, "OFF");
				playMaker.InsertAction("Position", delegate
				{
					using Packet packet4 = new Packet(1);
					packet4.Write(fsm.transform.position.GetHashCode());
					packet4.Write(!tapOn.Value);
					packet4.Write(_value: false);
					NetEvent<NetWaterSourceManager>.Send("Shower", packet4);
				}, 0);
				showers.Add(new Shower
				{
					valve = playMaker,
					showerSwitch = fsm,
					tapOn = tapOn,
					showerOn = showerSwitch
				});
			}
			else
			{
				if (!(fsm.transform.name == "Trigger"))
				{
					continue;
				}
				bool flag = false;
				Transform parent = fsm.transform;
				while (parent.parent != null)
				{
					if (!(parent.name == "WaterWell"))
					{
						parent = parent.parent;
						continue;
					}
					flag = true;
					break;
				}
				if (!flag)
				{
					continue;
				}
				FsmEvent fsmEvent5 = fsm.AddEvent("MP_USE");
				WaterWell well = new WaterWell
				{
					fsm = fsm
				};
				fsm.AddGlobalTransition(fsmEvent5, "Move lever");
				fsm.InsertAction("Move lever", delegate
				{
					if (well.receivedWellEvent)
					{
						well.receivedWellEvent = false;
						return;
					}
					using Packet packet3 = new Packet(1);
					packet3.Write(fsm.transform.position.GetHashCode());
					NetEvent<NetWaterSourceManager>.Send("Well", packet3);
				}, 0);
				wells.Add(well);
			}
		}
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate
		{
			if (BeerMPGlobals.IsHost)
			{
				for (int j = 0; j < waterTaps.Count; j++)
				{
					using Packet packet = new Packet(1);
					packet.Write(waterTaps[j].fsm.transform.position.GetHashCode());
					packet.Write(waterTaps[j].tapOn.Value);
					NetEvent<NetWaterSourceManager>.Send("Tap", packet);
				}
				for (int k = 0; k < showers.Count; k++)
				{
					using Packet packet2 = new Packet(1);
					packet2.Write(showers[k].showerSwitch.transform.position.GetHashCode());
					packet2.Write(showers[k].tapOn.Value);
					packet2.Write(showers[k].showerOn.Value);
					NetEvent<NetWaterSourceManager>.Send("Shower", packet2);
				}
			}
		};
	}

	private void OnWaterTap(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		bool flag = packet.ReadBool();
		WaterTap waterTap = waterTaps.FirstOrDefault((WaterTap t) => t.fsm.transform.position.GetHashCode() == hash);
		if (waterTap != null)
		{
			bool num = waterTap.tapOn.Value != flag;
			waterTap.tapOn.Value = flag;
			if (num)
			{
				waterTap.fsm.SendEvent(flag ? "MP_ON" : "MP_OFF");
			}
		}
	}

	private void OnShower(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		bool flag = packet.ReadBool();
		bool flag2 = packet.ReadBool();
		Shower shower = showers.FirstOrDefault((Shower t) => t.showerSwitch.transform.position.GetHashCode() == hash);
		if (shower != null)
		{
			bool num = shower.tapOn.Value != flag;
			shower.tapOn.Value = flag;
			if (num)
			{
				shower.valve.SendEvent(flag ? "MP_ON" : "MP_OFF");
			}
			bool num2 = shower.showerOn.Value != flag2;
			shower.showerOn.Value = flag2;
			if (num2)
			{
				shower.showerSwitch.SendEvent(flag2 ? "MP_ON" : "MP_OFF");
			}
		}
	}

	private void OnWaterWell(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		WaterWell waterWell = wells.FirstOrDefault((WaterWell f) => f.fsm.transform.position.GetHashCode() == hash);
		if (waterWell != null)
		{
			waterWell.receivedWellEvent = true;
			waterWell.fsm.SendEvent("MP_USE");
		}
	}
}
