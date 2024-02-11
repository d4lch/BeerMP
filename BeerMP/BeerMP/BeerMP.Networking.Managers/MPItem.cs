using BeerMP.Networking.PlayerManagers;
using UnityEngine;

namespace BeerMP.Networking.Managers;

internal class MPItem : MonoBehaviour
{
	internal NetRigidbodyManager.OwnedRigidbody RB;

	internal bool doUpdate = true;

	private static Vector3[] vehicleItemCollidersTransforms = new Vector3[0];

	private static float[] vehicleItemCollidersRadiuses = new float[0];

	internal void UpdateOwner()
	{
		if (vehicleItemCollidersTransforms.Length != NetVehicleManager.vehicles.Count + 1 || vehicleItemCollidersRadiuses.Length != NetVehicleManager.vehicles.Count + 1)
		{
			vehicleItemCollidersTransforms = new Vector3[NetVehicleManager.vehicles.Count + 1];
			vehicleItemCollidersRadiuses = new float[NetVehicleManager.vehicles.Count + 1];
			vehicleItemCollidersTransforms[0] = NetBoatManager.instance.itemCollider.transform.localPosition;
			vehicleItemCollidersRadiuses[0] = NetBoatManager.instance.itemCollider.radius;
			for (int i = 1; i < vehicleItemCollidersTransforms.Length; i++)
			{
				vehicleItemCollidersTransforms[i] = NetVehicleManager.vehicles[i - 1].itemCollider.transform.localPosition;
				vehicleItemCollidersRadiuses[i] = NetVehicleManager.vehicles[i - 1].itemCollider.radius;
			}
		}
		if (!doUpdate)
		{
			return;
		}
		if (!RB.Rigidbody)
		{
			doUpdate = false;
		}
		else
		{
			if (NetPlayer.grabbedItemsHashes.Contains(RB.hash))
			{
				return;
			}
			int num = 0;
			while (true)
			{
				if (num < vehicleItemCollidersTransforms.Length)
				{
					Vector3 vector = ((num == 0) ? NetBoatManager.instance.boat.transform : NetVehicleManager.vehicles[num - 1].transform).position + vehicleItemCollidersTransforms[num];
					float num2 = vehicleItemCollidersRadiuses[num];
					num2 *= num2;
					if (!((base.transform.position - vector).sqrMagnitude >= num2))
					{
						break;
					}
					num++;
					continue;
				}
				return;
			}
			ulong num3 = ((num == 0) ? NetBoatManager.instance.owner : NetVehicleManager.vehicles[num - 1].owner);
			if (RB.OwnerID != num3)
			{
				NetRigidbodyManager.RequestOwnership(RB, num3);
			}
		}
	}
}
