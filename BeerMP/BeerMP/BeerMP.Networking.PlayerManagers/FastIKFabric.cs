using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class FastIKFabric : MonoBehaviour
{
	public int ChainLength = 2;

	public Transform Target;

	public Transform Pole;

	[Header("Solver Parameters")]
	public int Iterations = 10;

	public float Delta = 0.001f;

	[Range(0f, 1f)]
	public float SnapBackStrength = 1f;

	public bool AllowIK = true;

	protected float[] BonesLength;

	protected float CompleteLength;

	protected Transform[] Bones;

	protected Vector3[] Positions;

	protected Vector3[] StartDirectionSucc;

	protected Quaternion[] StartRotationBone;

	protected Quaternion StartRotationTarget;

	protected Transform Root;

	public static FastIKFabric CreateInstance(Transform lastBone, int length, Transform hint)
	{
		FastIKFabric fastIKFabric = lastBone.gameObject.AddComponent<FastIKFabric>();
		fastIKFabric.ChainLength = length;
		fastIKFabric.Pole = hint;
		fastIKFabric.Target = new GameObject(lastBone.name + "_target").transform;
		return fastIKFabric;
	}

	private void Awake()
	{
		Init();
	}

	private void Init()
	{
		Bones = new Transform[ChainLength + 1];
		Positions = new Vector3[ChainLength + 1];
		BonesLength = new float[ChainLength];
		StartDirectionSucc = new Vector3[ChainLength + 1];
		StartRotationBone = new Quaternion[ChainLength + 1];
		Root = base.transform;
		int num = 0;
		while (true)
		{
			if (num <= ChainLength)
			{
				if (Root == null)
				{
					break;
				}
				Root = Root.parent;
				num++;
				continue;
			}
			if (Target == null)
			{
				Target = new GameObject(base.gameObject.name + " Target").transform;
				SetPositionRootSpace(Target, GetPositionRootSpace(base.transform));
			}
			StartRotationTarget = GetRotationRootSpace(Target);
			Transform parent = base.transform;
			CompleteLength = 0f;
			for (int num2 = Bones.Length - 1; num2 >= 0; num2--)
			{
				Bones[num2] = parent;
				StartRotationBone[num2] = GetRotationRootSpace(parent);
				if (num2 == Bones.Length - 1)
				{
					StartDirectionSucc[num2] = GetPositionRootSpace(Target) - GetPositionRootSpace(parent);
				}
				else
				{
					StartDirectionSucc[num2] = GetPositionRootSpace(Bones[num2 + 1]) - GetPositionRootSpace(parent);
					BonesLength[num2] = StartDirectionSucc[num2].magnitude;
					CompleteLength += BonesLength[num2];
				}
				parent = parent.parent;
			}
			return;
		}
		throw new UnityException("The chain value is longer than the ancestor chain!");
	}

	private void Update()
	{
	}

	private void LateUpdate()
	{
		ResolveIK();
	}

	private void ResolveIK()
	{
		if (Target == null || !AllowIK)
		{
			return;
		}
		if (BonesLength.Length != ChainLength)
		{
			Init();
		}
		for (int i = 0; i < Bones.Length; i++)
		{
			Positions[i] = GetPositionRootSpace(Bones[i]);
		}
		Vector3 positionRootSpace = GetPositionRootSpace(Target);
		Quaternion rotationRootSpace = GetRotationRootSpace(Target);
		if ((positionRootSpace - GetPositionRootSpace(Bones[0])).sqrMagnitude >= CompleteLength * CompleteLength)
		{
			Vector3 normalized = (positionRootSpace - Positions[0]).normalized;
			for (int j = 1; j < Positions.Length; j++)
			{
				Positions[j] = Positions[j - 1] + normalized * BonesLength[j - 1];
			}
		}
		else
		{
			for (int k = 0; k < Positions.Length - 1; k++)
			{
				Positions[k + 1] = Vector3.Lerp(Positions[k + 1], Positions[k] + StartDirectionSucc[k], SnapBackStrength);
			}
			for (int l = 0; l < Iterations; l++)
			{
				for (int num = Positions.Length - 1; num > 0; num--)
				{
					if (num == Positions.Length - 1)
					{
						Positions[num] = positionRootSpace;
					}
					else
					{
						Positions[num] = Positions[num + 1] + (Positions[num] - Positions[num + 1]).normalized * BonesLength[num];
					}
				}
				for (int m = 1; m < Positions.Length; m++)
				{
					Positions[m] = Positions[m - 1] + (Positions[m] - Positions[m - 1]).normalized * BonesLength[m - 1];
				}
				if ((Positions[Positions.Length - 1] - positionRootSpace).sqrMagnitude < Delta * Delta)
				{
					break;
				}
			}
		}
		if (Pole != null)
		{
			Vector3 positionRootSpace2 = GetPositionRootSpace(Pole);
			for (int n = 1; n < Positions.Length - 1; n++)
			{
				Plane plane = new Plane(Positions[n + 1] - Positions[n - 1], Positions[n - 1]);
				Vector3 vector = ClosestPointOnPlane(plane, positionRootSpace2);
				float angle = SignedAngle(ClosestPointOnPlane(plane, Positions[n]) - Positions[n - 1], vector - Positions[n - 1], plane.normal);
				Positions[n] = Quaternion.AngleAxis(angle, plane.normal) * (Positions[n] - Positions[n - 1]) + Positions[n - 1];
			}
		}
		for (int num2 = 0; num2 < Positions.Length; num2++)
		{
			if (num2 == Positions.Length - 1)
			{
				SetRotationRootSpace(Bones[num2], Quaternion.Inverse(rotationRootSpace) * StartRotationTarget * Quaternion.Inverse(StartRotationBone[num2]));
			}
			else
			{
				SetRotationRootSpace(Bones[num2], Quaternion.FromToRotation(StartDirectionSucc[num2], Positions[num2 + 1] - Positions[num2]) * Quaternion.Inverse(StartRotationBone[num2]));
			}
			SetPositionRootSpace(Bones[num2], Positions[num2]);
		}
	}

	private Vector3 ClosestPointOnPlane(Plane plane, Vector3 point)
	{
		float num = Vector3.Dot(plane.normal, point) + plane.distance;
		return point - plane.normal * num;
	}

	public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
	{
		float num = Vector3.Angle(from, to);
		float num2 = from.y * to.z - from.z * to.y;
		float num3 = from.z * to.x - from.x * to.z;
		float num4 = from.x * to.y - from.y * to.x;
		float num5 = Mathf.Sign(axis.x * num2 + axis.y * num3 + axis.z * num4);
		return num * num5;
	}

	private Vector3 GetPositionRootSpace(Transform current)
	{
		if (Root == null)
		{
			return current.position;
		}
		return Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
	}

	private void SetPositionRootSpace(Transform current, Vector3 position)
	{
		if (Root == null)
		{
			current.position = position;
		}
		else
		{
			current.position = Root.rotation * position + Root.position;
		}
	}

	private Quaternion GetRotationRootSpace(Transform current)
	{
		if (Root == null)
		{
			return current.rotation;
		}
		return Quaternion.Inverse(current.rotation) * Root.rotation;
	}

	private void SetRotationRootSpace(Transform current, Quaternion rotation)
	{
		if (Root == null)
		{
			current.rotation = rotation;
		}
		else
		{
			current.rotation = Root.rotation * rotation;
		}
	}
}
