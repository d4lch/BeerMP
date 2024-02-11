using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetStoreManager : MonoBehaviour
{
	internal class StoreProduct
	{
		public PlayMakerFSM fsm;

		public FsmInt Quantity;

		public FsmInt Bought;

		public string Name;

		public FsmEvent Purchase;

		public FsmEvent Depurchase;

		public NetEvent<StoreProduct> purchase;

		public NetEvent<StoreProduct> depurchase;

		public NetEvent<StoreProduct> syncInitial;

		public bool doSync = true;

		public StoreProduct(PlayMakerFSM fsm)
		{
			this.fsm = fsm;
			fsm.Initialize();
			Quantity = fsm.FsmVariables.FindFsmInt("Quantity");
			Bought = fsm.FsmVariables.FindFsmInt("Bought");
			Name = fsm.name;
			Purchase = fsm.AddEvent("MP_PURCHASE");
			if (fsm.HasState("Check inventory"))
			{
				fsm.AddGlobalTransition(Purchase, "Check inventory");
				fsm.InsertAction("Check inventory", delegate
				{
					using Packet packet3 = new Packet();
					if (doSync)
					{
						purchase.Send(packet3);
					}
					doSync = true;
				}, 2);
			}
			else
			{
				fsm.AddGlobalTransition(Purchase, "Play anim");
				fsm.InsertAction("Play anim", delegate
				{
					using Packet packet2 = new Packet(1);
					if (doSync)
					{
						purchase.Send(packet2);
					}
					doSync = true;
				}, 2);
			}
			if (fsm.HasState("Check if 0"))
			{
				Depurchase = fsm.AddEvent("MP_DEPURCHASE");
				fsm.AddGlobalTransition(Depurchase, "Check if 0");
				fsm.InsertAction("Check if 0", delegate
				{
					using Packet packet = new Packet(1);
					if (doSync)
					{
						depurchase.Send(packet);
					}
					doSync = true;
				}, 2);
			}
			purchase = NetEvent<StoreProduct>.Register("Purchase" + Name, OnPurchase);
			depurchase = NetEvent<StoreProduct>.Register("Depurchase" + Name, OnDepurchase);
			syncInitial = NetEvent<StoreProduct>.Register("SyncInitial" + Name, OnSyncInitial);
		}

		private void OnSyncInitial(ulong sender, Packet packet)
		{
			Quantity.Value = packet.ReadInt();
			Bought.Value = packet.ReadInt();
		}

		private void OnPurchase(ulong sender, Packet packet)
		{
			if (sender != BeerMPGlobals.UserID)
			{
				doSync = false;
				fsm.SendEvent(Purchase.Name);
			}
		}

		private void OnDepurchase(ulong sender, Packet packet)
		{
			if (sender != BeerMPGlobals.UserID)
			{
				doSync = false;
				fsm.SendEvent(Depurchase.Name);
			}
		}

		public void SyncInitial(ulong target)
		{
			using Packet packet = new Packet(1);
			if (Quantity != null && Bought != null)
			{
				packet.Write(Quantity.Value);
				packet.Write(Bought.Value);
				syncInitial.Send(packet, target);
				return;
			}
			if (Quantity == null)
			{
				Console.LogWarning("Quanitity is null on StoreProduct '" + Name + "'");
			}
			if (Bought == null)
			{
				Console.LogWarning("Bought is null on StoreProduct '" + Name + "'");
			}
		}
	}

	internal class CashRegister
	{
		public PlayMakerFSM fsm;

		public FsmEvent interact;

		public NetEvent<CashRegister> useRegister;

		public NetEvent<CashRegister> syncInitial;

		public bool doSync = true;

		public FsmInt[] intVars;

		public CashRegister(PlayMakerFSM fsm)
		{
			this.fsm = fsm;
			fsm.Initialize();
			interact = fsm.AddEvent("MP_INTERACT");
			fsm.AddGlobalTransition(interact, "Check money");
			fsm.InsertAction("Check money", delegate
			{
				using Packet packet = new Packet();
				if (doSync)
				{
					useRegister.Send(packet);
				}
				doSync = true;
			}, 0);
			intVars = fsm.FsmVariables.IntVariables;
			useRegister = NetEvent<CashRegister>.Register("UseStoreRegister", OnUseRegister);
			syncInitial = NetEvent<CashRegister>.Register("SyncStoreRegisterInitial", OnSyncInitial);
		}

		private void OnUseRegister(ulong sender, Packet packet)
		{
			if (sender != BeerMPGlobals.UserID)
			{
				doSync = false;
				fsm.SendEvent(interact.Name);
			}
		}

		public void SyncInitial(ulong target)
		{
			using Packet packet = new Packet();
			int num = 0;
			for (int i = 0; i < intVars.Length; i++)
			{
				packet.Write(intVars[i].Name);
				packet.Write(intVars[i].Value);
				num++;
			}
			packet.Write(num, 0);
			syncInitial.Send(packet, target);
		}

		private void OnSyncInitial(ulong sender, Packet packet)
		{
			int num = packet.ReadInt();
			for (int i = 0; i < num; i++)
			{
				string name = packet.ReadString();
				int value = packet.ReadInt();
				if (intVars.Any((FsmInt x) => x.Name == name))
				{
					intVars.FirstOrDefault((FsmInt x) => x.Name == name).Value = value;
				}
			}
		}
	}

	public GameObject store;

	public List<StoreProduct> products = new List<StoreProduct>();

	public CashRegister register;

	public PlayMakerHashTableProxy boughtInventory;

	public NetEvent<NetStoreManager> syncInventory;

	private void Start()
	{
		store = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault((GameObject x) => x.name == "STORE");
		store.GetPlayMaker("LOD").enabled = false;
		syncInventory = NetEvent<NetStoreManager>.Register("SyncStoreInventory", OnSyncInventory);
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			if (BeerMPGlobals.IsHost)
			{
				SyncInventory(user);
				for (int j = 0; j < products.Count; j++)
				{
					products[j].SyncInitial(user);
				}
				register.SyncInitial(user);
			}
		};
		Transform obj = store.transform.Find("LOD");
		obj.gameObject.SetActive(value: true);
		PlayMakerFSM[] array = (from x in obj.Find("ActivateStore").GetComponentsInChildren<PlayMakerFSM>(includeInactive: true)
			where x.FsmName == "Buy"
			select x).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			products.Add(new StoreProduct(array[i]));
		}
		Transform transform = store.transform.Find("Inventory");
		boughtInventory = transform.GetComponents<PlayMakerHashTableProxy>().FirstOrDefault((PlayMakerHashTableProxy x) => x.referenceName == "Bought");
		Transform tf = store.transform.Find("StoreCashRegister/Register");
		register = new CashRegister(tf.GetPlayMaker("Data"));
	}

	private void SyncInventory(ulong target)
	{
		using Packet packet = new Packet();
		object[] array = new object[boughtInventory._hashTable.Count];
		boughtInventory._hashTable.Keys.CopyTo(array, 0);
		int num = 0;
		for (num = 0; num < boughtInventory._hashTable.Count; num++)
		{
			packet.Write(array[num].ToString());
			packet.Write((int)boughtInventory._hashTable[array[num]]);
		}
		packet.Write(num, 0);
		syncInventory.Send(packet, target);
	}

	private void OnSyncInventory(ulong sender, Packet packet)
	{
		int num = packet.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string key = packet.ReadString();
			int num2 = packet.ReadInt();
			boughtInventory._hashTable[key] = num2;
		}
	}
}
