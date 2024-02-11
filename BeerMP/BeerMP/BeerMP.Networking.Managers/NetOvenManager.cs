using System;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetOvenManager : MonoBehaviour
{
	private FsmFloat[] knobData;

	private FsmFloat[] knobRot;

	private FsmFloat[] hotplateTemps;

	private Transform[] knobMesh;

	private const string KnobTurnEvent = "KnobTurn";

	private const string SimSyncEvent = "SimSync";

	private float stoveSimulationSyncTime = 10f;

	private void Start()
	{
		NetEvent<NetOvenManager>.Register("KnobTurn", OnKnobTurn);
		NetEvent<NetOvenManager>.Register("SimSync", OnSimSync);
		knobData = new FsmFloat[4];
		knobRot = new FsmFloat[4];
		hotplateTemps = new FsmFloat[4];
		knobMesh = new Transform[4];
		Action<ulong>[] knobSyncs = new Action<ulong>[4];
		Transform transform = GameObject.Find("YARD").transform.Find("Building/KITCHEN/OvenStove");
		for (int i = 0; i < 4; i++)
		{
			PlayMakerFSM playMaker = transform.Find("KnobPower" + (i + 1)).GetPlayMaker("Screw");
			FsmFloat data = playMaker.FsmVariables.FindFsmFloat("Data");
			FsmFloat fsmFloat = playMaker.FsmVariables.FindFsmFloat("Rot");
			Transform mesh = playMaker.FsmVariables.FindFsmGameObject("Mesh").Value.transform;
			knobData[i] = data;
			knobRot[i] = fsmFloat;
			knobMesh[i] = mesh;
			Action<ulong> a = delegate(ulong target)
			{
				using Packet packet = new Packet(1);
				int value = 0;
				for (int l = 0; l < 4; l++)
				{
					if (knobMesh[l] == mesh)
					{
						value = l;
						break;
					}
				}
				packet.Write(value);
				packet.Write(data.Value);
				if (target == 0L)
				{
					NetEvent<NetOvenManager>.Send("KnobTurn", packet);
				}
				else
				{
					NetEvent<NetOvenManager>.Send("KnobTurn", packet, target);
				}
			};
			playMaker.InsertAction("State 1", delegate
			{
				a(0uL);
			});
			knobSyncs[i] = a;
		}
		PlayMakerFSM playMaker2 = GameObject.Find("YARD").transform.Find("Building/KITCHEN/OvenStove/Simulation").GetPlayMaker("Data");
		for (int j = 0; j < 4; j++)
		{
			hotplateTemps[j] = playMaker2.FsmVariables.FindFsmFloat($"HotPlate{j + 1}Heat");
		}
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			if (BeerMPGlobals.IsHost)
			{
				for (int k = 0; k < 4; k++)
				{
					knobSyncs[k](user);
				}
				SyncSim(user);
			}
		};
	}

	private void Update()
	{
		if (BeerMPGlobals.IsHost)
		{
			stoveSimulationSyncTime += Time.deltaTime;
			if (stoveSimulationSyncTime >= 10f)
			{
				stoveSimulationSyncTime = 0f;
				SyncSim();
			}
		}
	}

	private void SyncSim(ulong target = 0uL)
	{
		using Packet packet = new Packet(1);
		for (int i = 0; i < 4; i++)
		{
			packet.Write(hotplateTemps[i].Value);
		}
		if (target == 0L)
		{
			NetEvent<NetOvenManager>.Send("SimSync", packet);
		}
		else
		{
			NetEvent<NetOvenManager>.Send("SimSync", packet, target);
		}
	}

	private void OnSimSync(ulong sender, Packet packet)
	{
		for (int i = 0; i < hotplateTemps.Length; i++)
		{
			hotplateTemps[i].Value = packet.ReadFloat();
		}
	}

	private void OnKnobTurn(ulong sender, Packet packet)
	{
		int num = packet.ReadInt();
		float num2 = packet.ReadFloat();
		if (num <= 4)
		{
			FsmFloat obj = knobData[num];
			float value = (knobRot[num].Value = num2);
			obj.Value = value;
			knobMesh[num].localEulerAngles = Vector3.up * num2;
		}
	}
}
