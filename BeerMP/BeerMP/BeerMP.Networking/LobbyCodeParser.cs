using System;

namespace BeerMP.Networking;

internal static class LobbyCodeParser
{
	internal static string GetString(ulong lobbyCode)
	{
		string text = "";
		for (int i = 0; i < 11; i++)
		{
			if (lobbyCode == 0L)
			{
				break;
			}
			text += Get6BitChar((int)lobbyCode & 0x3F);
			lobbyCode >>= 6;
		}
		return text;
	}

	internal static ulong GetUlong(string lobbyCode)
	{
		ulong num = 0uL;
		for (int i = 0; i < lobbyCode.Length; i++)
		{
			num |= (ulong)GetByte(lobbyCode[i]) << i * 6;
		}
		return num;
	}

	private static char Get6BitChar(int b)
	{
		int num = 0;
		num = ((b < 10) ? (48 + b) : ((b < 36) ? (65 + (b - 10)) : ((b == 36) ? 95 : ((b >= 63) ? 45 : (97 + (b - 37))))));
		return Convert.ToChar((byte)num);
	}

	private static byte GetByte(char b)
	{
		int num = 0;
		if (b == '-')
		{
			num = 63;
		}
		else if (b < ':')
		{
			num = b - 48;
		}
		else if (b < '[')
		{
			num = b - 65 + 10;
		}
		else if (b == '_')
		{
			num = 36;
		}
		else if (b < '{')
		{
			num = b - 97 + 37;
		}
		return (byte)num;
	}
}
