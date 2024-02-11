using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;

namespace BeerMP.Networking.Managers;

internal class FsmDashboardLever
{
	private PlayMakerFSM fsm;

	private FsmFloat knobPos;

	private FsmFloat multiplyResult;

	private Transform mesh;

	private int axis;

	private int hash;

	private ulong owner;

	private float moveRate;

	private float minMove;

	private float maxMove;

	private bool movePerSecond;

	private bool moveDirection;

	private float multiplyRate;

	private float minmultiply;

	private float maxmultiply;

	private static NetEvent<FsmDashboardLever> updateEvent;

	private static NetEvent<FsmDashboardLever> initSync;

	private static List<FsmDashboardLever> levers = new List<FsmDashboardLever>();

	public FsmDashboardLever(PlayMakerFSM fsm)
	{
		hash = fsm.transform.GetGameobjectHashString().GetHashCode();
		this.fsm = fsm;
		SetupFSM();
		if (updateEvent == null)
		{
			updateEvent = NetEvent<FsmDashboardLever>.Register("Update", OnLeverUpdate);
		}
		if (initSync == null)
		{
			int hash;
			initSync = NetEvent<FsmDashboardLever>.Register("InitSync", delegate(ulong s, Packet p)
			{
				while (true)
				{
					if (p.UnreadLength() <= 0)
					{
						return;
					}
					hash = p.ReadInt();
					ulong num = (ulong)p.ReadLong();
					float num2 = p.ReadFloat();
					FsmDashboardLever fsmDashboardLever = levers.FirstOrDefault((FsmDashboardLever l) => l.hash == hash);
					if (fsmDashboardLever == null)
					{
						break;
					}
					fsmDashboardLever.owner = num;
					fsmDashboardLever.SetKnobPos(num2);
				}
				Console.LogError($"Received dashboard lever init sync from {NetManager.playerNames[(CSteamID)s]} but the hash {hash} cannot be found");
			});
			BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong u)
			{
				if (!BeerMPGlobals.IsHost)
				{
					return;
				}
				using Packet packet = new Packet(0);
				for (int i = 0; i < levers.Count; i++)
				{
					packet.Write(levers[i].hash);
					packet.Write((long)levers[i].owner);
					packet.Write(levers[i].knobPos.Value);
				}
				initSync.Send(packet, u);
			};
		}
		BeerMPGlobals.OnMemberExit += (Action<ulong>)delegate(ulong user)
		{
			if (owner == user)
			{
				StopMoving(null);
			}
		};
		levers.Add(this);
		NetManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(NetManager.sceneLoaded, (Action<GameScene>)delegate
		{
			if (levers.Contains(this))
			{
				levers.Remove(this);
			}
		});
	}

	private static void OnLeverUpdate(ulong sender, Packet packet)
	{
		int hash = packet.ReadInt();
		FsmDashboardLever fsmDashboardLever = levers.FirstOrDefault((FsmDashboardLever l) => l.hash == hash);
		if (fsmDashboardLever == null)
		{
			Console.LogError($"Received dashboard lever triggered action from {NetManager.playerNames[(CSteamID)sender]} but the hash {hash} cannot be found");
			return;
		}
		bool num = packet.ReadBool();
		bool direction = packet.ReadBool();
		float value = packet.ReadFloat();
		if (num)
		{
			fsmDashboardLever.StartMoving(direction, sender);
		}
		else
		{
			fsmDashboardLever.StopMoving(value);
		}
	}

	public void Update()
	{
		if (owner != 0L && owner != BeerMPGlobals.UserID)
		{
			float value = knobPos.Value;
			value += (moveDirection ? moveRate : (0f - moveRate)) * (movePerSecond ? Time.deltaTime : 1f);
			value = Mathf.Clamp(value, minMove, maxMove);
			SetKnobPos(value);
		}
		else if (owner == BeerMPGlobals.UserID && !Input.GetMouseButton(0) && !Input.GetMouseButton(1))
		{
			Move(null);
		}
	}

	private void StartMoving(bool direction, ulong newOwner)
	{
		owner = newOwner;
		fsm.enabled = false;
		moveDirection = direction;
	}

	private void StopMoving(float? knobPos)
	{
		owner = 0uL;
		fsm.enabled = true;
		if (knobPos.HasValue)
		{
			float value = knobPos.Value;
			SetKnobPos(value);
		}
	}

	private void SetKnobPos(float value)
	{
		knobPos.Value = value;
		if (multiplyResult != null)
		{
			float value2 = value * multiplyRate;
			if (!float.IsNaN(minmultiply) && !float.IsNaN(maxmultiply))
			{
				value2 = Mathf.Clamp(value2, minmultiply, maxmultiply);
			}
			multiplyResult.Value = value2;
		}
		if (mesh != null)
		{
			Vector3 localPosition = mesh.localPosition;
			localPosition[axis] = value;
			mesh.localPosition = localPosition;
		}
	}

	private void SetupFSM()
	{
		fsm.InsertAction("INCREASE", delegate
		{
			Move(true);
		}, 0);
		fsm.InsertAction("DECREASE", delegate
		{
			Move(false);
		}, 0);
		try
		{
			FsmState state = fsm.GetState("INCREASE");
			FloatAdd floatAdd = state.Actions.First((FsmStateAction f) => f is FloatAdd) as FloatAdd;
			knobPos = floatAdd.floatVariable;
			moveRate = floatAdd.add.Value;
			movePerSecond = floatAdd.perSecond;
			FsmStateAction[] array = state.Actions.Where((FsmStateAction f) => f is FloatClamp).ToArray();
			FloatClamp floatClamp = array[0] as FloatClamp;
			minMove = floatClamp.minValue.Value;
			maxMove = floatClamp.maxValue.Value;
			if (state.Actions.FirstOrDefault((FsmStateAction f) => f is FloatOperator) is FloatOperator floatOperator)
			{
				multiplyResult = floatOperator.storeResult;
				multiplyRate = floatOperator.float2.Value;
				if (floatOperator.operation == FloatOperator.Operation.Divide)
				{
					multiplyRate = 1f / multiplyRate;
				}
				if (array.Length >= 2)
				{
					FloatClamp floatClamp2 = array[1] as FloatClamp;
					minmultiply = floatClamp2.minValue.Value;
					maxmultiply = floatClamp2.maxValue.Value;
				}
				else
				{
					float num = float.NaN;
					maxmultiply = float.NaN;
					minmultiply = num;
				}
			}
			if (state.Actions.FirstOrDefault((FsmStateAction f) => f is SetPosition) is SetPosition setPosition)
			{
				mesh = setPosition.gameObject.GameObject.Value.transform;
				if (setPosition.x != null && !setPosition.x.IsNone)
				{
					axis = 0;
				}
				else if (setPosition.y != null && !setPosition.y.IsNone)
				{
					axis = 1;
				}
				else if (setPosition.z != null && !setPosition.z.IsNone)
				{
					axis = 2;
				}
			}
		}
		catch (Exception ex)
		{
			Console.LogError($"Failed to setup dashboard lever {hash} ({fsm.transform.GetGameobjectHashString()}): {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
		}
	}

	private void Move(bool? direction)
	{
		using Packet packet = new Packet(1);
		packet.Write(hash);
		packet.Write(direction.HasValue);
		packet.Write(direction.HasValue && direction.Value);
		packet.Write(knobPos.Value);
		owner = (direction.HasValue ? BeerMPGlobals.UserID : 0uL);
		updateEvent.Send(packet);
	}
}
