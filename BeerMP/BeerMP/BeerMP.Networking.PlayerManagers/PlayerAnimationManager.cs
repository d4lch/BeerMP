using System;
using System.Collections;
using BeerMP.Networking.Managers;
using UnityEngine;

namespace BeerMP.Networking.PlayerManagers;

internal class PlayerAnimationManager : MonoBehaviour
{
	public bool isRunning;

	public byte crouch;

	public FastIKFabric leftHand;

	public FastIKFabric rightHand;

	public FastIKFabric leftLeg;

	public FastIKFabric rightLeg;

	private HandPositionFixer rightFingers;

	internal Transform charTf;

	private const float normalY = -0.38f;

	private const float crouchY = -0.88f;

	private const float crouch2Y = -1.08f;

	private Animator animator;

	private AnimatorSyncer syncer;

	private Vector3 lastPosition;

	private Transform rotationBendPivot;

	private Transform grabbedItem;

	internal bool isWalking;

	internal bool leftHandOn;

	internal bool rightHandOn;

	private NetVehicle currentCar;

	private float brakeClutchLerp;

	private bool brakeClutchLerping;

	private Coroutine onPlayerClick;

	public const string sprintingEvent = "IsSprinting";

	public const string clickEvent = "PlayerClicked";

	public const string crouchEvent = "Crouch";

	public static void RegisterEvents()
	{
		NetEvent<PlayerAnimationManager>.Register("IsSprinting", delegate(ulong sender, Packet p)
		{
			NetManager.GetPlayerComponentById<NetPlayer>(sender).playerAnimationManager.isRunning = p.ReadBool();
		});
		NetEvent<PlayerAnimationManager>.Register("PlayerClicked", delegate(ulong sender, Packet p)
		{
			PlayerAnimationManager playerAnimationManager = NetManager.GetPlayerComponentById<NetPlayer>(sender).playerAnimationManager;
			if (playerAnimationManager.currentCar == null)
			{
				playerAnimationManager.OnPlayerClick();
			}
		});
		NetEvent<PlayerAnimationManager>.Register("Crouch", delegate(ulong sender, Packet p)
		{
			NetManager.GetPlayerComponentById<NetPlayer>(sender).playerAnimationManager.SetCrouch(p.ReadByte());
		});
	}

	public void SetPassengerMode(bool set)
	{
		animator.SetBool("passenger", set);
		if (set)
		{
			animator.Play("PassengerMode");
		}
	}

	public void GrabItem(Rigidbody item)
	{
		bool flag = item != null;
		rightHand.enabled = flag;
		rightHandOn = flag;
		syncer.rightArm = !flag;
		grabbedItem = (flag ? item.transform : null);
	}

	public void OnPlayerClick()
	{
		if (!rightHandOn)
		{
			if (onPlayerClick != null)
			{
				StopCoroutine(onPlayerClick);
			}
			onPlayerClick = StartCoroutine(C_OnPlayerClick());
		}
	}

	private IEnumerator C_OnPlayerClick()
	{
		yield return StartCoroutine(C_LerpLayerWeight(1, on: true, 0.25f));
		yield return StartCoroutine(C_LerpLayerWeight(1, on: false, 0.25f));
	}

