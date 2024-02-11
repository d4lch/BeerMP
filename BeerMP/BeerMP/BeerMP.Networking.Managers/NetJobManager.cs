using System;
using System.Collections.Generic;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
public class NetJobManager : MonoBehaviour
{
	private PlayMakerFSM logwall;

	private PlayMakerFSM inspectionOrder;

	private PlayMakerFSM teimoAdvertPile;

	private PlayMakerFSM waterFacilityCashRegister;

	private List<PlayMakerFSM> mailboxes = new List<PlayMakerFSM>();

	private List<FsmGameObject> mailboxAdvertVariables = new List<FsmGameObject>();

	private FsmGameObject currentLog;

	private FsmGameObject newSheet;

	private FsmEvent logwallEvent;

	private FsmEvent inspectionOrderEvent;

	private FsmEvent spawnAdvertSheetEvent;

	private FsmEvent waterFacilityCalcPriceEvent;

	private FsmEvent waterFacilityPayEvent;

	private List<FsmEvent> mailboxDropAdvertEvents = new List<FsmEvent>();

	private Dictionary<int, FixedJoint> logs = new Dictionary<int, FixedJoint>();

	private List<GameObject> advertSheets = new List<GameObject>();

	private List<bool> mailboxesDropping = new List<bool>();

	private bool updatingLogs;

	private bool triggeringInspection;

	private bool spawningSheet;

	private bool updatingWaterFacility;

	internal static int logsCount;

	private NetEvent<NetJobManager> spawnLog;

	private NetEvent<NetJobManager> cutLog;

	private NetEvent<NetJobManager> triggerInspection;

	private NetEvent<NetJobManager> advertUpdate;

	private NetEvent<NetJobManager> advertInMailbox;

	private NetEvent<NetJobManager> waterFacilityUpdateEvent;

	private void Start()
	{
		Action action = delegate
		{
		};
		if (ObjectsLoader.IsGameLoaded)
		{
			action();
		}
		else
		{
			ObjectsLoader.gameLoaded += action;
		}
	}
}
