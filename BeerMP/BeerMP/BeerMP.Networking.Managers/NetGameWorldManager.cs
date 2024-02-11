using System;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetGameWorldManager : MonoBehaviour
{
	public class TimeUpdateAction : FsmStateAction
	{
		public override void OnEnter()
		{
			if (IsLocal)
			{
				Instance.SendTimeUpdate();
			}
			IsLocal = true;
			Finish();
		}
	}

	public class WeatherUpdateAction : FsmStateAction
	{
		public override void OnEnter()
		{
			if (BeerMPGlobals.IsHost)
			{
				Instance.SendWeatherUpdate();
			}
			Finish();
		}
	}

	public static NetGameWorldManager Instance;

	private PlayMakerFSM sunFSM;

	private FsmInt day;

	private FsmInt time;

	private FsmFloat minutes;

	private PlayMakerFSM weatherFSM;

	private FsmFloat offset;

	private FsmFloat posX;

	private FsmFloat posZ;

	private FsmFloat rotation;

	private FsmFloat x;

	private FsmFloat z;

	private FsmInt weatherCloudID;

	private FsmInt weatherType;

	private FsmBool rain;

	private FsmEvent updateWeather;

	private static bool IsLocal = true;

	private static bool UpdatingMinutes = false;

	private NetEvent<NetGameWorldManager> TimeChange;

	private NetEvent<NetGameWorldManager> WeatherChange;

	private void OnMemberReady(ulong userId)
	{
		if (BeerMPGlobals.IsHost)
		{
			SendTimeUpdate(userId);
			SendWeatherUpdate(userId);
		}
	}

	private void Start()
	{
		Instance = this;
		GameObject go = GameObject.Find("MAP/SUN/Pivot/SUN");
		sunFSM = go.GetPlayMaker("Color");
		day = FsmVariables.GlobalVariables.FindFsmInt("GlobalDay");
		time = sunFSM.FsmVariables.FindFsmInt("Time");
		minutes = sunFSM.FsmVariables.FindFsmFloat("Minutes");
		sunFSM.InsertAction("State 3", new TimeUpdateAction(), 0);
		PlayMakerFSM playMaker = GameObject.Find("YARD").transform.Find("Building/Dynamics/SuomiClock/Clock").GetPlayMaker("Time");
		playMaker.Initialize();
		FsmState state = playMaker.GetState("Set time");
		FsmStateAction[] actions = state.Actions;
		SetRotation setRotation = actions[1] as SetRotation;
		GameObject needle = setRotation.gameObject.GameObject.Value;
		actions[1] = new PlayMakerUtilities.PM_Hook(delegate
		{
			float num = minutes.Value % 60f;
			num = 60f - num;
			num = num / 60f * 360f;
			needle.transform.localEulerAngles = Vector3.up * num;
		});
		state.Actions = actions;
		for (int i = 0; i < 12; i++)
		{
			FsmState fsmState = sunFSM.FsmStates[i];
			FsmStateAction[] actions2 = fsmState.Actions;
			for (int j = 0; j < actions2.Length; j++)
			{
				if (!(actions2[j] is SetFloatValue setFloatValue) || setFloatValue.floatVariable != minutes)
				{
					continue;
				}
				actions2[j] = new PlayMakerUtilities.PM_Hook(delegate
				{
					if (!UpdatingMinutes)
					{
						minutes.Value = 0f;
					}
					UpdatingMinutes = false;
				});
			}
			fsmState.Actions = actions2;
		}
		GameObject go2 = GameObject.Find("MAP/CloudSystem/Clouds");
		weatherFSM = go2.GetPlayMaker("Weather");
		offset = weatherFSM.FsmVariables.FindFsmFloat("Offset");
		posX = weatherFSM.FsmVariables.FindFsmFloat("PosX");
		posZ = weatherFSM.FsmVariables.FindFsmFloat("PosZ");
		rotation = weatherFSM.FsmVariables.FindFsmFloat("Rotation");
		x = weatherFSM.FsmVariables.FindFsmFloat("X");
		z = weatherFSM.FsmVariables.FindFsmFloat("Z");
		weatherCloudID = weatherFSM.FsmVariables.FindFsmInt("WeatherCloudID");
		weatherType = weatherFSM.FsmVariables.FindFsmInt("WeatherType");
		rain = weatherFSM.FsmVariables.FindFsmBool("Rain");
		updateWeather = weatherFSM.AddEvent("MP_UpdateWeather");
		weatherFSM.AddGlobalTransition(updateWeather, "Set cloud");
		weatherFSM.InsertAction("Move clouds", new WeatherUpdateAction(), 0);
		BeerMPGlobals.OnMemberReady += new Action<ulong>(OnMemberReady);
		TimeChange = NetEvent<NetGameWorldManager>.Register("TimeChange", OnTimeChange);
		WeatherChange = NetEvent<NetGameWorldManager>.Register("WeatherChange", OnWeatherChange);
		if (!BeerMPGlobals.IsHost)
		{
			FsmStateAction[] actions3 = weatherFSM.GetState("Load game").Actions;
			for (int k = 0; k < actions3.Length; k++)
			{
				actions3[k].Enabled = false;
			}
			FsmStateAction[] actions4 = sunFSM.GetState("Load").Actions;
			for (int l = 0; l < actions4.Length; l++)
			{
				actions4[l].Enabled = false;
			}
		}
	}

	private void OnTimeChange(ulong userId, Packet packet)
	{
		int num = packet.ReadInt();
		int num2 = packet.ReadInt();
		float value = packet.ReadFloat();
		Console.Log($"NetGameTimeManager: received Time Update! new day is {num} new time is {num2}");
		day.Value = num;
		time.Value = num2;
		minutes.Value = value;
		UpdatingMinutes = true;
		IsLocal = false;
		sunFSM.SendEvent("WAKEUP");
	}

	private void SendTimeUpdate()
	{
		using Packet packet = new Packet();
		packet.Write(day.Value);
		packet.Write(time.Value);
		packet.Write(minutes.Value);
		NetEvent<NetGameWorldManager>.Send("TimeChange", packet);
	}

	private void SendTimeUpdate(ulong userId)
	{
		using Packet packet = new Packet();
		packet.Write(day.Value);
		packet.Write(time.Value);
		packet.Write(minutes.Value);
		NetEvent<NetGameWorldManager>.Send("TimeChange", packet, userId);
	}

	private void OnWeatherChange(ulong userId, Packet packet)
	{
		float value = packet.ReadFloat();
		float value2 = packet.ReadFloat();
		float value3 = packet.ReadFloat();
		float num = packet.ReadFloat();
		float value4 = packet.ReadFloat();
		float value5 = packet.ReadFloat();
		weatherCloudID.Value = packet.ReadInt();
		weatherType.Value = packet.ReadInt();
		weatherFSM.Fsm.Event(updateWeather);
		offset.Value = value;
		posX.Value = value2;
		posZ.Value = value3;
		rotation.Value = num;
		weatherFSM.transform.eulerAngles = Vector3.up * num;
		x.Value = value4;
		z.Value = value5;
	}

	private void SendWeatherUpdate()
	{
		using Packet packet = new Packet();
		packet.Write(offset.Value);
		packet.Write(posX.Value);
		packet.Write(posZ.Value);
		packet.Write(rotation.Value);
		packet.Write(x.Value);
		packet.Write(z.Value);
		packet.Write(weatherCloudID.Value);
		packet.Write(weatherType.Value);
		NetEvent<NetGameWorldManager>.Send("WeatherChange", packet);
	}

	private void SendWeatherUpdate(ulong userId)
	{
		using Packet packet = new Packet();
		packet.Write(offset.Value);
		packet.Write(posX.Value);
		packet.Write(posZ.Value);
		packet.Write(rotation.Value);
		packet.Write(x.Value);
		packet.Write(z.Value);
		packet.Write(weatherCloudID.Value);
		packet.Write(weatherType.Value);
		NetEvent<NetGameWorldManager>.Send("WeatherChange", packet, userId);
	}
}
