using System;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class ProximityVoiceChat : MonoBehaviour
{
	internal NetPlayer net;

	public static bool allowVC = false;

	public static bool push_to_talk = false;

	public static KeyCode push_to_talk_key = KeyCode.None;

	internal static bool isRecording = false;

	internal static bool changeKey = false;

	public static float masterVolume = 1f;

	internal static string pvcDataPath = Path.Combine(Application.persistentDataPath, "BeerMP_proximityvoicechat");

	private NetEvent<ProximityVoiceChat> ReceiveVoice;

	private AudioSource audioSource;

	private void Start()
	{
		if (net == null)
		{
			LoadSettings();
			return;
		}
		audioSource = base.gameObject.AddComponent<AudioSource>();
		audioSource.loop = false;
		audioSource.volume = 1f;
		audioSource.rolloffMode = AudioRolloffMode.Linear;
		audioSource.maxDistance = 15f;
		audioSource.spatialBlend = 0.7f;
		audioSource.playOnAwake = false;
		ReceiveVoice = NetEvent<ProximityVoiceChat>.Register($"ProximityVoiceUpdate{net.steamID.m_SteamID}", ReceiveVoiceUpdate);
	}

	private static void SaveSettings()
	{
		List<byte> list = new List<byte>();
		list.Add((byte)(allowVC ? byte.MaxValue : 0));
		list.Add((byte)(push_to_talk ? byte.MaxValue : 0));
		list.AddRange(BitConverter.GetBytes((int)push_to_talk_key));
		list.AddRange(BitConverter.GetBytes(masterVolume));
		File.WriteAllBytes(pvcDataPath, list.ToArray());
	}

	private static void LoadSettings()
	{
		if (File.Exists(pvcDataPath))
		{
			byte[] array = File.ReadAllBytes(pvcDataPath);
			allowVC = array[0] > 0;
			push_to_talk = array[1] > 0;
			push_to_talk_key = (KeyCode)BitConverter.ToInt32(array, 2);
			masterVolume = BitConverter.ToSingle(array, 6);
		}
	}

	public static void SetEnabled(bool enable)
	{
		allowVC = enable;
		Console.Log("PVC " + (enable ? "enabled" : "disabled"));
		SaveSettings();
	}

	public static void SetPushToTalk(bool enable)
	{
		push_to_talk = enable;
		Console.Log("PVC: Push to Talk " + (enable ? "enabled" : "disabled"));
		SaveSettings();
	}

	public static void ChangePTT_Keybing()
	{
		changeKey = true;
	}

	public static void SetMasterVolume(float volume)
	{
		volume = Mathf.Clamp(volume, 0f, 10f);
		masterVolume = volume;
		Console.Log($"PVC: Master Volume set to {volume}");
		SaveSettings();
	}

	private void OnApplicationQuit()
	{
		if (isRecording)
		{
			isRecording = false;
			SteamUser.StopVoiceRecording();
		}
	}

	private void Update()
	{
		if (net != null || !allowVC)
		{
			return;
		}
		if (changeKey)
		{
			foreach (object value in Enum.GetValues(typeof(KeyCode)))
			{
				if ((KeyCode)value != KeyCode.Return && (KeyCode)value != KeyCode.KeypadEnter && Input.GetKey((KeyCode)value))
				{
					push_to_talk_key = (KeyCode)value;
					changeKey = false;
					Console.Log($"PVC: changed Push to Talk key to {push_to_talk_key}");
					SaveSettings();
				}
			}
		}
		if (SteamNetworking.IsP2PPacketAvailable(out var pcubMsgSize, 1))
		{
			byte[] array = new byte[pcubMsgSize];
			uint pcubMsgSize2 = 0u;
			if (SteamNetworking.ReadP2PPacket(array, pcubMsgSize, out pcubMsgSize2, out var psteamIDRemote))
			{
				using Packet packet = new Packet(array);
				if (psteamIDRemote != SteamUser.GetSteamID())
				{
					ReceiveVoiceUpdate(psteamIDRemote.m_SteamID, packet);
				}
			}
		}
		if (push_to_talk)
		{
			if (Input.GetKey(push_to_talk_key))
			{
				if (!isRecording)
				{
					isRecording = true;
					SteamUser.StartVoiceRecording();
				}
			}
			else if (isRecording)
			{
				isRecording = false;
				SteamUser.StopVoiceRecording();
			}
		}
		else if (!isRecording)
		{
			isRecording = true;
			SteamUser.StartVoiceRecording();
		}
		if (SteamUser.GetAvailableVoice(out var pcbCompressed, out var _, 0u) != 0 || pcbCompressed <= 1024)
		{
			return;
		}
		Debug.Log(pcbCompressed);
		byte[] array2 = new byte[1024];
		if (SteamUser.GetVoice(bWantCompressed: true, array2, 1024u, out var nBytesWritten, bWantUncompressed: false, new byte[0], 0u, out var _, 0u) != 0 || nBytesWritten == 0)
		{
			return;
		}
		using Packet packet2 = new Packet(1);
		packet2.Write(nBytesWritten);
		packet2.Write(array2);
		NetEvent<ProximityVoiceChat>.Send($"ProximityVoiceUpdate{BeerMPGlobals.UserID}", packet2, sendReliable: false);
	}

	private void ReceiveVoiceUpdate(ulong sender, Packet packet)
	{
		uint num = (uint)packet.ReadLong();
		byte[] pCompressed = packet.ReadBytes((int)num);
		uint voiceOptimalSampleRate = SteamUser.GetVoiceOptimalSampleRate();
		byte[] array = new byte[voiceOptimalSampleRate * 2];
		if (SteamUser.DecompressVoice(pCompressed, num, array, (uint)array.Length, out var nBytesWritten, voiceOptimalSampleRate) == EVoiceResult.k_EVoiceResultOK && nBytesWritten != 0)
		{
			audioSource.clip = AudioClip.Create(Guid.NewGuid().ToString(), (int)voiceOptimalSampleRate, 1, (int)voiceOptimalSampleRate, stream: false);
			float[] array2 = new float[voiceOptimalSampleRate];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = (float)(short)(array[i * 2] | (array[i * 2 + 1] << 8)) / 32768f;
				array2[i] *= 2f;
				array2[i] *= masterVolume;
			}
			audioSource.clip.SetData(array2, 0);
			audioSource.Play();
		}
	}
}
