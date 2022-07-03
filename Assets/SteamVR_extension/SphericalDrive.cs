//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Interactable that can be used to move in a circular motion
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Valve.VR.InteractionSystem
{

	//-------------------------------------------------------------------------
	[RequireComponent(typeof(Interactable))]
	public class SphericalDrive : MonoBehaviour
	{
		public enum Axis_t
		{
			XAxis,
			YAxis,
			ZAxis
		};

		//[Tooltip("The axis around which the circular drive will rotate in local space")]
		/*public Axis_t axisOfRotation = Axis_t.XAxis;*/ //todo make axis locabel

		[Tooltip("Child GameObject which has the Collider component to initiate interaction, only needs to be set if there is more than one Collider child")]
		public Collider childCollider = null;

		[Tooltip("A LinearMapping component to drive, if not specified one will be dynamically added to this GameObject")]
		public nDMapping nDMapping;

		[Tooltip("If true, the drive will stay manipulating as long as the button is held down, if false, it will stop if the controller moves out of the collider")]
		public bool hoverLock = false;

		[HeaderAttribute("Limited Rotation")]
		[Tooltip("If true, the rotation will be limited to [minAngle, maxAngle], if false, the rotation is unlimited")]
		public bool limited = false;
		public Vector2 frozenDistanceMinMaxThreshold = new Vector2(0.1f, 0.2f);
		public UnityEvent onFrozenDistanceThreshold;

		[HeaderAttribute("Limited Rotation Min")]
		[Tooltip("If limited is true, the specifies the lower limit, otherwise value is unused")]
		public Vector3 minAngle = new(-45.0f, -45.0f, -45.0f);

		[Tooltip("If limited, set whether drive will freeze its angle when the min angle is reached")]
		public bool freezeOnMinX = false;
		[Tooltip("If limited, event invoked when minAngle is reached")]
		public UnityEvent onMinXAngle;

		[Tooltip("If limited, set whether drive will freeze its angle when the min angle is reached")]
		public bool freezeOnMinY = false;
		[Tooltip("If limited, event invoked when minAngle is reached")]
		public UnityEvent onMinYAngle;

		[Tooltip("If limited, set whether drive will freeze its angle when the min angle is reached")]
		public bool freezeOnMinZ = false;
		[Tooltip("If limited, event invoked when minAngle is reached")]
		public UnityEvent onMinZAngle;

		[HeaderAttribute("Limited Rotation Max")]
		[Tooltip("If limited is true, the specifies the upper limit, otherwise value is unused")]
		public Vector3 maxAngle = new(45.0f, 45.0f, 45.0f);

		[Tooltip("If limited, set whether drive will freeze its angle when the max angle is reached")]
		public bool freezeOnMaxX = false;
		[Tooltip("If limited, event invoked when maxAngle is reached")]
		public UnityEvent onMaxXAngle;

		[Tooltip("If limited, set whether drive will freeze its angle when the max angle is reached")]
		public bool freezeOnMaxY = false;
		[Tooltip("If limited, event invoked when maxAngle is reached")]
		public UnityEvent onMaxYAngle;

		[Tooltip("If limited, set whether drive will freeze its angle when the max angle is reached")]
		public bool freezeOnMaxZ = false;
		[Tooltip("If limited, event invoked when maxAngle is reached")]
		public UnityEvent onMaxZAngle;

		[Tooltip("If limited is true, this forces the starting angle to be startAngle, clamped to [minAngle, maxAngle]")]
		public bool forceStart = false;
		[Tooltip("If limited is true and forceStart is true, the starting angle will be this, clamped to [minAngle, maxAngle]")]
		//public float startAngle = 0.0f;
		public Vector3 startAngle = new Vector3(0.0f, 0.0f, 0.0f);

		[Tooltip("If true, the transform of the GameObject this component is on will be rotated accordingly")]
		public bool rotateGameObject = true;

		[Tooltip("If true, the path of the Hand (red) and the projected value (green) will be drawn")]
		public bool debugPath = false;
		[Tooltip("If debugPath is true, this is the maximum number of GameObjects to create to draw the path")]
		public int dbgPathLimit = 50;

		[Tooltip("If not null, the TextMesh will display the linear value and the angular value of this circular drive")]
		public TextMesh debugText = null;

		[Tooltip("The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations")]
		public Vector3 outAngle;

		/// <summary>
		/// Default angle
		/// </summary>
		private Quaternion start;

		private Vector3 worldPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);
		private Vector3 localPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);

		/// <summary>
		/// last hand location
		/// </summary>
		private Vector3 lastHandProjected;

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
		private Vector3 frozenAngle = new Vector3(0.0f, 0.0f, 0.0f);
		private Vector3 frozenHandWorldPos = new Vector3(0.0f, 0.0f, 0.0f);
		private Vector2 frozenSqDistanceMinMaxThreshold = new Vector2(0.0f, 0.0f);

		private Hand handHoverLocked = null;

		private Interactable interactable;

		//-------------------------------------------------
		private void Freeze(Hand hand)
		{
			frozen = true;
			frozenAngle = outAngle;
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
			//todo get collider in children?
			if (childCollider == null)
			{
				childCollider = GetComponentInChildren<Collider>();
			}

			//grab nDMapping from Object
			if (nDMapping == null)
			{
				nDMapping = GetComponent<nDMapping>();
				if (nDMapping != null)
				{
					nDMapping.values.TryAdd("X", 0.0f);
					nDMapping.values.TryAdd("Y", 0.0f);
					nDMapping.values.TryAdd("Z", 0.0f);
				}
			}

			//if grabbing was unsuccessfull, create new
			if (nDMapping == null)
			{
				nDMapping = gameObject.AddComponent<nDMapping>();
				nDMapping.values.TryAdd("X", 0.0f);
				nDMapping.values.TryAdd("Y", 0.0f);
				nDMapping.values.TryAdd("Z", 0.0f);
			}

			//reference worldspace
			worldPlaneNormal = new Vector3(0.0f, 0.0f, 0.0f);
			//worldPlaneNormal[(int)axisOfRotation] = 1.0f; TODO

			localPlaneNormal = worldPlaneNormal;

			//add parenting offset to worldspace
			if (transform.parent)
			{
				worldPlaneNormal = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormal).normalized;
			}

			//clamp angles?
			if (limited)
			{
				//reset start rotation
				start = Quaternion.identity;
				outAngle = transform.localEulerAngles;

				//clamps start angle
				if (forceStart)
				{
					outAngle.x = Mathf.Clamp(startAngle.x, minAngle.x, maxAngle.x);
					outAngle.y = Mathf.Clamp(startAngle.y, minAngle.y, maxAngle.y);
					outAngle.z = Mathf.Clamp(startAngle.z, minAngle.z, maxAngle.z);
				}
			}
			else //dont clamp angles
			{
				//set start rotation to a quarternion with a rotation aligned to the local normal
				//start = Quaternion.AngleAxis(transform.localEulerAngles[(int)axisOfRotation], localPlaneNormal);
				start = Quaternion.identity; //todo test
				outAngle = new Vector3(0.0f, 0.0f, 0.0f);
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
		
		/// <summary>
		/// todo update functoin desriptoin
		/// </summary>
		/// <param name="hand"></param>
		private void HandHoverUpdate(Hand hand)
		{
			GrabTypes startingGrabType = hand.GetGrabStarting();
			bool isGrabEnding = hand.IsGrabbingWithType(grabbedWithType) == false;

			if (grabbedWithType == GrabTypes.None && startingGrabType != GrabTypes.None)
			{
				grabbedWithType = startingGrabType;
				// Trigger was just pressed
				lastHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);

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
		private Vector3 ComputeToTransformProjected(Transform xForm)
		{
			Vector3 toTransform = (xForm.position - transform.position).normalized;
			Vector3 toTransformProjected = new Vector3(0.0f, 0.0f, 0.0f);

			// Need a non-zero distance from the hand to the center of the CircularDrive
			if (toTransform.sqrMagnitude > 0.0f)
			{
				toTransformProjected = Vector3.ProjectOnPlane(toTransform, worldPlaneNormal).normalized;
			}
			else
			{
				Debug.LogFormat("<b>[SteamVR Interaction]</b> The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString());
				Debug.Assert(false, string.Format("<b>[SteamVR Interaction]</b> The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString()));
			}

			if (debugPath && dbgPathLimit > 0)
			{
				DrawDebugPath(xForm, toTransformProjected);
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
				nDMapping.values["X"] = (outAngle.x - minAngle.x) / (maxAngle.x - minAngle.x);
				nDMapping.values["Y"] = (outAngle.y - minAngle.y) / (maxAngle.y - minAngle.y);
				nDMapping.values["Z"] = (outAngle.z - minAngle.z) / (maxAngle.z - minAngle.z);
			}
			else
			{
				// Normalize to [0, 1] based on 360 degree windings
				Vector3 flTmp = outAngle / 360.0f;
				nDMapping.values["X"] = flTmp.x - Mathf.Floor(flTmp.x);
				nDMapping.values["Y"] = flTmp.y - Mathf.Floor(flTmp.y);
				nDMapping.values["Z"] = flTmp.z - Mathf.Floor(flTmp.z);
			}

			UpdateDebugText();
		}


		//-------------------------------------------------
		// Updates the LinearMapping value from the angle
		//-------------------------------------------------
		private void UpdateGameObject() //todo was?
		{
			if (rotateGameObject)
			{
				//transform.localRotation = start * Quaternion.AngleAxis(outAngle, localPlaneNormal);
			}
		}


		//-------------------------------------------------
		// Updates the Debug TextMesh with the linear mapping value and the angle
		//-------------------------------------------------
		private void UpdateDebugText() //todo debug mode
		{
			if (debugText)
			{
				//debugText.text = string.Format("Linear: {0}\nAngle:  {1}\n", nDMapping.value, outAngle);
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
		private void ComputeAngle(Hand hand)
		{
			Vector3 toHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform);

			if (!toHandProjected.Equals(lastHandProjected))
			{
				//calc delta of hand positions
				Vector3 absAngleDelta = new Vector3(Vector3.SignedAngle(lastHandProjected, toHandProjected, Vector3.right),
										Vector3.SignedAngle(lastHandProjected, toHandProjected, Vector3.up),
										Vector3.SignedAngle(lastHandProjected, toHandProjected, Vector3.forward));

				if (absAngleDelta.magnitude > 0.0f) //is not null
				{
					if (frozen)
					{
						float frozenSqDist = (hand.hoverSphereTransform.position - frozenHandWorldPos).sqrMagnitude;
						if (frozenSqDist > frozenSqDistanceMinMaxThreshold.x)   // todo was?
						{
							//outAngle = frozenAngle + new Vector3(1.0f * Random.Range(-1.0f, 1.0f), 1.0f * Random.Range(-1.0f, 1.0f), 1.0f * Random.Range(-1.0f, 1.0f));
							outAngle = frozenAngle;//todo freeze angle

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
					else // if not frozen
					{
						Vector3 cross = Vector3.Cross(lastHandProjected, toHandProjected).normalized;
						float dot = Vector3.Dot(worldPlaneNormal, cross);

						//absAngleDelta signed used for
						Vector3 signedAngleDelta = absAngleDelta;

						if (dot < 0.0f) //calc vector direction in space from dot
						{
							signedAngleDelta = -signedAngleDelta;
						}


						for (int i = 0; i > 3; i++)
						{
							if (limited)
							{
								Vector3 angleTmp = new Vector3(0.0f, 0.0f, 0.0f);
								//angleTmp.x = Mathf.Clamp(outAngle.x + signedAngleDelta, minAngle, maxAngle);
								angleTmp[i] = Mathf.Clamp(outAngle[i] + signedAngleDelta[i], minAngle[i], maxAngle[i]);

								if (outAngle[i] == minAngle[i])  //is min
								{
									if (angleTmp[i] > minAngle[i] && absAngleDelta[i] < minMaxAngularThreshold) //todo absAngleDelta
									{
										outAngle = angleTmp;
										lastHandProjected = toHandProjected;
									}
								}
								else if (outAngle[i] == maxAngle[i]) // is max
								{
									if (angleTmp[i] < maxAngle[i] && absAngleDelta[i] < minMaxAngularThreshold) //todo absAngleDelta
									{
										outAngle = angleTmp;
										lastHandProjected = toHandProjected;
									}
								}
								else if (angleTmp[i] == minAngle[i]) // is clamped min
								{
									outAngle = angleTmp;
									lastHandProjected = toHandProjected;
									onMinXAngle.Invoke();
									if (freezeOnMinX)
									{
										Freeze(hand);
									}
								}
								else if (angleTmp[i] == maxAngle[i]) // is clamped max
								{
									outAngle = angleTmp;
									lastHandProjected = toHandProjected;
									onMaxXAngle.Invoke();
									if (freezeOnMaxX)
									{
										Freeze(hand);
									}
								}
								else // not at limit
								{
									outAngle = angleTmp;
									lastHandProjected = toHandProjected;
								}
							}
							else // not limited
							{
								outAngle[i] += signedAngleDelta[i];
								lastHandProjected = toHandProjected;
							}
						}
					}
				}
			}
		}
	}
}
