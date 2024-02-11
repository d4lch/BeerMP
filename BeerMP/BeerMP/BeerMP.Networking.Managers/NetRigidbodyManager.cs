using System;
using System.Collections.Generic;
using System.IO;
using BeerMP.Helpers;
using HutongGames.PlayMaker;
using Steamworks;
using UnityEngine;

namespace BeerMP.Networking.Managers;

[ManagerCreate(10)]
public class NetRigidbodyManager : MonoBehaviour
{
	public class OwnedRigidbody
	{
		internal int hash;

		internal Rigidbody rigidbody;

		internal Rigidbody rigidbodyPart;

		internal Vector3 cachedPosition;

		internal Vector3 cachedEulerAngles;

		public Transform transform;

		private float lastRBcheckTime;

		internal FsmObject Removal_Rigidbody;

		private Rigidbody Removal_Rigidbody_Cache;

		internal FsmGameObject Removal_Part;

		internal PlayMakerFSM assemble;

		internal PlayMakerFSM remove;

		public ulong OwnerID { get; internal set; }

		public Rigidbody Rigidbody
		{
			get
			{
				if (this.rigidbody != null)
				{
					return this.rigidbody;
				}
				if (remove != null && Removal_Rigidbody != null)
				{
					Rigidbody rigidbody = Removal_Rigidbody.Value as Rigidbody;
					if (rigidbody != null)
					{
						return rigidbody;
					}
					if ((bool)Removal_Rigidbody_Cache)
					{
						return Removal_Rigidbody_Cache;
					}
					if (remove.enabled)
					{
						return null;
					}
					if (Time.time - lastRBcheckTime > 0.5f)
					{
						lastRBcheckTime = Time.time;
						rigidbody = (Removal_Rigidbody_Cache = remove.transform.GetComponent<Rigidbody>());
						Removal_Rigidbody.Value = rigidbody;
						return rigidbody;
					}
					return null;
				}
				if (remove != null && Removal_Part != null)
				{
					if ((bool)rigidbodyPart)
					{
						return rigidbodyPart;
					}
					if (Removal_Part.Value != null)
					{
						lastRBcheckTime = Time.time;
						rigidbodyPart = Removal_Part.Value.GetComponent<Rigidbody>();
						return rigidbodyPart;
					}
					return null;
				}
				return null;
			}
		}
	}

	private struct RBUpdate
	{
		public OwnedRigidbody orb;

		public Vector3 pos;

		public Vector3 rot;

		public Vector3 vel;

		public Vector3 ang;
	}

	private static List<RBUpdate[]> receivedUpdates = new List<RBUpdate[]>();

	internal static List<int> rigidbodyHashes = new List<int>();

	internal static List<int> unknownHashes = new List<int>();

	internal static List<OwnedRigidbody> ownedRigidbodies = new List<OwnedRigidbody>();

	private static readonly int datunLayer = LayerMask.NameToLayer("Datsun");

	private float syncUpdateTime;

