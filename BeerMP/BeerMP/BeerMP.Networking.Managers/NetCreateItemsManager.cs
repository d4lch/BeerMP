using System;
using System.Collections.Generic;
using System.Linq;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
internal class NetCreateItemsManager : MonoBehaviour
{
	public class Item
	{
		public NetRigidbodyManager.OwnedRigidbody orb;

		public PlayMakerFSM fsm;

		public Creator creator;

		public int hash;

		public Rigidbody rb;

		public string ID;

		public FsmGameObject Owner;

		public PlayMakerFSM ProductSpawner;

		public PlayMakerArrayListProxy Items;

		public PlayMakerArrayListProxy Spraycans;

		private bool doSync = true;

		private FsmEvent spawnAll;

		private NetEvent<Item> ShoppingSpawnOne;

		private NetEvent<Item> ShoppingSpawnAll;

		public Item(PlayMakerFSM fsm, Creator creator, string id)
		{
			this.fsm = fsm;
			ID = id;
			hash = ID.GetHashCode();
			rb = fsm.gameObject.GetComponent<Rigidbody>();
			orb = NetRigidbodyManager.AddRigidbody(rb, hash);
			this.creator = creator;
			Owner = fsm.FsmVariables.FindFsmGameObject("Owner");
			CheckPart();
			CheckShoppingbag();
			CheckBeercase();
		}

		private void CheckBeercase()
		{
			string text = rb.gameObject.name.ToLower();
			if (text.Contains("beer") && text.Contains("case"))
			{
				NetItemsManager.SetupBeercaseFSM(fsm, this);
			}
		}

		private void CheckPart()
		{
			PlayMakerFSM playMaker = fsm.gameObject.GetPlayMaker("Removal");
			if (playMaker != null)
			{
				NetPartManager.SetupRemovalPlaymaker(playMaker, hash);
				orb.Removal_Rigidbody = playMaker.FsmVariables.FindFsmObject("Rigidbody");
				orb.remove = playMaker;
			}
			PlayMakerFSM playMaker2 = fsm.gameObject.GetPlayMaker("Screw");
			if (playMaker2 != null && !NetPartManager.AddBolt(playMaker2, hash))
			{
				Console.LogError($"Bolt of hash {hash} ({ID}) doesn't have stage variable");
			}
		}

		private void CheckShoppingbag()
		{
			FsmGameObject fsmGameObject = fsm.FsmVariables.FindFsmGameObject("ProductSpawner");
			if (fsmGameObject == null)
			{
				return;
			}
			ProductSpawner = fsmGameObject.Value.GetPlayMaker("Logic");
			FsmGameObject currentBag = ProductSpawner.FsmVariables.FindFsmGameObject("CurrentBag");
			PlayMakerArrayListProxy[] components = fsm.gameObject.GetComponents<PlayMakerArrayListProxy>();
			Items = components.FirstOrDefault((PlayMakerArrayListProxy x) => x.referenceName == "Items");
			Spraycans = components.FirstOrDefault((PlayMakerArrayListProxy x) => x.referenceName == "Spraycans");
			fsm.InsertAction("Spawn one", delegate
			{
				if (doSync)
				{
					Creator.doSync = true;
					using Packet packet2 = new Packet();
					int num = 0;
					int num2 = 0;
					for (int i = 0; i < Items._arrayList.Count; i++)
					{
						packet2.Write((int)Items._arrayList[i]);
						num++;
					}
					for (int j = 0; j < Spraycans._arrayList.Count; j++)
					{
						packet2.Write((int)Spraycans._arrayList[j]);
						num2++;
					}
					packet2.Write(num2, 0);
					packet2.Write(num, 0);
					ShoppingSpawnOne.Send(packet2);
				}
				doSync = true;
			}, 0);
			spawnAll = fsm.AddEvent("MP_SPAWNALL");
			fsm.AddGlobalTransition(spawnAll, "Spawn all");
			fsm.InsertAction("Spawn all", delegate
			{
				currentBag.Value = Owner.Value;
				if (doSync)
				{
					using Packet packet = new Packet();
					ShoppingSpawnAll.Send(packet);
				}
				doSync = true;
			}, 0);
			ShoppingSpawnOne = NetEvent<Item>.Register(ID + "SpawnOne", OnShoppingSpawnOne);
			ShoppingSpawnAll = NetEvent<Item>.Register(ID + "SpawnAll", OnShoppingSpawnAll);
		}

		private void OnShoppingSpawnOne(ulong sender, Packet packet)
		{
			if (sender != BeerMPGlobals.UserID)
			{
				int num = packet.ReadInt();
				int num2 = packet.ReadInt();
				for (int i = 0; i < num; i++)
				{
					Items._arrayList[i] = packet.ReadInt();
				}
				for (int j = 0; j < num2; j++)
				{
					Spraycans._arrayList[j] = packet.ReadInt();
				}
			}
		}

		private void OnShoppingSpawnAll(ulong sender, Packet packet)
		{
			if (sender != BeerMPGlobals.UserID)
			{
				doSync = false;
				fsm.SendEvent(spawnAll.Name);
			}
		}
	}

	public class Creator
	{
		public string name;

		public PlayMakerFSM fsm;

		public FsmString newItemID;

		public FsmFloat Condition;

