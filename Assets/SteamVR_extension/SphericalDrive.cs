//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Interactable that can be used to move in a circular motion
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Valve.VR.InteractionSystem
{

	//-------------------------------------------------------------------------
	[RequireComponent(typeof(Interactable))]
	public class SphericalDrive : MonoBehaviour
	{
		public enum Axis_t
		{
			Disabled,
			Angular,
			Rotational
		};

		[Tooltip("The axis' around which the circular drive will rotate in local space")]
		public Axis_t rotateX = Axis_t.Disabled;
		public Axis_t rotateY = Axis_t.Disabled;
		public Axis_t rotateZ = Axis_t.Disabled;

		[Tooltip("Child GameObject which has the Collider component to initiate interaction, only needs to be set if there is more than one Collider child")]
		public Collider childCollider = null;

		[Tooltip("A LinearMapping component to drive, if not specified one will be dynamically added to this GameObject")]
		public nDMapping threeAxisMapping;

		[Tooltip("If true, the drive will stay manipulating as long as the button is held down, if false, it will stop if the controller moves out of the collider")]
		public bool hoverLock = false;

		[HeaderAttribute("Limited Rotation")]
		[Tooltip("If true, the rotation will be limited to [minAngle, maxAngle], if false, the rotation is unlimited")]
		public bool limited = false;
		public Vector2 frozenDistanceMinMaxThreshold = new Vector2(0.1f, 0.2f);
		public UnityEvent onFrozenDistanceThreshold;

		[HeaderAttribute("Limited Rotation Min")]
		[Tooltip("If limited is true, the specifies the lower limit, otherwise value is unused")]
		public Vector3 minAngle = new Vector3(-45.0f, -45.0f, -45.0f);
		[Tooltip("If limited, set whether drive will freeze its angle when the min angle is reached")]
		public bool freezeXOnMin = false;
		public bool freezeYOnMin = false;
		public bool freezeZOnMin = false;
		[Tooltip("If limited, event invoked when minAngle is reached")]
		public UnityEvent onXMinAngle;
		public UnityEvent onYMinAngle;
		public UnityEvent onZMinAngle;

		[HeaderAttribute("Limited Rotation Max")]
		[Tooltip("If limited is true, the specifies the upper limit, otherwise value is unused")]
		public Vector3 maxAngle = new Vector3(45.0f, 45.0f, 45.0f);
		[Tooltip("If limited, set whether drive will freeze its angle when the max angle is reached")]
		public bool freezeXOnMax = false;
		public bool freezeYOnMax = false;
		public bool freezeZOnMax = false;
		[Tooltip("If limited, event invoked when maxAngle is reached")]
		public UnityEvent onXMaxAngle;
		public UnityEvent onYMaxAngle;
		public UnityEvent onZMaxAngle;

		[Tooltip("If limited is true, this forces the starting angle to be startAngle, clamped to [minAngle, maxAngle]")]
		public bool forceStart = false;
		[Tooltip("If limited is true and forceStart is true, the starting angle will be this, clamped to [minAngle, maxAngle]")]
		public float startXAngle = 0.0f;
		public float startYAngle = 0.0f;
		public float startZAngle = 0.0f;

		[Tooltip("If true, the transform of the GameObject this component is on will be rotated accordingly")]
		public bool rotateGameObject = true;

		[Tooltip("If true, the path of the Hand (red) and the projected value (green) will be drawn")]
		public bool debugPath = false;
		[Tooltip("If debugPath is true, this is the maximum number of GameObjects to create to draw the path")]
		public int dbgPathLimit = 50;

		[Tooltip("If not null, the TextMesh will display the linear value and the angular value of this circular drive")]
		public TextMesh debugText = null;

		[Tooltip("The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations")]
		public float outXAngle;
		public float outYAngle;
		public float outZAngle;

		private Quaternion start;

		private Vector3 worldPlaneNormalX = Vector3.right;
		private Vector3 worldPlaneNormalY = Vector3.up;
		private Vector3 worldPlaneNormalZ = Vector3.forward;

		private Vector3 localPlaneNormalX = Vector3.right;
		private Vector3 localPlaneNormalY = Vector3.up;
		private Vector3 localPlaneNormalZ = Vector3.forward;

		/// <summary>
		/// buffers the last hand location projected on the YZ plane
		/// </summary>
		private Vector3 lastHandProjectedOnX;

		/// <summary>
		/// buffers the last hand location projected on the XZ plane
		/// </summary>
		private Vector3 lastHandProjectedOnY;

		/// <summary>
		/// buffers the last hand location projected on the XY plane
		/// </summary>
		private Vector3 lastHandProjectedOnZ;

		private Color red = new Color(1.0f, 0.0f, 0.0f);
		private Color green = new Color(0.0f, 1.0f, 0.0f);

		private GameObject[] dbgHandObjects;
		private GameObject[] dbgProjObjects;
		private GameObject dbgObjectsParent;
		private int dbgObjectCount = 0;
		private int dbgObjectIndex = 0;

		private bool driving = false;

		// If the drive is limited as is at min/max, angles greater than this are ignored
		private float minMaxAngularThreshold = 1.0f;

		private bool frozen = false;
		private float frozenXAngle = 0.0f;
		private float frozenYAngle = 0.0f;
		private float frozenZAngle = 0.0f;
		private Vector3 frozenHandWorldPos = new Vector3(0.0f, 0.0f, 0.0f);
		private Vector2 frozenSqDistanceMinMaxThreshold = new Vector2(0.0f, 0.0f);

		private Hand handHoverLocked = null;

		private Interactable interactable;

		//-------------------------------------------------
		private void Freeze(Hand hand)
		{
			frozen = true;
			frozenXAngle = outXAngle;
			frozenYAngle = outYAngle;
			frozenZAngle = outZAngle;
			frozenHandWorldPos = hand.hoverSphereTransform.position;
			frozenSqDistanceMinMaxThreshold.x = frozenDistanceMinMaxThreshold.x * frozenDistanceMinMaxThreshold.x;
			frozenSqDistanceMinMaxThreshold.y = frozenDistanceMinMaxThreshold.y * frozenDistanceMinMaxThreshold.y;
		}


		//-------------------------------------------------
		private void UnFreeze()
		{
			frozen = false;
			frozenHandWorldPos.Set(0.0f, 0.0f, 0.0f);
		}

		private void Awake()
		{
			interactable = this.GetComponent<Interactable>();
		}

		//-------------------------------------------------
		private void Start()
		{
			if (childCollider == null)
			{
				childCollider = GetComponentInChildren<Collider>();
			}

			if (threeAxisMapping == null)
			{
				threeAxisMapping = GetComponent<nDMapping>();
				if (threeAxisMapping != null)
				{
					threeAxisMapping.values.TryAdd("X", 0.0f);
					threeAxisMapping.values.TryAdd("Y", 0.0f);
					threeAxisMapping.values.TryAdd("Z", 0.0f);
				}
			}

			if (threeAxisMapping == null)
			{
				threeAxisMapping = gameObject.AddComponent<nDMapping>();
				threeAxisMapping.values.TryAdd("X", 0.0f);
				threeAxisMapping.values.TryAdd("Y", 0.0f);
				threeAxisMapping.values.TryAdd("Z", 0.0f);
			}

			//worldPlaneNormal = new Vector3(0.0f, 0.0f, 0.0f); now handled locally
			//worldPlaneNormal[(int)axisOfRotation] = 1.0f;

			//localPlaneNormal = worldPlaneNormal;

			if (transform.parent)
			{
				worldPlaneNormalX = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormalX).normalized;
				worldPlaneNormalY = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormalY).normalized;
				worldPlaneNormalZ = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormalZ).normalized;
			}

			if (limited)
			{
				start = Quaternion.identity;
				outXAngle = transform.localEulerAngles.x;
				outYAngle = transform.localEulerAngles.y;
				outZAngle = transform.localEulerAngles.z;

				if (forceStart)
				{
					outXAngle = Mathf.Clamp(startXAngle, minAngle.x, maxAngle.x);
					outYAngle = Mathf.Clamp(startYAngle, minAngle.y, maxAngle.y);
					outZAngle = Mathf.Clamp(startZAngle, minAngle.z, maxAngle.z);
				}
			}
			else //for each selected axis, set absolute start angle
			{
				start = Quaternion.identity;
				switch (rotateX)
				{
					case Axis_t.Angular:
						start *= Quaternion.AngleAxis(transform.localEulerAngles.x, localPlaneNormalX);
						break;
					case Axis_t.Rotational:
						break;
				}
				switch (rotateY)
				{
					case Axis_t.Angular:
						start *= Quaternion.AngleAxis(transform.localEulerAngles.y, localPlaneNormalY);
						break;
					case Axis_t.Rotational:
						break;
				}
				switch (rotateZ)
				{
					case Axis_t.Angular:
						start *= Quaternion.AngleAxis(transform.localEulerAngles.z, localPlaneNormalZ);
						break;
					case Axis_t.Rotational:
						break;
				}

				//reset relative start angle
				outXAngle = 0.0f;
				outYAngle = 0.0f;
				outZAngle = 0.0f;
			}

			if (debugText)
			{
				debugText.alignment = TextAlignment.Left;
				debugText.anchor = TextAnchor.UpperLeft;
			}

			UpdateAll();
		}


		//-------------------------------------------------
		void OnDisable()
		{
			if (handHoverLocked)
			{
				handHoverLocked.HideGrabHint();
				handHoverLocked.HoverUnlock(interactable);
				handHoverLocked = null;
			}
		}


		//-------------------------------------------------
		private IEnumerator HapticPulses(Hand hand, float flMagnitude, int nCount)
		{
			if (hand != null)
			{
				int nRangeMax = (int)Util.RemapNumberClamped(flMagnitude, 0.0f, 1.0f, 100.0f, 900.0f);
				nCount = Mathf.Clamp(nCount, 1, 10);

				//float hapticDuration = nRangeMax * nCount;

				//hand.TriggerHapticPulse(hapticDuration, nRangeMax, flMagnitude);

				for (ushort i = 0; i < nCount; ++i)
				{
					ushort duration = (ushort)Random.Range(100, nRangeMax);
					hand.TriggerHapticPulse(duration);
					yield return new WaitForSeconds(.01f);
				}
			}
		}


		//-------------------------------------------------
		private void OnHandHoverBegin(Hand hand)
		{
			hand.ShowGrabHint();
		}


		//-------------------------------------------------
		private void OnHandHoverEnd(Hand hand)
		{
			hand.HideGrabHint();

			if (driving && hand)
			{
				//hand.TriggerHapticPulse() //todo: fix
				StartCoroutine(HapticPulses(hand, 1.0f, 10));
			}

			driving = false;
			handHoverLocked = null;
		}

		private GrabTypes grabbedWithType;
		//-------------------------------------------------
		private void HandHoverUpdate(Hand hand)
		{
			GrabTypes startingGrabType = hand.GetGrabStarting();
			bool isGrabEnding = hand.IsGrabbingWithType(grabbedWithType) == false;

			if (grabbedWithType == GrabTypes.None && startingGrabType != GrabTypes.None)
			{
				grabbedWithType = startingGrabType;
				// Trigger was just pressed
				lastHandProjectedOnX = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormalX);
				lastHandProjectedOnY = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormalY);
				lastHandProjectedOnZ = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormalZ);

				if (hoverLock)
				{
					hand.HoverLock(interactable);
					handHoverLocked = hand;
				}

				driving = true;

				ComputeAngle(hand);
				UpdateAll();

				hand.HideGrabHint();
			}
			else if (grabbedWithType != GrabTypes.None && isGrabEnding)
			{
				// Trigger was just released
				if (hoverLock)
				{
					hand.HoverUnlock(interactable);
					handHoverLocked = null;
				}

				driving = false;
				grabbedWithType = GrabTypes.None;
			}

			if (driving && isGrabEnding == false && hand.hoveringInteractable == this.interactable)
			{
				ComputeAngle(hand);
				UpdateAll();
			}
		}


		//-------------------------------------------------
		private Vector3 ComputeToTransformProjected(Transform xForm, Vector3 Normal)
		{
			Vector3 toTransform = (xForm.position - transform.position).normalized; //get 3D movement
			Vector3 toTransformProjected = new Vector3(0.0f, 0.0f, 0.0f);

			// Need a non-zero distance from the hand to the center of the sphericalDrive
			if (toTransform.sqrMagnitude > 0.0f) //if movement
			{
				toTransformProjected = Vector3.ProjectOnPlane(toTransform, Normal).normalized;
			}
			else
			{
				Debug.LogFormat("<b>[SteamVR Interaction]</b> The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString());
				Debug.Assert(false, string.Format("<b>[SteamVR Interaction]</b> The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString()));
			}

			if (debugPath && dbgPathLimit > 0)
			{
				DrawDebugPath(xForm, toTransformProjected); //todo might not work
			}

			return toTransformProjected;
		}


		//-------------------------------------------------
		private void DrawDebugPath(Transform xForm, Vector3 toTransformProjected)
		{
			if (dbgObjectCount == 0)
			{
				dbgObjectsParent = new GameObject("Circular Drive Debug");
				dbgHandObjects = new GameObject[dbgPathLimit];
				dbgProjObjects = new GameObject[dbgPathLimit];
				dbgObjectCount = dbgPathLimit;
				dbgObjectIndex = 0;
			}

			//Actual path
			GameObject gSphere = null;

			if (dbgHandObjects[dbgObjectIndex])
			{
				gSphere = dbgHandObjects[dbgObjectIndex];
			}
			else
			{
				gSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				gSphere.transform.SetParent(dbgObjectsParent.transform);
				dbgHandObjects[dbgObjectIndex] = gSphere;
			}

			gSphere.name = string.Format("actual_{0}", (int)((1.0f - red.r) * 10.0f));
			gSphere.transform.position = xForm.position;
			gSphere.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			gSphere.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
			gSphere.gameObject.GetComponent<Renderer>().material.color = red;

			if (red.r > 0.1f)
			{
				red.r -= 0.1f;
			}
			else
			{
				red.r = 1.0f;
			}

			//Projected path
			gSphere = null;

			if (dbgProjObjects[dbgObjectIndex])
			{
				gSphere = dbgProjObjects[dbgObjectIndex];
			}
			else
			{
				gSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				gSphere.transform.SetParent(dbgObjectsParent.transform);
				dbgProjObjects[dbgObjectIndex] = gSphere;
			}

			gSphere.name = string.Format("projed_{0}", (int)((1.0f - green.g) * 10.0f));
			gSphere.transform.position = transform.position + toTransformProjected * 0.25f;
			gSphere.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
			gSphere.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
			gSphere.gameObject.GetComponent<Renderer>().material.color = green;

			if (green.g > 0.1f)
			{
				green.g -= 0.1f;
			}
			else
			{
				green.g = 1.0f;
			}

			dbgObjectIndex = (dbgObjectIndex + 1) % dbgObjectCount;
		}


		//-------------------------------------------------
		// Updates the LinearMapping value from the angle
		//-------------------------------------------------
		private void UpdateLinearMapping()
		{
			if (limited)
			{
				// Map it to a [0, 1] value
				threeAxisMapping.values["X"] = (outXAngle - minAngle.x) / (maxAngle.x - minAngle.x);
				threeAxisMapping.values["Y"] = (outYAngle - minAngle.y) / (maxAngle.y - minAngle.y);
				threeAxisMapping.values["Z"] = (outZAngle - minAngle.z) / (maxAngle.z - minAngle.z);
			}
			else
			{
				// Normalize to [0, 1] based on 360 degree windings
				float flTmp = outXAngle / 360.0f;
				threeAxisMapping.values["X"] = flTmp - Mathf.Floor(flTmp);

				flTmp = outYAngle / 360.0f;
				threeAxisMapping.values["Y"] = flTmp - Mathf.Floor(flTmp);

				flTmp = outZAngle / 360.0f;
				threeAxisMapping.values["Z"] = flTmp - Mathf.Floor(flTmp);
			}

			UpdateDebugText();
		}


		//-------------------------------------------------
		// Updates the LinearMapping value from the angle
		//-------------------------------------------------
		private void UpdateGameObject()
		{
			if (rotateGameObject) //get absolute rotation from adding all virtual angles to the relative starting point
			{
				Quaternion rTemp = start * Quaternion.AngleAxis(outXAngle, localPlaneNormalX);
				rTemp *= Quaternion.AngleAxis(outYAngle, localPlaneNormalY);
				rTemp *= Quaternion.AngleAxis(outZAngle, localPlaneNormalZ);
				transform.localRotation = rTemp;
			}
		}


		//-------------------------------------------------
		// Updates the Debug TextMesh with the linear mapping value and the angle
		//-------------------------------------------------
		private void UpdateDebugText()
		{
			if (debugText)
			{
				debugText.text = string.Format("Linear: {0}\nAngle:  {1}\n", threeAxisMapping.values.ToString(), new string(outXAngle + " | " + outYAngle + " | " + outZAngle));
			}
		}


		//-------------------------------------------------
		// Updates the Debug TextMesh with the linear mapping value and the angle
		//-------------------------------------------------
		private void UpdateAll()
		{
			UpdateLinearMapping();
			UpdateGameObject();
			UpdateDebugText();
		}


		//-------------------------------------------------
		// Computes the angle to rotate the game object based on the change in the transform
		//-------------------------------------------------
		private void ComputeAngle(Hand hand) //todo currently x axis only and only for axial movement
		{
			//get projection of hand position
			Dictionary<string, Vector3> toHandProjected;
			Vector3 toHandProjectedOnX = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormalX);
			Vector3 toHandProjectedOnY = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormalY);
			Vector3 toHandProjectedOnZ = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormalZ);

			if (!toHandProjectedOnX.Equals(lastHandProjectedOnX)) //has there been movement?
			{
				float absXAngleDelta = Vector3.Angle(lastHandProjectedOnX, toHandProjectedOnX); //get time delta angle of projections

				if (absXAngleDelta > 0.0f) //is angle positive? savety check
				{
					if (frozen) //is frozen but movement is attemptet -> indicate locked state
					{
						float frozenSqDist = (hand.hoverSphereTransform.position - frozenHandWorldPos).sqrMagnitude;
						if (frozenSqDist > frozenSqDistanceMinMaxThreshold.x)
						{
							outXAngle = frozenXAngle + Random.Range(-1.0f, 1.0f);

							float magnitude = Util.RemapNumberClamped(frozenSqDist, frozenSqDistanceMinMaxThreshold.x, frozenSqDistanceMinMaxThreshold.y, 0.0f, 1.0f);
							if (magnitude > 0)
							{
								StartCoroutine(HapticPulses(hand, magnitude, 10));
							}
							else
							{
								StartCoroutine(HapticPulses(hand, 0.5f, 10));
							}

							if (frozenSqDist >= frozenSqDistanceMinMaxThreshold.y)
							{
								onFrozenDistanceThreshold.Invoke();
							}
						}
					}
					else //is not frozen
					{
						Vector3 cross = Vector3.Cross(lastHandProjectedOnX, toHandProjectedOnX).normalized; //get normal to moved angle
						float dot = Vector3.Dot(worldPlaneNormalX, cross);

						float signedXAngleDelta = (dot < 0.0f) ? -absXAngleDelta : absXAngleDelta; //assign direction to angle

						if (limited)
						{
							float angleXTmp = Mathf.Clamp(outXAngle + signedXAngleDelta, minAngle.x, maxAngle.x); //clamp on axis

							if (outXAngle == minAngle.x) //is min
							{
								if (angleXTmp > minAngle.x && absXAngleDelta < minMaxAngularThreshold) //ignore small movement
								{
									outXAngle = angleXTmp;
									lastHandProjectedOnX = toHandProjectedOnX;
								}
							}
							else if (outXAngle == maxAngle.x) //is max
							{
								if (angleXTmp < maxAngle.x && absXAngleDelta < minMaxAngularThreshold) //ignore small movement
								{
									outXAngle = angleXTmp;
									lastHandProjectedOnX = toHandProjectedOnX;
								}
							}
							else if (angleXTmp == minAngle.x)
							{
								outXAngle = angleXTmp;
								lastHandProjectedOnX = toHandProjectedOnX;
								onXMinAngle.Invoke();
								if (freezeXOnMin)
								{
									Freeze(hand);
								}
							}
							else if (angleXTmp == maxAngle.x)
							{
								outXAngle = angleXTmp;
								lastHandProjectedOnX = toHandProjectedOnX;
								onXMaxAngle.Invoke();
								if (freezeXOnMax)
								{
									Freeze(hand);
								}
							}
							else
							{
								outXAngle = angleXTmp;
								lastHandProjectedOnX = toHandProjectedOnX;
							}
						}
						else
						{
							outXAngle += signedXAngleDelta;
							lastHandProjectedOnX = toHandProjectedOnX;
						}
					}
				}
			}
		}
	}
}