	private IEnumerator C_LerpLayerWeight(int layer, bool on, float time = 0.2f)
	{
		time = 1f / time;
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * time;
			animator.SetLayerWeight(layer, on ? t : (1f - t));
			yield return new WaitForEndOfFrame();
		}
		animator.SetLayerWeight(layer, on ? 1f : 0f);
	}

	public void SetCrouch(byte val)
	{
		if (crouch > 0 != val > 0)
		{
			StartCoroutine(Crouch(val));
		}
		crouch = val;
	}

	public void SetPlayerInCar(bool inCar, NetVehicle car)
	{
		FastIKFabric fastIKFabric = leftHand;
		FastIKFabric fastIKFabric2 = rightHand;
		FastIKFabric fastIKFabric3 = leftLeg;
		bool flag2 = (rightLeg.enabled = inCar);
		bool flag4 = (fastIKFabric3.enabled = flag2);
		bool flag6 = (fastIKFabric2.enabled = flag4);
		fastIKFabric.enabled = flag6;
		if (inCar)
		{
			syncer.leftArm = false;
			syncer.rightArm = false;
			syncer.leftLeg = false;
			syncer.rightLeg = false;
		}
		else
		{
			syncer.leftArm = !leftHandOn;
			syncer.rightArm = !rightHandOn;
			syncer.leftLeg = true;
			syncer.rightLeg = true;
		}
		animator.enabled = !inCar;
		currentCar = (inCar ? car : null);
		rotationBendPivot.localEulerAngles = (inCar ? (Vector3.forward * 16f) : Vector3.zero);
	}

	private void Start()
	{
		Transform transform = base.transform.parent.Find("skeleton ANIMATOR");
		animator = transform.GetComponent<Animator>();
		syncer = base.gameObject.AddComponent<AnimatorSyncer>();
		syncer.sourceSkeleton = transform;
		rotationBendPivot = base.transform.Find("pelvis/RotationBendPivot");
		leftHand = FastIKFabric.CreateInstance(base.transform.Find("pelvis/RotationBendPivot/spine_middle/spine_upper/collar_left/shoulder_left/arm_left/hand_left/finger_left"), 3, base.transform.Find("la_hint"));
		Transform transform2 = base.transform.Find("pelvis/RotationBendPivot/spine_middle/spine_upper/collar_right/shoulder_right/arm_right/hand_right/fingers_right");
		rightFingers = transform2.gameObject.AddComponent<HandPositionFixer>();
		rightHand = FastIKFabric.CreateInstance(transform2, 3, base.transform.Find("ra_hint"));
		leftLeg = FastIKFabric.CreateInstance(base.transform.Find("thig_left/knee_left/ankle_left"), 2, base.transform.Find("ll_hint"));
		rightLeg = FastIKFabric.CreateInstance(base.transform.Find("thig_right/knee_right/ankle_right"), 2, base.transform.Find("rl_hint"));
		SetPlayerInCar(inCar: false, null);
	}

	private void Update()
	{
		if (currentCar == null)
		{
			animator.GetCurrentAnimatorStateInfo(0);
			Vector3 position = base.transform.position;
			float sqrMagnitude = (position - lastPosition).sqrMagnitude;
			isWalking = sqrMagnitude > 1E-05f;
			animator.SetBool("walking", isWalking);
			animator.SetBool("running", isRunning);
			animator.SetInteger("crouch", crouch);
			lastPosition = position;
		}
		else
		{
			leftHand.Target.position = currentCar.driverPivots.steeringWheel.position;
			leftHand.Target.rotation = currentCar.driverPivots.steeringWheel.rotation;
			rightHand.Target.position = currentCar.driverPivots.gearStick.position;
			rightHand.Target.rotation = currentCar.driverPivots.gearStick.rotation;
			rightLeg.Target.position = currentCar.driverPivots.throttlePedal.position;
			rightLeg.Target.rotation = currentCar.driverPivots.throttlePedal.rotation;
			if (currentCar.driverPivots.clutchPedal == null)
			{
				leftLeg.Target.position = currentCar.driverPivots.brakePedal.position;
				leftLeg.Target.rotation = currentCar.driverPivots.brakePedal.rotation;
			}
			else if (!brakeClutchLerping)
			{
				leftLeg.Target.position = Vector3.Lerp(currentCar.driverPivots.brakePedal.position, currentCar.driverPivots.clutchPedal.position, brakeClutchLerp);
				leftLeg.Target.rotation = Quaternion.Lerp(currentCar.driverPivots.brakePedal.rotation, currentCar.driverPivots.clutchPedal.rotation, brakeClutchLerp);
				if (currentCar.acc.brakeInput > 0f || currentCar.acc.clutchInput > 0f)
				{
					float num = 1f;
					if (currentCar.acc.brakeInput > 0f)
					{
						num = 0f;
					}
					if (num != brakeClutchLerp)
					{
						brakeClutchLerping = true;
						StartCoroutine(MoveIKTarget(leftLeg, currentCar.driverPivots.brakePedal, currentCar.driverPivots.clutchPedal, brakeClutchLerp, num, 0.2f, delegate
						{
							brakeClutchLerping = false;
						}));
						brakeClutchLerp = num;
					}
				}
			}
		}
		if (grabbedItem != null)
		{
			rightHand.Target.position = grabbedItem.position;
			rightHand.Target.rotation = base.transform.rotation;
		}
	}

	private IEnumerator MoveIKTarget(FastIKFabric target, Transform from, Transform to, float oldT, float newT, float time, Action onFinished)
	{
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * (1f / time);
			float t2 = Mathf.Lerp(oldT, newT, t);
			target.Target.position = Vector3.Lerp(from.position, to.position, t2);
			target.Target.rotation = Quaternion.Lerp(from.rotation, to.rotation, t2);
			yield return new WaitForEndOfFrame();
		}
		target.Target.position = to.position;
		target.Target.rotation = to.rotation;
		onFinished?.Invoke();
	}

	private IEnumerator Crouch(byte newCrouch)
	{
		float a = ((crouch == 0) ? (-0.38f) : ((crouch == 1) ? (-0.88f) : (-1.08f)));
		float b = newCrouch switch
		{
			1 => -0.88f, 
			0 => -0.38f, 
			_ => -1.08f, 
		};
		float t = 0f;
		while (t < 1f)
		{
			t += Time.deltaTime * 3f;
			Vector3 localPosition = charTf.localPosition;
			localPosition.y = Mathf.Lerp(a, b, t);
			charTf.localPosition = localPosition;
			yield return new WaitForEndOfFrame();
		}
		Vector3 localPosition2 = charTf.localPosition;
		localPosition2.y = b;
		charTf.localPosition = localPosition2;
	}
}