		public FsmInt ObjectNumberInt;

		public FsmGameObject New;

		public FsmEvent SpawnItem;

		public FsmEvent CreateItem;

		public List<Item> items = new List<Item>();

		internal static bool doSync;

		private NetEvent<Creator> spawnInitItem;

		private NetEvent<Creator> spawnItem;

		public Creator(PlayMakerFSM fsm)
		{
			Creator creator = this;
			name = fsm.FsmName;
			this.fsm = fsm;
			fsm.Initialize();
			Condition = fsm.FsmVariables.FindFsmFloat("Condition");
			ObjectNumberInt = fsm.FsmVariables.FindFsmInt("ObjectNumberInt");
			New = fsm.FsmVariables.FindFsmGameObject("New");
			newItemID = fsm.FsmVariables.FindFsmString("ID");
			CreateItem = fsm.AddEvent("MP_CREATEITEM");
			fsm.AddGlobalTransition(CreateItem, fsm.HasState("Create") ? "Create" : "Add ID");
			SpawnItem = fsm.AddEvent("MP_SPAWNITEM");
			fsm.AddGlobalTransition(SpawnItem, fsm.HasState("Spawn") ? "Spawn" : "Create product");
			spawnInitItem = NetEvent<Creator>.Register("SpawnInit" + name, OnSpawnInitItem);
			spawnItem = NetEvent<Creator>.Register("Spawn" + name, delegate(ulong sender, Packet packet)
			{
				if (sender != BeerMPGlobals.UserID)
				{
					doSync = false;
					fsm.SendEvent(creator.SpawnItem.Name);
				}
			});
			fsm.Initialize();
			if (fsm.HasState("Spawn"))
			{
				fsm.InsertAction("Spawn", delegate
				{
					creator.OnCreateItem();
				});
			}
			if (fsm.HasState("Create product"))
			{
				fsm.InsertAction("Create product", delegate
				{
					creator.OnCreateItem();
				});
			}
			if (fsm.HasState("Create"))
			{
				fsm.InsertAction("Create", delegate
				{
					creator.OnCreateItem();
				}, 5);
			}
			if (fsm.HasState("Add ID"))
			{
				fsm.InsertAction("Add ID", delegate
				{
					creator.OnCreateItem();
				}, 5);
			}
		}

		private void OnCreateItem()
		{
			items.Add(new Item(New.Value.GetPlayMaker("Use"), this, newItemID.Value));
			if (doSync)
			{
				using Packet packet = new Packet(1);
				spawnItem.Send(packet);
			}
			doSync = false;
		}

		private void OnSpawnInitItem(ulong sender, Packet packet)
		{
			if (sender != BeerMPGlobals.UserID)
			{
				int num = packet.ReadInt();
				if (ObjectNumberInt.Value != num && num > 0)
				{
					ObjectNumberInt.Value = num;
					fsm.SendEvent(CreateItem.Name);
				}
			}
		}

		public void SyncInitial(ulong target)
		{
			if (ObjectNumberInt.Value <= 0)
			{
				return;
			}
			using Packet packet = new Packet(1);
			packet.Write(ObjectNumberInt.Value);
			spawnInitItem.Send(packet, target);
		}
	}

	public GameObject createItems;

	public GameObject createSpraycans;

	public GameObject createMooseMeat;

	public GameObject createShoppingbag;

	public static List<Creator> creators = new List<Creator>();

	private static PlayMakerArrayListProxy beerDB;

	public static PlayMakerFSM[] Beercases
	{
		get
		{
			object[] array = beerDB.arrayList.ToArray();
			PlayMakerFSM[] array2 = new PlayMakerFSM[array.Length];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = (array[i] as GameObject).GetPlayMaker("Use");
			}
			return array2;
		}
	}

	private void Start()
	{
		GameObject gameObject = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault((GameObject x) => x.name == "Spawner" && x.transform.root == x.transform);
		createItems = gameObject.transform.Find("CreateItems").gameObject;
		PlayMakerFSM[] components = createItems.GetComponents<PlayMakerFSM>();
		for (int i = 0; i < components.Length; i++)
		{
			creators.Add(new Creator(components[i]));
		}
		createSpraycans = gameObject.transform.Find("CreateSpraycans").gameObject;
		components = createSpraycans.GetComponents<PlayMakerFSM>();
		for (int j = 0; j < components.Length; j++)
		{
			creators.Add(new Creator(components[j]));
		}
		createMooseMeat = gameObject.transform.Find("CreateMooseMeat").gameObject;
		components = createMooseMeat.GetComponents<PlayMakerFSM>();
		for (int k = 0; k < components.Length; k++)
		{
			creators.Add(new Creator(components[k]));
		}
		createShoppingbag = gameObject.transform.Find("CreateShoppingbag").gameObject;
		components = createShoppingbag.GetComponents<PlayMakerFSM>();
		for (int l = 0; l < components.Length; l++)
		{
			creators.Add(new Creator(components[l]));
		}
		beerDB = gameObject.transform.Find("BeerDB").GetComponent<PlayMakerArrayListProxy>();
		BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
		{
			if (BeerMPGlobals.IsHost)
			{
				for (int m = 0; m < creators.Count; m++)
				{
					creators[m].SyncInitial(user);
				}
			}
		};
	}
}
