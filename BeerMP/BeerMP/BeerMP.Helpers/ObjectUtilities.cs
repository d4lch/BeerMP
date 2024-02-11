using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BeerMP.Helpers;

public static class ObjectUtilities
{
	public static int GetPlaymakerHash(this PlayMakerFSM fsm)
	{
		return (fsm.transform.GetGameobjectHashString() + "_" + fsm.FsmName).GetHashCode();
	}

	public static string GetGameobjectHashString(this Transform obj)
	{
		if (obj.gameObject.IsPrefab())
		{
			return obj.name + "_PREFAB";
		}
		PlayMakerFSM playMakerFSM = obj.GetComponents<PlayMakerFSM>().FirstOrDefault((PlayMakerFSM f) => f.FsmName == "Use");
		if (playMakerFSM == null)
		{
			return obj.GetPath();
		}
		FsmString fsmString = playMakerFSM.FsmVariables.StringVariables.FirstOrDefault((FsmString s) => s.Name == "ID");
		if (fsmString == null)
		{
			return obj.GetPath();
		}
		if (string.IsNullOrEmpty(fsmString.Value))
		{
			return obj.GetPath();
		}
		return fsmString.Value;
	}

	public static bool IsPrefab(this GameObject go)
	{
		if (!go.activeInHierarchy && go.activeSelf)
		{
			return go.transform.parent == null;
		}
		return false;
	}

	public static string GetPath(this Transform transform)
	{
		string text = "";
		if (transform.parent == null)
		{
			return transform.name ?? "";
		}
		return $"{transform.parent.GetFullPath()}/{transform.name}_{transform.GetSiblingIndex()}";
	}

	public static string GetFullPath(this Transform transform)
	{
		string text = $"{transform.name}_{transform.GetSiblingIndex()}";
		if (transform.parent == null)
		{
			return text;
		}
		Transform parent = transform.parent;
		while (parent != null)
		{
			text = $"{parent.name}_{parent.GetSiblingIndex()}/{text}";
			parent = parent.parent;
		}
		return text;
	}
}
