using UnityEngine;

namespace BeerMP.Networking.Managers;

public class NetVehicleDriverPivots
{
	public Transform throttlePedal;

	public Transform brakePedal;

	public Transform clutchPedal;

	public Transform steeringWheel;

	public Transform driverParent;

	public Transform[] gearSticks;

	public Transform gearStick
	{
		get
		{
			if (gearSticks == null)
			{
				return null;
			}
			int num = 0;
			while (true)
			{
				if (num < gearSticks.Length)
				{
					if (gearSticks[num].gameObject.activeInHierarchy)
					{
						break;
					}
					num++;
					continue;
				}
				if (gearSticks.Length == 0)
				{
					return null;
				}
				return gearSticks[0];
			}
			return gearSticks[num];
		}
	}
}
