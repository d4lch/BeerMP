using System;

namespace BeerMP;

internal class ManagerCreate : Attribute
{
	public int priority { get; private set; }

	public ManagerCreate(int priority = 10)
	{
		this.priority = priority;
	}
}
