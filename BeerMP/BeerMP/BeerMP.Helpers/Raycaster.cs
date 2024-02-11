using System.Collections.Generic;
using UnityEngine;

namespace BeerMP.Helpers;

public static class Raycaster
{
	private static readonly Dictionary<int, RaycastHit> raycasts = new Dictionary<int, RaycastHit>();

	private static int lastRaycastFrame;

	private static Camera camera;

	public static bool Raycast(Collider collider, float distance = 1f, int layerMask = -1)
	{
		if (!Raycast(out var hit, distance, layerMask))
		{
			return false;
		}
		return hit.collider == collider;
	}

	public static bool Raycast(out RaycastHit hit, float distance = 1f, int layerMask = -1)
	{
		if (camera == null)
		{
			camera = Camera.main;
		}
		if (Time.frameCount != lastRaycastFrame)
		{
			raycasts.Clear();
			lastRaycastFrame = Time.frameCount;
		}
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);
		if (raycasts.ContainsKey(layerMask))
		{
			RaycastHit raycastHit = raycasts[layerMask];
			if (raycastHit.distance >= distance || raycastHit.collider != null)
			{
				hit = raycastHit;
				return raycastHit.distance < distance;
			}
		}
		Physics.Raycast(ray, out hit, distance, layerMask);
		raycasts[layerMask] = hit;
		return hit.collider;
	}
}
