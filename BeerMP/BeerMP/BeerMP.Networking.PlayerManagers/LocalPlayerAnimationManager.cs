using BeerMP.Helpers;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class LocalPlayerAnimationManager : MonoBehaviour
{
	private FsmGameObject player;

	private FsmFloat playerHeight;

	private FsmBool playerInCar;

	private bool lastSprint;

	private byte lastCrouch;

	private void Start()
	{
		player = FsmVariables.GlobalVariables.FindFsmGameObject("SavePlayer");
	}

	private void Update()
	{
		if (player.Value == null)
		{
			return;
		}
		if (playerHeight == null)
		{
			InitPlayerVariables();
		}
		bool key;
		if ((key = cInput.GetKey("Run")) != lastSprint)
		{
			lastSprint = key;
			using Packet packet = new Packet(1);
			packet.Write(key);
			NetEvent<PlayerAnimationManager>.Send("IsSprinting", packet);
		}
		if (Input.GetMouseButtonDown(0))
		{
			using Packet packet2 = new Packet(1);
			NetEvent<PlayerAnimationManager>.Send("PlayerClicked", packet2);
		}
		byte b = (byte)(playerInCar.Value ? 2u : ((!(playerHeight.Value >= 1.3f)) ? ((playerHeight.Value >= 0.5f) ? 1u : 2u) : 0u));
		if (b == lastCrouch)
		{
			return;
		}
		lastCrouch = b;
		using Packet packet3 = new Packet(1);
		packet3.Write(b);
		NetEvent<PlayerAnimationManager>.Send("Crouch", packet3);
	}

	private void InitPlayerVariables()
	{
		PlayMakerFSM playMaker = player.Value.GetPlayMaker("Crouch");
		playerHeight = playMaker.FsmVariables.FindFsmFloat("Position");
		playerInCar = playMaker.FsmVariables.FindFsmBool("PlayerInCar");
	}
}
