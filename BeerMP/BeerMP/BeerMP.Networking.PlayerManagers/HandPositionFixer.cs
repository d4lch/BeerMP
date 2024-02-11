using System.Collections.Generic;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class HandPositionFixer : MonoBehaviour
{
	private bool hasItem;

	public Vector3 worldCenter;

	public Transform item;

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void RecalculatePivot()
	{
		hasItem = base.transform.childCount == 1;
		if (!hasItem)
		{
			return;
		}
		item = base.transform.GetChild(0);
		Transform obj = item;
		Vector3 localPosition = (item.localEulerAngles = Vector3.zero);
		obj.localPosition = localPosition;
		MeshFilter meshFilter = item.GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			MeshFilter[] componentsInChildren = item.GetComponentsInChildren<MeshFilter>(includeInactive: true);
			if (componentsInChildren.Length != 0)
			{
				meshFilter = componentsInChildren[0];
			}
		}
		if (meshFilter == null)
		{
			return;
		}
		List<int> list = new List<int>();
		Transform parent = meshFilter.transform;
		while (parent != item)
		{
			list.Add(parent.GetSiblingIndex());
			parent = parent.parent;
		}
		Quaternion identity = Quaternion.identity;
		Transform child = item;
		for (int i = 0; i < list.Count; i++)
		{
			child = child.GetChild(list[i]);
			identity *= child.localRotation;
		}
		item.localRotation = Quaternion.Inverse(identity);
		Bounds bounds = meshFilter.mesh.bounds;
		Transform parent2 = meshFilter.transform;
		while (parent2 != base.transform)
		{
			bounds.size = new Vector3(bounds.size.x * parent2.localScale.x, bounds.size.y * parent2.localScale.y, bounds.size.z * parent2.localScale.z);
			parent2 = parent2.parent;
		}
		bounds.center = base.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(bounds.center));
		worldCenter = base.transform.TransformPoint(bounds.center);
		int num = 0;
		int num2 = 0;
		int index = 0;
		for (int j = 0; j < 3; j++)
		{
			if (bounds.size[j] < bounds.size[num])
			{
				num = j;
			}
			if (bounds.size[j] > bounds.size[num2])
			{
				num2 = j;
			}
		}
		for (int k = 0; k < 3; k++)
		{
			if (num != k && num2 != k)
			{
				index = k;
				break;
			}
		}
		Vector3 vector = -Vector3.forward * (bounds.size[index] / 2f);
		item.localPosition = -bounds.center + vector;
		worldCenter = base.transform.TransformPoint(vector);
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawSphere(worldCenter, 0.05f);
	}
}
