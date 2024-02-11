using System;
using BeerMP.Helpers;
using BeerMP.Properties;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class LocalNetPlayer : MonoBehaviour
{
	public FsmGameObject player;

	public FsmGameObject headTrans;

	public bool inCar;

	private Transform fpsCameraDefaultParent;

	private GameObject fpsCamera;

	private GameObject death;

	private GameObject gui;

	private GameObject gameOverScreen;

	private GameObject gameOverRespawningLabel;

	private FsmFloat gameVolume;

	private bool respawning;

	private CharacterCustomization characterCustomization;

	private ProximityVoiceChat pvc;

	public static LocalNetPlayer Instance { get; private set; }

	public Transform playerRoot => player.Value.transform.root;

	private void Awake()
	{
		AssetBundle assetBundle = AssetBundle.CreateFromMemoryImmediate(global::BeerMP.Properties.Resources.clothes);
		CharacterCustomization.LoadTextures(assetBundle);
		assetBundle.Unload(unloadAllLoadedObjects: false);
		Instance = this;
	}

	private void Start()
	{
		string text = "PlayerJoined";
		NetEvent<LocalNetPlayer>.Register(text, delegate(ulong player, Packet p)
		{
			BeerMPGlobals.OnMemberJoin?.Invoke(player);
		});
		using Packet packet = new Packet(0);
		NetEvent<LocalNetPlayer>.Send(text, packet);
		player = FsmVariables.GlobalVariables.FindFsmGameObject("SavePlayer");
		headTrans = FsmVariables.GlobalVariables.FindFsmGameObject("SavePlayerCam");
		PlayerAnimationManager.RegisterEvents();
		base.gameObject.AddComponent<LocalPlayerAnimationManager>();
		ObjectsLoader.gameLoaded += (Action)delegate
		{
			AssetBundle assetBundle = AssetBundle.CreateFromMemoryImmediate(global::BeerMP.Properties.Resources.clothes);
			Console.Log("charcustom init", show: false);
			characterCustomization = CharacterCustomization.Init(assetBundle);
			Console.Log("charcustom init 2", show: false);
			BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong userId)
			{
				characterCustomization.InitialSkinSync(null, userId);
			};
			new GameObject("BeerMPChat").AddComponent<Chat>();
			assetBundle.Unload(unloadAllLoadedObjects: false);
			EditDeath();
		};
	}

	private void EditDeath()
	{
		death = GameObject.Find("Systems").transform.Find("Death").gameObject;
		FsmState obj = death.GetPlayMaker("Activate Dead Body").FsmStates[0];
		obj.Actions = new FsmStateAction[0];
		obj.Transitions = new FsmTransition[0];
		death.AddComponent<PlayerDeathManager>();
		pvc = base.gameObject.AddComponent<ProximityVoiceChat>();
	}

	private void FixedUpdate()
	{
		if (player.Value == null || headTrans.Value == null)
		{
			return;
		}
		using Packet packet = new Packet(1);
		Transform transform = player.Value.transform;
		packet.Write(transform.position);
		packet.Write(transform.rotation);
		packet.Write(headTrans.Value.transform.localEulerAngles.x);
		NetEvent<NetPlayer>.Send($"SyncPosition{BeerMPGlobals.UserID}", packet, sendReliable: false);
	}

	private void OnDestroy()
	{
		Instance = null;
	}
}
