using System;
using System.IO;
using System.Linq;
using BeerMP.Helpers;
using BeerMP.Networking.Managers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class CharacterCustomization : MonoBehaviour
{
	public static readonly string[] NAMES_PANTS = new string[21]
	{
		"Blue", "Checked black", "Stripped blue", "Beige", "Mirrored shorts", "Blue jeans", "BP pants", "Black", "Hayosiko shorts", "Blue with belt",
		"Teal", "Checked brown", "Checked blue", "Checked green", "Checked orange", "Checked red", "Green shorts", "Red shorts", "Stripped black", "Cop jeans",
		"White shorts"
	};

	public static readonly string[] NAMES_SHIRTS = new string[47]
	{
		"None", "Teimo", "Gifu", "Yellow", "Grey", "Teal", "Van craze", "White shirt", "Black", "Blue amis",
		"Arvo", "Office", "Yellow FUstreet", "Black shirt", "Green shirt", "Suski", "White shirt 2", "BP top", "White shirt 3", "Blue shirt",
		"Maceeiko", "Black shirt 2", "Maceeiko 2", "Kekmet", "Balfield", "White FUstreet", "RCO", "CORRIS", "Voittous", "Slate blue",
		"Teal shirt", "Satsuma shirt", "Yellow amis", "CORRIS 2", "Polaventris", "Hayosiko", "Hayosiko 2", "CORRIS 3", "Suvi sprint 1992", "Talvi sprint 1993",
		"Office 2", "Blue shirt", "Mafia shirt", "Dassler shirt", "Black shirt 3", "Cop", "Cop 2"
	};

	public static Texture2D[] faces;

	public static Texture2D[] shirts;

	public static Texture2D[] pants;

	public static Texture2D[] shoes;

	private const int numFaces = 17;

	private const int numPants = 21;

	private const int numShirts = 47;

	private const int numShoes = 7;

	public GameObject settingTab;

	public GameObject guiCharacter;

	public GameObject playerModel;

	private bool mouseOver;

	private bool uiVisible;

	public Transform hud;

	public const string initSyncEvent = "InitSkinSync";

	public const string skinChangeEvent = "SkinChange";

	private readonly string skinPresetFilePath = Path.Combine(Application.persistentDataPath, "BeerMP_playerskin");

	private GameObject optionsmenu;

	private Transform guiTextLabel;

	private Collider buttonCollider;

	private GameObject[] othermenus;

	private CharacterCustomizationItem[] characterCustomizationItems;

	public static void LoadTextures(AssetBundle ab)
	{
		faces = new Texture2D[17];
		pants = new Texture2D[21];
		shirts = new Texture2D[47];
		shoes = new Texture2D[7];
		for (int num = 1; num <= Mathf.Max(21, 7, 47, 17); num++)
		{
			string text = num.ToString();
			if (num < 10)
			{
				text = "0" + text;
			}
			if (num <= 17)
			{
				faces[num - 1] = ab.LoadAsset<Texture2D>("char_face" + text + ".png");
			}
			if (num <= 21)
			{
				pants[num - 1] = ab.LoadAsset<Texture2D>("char_pants" + text + ".png");
			}
			if (num <= 47)
			{
				shirts[num - 1] = ab.LoadAsset<Texture2D>("char_shirt" + text + ".png");
			}
			if (num <= 7)
			{
				shoes[num - 1] = ab.LoadAsset<Texture2D>("char_shoes" + text + ".png");
			}
		}
	}

	public static CharacterCustomization Init(AssetBundle ab)
	{
		Console.Log($"Enter charcustom init {ab == null}", show: false);
		GameObject gameObject = UnityEngine.Object.Instantiate(ab.LoadAsset<GameObject>("LocalPlayerSkinRender.prefab"));
		gameObject.transform.position = Vector3.up * -10f;
		gameObject.name = "LocalPlayerSkinRender";
		Console.Log("Enter charcustom init 0.1", show: false);
		GameObject gameObject2 = UnityEngine.Object.Instantiate(ab.LoadAsset<GameObject>("char.prefab"));
		Console.Log("Enter charcustom init 0.2", show: false);
		gameObject2.name = "localPlayerModel";
		gameObject2.transform.parent = gameObject.transform;
		Transform obj = gameObject2.transform;
		Vector3 localPosition = (gameObject2.transform.localEulerAngles = Vector3.zero);
		obj.localPosition = localPosition;
		gameObject2.transform.Find("char/Camera").gameObject.SetActive(value: true);
		gameObject2.SetActive(value: false);
		Console.Log("Enter charcustom init 1", show: false);
		GameObject gameObject3 = UnityEngine.Object.Instantiate(ab.LoadAsset<GameObject>("Settings_Char.prefab"));
		gameObject3.transform.SetParent(GameObject.Find("Systems").transform.Find("OptionsMenu"));
		gameObject3.name = "PlayerCustomization";
		gameObject3.transform.localPosition = new Vector3(4f, -0.1f, 0f);
		gameObject3.transform.localEulerAngles = new Vector3(270f, 0f, 0f);
		gameObject3.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		gameObject3.SetActive(value: false);
		Console.Log("Enter charcustom init 2", show: false);
		GameObject gameObject4 = GameObject.Find("Systems").transform.Find("OptionsMenu/Menu").gameObject;
		gameObject4.transform.Find("Table 5").localPosition = new Vector3(0f, -1.1f, 0.01f);
		gameObject4.transform.Find("Table 5").localScale = new Vector3(1f, 1.3f, 1f);
		gameObject4.transform.Find("Table 3").localPosition = new Vector3(0f, 1.55f, 0.01f);
		gameObject4.transform.Find("Header 4").localPosition = new Vector3(0f, 2.55f, 0.02f);
		gameObject4.transform.Find("BoxBG").localPosition = new Vector3(0f, -0.7f, 1f);
		gameObject4.transform.Find("BoxBG").localScale = new Vector3(6.5f, 7.75f, 1f);
		gameObject4.transform.Find("Btn_Resume").localPosition = new Vector3(2.5f, 1.5f, -0.1f);
		TextMesh component = gameObject4.transform.Find("Btn_Quit/GUITextLabel").GetComponent<TextMesh>();
		string text2 = (gameObject4.transform.Find("Btn_Quit/GUITextLabel/GUITextLabelShadow").GetComponent<TextMesh>().text = "DISCONNECT");
		component.text = text2;
		PlayMakerFSM component2 = GameObject.Find("Systems").transform.Find("OptionsMenu/Menu/Btn_ConfirmQuit/Button").GetComponent<PlayMakerFSM>();
		component2.Initialize();
		FsmState fsmState = component2.FsmStates.First((FsmState s) => s.Name == "State 3");
		FsmStateAction[] actions = new PlayMakerUtilities.PM_Hook[1]
		{
			new PlayMakerUtilities.PM_Hook(delegate
			{
				NetManager.Disconnect();
			})
		};
		fsmState.Actions = actions;
		Console.Log("Enter charcustom init 3", show: false);
		CharacterCustomization characterCustomization = gameObject4.AddComponent<CharacterCustomization>();
		characterCustomization.guiCharacter = gameObject3;
		characterCustomization.playerModel = gameObject2;
		Console.Log("Registered Skinchange events", show: false);
		NetEvent<CharacterCustomization>.Register("InitSkinSync", characterCustomization.OnInitialSkinSync);
		NetEvent<CharacterCustomization>.Register("SkinChange", characterCustomization.OnSkinChange);
		Console.Log("Registered Skinchange events done", show: false);
		characterCustomization._Awake();
		return characterCustomization;
	}

	private void _Awake()
	{
		hud = GameObject.Find("Systems").transform.Find("OptionsMenu/Menu");
		optionsmenu = GameObject.Find("Systems").transform.Find("OptionsMenu").gameObject;
		settingTab = UnityEngine.Object.Instantiate(hud.transform.Find("Btn_Graphics").gameObject);
		settingTab.transform.SetParent(hud);
		settingTab.name = "Btn_PlayerCustomization";
		settingTab.transform.localPosition = new Vector3(2.5f, 0f, -0.1f);
		UnityEngine.Object.Destroy(settingTab.transform.Find("Button").GetComponent<PlayMakerFSM>());
		guiTextLabel = settingTab.transform.Find("GUITextLabel");
		guiTextLabel.GetComponent<TextMesh>().text = "SKIN CONFIG";
		guiTextLabel.GetChild(0).GetComponent<TextMesh>().text = "SKIN CONFIG";
		buttonCollider = settingTab.transform.Find("Button").GetComponent<Collider>();
		othermenus = new GameObject[4]
		{
			optionsmenu.transform.Find("DEBUG").gameObject,
			optionsmenu.transform.Find("Graphics").gameObject,
			optionsmenu.transform.Find("VehicleControls").gameObject,
			optionsmenu.transform.Find("PlayerControls").gameObject
		};
		Transform transform = guiCharacter.transform.Find("Page/Buttons");
		Transform transform2 = guiCharacter.transform.Find("Page/FieldString");
		SkinnedMeshRenderer component = playerModel.transform.Find("char/bodymesh").GetComponent<SkinnedMeshRenderer>();
		component.materials[0] = new Material(component.materials[0]);
		component.materials[1] = new Material(component.materials[1]);
		component.materials[2] = new Material(component.materials[2]);
		MeshRenderer component2 = playerModel.transform.Find("char/skeleton/thig_left/knee_left/ankle_left/shoeLeft").GetComponent<MeshRenderer>();
		MeshRenderer component3 = playerModel.transform.Find("char/skeleton/thig_right/knee_right/ankle_right/shoeRight").GetComponent<MeshRenderer>();
		Material targetMaterial = (component3.material = (component2.material = new Material(component2.material)));
		characterCustomizationItems = new CharacterCustomizationItem[6]
		{
			CharacterCustomizationItem.Init(0, SaveSkin, transform.GetChild(0), transform2.GetChild(0), null, null, null, playerModel.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot/head/glasses")),
			CharacterCustomizationItem.Init(1, SaveSkin, transform.GetChild(1), transform2.GetChild(1), null, null, null, playerModel.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot/head/head_end")),
			CharacterCustomizationItem.Init(2, SaveSkin, transform.GetChild(2), transform2.GetChild(2), null, faces, component.materials[2]),
			CharacterCustomizationItem.Init(3, SaveSkin, transform.GetChild(3), transform2.GetChild(3), NAMES_SHIRTS, shirts, component.materials[0]),
			CharacterCustomizationItem.Init(4, SaveSkin, transform.GetChild(4), transform2.GetChild(4), NAMES_PANTS, pants, component.materials[1]),
			CharacterCustomizationItem.Init(5, SaveSkin, transform.GetChild(5), transform2.GetChild(5), null, shoes, targetMaterial, component2.transform, component3.transform)
		};
		LoadSkin();
	}

	private void LoadSkin()
	{
		int[] array = new int[6] { 0, 0, 6, 12, 4, 3 };
		if (File.Exists(skinPresetFilePath))
		{
			byte[] array2 = File.ReadAllBytes(skinPresetFilePath);
			if (array2.Length == 8)
			{
				long num = BitConverter.ToInt64(array2, 0);
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = (int)((num >> i * 6) & 0x3FL);
				}
			}
		}
		for (int j = 0; j < array.Length; j++)
		{
			characterCustomizationItems[j].SetOption(array[j], sendEvent: false);
		}
		InitialSkinSync(array);
	}

	private void SaveSkin()
	{
		long num = 0L;
		for (int i = 0; i < characterCustomizationItems.Length; i++)
		{
			num |= characterCustomizationItems[i].SelectedIndex << i * 6;
		}
		File.WriteAllBytes(skinPresetFilePath, BitConverter.GetBytes(num));
	}

	public void InitialSkinSync(int[] skinPreset, ulong sendTo = 0uL)
	{
		if (skinPreset == null)
		{
			skinPreset = new int[characterCustomizationItems.Length];
			for (int i = 0; i < skinPreset.Length; i++)
			{
				skinPreset[i] = characterCustomizationItems[i].SelectedIndex;
			}
		}
		using Packet packet = new Packet(1);
		packet.Write(skinPreset[0]);
		packet.Write(skinPreset[1]);
		packet.Write(skinPreset[2]);
		packet.Write(skinPreset[3]);
		packet.Write(skinPreset[4]);
		packet.Write(skinPreset[5]);
		if (sendTo == 0L)
		{
			NetEvent<CharacterCustomization>.Send("InitSkinSync", packet);
		}
		else
		{
			NetEvent<CharacterCustomization>.Send("InitSkinSync", packet, sendTo);
		}
	}

	private void OnInitialSkinSync(ulong player, Packet p)
	{
		NetPlayer playerComponentById = NetManager.GetPlayerComponentById<NetPlayer>(player);
		if (playerComponentById == null)
		{
			Console.LogError($"CharacterCustomization.OnInitSkinSync: NetPlayer with ID {player} is null");
			return;
		}
		playerComponentById.OnInitialSkinSync(new int[6]
		{
			p.ReadInt(),
			p.ReadInt(),
			p.ReadInt(),
			p.ReadInt(),
			p.ReadInt(),
			p.ReadInt()
		});
	}

	private void OnSkinChange(ulong player, Packet p)
	{
		NetPlayer playerComponentById = NetManager.GetPlayerComponentById<NetPlayer>(player);
		if (playerComponentById == null)
		{
			Console.LogError($"CharacterCustomization.OnSkinChange: NetPlayer with ID {player} is null");
		}
		else
		{
			playerComponentById.OnSkinChange(p.ReadInt(), p.ReadInt());
		}
	}

	private void Update()
	{
		bool flag;
		if ((flag = Raycaster.Raycast(buttonCollider, 1.35f)) != mouseOver)
		{
			guiTextLabel.localScale = Vector3.one * (flag ? 0.95f : 1f);
			mouseOver = flag;
		}
		if (mouseOver && !uiVisible && Input.GetMouseButton(0))
		{
			guiCharacter.SetActive(value: true);
			playerModel.SetActive(value: true);
			for (int i = 0; i < othermenus.Length; i++)
			{
				othermenus[i].SetActive(value: false);
			}
			uiVisible = true;
		}
		if (uiVisible && othermenus.Any((GameObject go) => go.activeSelf))
		{
			guiCharacter.SetActive(value: false);
			playerModel.SetActive(value: false);
			uiVisible = false;
		}
	}
}
