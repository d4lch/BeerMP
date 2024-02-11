using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
public class NetRadioManager : MonoBehaviour
{
	private List<AudioSource> radioSources = new List<AudioSource>();

	private List<PlayMakerFSM> radios = new List<PlayMakerFSM>();

	private List<FsmEvent> radioPlayNextTrack = new List<FsmEvent>();

	private List<FsmInt> radioNextTrackIndex = new List<FsmInt>();

	private List<float> audioTimes = new List<float>();

	private NetEvent<NetRadioManager> newTrackEvent;

	internal static bool radioLoaded;

	private void Start()
	{
		ObjectsLoader.gameLoaded += (Action)delegate
		{
			int num = 0;
			GameObject gameObject;
			while (true)
			{
				if (num >= ObjectsLoader.ObjectsInGame.Length)
				{
					return;
				}
				gameObject = ObjectsLoader.ObjectsInGame[num];
				if (!(gameObject.name != "RadioChannels"))
				{
					break;
				}
				num++;
			}
			Transform transform = GameObject.Find("RADIO").transform.Find("Paikallisradio");
			transform.gameObject.SetActive(value: false);
			InitChannel1(gameObject.transform, transform);
			InitFolk(gameObject.transform);
			radioLoaded = true;
			Console.Log("Init radio done");
			newTrackEvent = NetEvent<NetRadioManager>.Register("NewTrack", OnNewTrackSelected);
		};
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			if (BeerMPGlobals.IsHost)
			{
				for (int i = 0; i < radios.Count; i++)
				{
					NewTrackSelected(i, includeTime: true, user);
				}
				Console.Log("init sync radio");
			}
		};
	}

	private void InitFolk(Transform radio)
	{
		PlayMakerFSM component = radio.Find("Folk").GetComponent<PlayMakerFSM>();
		component.Initialize();
		PlayMakerFSM playMaker = (component.GetState("State 1").Actions[1] as SendEvent).eventTarget.gameObject.GameObject.Value.GetPlayMaker("Kansanradio");
		playMaker.Initialize();
		radios.Add(component);
		radioSources.Add(component.GetComponent<AudioSource>());
		FsmState state = playMaker.GetState("Play advert 1");
		FsmEvent fsmEvent = component.AddEvent("MP_NEXTRACK");
		radioPlayNextTrack.Add(fsmEvent);
		component.AddGlobalTransition(fsmEvent, "State 1");
		FsmInt trackID = new FsmInt("MP_NextTrack");
		List<FsmInt> list = playMaker.FsmVariables.IntVariables.ToList();
		list.Add(trackID);
		radioNextTrackIndex.Add(trackID);
		playMaker.FsmVariables.IntVariables = list.ToArray();
		playMaker.FsmGlobalTransitions[0].ToState = state.Name;
		if (BeerMPGlobals.IsHost)
		{
			(state.Actions[0] as ArrayListGetRandom).randomIndex = trackID;
			playMaker.InsertAction(state.Name, delegate
			{
				NewTrackSelected(1);
			}, 2);
		}
		else
		{
			FsmStateAction[] actions = state.Actions;
			ArrayListGetRandom oldAction = actions[0] as ArrayListGetRandom;
			PlayMakerArrayListProxy arrayList = playMaker.GetComponents<PlayMakerArrayListProxy>().FirstOrDefault((PlayMakerArrayListProxy al) => al.referenceName == oldAction.reference.Value);
			FsmVar targetVar = oldAction.randomItem;
			actions[0] = new PlayMakerUtilities.PM_Hook(delegate
			{
				object value = arrayList.arrayList[trackID.Value];
				targetVar.SetValue(value);
			});
			actions[1].Enabled = false;
			state.Actions = actions;
			component.InsertAction("Play radio", delegate
			{
				SetSRCtime(1);
			}, 1);
		}
		audioTimes.Add(0f);
	}

	private void InitChannel1(Transform radio, Transform pakaliradioidk)
	{
		PlayMakerFSM component = radio.Find("Channel1").GetComponent<PlayMakerFSM>();
		component.Initialize();
		radios.Add(component);
		radioSources.Add(component.GetComponent<AudioSource>());
		FsmState state = component.GetState("Channel1");
		FsmEvent fsmEvent = component.AddEvent("MP_NEXTRACK");
		radioPlayNextTrack.Add(fsmEvent);
		component.AddGlobalTransition(fsmEvent, state.Name);
		component.Fsm.Event(fsmEvent);
		FsmInt trackID = new FsmInt("MP_NextTrack");
		List<FsmInt> list = component.FsmVariables.IntVariables.ToList();
		list.Add(trackID);
		radioNextTrackIndex.Add(trackID);
		component.FsmVariables.IntVariables = list.ToArray();
		if (BeerMPGlobals.IsHost)
		{
			(state.Actions[0] as ArrayListGetRandom).randomIndex = trackID;
			component.InsertAction(state.Name, delegate
			{
				NewTrackSelected(0);
			}, 2);
		}
		else
		{
			FsmStateAction[] actions = state.Actions;
			ArrayListGetRandom oldAction = actions[0] as ArrayListGetRandom;
			PlayMakerArrayListProxy arrayList = pakaliradioidk.GetComponents<PlayMakerArrayListProxy>().FirstOrDefault((PlayMakerArrayListProxy al) => al.referenceName == oldAction.reference.Value);
			FsmVar targetVar = oldAction.randomItem;
			actions[0] = new PlayMakerUtilities.PM_Hook(delegate
			{
				object value = arrayList.arrayList[trackID.Value];
				targetVar.SetValue(value);
			});
			actions[1].Enabled = false;
			state.Actions = actions;
			component.InsertAction("Play radio 2", delegate
			{
				SetSRCtime(0);
			}, 2);
		}
		audioTimes.Add(0f);
	}

	private void SetSRCtime(int index)
	{
		radioSources[index].time = audioTimes[index];
	}

	private void OnNewTrackSelected(ulong u, Packet p)
	{
		int index = p.ReadInt();
		int value = p.ReadInt();
		float num = p.ReadFloat();
		audioTimes[index] = ((num < 0f) ? 0f : num);
		radioNextTrackIndex[index].Value = value;
		radios[index].Fsm.Event(radioPlayNextTrack[index]);
	}

	private void NewTrackSelected(int index, bool includeTime = false, ulong? target = null)
	{
		using Packet packet = new Packet(1);
		packet.Write(index);
		packet.Write(radioNextTrackIndex[index].Value);
		packet.Write(includeTime ? radioSources[index].time : (-1f));
		if (!target.HasValue)
		{
			newTrackEvent.Send(packet);
		}
		else
		{
			newTrackEvent.Send(packet, target.Value);
		}
	}
}
