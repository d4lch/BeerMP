using System;
using System.Linq;
using BeerMP.Helpers;
using BeerMP.Networking;
using HutongGames.PlayMaker;
using Steamworks;
using UnityEngine;

namespace BeerMP;

public class Chat : MonoBehaviour
{
	public static FsmBool night;

	private string msg = "";

	private bool forceShow;

	private const string messageEventName = "ChatMessage";

	private bool visible
	{
		get
		{
			if (msg.Length <= 0)
			{
				return forceShow;
			}
			return true;
		}
	}

	private void Start()
	{
		NetEvent<Chat>.Register("ChatMessage", delegate(ulong sender, Packet p)
		{
			string message = p.ReadString();
			SendChatMessage(SteamFriends.GetFriendPersonaName((CSteamID)sender), message);
		});
		Action action = delegate
		{
			night = GameObject.Find("MAP").transform.Find("SUN/Pivot/SUN").GetComponent<PlayMakerFSM>().FsmVariables.GetFsmBool("Night");
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

	private void OnGUI()
	{
		if (visible)
		{
			GUI.Label(new Rect(10f, (float)Screen.height - 80f, (float)Screen.width - 20f, (float)Screen.height - 20f), ("Chat message: ".Color("yellow") + msg).Size(20).Bold());
		}
	}

	private void Update()
	{
		if (visible && GetInput(out var s, out var sendMessage))
		{
			forceShow = false;
			if (s == "" && msg.Length >= 1 && !sendMessage)
			{
				msg = ((msg.Length == 1) ? "" : msg.Substring(0, msg.Length - 1));
			}
			else
			{
				msg += s;
			}
			if (sendMessage)
			{
				if (msg.StartsWith("/"))
				{
					if (msg.Contains(' '))
					{
						string[] array = msg.Split(' ');
						string[] array2 = new string[array.Length - 1];
						for (int i = 0; i < array2.Length; i++)
						{
							array2[i] = array[i + 1];
						}
						CommandHandler.Execute(array[0], array2);
					}
					else
					{
						CommandHandler.Execute(msg);
					}
				}
				else
				{
					SendChatMessage(SteamFriends.GetPersonaName(), msg);
					using Packet packet = new Packet(1);
					packet.Write(msg);
					NetEvent<Chat>.Send("ChatMessage", packet);
				}
				msg = "";
			}
		}
		if (!visible && Input.GetKeyDown(KeyCode.T))
		{
			forceShow = true;
		}
	}

	private void SendChatMessage(string senderName, string message)
	{
		Console.Log(senderName.Bold() + ": " + message.Color("white"));
	}

	private bool GetInput(out string s, out bool sendMessage)
	{
		s = "";
		sendMessage = false;
		for (int i = 0; i < Input.inputString.Length; i++)
		{
			char c = Input.inputString[i];
			switch (c)
			{
			case '\b':
				if (s.Length > 0)
				{
					s = s.Substring(0, s.Length - 1);
					break;
				}
				return true;
			default:
				s += c;
				break;
			case '\n':
			case '\r':
				sendMessage = true;
				return true;
			}
		}
		return Input.inputString.Length > 0;
	}
}