	private void Start()
	{
		Action action = delegate
		{
			string text = "";
			List<PlayMakerFSM> list = new List<PlayMakerFSM>();
			for (int i = 0; i < ObjectsLoader.ObjectsInGame.Length; i++)
			{
				if ((ObjectsLoader.ObjectsInGame[i].activeInHierarchy || !ObjectsLoader.ObjectsInGame[i].activeSelf || !(ObjectsLoader.ObjectsInGame[i].transform.parent == null)) && (!(ObjectsLoader.ObjectsInGame[i].name == "Ax") || ObjectsLoader.ObjectsInGame[i].layer != 20))
				{
					if (ObjectsLoader.ObjectsInGame[i].name == "wiring mess(itemx)")
					{
						NetPartManager.wiringMess = ObjectsLoader.ObjectsInGame[i].transform;
					}
					string text2 = ObjectsLoader.ObjectsInGame[i].transform.GetGameobjectHashString();
					PlayMakerFSM playMaker = ObjectsLoader.ObjectsInGame[i].GetPlayMaker("Use");
					if (playMaker != null)
					{
						FsmString fsmString = playMaker.FsmVariables.FindFsmString("ID");
						if (fsmString != null)
						{
							text2 = fsmString.Value;
						}
					}
					int hashCode = text2.GetHashCode();
					PlayMakerFSM[] components = ObjectsLoader.ObjectsInGame[i].GetComponents<PlayMakerFSM>();
					PlayMakerFSM playMakerFSM = null;
					FsmObject removal_Rigidbody = null;
					bool flag = false;
					bool flag2 = false;
					foreach (PlayMakerFSM playMakerFSM2 in components)
					{
						playMakerFSM2.Initialize();
						if (playMakerFSM2.FsmName == "Removal")
						{
							playMakerFSM = playMakerFSM2;
							removal_Rigidbody = playMakerFSM2.FsmVariables.FindFsmObject("Rigidbody");
							NetPartManager.SetupRemovalPlaymaker(playMakerFSM2, hashCode);
							if (playMakerFSM2.FsmVariables.FindFsmGameObject("db_ThisPart") != null && playMakerFSM2.GetComponent<Rigidbody>() == null)
							{
								list.Add(playMakerFSM2);
								flag2 = true;
								break;
							}
						}
						else if (!(playMakerFSM2.FsmName == "Assembly") && !(playMakerFSM2.FsmName == "Assemble"))
						{
							if (playMakerFSM2.FsmName == "Screw" && (ObjectsLoader.ObjectsInGame[i].layer == 12 || ObjectsLoader.ObjectsInGame[i].layer == 19))
							{
								if (!NetPartManager.AddBolt(playMakerFSM2, hashCode))
								{
									Console.LogError($"Bolt of hash {hashCode} ({text2}) doesn't have stage variable");
								}
								flag = true;
								break;
							}
						}
						else
						{
							int playmakerHash = playMakerFSM2.GetPlaymakerHash();
							NetPartManager.AddAssembleFsm(playmakerHash, playMakerFSM2);
							NetPartManager.SetupAssemblePlaymaker(playMakerFSM2, playmakerHash);
						}
					}
					if (!(flag || flag2))
					{
						Rigidbody component = ObjectsLoader.ObjectsInGame[i].GetComponent<Rigidbody>();
						if (!(component == null) || !(playMakerFSM == null))
						{
							if (component != null && component.transform.name == "SATSUMA(557kg, 248)")
							{
								new SatsumaProfiler(component);
							}
							OwnedRigidbody ownedRigidbody = new OwnedRigidbody
							{
								hash = hashCode,
								OwnerID = BeerMPGlobals.HostID,
								rigidbody = component,
								remove = playMakerFSM,
								Removal_Rigidbody = removal_Rigidbody,
								transform = ObjectsLoader.ObjectsInGame[i].transform
							};
							rigidbodyHashes.Add(hashCode);
							ownedRigidbodies.Add(ownedRigidbody);
							if (ObjectsLoader.ObjectsInGame[i].layer == 19)
							{
								ObjectsLoader.ObjectsInGame[i].AddComponent<MPItem>().RB = ownedRigidbody;
							}
							text += $"{hashCode} - {ObjectsLoader.ObjectsInGame[i].name} - {text2}\n";
						}
					}
				}
			}
			File.WriteAllText("hashesDebug.txt", text);
			for (int k = 0; k < list.Count; k++)
			{
				try
				{
					PlayMakerFSM component2 = list[k].FsmVariables.FindFsmGameObject("db_ThisPart").Value.GetComponent<PlayMakerFSM>();
					FsmGameObject removal_Part = list[k].FsmVariables.FindFsmGameObject("Part");
					FsmGameObject fsmGameObject = component2.FsmVariables.FindFsmGameObject("ThisPart");
					if (fsmGameObject == null)
					{
						fsmGameObject = component2.FsmVariables.FindFsmGameObject("SpawnThis");
					}
					int hashCode2 = list[k].transform.GetGameobjectHashString().GetHashCode();
					Rigidbody rb = fsmGameObject.Value.GetComponent<Rigidbody>();
					int num = ownedRigidbodies.FindIndex((OwnedRigidbody orb) => orb.Rigidbody == rb);
					OwnedRigidbody ownedRigidbody2 = new OwnedRigidbody
					{
						hash = hashCode2,
						OwnerID = BeerMPGlobals.HostID,
						rigidbody = rb,
						remove = list[k],
						Removal_Part = removal_Part,
						rigidbodyPart = rb,
						transform = list[k].transform
					};
					if (num == -1)
					{
						rigidbodyHashes.Add(hashCode2);
						ownedRigidbodies.Add(ownedRigidbody2);
					}
					else
					{
						rigidbodyHashes[num] = hashCode2;
						ownedRigidbodies[num] = ownedRigidbody2;
					}
				}
				catch (Exception ex)
				{
					Console.LogError($"xxxxx removal creation error: {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
				}
			}
			NetEvent<NetRigidbodyManager>.Register("RigidbodyUpdate", OnRigidbodyUpdate);
			NetEvent<NetRigidbodyManager>.Register("InitRigidbodyUpdate", OnInitRigidbodyUpdate);
			NetEvent<NetRigidbodyManager>.Register("RequestOwnership", OnRequestOwnership);
			NetEvent<NetRigidbodyManager>.Register("SetOwnership", delegate(ulong sender, Packet p)
			{
				ulong userId = (ulong)p.ReadLong();
				OnRequestOwnership(userId, p);
			});
			BeerMPGlobals.OnMemberReady += (Action<ulong>)delegate(ulong user)
			{
				InitSyncRb(user);
			};
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

	public static OwnedRigidbody AddRigidbody(Rigidbody rb, int hash)
	{
		OwnedRigidbody ownedRigidbody = null;
		ownedRigidbody = new OwnedRigidbody
		{
			hash = hash,
			OwnerID = BeerMPGlobals.HostID,
			rigidbody = rb,
			remove = null,
			Removal_Rigidbody = null,
			transform = rb.transform
		};
		rigidbodyHashes.Add(hash);
		ownedRigidbodies.Add(ownedRigidbody);
		if (rb.gameObject.layer == 19)
		{
			rb.gameObject.AddComponent<MPItem>().RB = ownedRigidbody;
		}
		return ownedRigidbody;
	}

	private void OnDestroy()
	{
		rigidbodyHashes.Clear();
		unknownHashes.Clear();
		ownedRigidbodies.Clear();
	}

	private void FixedUpdate()
	{
		HandleIncomingUpdates(out var receivedSatsuma);
		ulong owner = 0uL;
		using Packet packet = new Packet(1);
		syncUpdateTime += Time.fixedDeltaTime;
		if (syncUpdateTime > 5f)
		{
			syncUpdateTime = 0f;
		}
		int num = 0;
		for (int i = 0; i < rigidbodyHashes.Count; i++)
		{
			if ((bool)ownedRigidbodies[i].Rigidbody && ownedRigidbodies[i].Rigidbody.transform.name == "SATSUMA(557kg, 248)")
			{
				owner = ownedRigidbodies[i].OwnerID;
			}
			if (ownedRigidbodies[i].OwnerID != BeerMPGlobals.UserID)
			{
				continue;
			}
			if (!ownedRigidbodies[i].Rigidbody)
			{
				if (ownedRigidbodies[i].Removal_Rigidbody == null && ownedRigidbodies[i].Removal_Part == null)
				{
					rigidbodyHashes.RemoveAt(i);
					ownedRigidbodies.RemoveAt(i--);
				}
			}
			else if ((!((double)ownedRigidbodies[i].Rigidbody.velocity.sqrMagnitude <= 0.0001) || !(ownedRigidbodies[i].Rigidbody != PlayerGrabbingManager.GrabbedRigidbody)) && (!(ownedRigidbodies[i].Rigidbody.transform.parent != null) || ownedRigidbodies[i].Rigidbody.transform.root.gameObject.layer != datunLayer))
			{
				ownedRigidbodies[i].cachedPosition = ownedRigidbodies[i].transform.position;
				ownedRigidbodies[i].cachedEulerAngles = ownedRigidbodies[i].transform.eulerAngles;
				WriteRigidbody(packet, i);
				num++;
			}
		}
		SatsumaProfiler.Instance?.Update(receivedSatsuma, owner);
		if (num > 0)
		{
			packet.Write(num, 0);
			NetEvent<NetRigidbodyManager>.Send("RigidbodyUpdate", packet);
		}
	}

	private void HandleIncomingUpdates(out bool receivedSatsuma)
	{
		receivedSatsuma = false;
		for (int i = 0; i < receivedUpdates.Count; i++)
		{
			for (int j = 0; j < receivedUpdates[i].Length; j++)
			{
				RBUpdate rBUpdate = receivedUpdates[i][j];
				if (rBUpdate.orb == null || !rBUpdate.orb.Rigidbody)
				{
					continue;
				}
				if (rBUpdate.orb.assemble != null)
				{
					Console.LogWarning("Received update for rigidbody " + rBUpdate.orb.transform.name + " which is already assembled, skipping", show: false);
					continue;
				}
				if (rBUpdate.orb.Rigidbody.transform.name == "SATSUMA(557kg, 248)")
				{
					receivedSatsuma = true;
				}
				rBUpdate.orb.Rigidbody.transform.position = rBUpdate.pos;
				rBUpdate.orb.cachedPosition = rBUpdate.pos;
				rBUpdate.orb.Rigidbody.transform.eulerAngles = rBUpdate.rot;
				rBUpdate.orb.cachedEulerAngles = rBUpdate.rot;
				rBUpdate.orb.Rigidbody.velocity = rBUpdate.vel;
				rBUpdate.orb.Rigidbody.angularVelocity = rBUpdate.ang;
			}
		}
		receivedUpdates.Clear();
	}

	private void InitSyncRb(ulong target)
	{
		using Packet packet = new Packet(1);
		Console.Log("Init sync rb", show: false);
		int num = 0;
		for (int i = 0; i < rigidbodyHashes.Count; i++)
		{
			if (ownedRigidbodies[i].OwnerID != BeerMPGlobals.UserID)
			{
				continue;
			}
			if (!ownedRigidbodies[i].Rigidbody)
			{
				if (ownedRigidbodies[i].Removal_Rigidbody == null && ownedRigidbodies[i].Removal_Part == null)
				{
					rigidbodyHashes.RemoveAt(i);
					ownedRigidbodies.RemoveAt(i--);
				}
			}
			else if (ownedRigidbodies[i].Rigidbody.transform.root.gameObject.layer != datunLayer)
			{
				WriteRigidbody(packet, i);
				num++;
			}
		}
		Console.Log($"Init sync rb: {num}", show: false);
		if (num > 0)
		{
			packet.Write(num, 0);
			NetEvent<NetRigidbodyManager>.Send("InitRigidbodyUpdate", packet, target);
		}
	}

	private void WriteRigidbody(Packet _p, int i)
	{
		_p.Write(rigidbodyHashes[i]);
		_p.Write(ownedRigidbodies[i].Rigidbody.transform.position);
		_p.Write(ownedRigidbodies[i].Rigidbody.transform.eulerAngles);
		_p.Write(ownedRigidbodies[i].Rigidbody.velocity);
		_p.Write(ownedRigidbodies[i].Rigidbody.angularVelocity);
	}

	private void Update()
	{
	}

	private void OnRigidbodyUpdate(ulong userId, Packet packet)
	{
		int num = packet.ReadInt();
		RBUpdate[] array = new RBUpdate[num];
		for (int i = 0; i < num; i++)
		{
			try
			{
				int num2 = packet.ReadInt();
				int num3 = rigidbodyHashes.IndexOf(num2);
				Vector3 pos = packet.ReadVector3();
				Vector3 rot = packet.ReadVector3();
				Vector3 vel = packet.ReadVector3();
				Vector3 ang = packet.ReadVector3();
				if (num3 == -1)
				{
					if (!unknownHashes.Contains(num2))
					{
						Console.LogError($"Recieved an update for rigidbody with hash {num2} but it doesn't seem to exist");
						unknownHashes.Add(num2);
					}
				}
				else if (ownedRigidbodies[num3].OwnerID != userId)
				{
					Console.LogWarning("Received update from " + NetManager.playerNames[(CSteamID)userId] + " for rigidbody " + ownedRigidbodies[num3].Rigidbody.name + " but he's not its owner, skipping", show: false);
				}
				else
				{
					array[i].orb = ownedRigidbodies[num3];
					array[i].pos = pos;
					array[i].rot = rot;
					array[i].vel = vel;
					array[i].ang = ang;
				}
			}
			catch (Exception ex)
			{
				Console.LogError($"OnRigidbodyUpdate: {ex.GetType()}, {ex.Message}, {ex.StackTrace}");
			}
		}
		receivedUpdates.Add(array);
	}

	private void OnInitRigidbodyUpdate(ulong sender, Packet packet)
	{
		OnRigidbodyUpdate(sender, packet);
	}

	private void OnRequestOwnership(ulong userId, Packet packet)
	{
		userId = (ulong)packet.ReadLong();
		int num = packet.ReadInt();
		int num2 = rigidbodyHashes.IndexOf(num);
		if (num2 == -1)
		{
			Console.LogError($"Recieved an ownership request for rigidbody with hash {num} but it doesn't seem to exist");
		}
		else
		{
			ownedRigidbodies[num2].OwnerID = userId;
		}
	}

	public static Rigidbody GetRigidbody(int hash)
	{
		Rigidbody rigidbody = null;
		int num = rigidbodyHashes.IndexOf(hash);
		if (num == -1)
		{
			Console.LogError($"GetRigidbody: rigidbody with hash {hash} doesn't seem to exist");
			return null;
		}
		return ownedRigidbodies[num].Rigidbody;
	}

	public static OwnedRigidbody GetOwnedRigidbody(int hash)
	{
		OwnedRigidbody ownedRigidbody = null;
		int num = rigidbodyHashes.IndexOf(hash);
		if (num == -1)
		{
			Console.LogError($"GetOwnedRigidbody: rigidbody with hash {hash} doesn't seem to exist");
			return null;
		}
		return ownedRigidbodies[num];
	}

	public static int GetRigidbodyHash(Rigidbody rb)
	{
		int num = 0;
		int num2 = ownedRigidbodies.FindIndex((OwnedRigidbody or) => or.Rigidbody == rb);
		if (num2 == -1)
		{
			num = 0;
			return 0;
		}
		return rigidbodyHashes[num2];
	}

	public static void RequestOwnership(Rigidbody rigidbody)
	{
		int num = ownedRigidbodies.FindIndex((OwnedRigidbody x) => x.Rigidbody == rigidbody);
		if (num == -1)
		{
			Console.LogError("Request ownership failed: Didn't find rigidbody " + rigidbody.gameObject.name + " in orb list");
			return;
		}
		using Packet packet = new Packet(1);
		packet.Write((long)BeerMPGlobals.UserID);
		packet.Write(rigidbodyHashes[num]);
		NetEvent<NetRigidbodyManager>.Send("RequestOwnership", packet);
		ownedRigidbodies[num].OwnerID = BeerMPGlobals.UserID;
	}

	internal static void RequestOwnership(OwnedRigidbody orb)
	{
		int num = ownedRigidbodies.IndexOf(orb);
		if (num == -1)
		{
			return;
		}
		using Packet packet = new Packet(1);
		packet.Write((long)BeerMPGlobals.UserID);
		packet.Write(rigidbodyHashes[num]);
		NetEvent<NetRigidbodyManager>.Send("RequestOwnership", packet);
		orb.OwnerID = BeerMPGlobals.UserID;
	}

	internal static void RequestOwnership(OwnedRigidbody orb, ulong owner)
	{
		int num = ownedRigidbodies.IndexOf(orb);
		if (num == -1)
		{
			return;
		}
		using Packet packet = new Packet(1);
		packet.Write((long)owner);
		packet.Write(rigidbodyHashes[num]);
		NetEvent<NetRigidbodyManager>.Send("RequestOwnership", packet);
		orb.OwnerID = owner;
	}
}
