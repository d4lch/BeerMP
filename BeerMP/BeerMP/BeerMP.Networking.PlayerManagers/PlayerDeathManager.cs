using System.Collections;
using BeerMP.Helpers;
using BeerMP.Networking.Managers;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace BeerMP.Networking.PlayerManagers;

internal class PlayerDeathManager : MonoBehaviour
{
	private class PlayerCtrlCache
	{
		public float radius;

		public float height;

		public Vector3 center;

		public float slopeLimit;

		public float stepOffset;

		public bool detectCollisions;

		public PlayerCtrlCache(GameObject player)
		{
			CharacterController component = player.GetComponent<CharacterController>();
			radius = component.radius;
			height = component.height;
			center = component.center;
			slopeLimit = component.slopeLimit;
			stepOffset = component.stepOffset;
			detectCollisions = component.detectCollisions;
		}

		public void Apply(GameObject player)
		{
			CharacterController characterController = player.AddComponent<CharacterController>();
			player.AddComponent<CharacterMotor>();
			player.AddComponent<FPSInputController>();
			characterController.radius = radius;
			characterController.height = height;
			characterController.center = center;
			characterController.slopeLimit = slopeLimit;
			characterController.stepOffset = stepOffset;
			characterController.detectCollisions = detectCollisions;
		}
	}

	private Transform fpsCameraParent;

	private GameObject newsPhotos;

	private GameObject fpsCamera;

	private GameObject fpsCameraClone;

	private GameObject player;

	private GameObject gui;

	private GameObject gameOverScreen;

	private GameObject gameOverRespawningLabel;

	private GameObject optionsToggle;

	private FsmGameObject deadBody;

	private FsmFloat gameVolume;

	private FsmFloat playerThirst;

	private FsmFloat playerHunger;

	private FsmFloat playerStress;

	private FsmFloat playerFatigue;

	private FsmFloat playerDirtiness;

	private FsmFloat playerUrine;

	private FsmFloat playerMoney;

	private FsmBool playerInCar;

	private FsmBool playerInWater;

	private CharacterMotor charMotor;

	private PlayerCtrlCache playerCtrlCache;

	private ScreenOverlay blood;

	private NetEvent<PlayerDeathManager> DeathEvent;

