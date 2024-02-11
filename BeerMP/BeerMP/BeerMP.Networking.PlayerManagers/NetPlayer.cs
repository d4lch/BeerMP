using System.Collections.Generic;
using BeerMP.Networking.Managers;
using BeerMP.Properties;
using Steamworks;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class NetPlayer : MonoBehaviour
{
	public CSteamID steamID;

	public GameObject player;

	public PlayerAnimationManager playerAnimationManager;

	private NetEvent<NetPlayer> SyncPosition;

	private NetEvent<NetPlayer> grabItem;

	public Vector3 offest = new Vector3(0f, 0.17f, 0f);

	private Vector3 pos;

	private Quaternion rot;

	private Transform head;

	private CharacterCustomizationItem[] characterCustomizationItems;

	private Rigidbody grabbedItem;

	public const string grabItemEvent = "GrabItem";

	public static List<Rigidbody> grabbedItems = new List<Rigidbody>();

	public static List<int> grabbedItemsHashes = new List<int>();

	internal bool disableModel = true;

	private ProximityVoiceChat pvc;

	public Vector3 HeadPos => head.position;

	public void SetInCar(bool inCar, NetVehicle car)
	{
		SetPlayerParent(inCar ? car.driverPivots.driverParent : null, worldPositionStays: false);
		playerAnimationManager.SetPlayerInCar(inCar, car);
	}

	internal void SetPassengerMode(bool enter, Transform car, bool applySitAnim = true)
	{
		SetPlayerParent(enter ? car : null, worldPositionStays: true);
		playerAnimationManager.SetPassengerMode(enter && applySitAnim);
	}

	public void SetPlayerParent(Transform parent, bool worldPositionStays)
	{
		player.transform.SetParent((parent == null) ? base.transform : parent, parent == null || worldPositionStays);
		if (!worldPositionStays && parent != null)
		{
			Transform obj = player.transform;
			Vector3 localPosition = (player.transform.localEulerAngles = Vector3.zero);
			obj.localPosition = localPosition;
		}
	}

	private void Start()
	{
		base.transform.parent = BeerMP.instance.transform;
		SyncPosition = NetEvent<NetPlayer>.Register($"SyncPosition{steamID}", OnSyncPosition);
		CSteamID cSteamID = steamID;
		grabItem = NetEvent<NetPlayer>.Register("GrabItem" + cSteamID.ToString(), OnGrabItem);
		AssetBundle assetBundle = AssetBundle.CreateFromMemoryImmediate(global::BeerMP.Properties.Resources.clothes);
		player = Object.Instantiate(assetBundle.LoadAsset<GameObject>("char.prefab"));
		if (disableModel)
		{
			player.SetActive(value: false);
		}
		player.name = steamID.ToString();
		player.transform.parent = base.transform;
		head = player.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot");
		SkinnedMeshRenderer component = player.transform.Find("char/bodymesh").GetComponent<SkinnedMeshRenderer>();
		component.materials[0] = new Material(component.materials[0]);
		component.materials[1] = new Material(component.materials[1]);
		component.materials[2] = new Material(component.materials[2]);
		playerAnimationManager = player.transform.Find("char/skeleton").gameObject.AddComponent<PlayerAnimationManager>();
		playerAnimationManager.charTf = player.transform.Find("char");
		MeshRenderer component2 = player.transform.Find("char/skeleton/thig_left/knee_left/ankle_left/shoeLeft").GetComponent<MeshRenderer>();
		MeshRenderer component3 = player.transform.Find("char/skeleton/thig_right/knee_right/ankle_right/shoeRight").GetComponent<MeshRenderer>();
		Material targetMaterial = (component3.material = (component2.material = new Material(component2.material)));
		CharacterCustomizationItem.parentTo = base.transform;
		characterCustomizationItems = new CharacterCustomizationItem[6]
		{
			CharacterCustomizationItem.Init(0, null, null, null, null, null, null, player.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot/head/glasses")),
			CharacterCustomizationItem.Init(1, null, null, null, null, null, null, player.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot/head/head_end")),
			CharacterCustomizationItem.Init(2, null, null, null, null, CharacterCustomization.faces, component.materials[2]),
			CharacterCustomizationItem.Init(3, null, null, null, null, CharacterCustomization.shirts, component.materials[0]),
			CharacterCustomizationItem.Init(4, null, null, null, null, CharacterCustomization.pants, component.materials[1]),
			CharacterCustomizationItem.Init(5, null, null, null, null, CharacterCustomization.shoes, targetMaterial, component2.transform, component3.transform)
		};
		CharacterCustomizationItem.parentTo = null;
		assetBundle.Unload(unloadAllLoadedObjects: false);
		pvc = player.AddComponent<ProximityVoiceChat>();
		pvc.net = this;
	}

	private void OnGrabItem(ulong sender, Packet packet)
	{
		if (sender != steamID.m_SteamID)
		{
			return;
		}
		bool num = packet.ReadBool();
		int num2 = packet.ReadInt();
		if (num)
		{
			grabbedItem = NetRigidbodyManager.GetRigidbody(num2);
			if (grabbedItem == null)
			{
				Console.LogError($"Player {NetManager.playerNames[steamID]} grabbed unknown rigidbody of hash {num2}");
			}
			else
			{
				grabbedItem.isKinematic = true;
				grabbedItem.gameObject.layer = 16;
				grabbedItems.Add(grabbedItem);
				grabbedItemsHashes.Add(num2);
			}
		}
		else if (grabbedItem != null)
		{
			grabbedItem.isKinematic = false;
			grabbedItem.gameObject.layer = 19;
			grabbedItems.Remove(grabbedItem);
			grabbedItemsHashes.Remove(num2);
			grabbedItem = null;
		}
		playerAnimationManager.GrabItem(grabbedItem);
	}

	public void OnSyncPosition(ulong sender, Packet packet)
	{
		pos = packet.ReadVector3();
		pos += offest;
		rot = packet.ReadQuaternion();
		float num = packet.ReadFloat();
		if (player.transform.parent == base.transform)
		{
			head.localEulerAngles = Vector3.forward * (0f - num);
		}
		else
		{
			head.eulerAngles = new Vector3(0f, rot.eulerAngles.y - 90f, 0f - num);
		}
	}

	public void OnInitialSkinSync(int[] skinPreset)
	{
		for (int i = 0; i < skinPreset.Length; i++)
		{
			OnSkinChange(i, skinPreset[i]);
		}
	}

	public void OnSkinChange(int clothesIndex, int selectedIndex)
	{
		characterCustomizationItems[clothesIndex].SetOption(selectedIndex, sendEvent: false);
	}

	private void FixedUpdate()
	{
		if (player.transform.parent == base.transform)
		{
			player.transform.position = Vector3.Lerp(player.transform.position, pos, Time.deltaTime * 15f);
			player.transform.rotation = Quaternion.Lerp(player.transform.rotation, rot, Time.deltaTime * 30f);
		}
	}

	private void OnDestroy()
	{
		Object.Destroy(player);
		SyncPosition.Unregister();
		grabItem.Unregister();
	}
}
