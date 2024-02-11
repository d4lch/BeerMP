using System;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetMotorHoistManager : MonoBehaviour
{
	private FsmFloat angle;

	private Transform motorHoistArm;

	private Animation handle;

	private PlayMakerFSM usageFsm;

	private bool isHoistMoving;

	private ulong hoistOwner;

	private void Start()
	{
		NetEvent<NetMotorHoistManager>.Register("Init", OnInitSync);
		NetEvent<NetMotorHoistManager>.Register("BeginMove", OnBeginMovement);
		NetEvent<NetMotorHoistManager>.Register("EndMove", OnEndMovement);
		Transform transform = GameObject.Find("ITEMS").transform.Find("motor hoist(itemx)");
		motorHoistArm = transform.Find("motorhoist_arm");
		handle = transform.Find("Pump/HandlePivot").GetComponent<Animation>();
		usageFsm = transform.Find("Pump/Trigger").GetPlayMaker("Usage");
		angle = usageFsm.FsmVariables.FindFsmFloat("Angle");
		Action<bool> beginMove = delegate(bool isUp)
		{
			using Packet packet3 = new Packet(1);
			packet3.Write(isUp);
			packet3.Write(angle.Value);
			NetEvent<NetMotorHoistManager>.Send("BeginMove", packet3);
		};
		usageFsm.InsertAction("Up", delegate
		{
			beginMove(obj: true);
		});
		usageFsm.InsertAction("Down", delegate
		{
			beginMove(obj: false);
		});
		usageFsm.InsertAction("State 1", delegate
		{
			using Packet packet2 = new Packet(1);
			packet2.Write(angle.Value);
			NetEvent<NetMotorHoistManager>.Send("EndMove", packet2);
		});
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			using Packet packet = new Packet(1);
			packet.Write(angle.Value);
			NetEvent<NetMotorHoistManager>.Send("Init", packet, user);
		};
		BeerMPGlobals.OnMemberExit += (Action<ulong>)delegate(ulong user)
		{
			if (hoistOwner == user)
			{
				OnEndMovement(user, angle.Value);
			}
		};
	}

	private void OnInitSync(ulong sender, Packet packet)
	{
		float num = packet.ReadFloat();
		angle.Value = num;
		motorHoistArm.localEulerAngles = Vector3.right * num;
	}

	private void OnBeginMovement(ulong sender, Packet packet)
	{
		bool num = packet.ReadBool();
		float num2 = packet.ReadFloat();
		angle.Value = num2;
		motorHoistArm.localEulerAngles = Vector3.right * num2;
		handle.Play("motor_hoist_pump_down", PlayMode.StopAll);
		usageFsm.enabled = false;
		hoistOwner = sender;
		MasterAudio.PlaySound3DAndForget("HouseFoley", usageFsm.transform, attachToSource: false, 1f, null, 0f, "carjack1");
		if (num)
		{
			isHoistMoving = true;
		}
	}

	private void OnEndMovement(ulong sender, Packet packet)
	{
		OnEndMovement(sender, packet.ReadFloat());
	}

	private void OnEndMovement(ulong sender, float ang)
	{
		if (sender == hoistOwner || hoistOwner == 0L)
		{
			angle.Value = ang;
			motorHoistArm.localEulerAngles = Vector3.right * ang;
			handle.Play("motor_hoist_pump_up", PlayMode.StopAll);
			usageFsm.enabled = true;
			hoistOwner = 0uL;
			isHoistMoving = false;
		}
	}

	private void Update()
	{
		if (isHoistMoving)
		{
			float num = angle.Value + 0.07f;
			angle.Value = num;
			motorHoistArm.localEulerAngles = Vector3.right * num;
		}
	}
}