	public PlayerDeathManager()
	{
		player = GameObject.Find("PLAYER");
		charMotor = player.GetComponent<CharacterMotor>();
		PlayMakerFSM playMaker = player.GetPlayMaker("Crouch");
		playerInCar = playMaker.FsmVariables.FindFsmBool("PlayerInCar");
		playerInWater = playMaker.FsmVariables.FindFsmBool("PlayerInWater");
		playerThirst = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerThirst");
		playerHunger = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerHunger");
		playerStress = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerStress");
		playerFatigue = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerFatigue");
		playerDirtiness = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerDirtiness");
		playerUrine = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerUrine");
		playerMoney = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerMoney");
		GameObject gameObject = GameObject.Find("Systems");
		newsPhotos = gameObject.transform.Find("Photomode Cam/NewsPhotos").gameObject;
		optionsToggle = gameObject.transform.Find("Options").gameObject;
		fpsCameraParent = player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera");
		fpsCamera = fpsCameraParent.Find("FPSCamera").gameObject;
		blood = fpsCamera.GetComponent<ScreenOverlay>();
		fpsCameraClone = Object.Instantiate(fpsCamera);
		fpsCameraClone.transform.parent = null;
		fpsCameraClone.SetActive(value: false);
		gameVolume = PlayMakerGlobals.Instance.Variables.FindFsmFloat("GameVolume");
		gui = GameObject.Find("GUI");
		gameOverScreen = base.transform.Find("GameOverScreen").gameObject;
		gameOverRespawningLabel = gameOverScreen.transform.Find("Saving").gameObject;
		gameOverRespawningLabel.transform.GetComponent<TextMesh>().text = "RESPAWNING...";
		deadBody = GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("DeadBody");
		playerCtrlCache = new PlayerCtrlCache(player);
		DeathEvent = NetEvent<PlayerDeathManager>.Register("Death", OnSomeoneDieEvent);
		PlayMakerFSM[] array = Resources.FindObjectsOfTypeAll<PlayMakerFSM>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize();
			for (int j = 0; j < array[i].FsmStates.Length; j++)
			{
				for (int k = 0; k < array[i].FsmStates[j].Actions.Length; k++)
				{
					if (!(array[i].FsmStates[j].Actions[k] is DestroyComponent destroyComponent) || (destroyComponent.component.Value != "CharacterController" && destroyComponent.component.Value != "CharacterMotor" && destroyComponent.component.Value != "FPSInputController"))
					{
						continue;
					}
					destroyComponent.Enabled = false;
					if (destroyComponent.component.Value == "CharacterMotor")
					{
						array[i].InsertAction(array[i].FsmStates[j].Name, delegate
						{
							charMotor.canControl = false;
						}, k + 1);
					}
				}
			}
		}
	}

	private void OnSomeoneDieEvent(ulong sender, Packet packet)
	{
		CSteamID cSteamID = (CSteamID)sender;
		bool flag = packet.ReadBool();
		Console.Log(NetManager.playerNames[cSteamID] + " " + (flag ? "has respawned" : "has died") + ". You have been charged 300 MK!");
		if (!flag)
		{
			playerMoney.Value = Mathf.Clamp(playerMoney.Value - 300f, 0f, float.MaxValue);
		}
		NetManager.GetPlayerComponentById<NetPlayer>(cSteamID).player.SetActive(flag);
	}

	private void SendIDied(bool respawned)
	{
		using Packet packet = new Packet(1);
		packet.Write(respawned);
		DeathEvent.Send(packet);
	}

	private void OnEnable()
	{
		StartCoroutine(OnPlayerDie());
	}

	private IEnumerator OnPlayerDie()
	{
		SendIDied(respawned: false);
		newsPhotos.SetActive(value: true);
		float volume = 1f;
		while (volume > 0f)
		{
			volume = Mathf.Clamp01(volume - Time.deltaTime / 2f);
			gameVolume.Value = volume;
			yield return new WaitForEndOfFrame();
		}
		gameVolume.Value = 0f;
		yield return new WaitForSeconds(0.4f);
		fpsCamera.SetActive(value: false);
		gameVolume.Value = 1f;
		player.SetActive(value: false);
		deadBody.Value.SetActive(value: false);
		gui.SetActive(value: false);
		gameOverScreen.SetActive(value: true);
		while (!Input.GetKeyDown(KeyCode.Escape) && !Input.GetKeyDown(KeyCode.Return))
		{
			yield return new WaitForEndOfFrame();
		}
		gameOverRespawningLabel.SetActive(value: true);
		yield return new WaitForSeconds(2f);
		fpsCamera.transform.parent = fpsCameraParent;
		fpsCamera.transform.localPosition = Vector3.forward * -0.05f;
		fpsCamera.transform.localEulerAngles = Vector3.zero;
		fpsCamera.SetActive(value: true);
		fpsCamera.gameObject.tag = "MainCamera";
		Camera component = fpsCamera.GetComponent<Camera>();
		if (component == null)
		{
			Console.LogError("Player camera null after death");
		}
		else
		{
			component.enabled = true;
		}
		player.SetActive(value: true);
		player.transform.parent = null;
		player.transform.position = new Vector3(-1434.642f, 4.682786f, 1151.625f);
		player.transform.eulerAngles = new Vector3(0f, 252.6235f, 0f);
		charMotor.canControl = true;
		blood.enabled = false;
		FsmFloat fsmFloat = playerThirst;
		FsmFloat fsmFloat2 = playerHunger;
		FsmFloat fsmFloat3 = playerStress;
		FsmFloat fsmFloat4 = playerUrine;
		FsmFloat fsmFloat5 = playerDirtiness;
		FsmFloat fsmFloat6 = playerFatigue;
		float value = 0f;
		fsmFloat6.Value = 0f;
		float value2 = 0f;
		fsmFloat5.Value = value;
		float value3 = 0f;
		fsmFloat4.Value = value2;
		float value4 = 0f;
		fsmFloat3.Value = value3;
		float value5 = 0f;
		fsmFloat2.Value = value4;
		fsmFloat.Value = value5;
		FsmBool fsmBool = playerInCar;
		playerInWater.Value = false;
		fsmBool.Value = false;
		gui.SetActive(value: true);
		optionsToggle.SetActive(value: true);
		newsPhotos.SetActive(value: false);
		gameOverRespawningLabel.SetActive(value: false);
		gameOverScreen.SetActive(value: false);
		base.gameObject.SetActive(value: false);
		playerMoney.Value = Mathf.Clamp(playerMoney.Value - 300f, 0f, float.MaxValue);
		Console.Log("You died! You have been charged 300 MK for respawn.");
		SendIDied(respawned: true);
	}
}
