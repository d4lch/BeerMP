using System.Collections;
using BeerMP.Helpers;
using BeerMP.Networking.PlayerManagers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class PlayerGrabbingManager : MonoBehaviour
{
	private static FsmGameObject itemPivot = FsmVariables.GlobalVariables.FindFsmGameObject("ItemPivot");

	internal static PlayMakerFSM handFSM;

	private static FsmGameObject handItem;

	private static Rigidbody handItem_rb;

	public static Rigidbody GrabbedRigidbody => handItem_rb;

	private void Start()
	{
		StartCoroutine(init());
	}

	private IEnumerator init()
	{
		while (!itemPivot.Value)
		{
			yield return null;
		}
		handFSM = itemPivot.Value.transform.parent.GetChild(2).GetComponent<PlayMakerFSM>();
		handItem = handFSM.FsmVariables.FindFsmGameObject("RaycastHitObject");
		handFSM.InsertAction("State 1", OnItemGrabbed);
		handFSM.InsertAction("Wait", OnItemDropped);
		handFSM.InsertAction("Look for object", OnItemDropped);
	}

	private void OnItemGrabbed()
	{
		if (handItem.Value == null)
		{
			return;
		}
		handItem_rb = handItem.Value.GetComponent<Rigidbody>();
		if (!(handItem_rb == null))
		{
			MPItem mPItem = handItem_rb.GetComponent<MPItem>();
			if (mPItem == null)
			{
				mPItem = handItem_rb.gameObject.AddComponent<MPItem>();
			}
			mPItem.doUpdate = true;
			int rigidbodyHash = NetRigidbodyManager.GetRigidbodyHash(handItem_rb);
			if (rigidbodyHash == 0)
			{
				Console.LogError("Rigidbody " + handItem_rb.name + " is not registered!");
			}
			else
			{
				Console.Log($"Grabbed hash {rigidbodyHash}", show: false);
			}
			NetRigidbodyManager.RequestOwnership(handItem_rb);
			SendGrabItemEvent(grab: true, rigidbodyHash);
		}
	}

	private void OnItemDropped()
	{
		SendGrabItemEvent(grab: false, (!(handItem_rb == null)) ? NetRigidbodyManager.GetRigidbodyHash(handItem_rb) : 0);
		MPItem component = handItem_rb.GetComponent<MPItem>();
		component.doUpdate = true;
		component.UpdateOwner();
		handItem_rb = null;
	}

	private void SendGrabItemEvent(bool grab, int hash)
	{
		using Packet packet = new Packet(1);
		packet.Write(grab);
		packet.Write(hash);
		NetEvent<NetPlayer>.Send("GrabItem" + BeerMPGlobals.UserID, packet);
	}
}
