using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BeerMP.Networking.Managers;

internal class NetVehicleAudio
{
	public class WatchedAudioSource
	{
		private AudioSource src;

		private bool lastPlaying;

		private float lastVolume = 1f;

		private float lastPitch = 1f;

		public bool HasUpdate
		{
			get
			{
				if (lastPlaying == src.isPlaying && lastVolume == src.volume)
				{
					return lastPitch != src.pitch;
				}
				return true;
			}
		}

		public WatchedAudioSource(AudioSource src)
		{
			this.src = src;
			lastPlaying = src.isPlaying;
			lastVolume = src.volume;
			lastPitch = src.pitch;
		}

		public void WriteUpdates(Packet p, int srcIndex, bool initSync = false)
		{
			if (!HasUpdate && !initSync)
			{
				return;
			}
			p.Write((byte)15, -1);
			p.Write(srcIndex);
			if (lastPlaying != src.isPlaying || initSync)
			{
				p.Write((byte)31, -1);
				p.Write(src.isPlaying);
				if (src.isPlaying)
				{
					p.Write(src.time);
				}
				lastPlaying = src.isPlaying;
			}
			if (lastVolume != src.volume || initSync)
			{
				p.Write((byte)47, -1);
				p.Write(src.volume);
				lastVolume = src.volume;
			}
			if (lastPitch != src.pitch || initSync)
			{
				p.Write((byte)63, -1);
				p.Write(src.pitch);
				lastPitch = src.pitch;
			}
			p.Write(byte.MaxValue);
		}

		public void OnUpdate(bool? isPlaying, float? time, float? volume, float? pitch)
		{
			if (isPlaying.HasValue)
			{
				if (isPlaying.Value)
				{
					src.Play();
					if (time.HasValue)
					{
						src.time = time.Value;
					}
				}
				else
				{
					src.Stop();
				}
			}
			if (volume.HasValue)
			{
				src.volume = volume.Value;
			}
			if (pitch.HasValue)
			{
				src.pitch = pitch.Value;
			}
		}
	}

	public SoundController controller;

	public List<WatchedAudioSource> sources = new List<WatchedAudioSource>();

	private static MethodInfo SoundControllerStart = typeof(SoundController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);

	public bool IsDrivenBySoundController { get; set; }

	public NetVehicleAudio(Transform parent, SoundController ctrl)
	{
		ctor(parent, ctrl);
	}

	private void ctor(Transform parent, SoundController ctrl)
	{
		controller = ctrl;
		if (controller == null)
		{
			Console.LogError("Init " + parent.name + " SoundCOntroller is null");
			return;
		}
		if (!parent.gameObject.activeInHierarchy)
		{
			SoundControllerStart.Invoke(controller, null);
		}
		for (int i = 0; i < parent.childCount; i++)
		{
			GameObject gameObject = parent.GetChild(i).gameObject;
			if (gameObject.name == "audio")
			{
				AudioSource component = gameObject.GetComponent<AudioSource>();
				if (component != null)
				{
					sources.Add(new WatchedAudioSource(component));
				}
			}
		}
	}

	public void Update()
	{
		controller.enabled = IsDrivenBySoundController;
	}

	public bool WriteUpdate(Packet p, int vehicleHash, bool initSync = false)
	{
		if (p != null)
		{
			bool flag = initSync;
			for (int i = 0; i < sources.Count; i++)
			{
				if (flag)
				{
					break;
				}
				if (sources[i].HasUpdate)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				return false;
			}
			p.Write((byte)7, -1);
			p.Write(vehicleHash);
			for (int j = 0; j < sources.Count; j++)
			{
				sources[j].WriteUpdates(p, j, initSync);
			}
			p.Write((byte)247, -1);
			return true;
		}
		return false;
	}
}
