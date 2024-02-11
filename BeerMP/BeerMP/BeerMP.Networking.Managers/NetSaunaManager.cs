using System;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetSaunaManager : MonoBehaviour
{
	private FsmFloat powerRot;

	private FsmFloat maxHeat;

	private FsmFloat timerRot;

	private FsmFloat timerMath1;

	private FsmFloat simMaxHeat;

	private FsmFloat simTimer;

	private FsmFloat simMaxSaunaHeat;

	private FsmFloat simSaunaHeat;

	private FsmFloat simStoveHeat;

	private FsmFloat simCoolingSauna;

	private Transform powerKnobMesh;

	private Transform timerKnobMesh;

	private PlayMakerFSM stoveTrigger;

	private float saunaSimSyncTime = 10f;

	private bool receivedSteamEvent;

	private void Start()
	{
		NetEvent<NetSaunaManager>.Register("Knob", OnKnobScrew);
		NetEvent<NetSaunaManager>.Register("SimSync", OnSimSync);
		NetEvent<NetSaunaManager>.Register("Steam", OnSteam);
		Transform transform = GameObject.Find("YARD").transform.Find("Building/SAUNA/Sauna");
		PlayMakerFSM playMaker = transform.Find("Simulation").GetPlayMaker("Time");
		simMaxHeat = playMaker.FsmVariables.FindFsmFloat("MaxHeat");
		simTimer = playMaker.FsmVariables.FindFsmFloat("Time");
		simMaxSaunaHeat = playMaker.FsmVariables.FindFsmFloat("MaxSaunaHeat");
		simSaunaHeat = playMaker.FsmVariables.FindFsmFloat("SaunaHeat");
		simStoveHeat = playMaker.FsmVariables.FindFsmFloat("StoveHeat");
		simCoolingSauna = playMaker.FsmVariables.FindFsmFloat("CoolingSauna");
		PlayMakerFSM playMaker2 = transform.Find("Kiuas/ButtonPower").GetPlayMaker("Screw");
		powerRot = playMaker2.FsmVariables.FindFsmFloat("Rot");
		maxHeat = playMaker2.FsmVariables.FindFsmFloat("MaxHeat");
		powerKnobMesh = playMaker2.FsmVariables.FindFsmGameObject("CapMesh").Value.transform;
		Action<ulong> a = delegate(ulong target)
		{
			using Packet packet3 = new Packet(1);
			packet3.Write(_value: false);
			packet3.Write(powerRot.Value);
			if (target == 0L)
			{
				NetEvent<NetSaunaManager>.Send("Knob", packet3);
			}
			else
			{
				NetEvent<NetSaunaManager>.Send("Knob", packet3, target);
			}
		};
		playMaker2.InsertAction("Wait", delegate
		{
			a(0uL);
		});
		PlayMakerFSM playMaker3 = transform.Find("Kiuas/ButtonTime").GetPlayMaker("Screw");
		timerRot = playMaker3.FsmVariables.FindFsmFloat("Timer");
		timerMath1 = playMaker3.FsmVariables.FindFsmFloat("Math1");
		timerKnobMesh = playMaker3.FsmVariables.FindFsmGameObject("CapMesh").Value.transform;
		Action<ulong> b = delegate(ulong target)
		{
			using Packet packet2 = new Packet(1);
			packet2.Write(_value: true);
			packet2.Write(timerRot.Value);
			if (target == 0L)
			{
				NetEvent<NetSaunaManager>.Send("Knob", packet2);
			}
			else
			{
				NetEvent<NetSaunaManager>.Send("Knob", packet2, target);
			}
		};
		playMaker3.InsertAction("Wait", delegate
		{
			b(0uL);
		});
		stoveTrigger = transform.Find("Kiuas/StoveTrigger").GetPlayMaker("Steam");
		stoveTrigger.InsertAction("Calc blur", delegate
		{
			if (receivedSteamEvent)
			{
				receivedSteamEvent = false;
				return;
			}
			using Packet packet = new Packet(1);
			NetEvent<NetSaunaManager>.Send("Steam", packet);
		});
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			a(user);
			b(user);
			SyncSim(user);
		};
	}

	private void Update()
	{
		if (BeerMPGlobals.IsHost)
		{
			saunaSimSyncTime += Time.deltaTime;
			if (saunaSimSyncTime >= 10f)
			{
				saunaSimSyncTime = 0f;
				SyncSim();
			}
		}
	}

	private void SyncSim(ulong target = 0uL)
	{
		using Packet packet = new Packet(1);
		packet.Write(simMaxSaunaHeat.Value);
		packet.Write(simSaunaHeat.Value);
		packet.Write(simStoveHeat.Value);
		packet.Write(simCoolingSauna.Value);
		if (target == 0L)
		{
			NetEvent<NetSaunaManager>.Send("SimSync", packet);
		}
		else
		{
			NetEvent<NetSaunaManager>.Send("SimSync", packet, target);
		}
	}

	private void OnSteam(ulong sender, Packet packet)
	{
		receivedSteamEvent = true;
		stoveTrigger.SendEvent("GLOBALEVENT");
	}

	private void OnSimSync(ulong sender, Packet packet)
	{
		simMaxSaunaHeat.Value = packet.ReadFloat();
		simSaunaHeat.Value = packet.ReadFloat();
		simStoveHeat.Value = packet.ReadFloat();
		simCoolingSauna.Value = packet.ReadFloat();
	}

	private void OnKnobScrew(ulong sender, Packet packet)
	{
		bool flag = packet.ReadBool();
		float num = packet.ReadFloat();
		if (flag)
		{
			timerRot.Value = num;
			FsmFloat fsmFloat = timerMath1;
			float value = (simTimer.Value = num * 6f);
			fsmFloat.Value = value;
			timerKnobMesh.localEulerAngles = Vector3.up * num;
		}
		else
		{
			powerRot.Value = num;
			FsmFloat fsmFloat2 = maxHeat;
			float value = (simMaxHeat.Value = num / 300f);
			fsmFloat2.Value = value;
			powerKnobMesh.localEulerAngles = Vector3.up * num;
		}
		MasterAudio.PlaySound3DAndForget("HouseFoley", flag ? timerKnobMesh : powerKnobMesh, attachToSource: false, 1f, null, 0f, "sauna_stove_knob");
	}
}
