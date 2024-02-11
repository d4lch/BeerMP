using System;
using BeerMP.Helpers;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class CharacterCustomizationItem : MonoBehaviour
{
	public Collider buttonLeft;

	public Collider buttonRight;

	public TextMesh fieldString;

	public TextMesh fieldStringBackground;

	public string[] names;

	public Texture2D[] textures;

	public Material targetMaterial;

	public Transform targetParent;

	public Transform targetParent2;

	internal int clothingIndex = -1;

	private int selectedIndex;

	public static Transform parentTo;

	private Action clothingChanged;

	public int SelectedIndex => selectedIndex;

	public static CharacterCustomizationItem Init(int clothingIndex, Action clothingChanged, Transform buttonsParent, Transform fieldStringParent, string[] names = null, Texture2D[] textures = null, Material targetMaterial = null, Transform targetParent = null, Transform targetParent2 = null)
	{
		CharacterCustomizationItem characterCustomizationItem = (buttonsParent ?? parentTo).gameObject.AddComponent<CharacterCustomizationItem>();
		if (buttonsParent != null)
		{
			characterCustomizationItem.buttonLeft = buttonsParent.GetChild(1).GetComponent<Collider>();
			characterCustomizationItem.buttonRight = buttonsParent.GetChild(0).GetComponent<Collider>();
		}
		if (fieldStringParent != null)
		{
			characterCustomizationItem.fieldString = fieldStringParent.GetComponent<TextMesh>();
			characterCustomizationItem.fieldStringBackground = fieldStringParent.GetChild(0).GetComponent<TextMesh>();
		}
		characterCustomizationItem.names = names;
		characterCustomizationItem.textures = textures;
		characterCustomizationItem.targetMaterial = targetMaterial;
		characterCustomizationItem.targetParent = targetParent;
		characterCustomizationItem.targetParent2 = targetParent2;
		characterCustomizationItem.clothingIndex = clothingIndex;
		characterCustomizationItem.clothingChanged = clothingChanged;
		return characterCustomizationItem;
	}

	private void Update()
	{
		if (buttonLeft != null && buttonRight != null && Input.GetMouseButtonDown(0))
		{
			if (Raycaster.Raycast(buttonLeft, 1.35f))
			{
				SetOption(selectedIndex - 1);
			}
			else if (Raycaster.Raycast(buttonRight, 1.35f))
			{
				SetOption(selectedIndex + 1);
			}
		}
	}

	public void SetOption(int index, bool sendEvent = true)
	{
		int num = ((textures == null) ? targetParent.childCount : ((targetParent == null) ? textures.Length : (textures.Length + 1)));
		index = Mathf.Clamp(index, 0, num - 1);
		if (fieldString != null && fieldStringBackground != null)
		{
			string text = ((names != null) ? names[index] : ((targetParent != null && textures != null) ? ((index == 0) ? "None" : $"INDEX {index}") : ((targetParent != null) ? targetParent.GetChild(index).name : $"INDEX {index}")));
			text = text.ToUpper();
			TextMesh textMesh = fieldString;
			string text3 = (fieldStringBackground.text = text);
			textMesh.text = text3;
		}
		if (targetParent != null && textures != null)
		{
			targetParent.gameObject.SetActive(index > 0);
			if (targetParent2 != null)
			{
				targetParent2.gameObject.SetActive(index > 0);
			}
			targetMaterial.mainTexture = textures[(index != 0) ? (index - 1) : 0];
		}
		else if (textures != null)
		{
			targetMaterial.mainTexture = textures[index];
		}
		else
		{
			targetParent.GetChild(selectedIndex).gameObject.SetActive(value: false);
			targetParent.GetChild(index).gameObject.SetActive(value: true);
		}
		selectedIndex = index;
		if (sendEvent)
		{
			using Packet packet = new Packet(1);
			packet.Write(clothingIndex);
			packet.Write(selectedIndex);
			NetEvent<CharacterCustomization>.Send("SkinChange", packet);
		}
		clothingChanged?.Invoke();
	}
}
