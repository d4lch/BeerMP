using System.Collections.Generic;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class AnimatorSyncer : MonoBehaviour
{
	internal Transform sourceSkeleton;

	private List<Transform> sourceBones = new List<Transform>();

	private List<Transform> targetBones = new List<Transform>();

	private int pelvisEndIndex;

	public bool head;

	public bool leftLeg = true;

	public bool rightLeg = true;

	public bool leftArm = true;

	public bool rightArm = true;

	private void Start()
	{
		InitBones();
	}

	private void InitBones()
	{
		int successCount = 0;
		LoopBones(sourceSkeleton.Find("pelvis"), "", ref successCount);
		pelvisEndIndex = sourceBones.Count;
		LoopBones(sourceSkeleton.Find("thig_left"), "", ref successCount);
		LoopBones(sourceSkeleton.Find("thig_right"), "", ref successCount);
	}

	private void LoopBones(Transform bone, string subPath, ref int successCount)
	{
		if (subPath != "")
		{
			subPath += "/";
		}
		subPath += bone.name;
		for (int i = 0; i < bone.childCount; i++)
		{
			Transform child = bone.GetChild(i);
			LoopBones(child, subPath, ref successCount);
		}
		sourceBones.Add(bone);
		Transform transform = base.transform.Find(subPath);
		targetBones.Add(transform);
		if (transform != null && bone != null)
		{
			successCount++;
		}
	}

	private void LateUpdate()
	{
		for (int i = 0; i < sourceBones.Count; i++)
		{
			string text = sourceBones[i].name.ToLower();
			bool flag = i >= pelvisEndIndex;
			bool flag2 = text.Contains("head");
			bool flag3 = text.Contains("left");
			bool flag4 = text.Contains("right");
			if (!(!leftLeg && flag3 && flag) && !(!rightLeg && flag4 && flag) && (!(!leftArm && flag3) || flag) && (!(!rightArm && flag4) || flag) && !(!head && flag2))
			{
				targetBones[i].localPosition = sourceBones[i].localPosition;
				targetBones[i].localEulerAngles = sourceBones[i].localEulerAngles;
			}
		}
	}
}
