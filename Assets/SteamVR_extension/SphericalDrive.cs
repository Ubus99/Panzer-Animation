//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Interactable that can be used to move in a circular motion
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	[RequireComponent(typeof(Interactable))]
	public class SphericalDrive : MonoBehaviour
	{
		public enum Mode
		{
			Disabled,
			Lever,
			Grip
		};

		[Tooltip("The axis' around which the circular drive will rotate in local space")]
		public Mode rotateX = Mode.Disabled;

		public Mode rotateY = Mode.Disabled;
		public Mode rotateZ = Mode.Disabled;

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
		public Vector3 outAngle;

		private enum Axis : int
		{
			X, Y, Z
		};

		private Quaternion start;

		private Dictionary<Axis, Vector3> worldPlaneNormals = new Dictionary<Axis, Vector3>
		{
			{Axis.X, Vector3.right},
			{Axis.Y, Vector3.up},
			{Axis.Z, Vector3.forward}
		};

		private Dictionary<Axis, Vector3> localPlaneNormals = new Dictionary<Axis, Vector3>
		{
			{Axis.X, Vector3.right},
			{Axis.Y, Vector3.up},
			{Axis.Z, Vector3.forward}
		};

		/// <summary>
		/// buffers the last hand location projected on the YZ plane
		/// </summary>
		private Dictionary<Axis, Vector3> lastHandVectorProjected = new Dictionary<Axis, Vector3>
		{
			{Axis.X, new Vector3() },
			{Axis.Y, new Vector3() },
			{Axis.Z, new Vector3() }
		};

		private Dictionary<Axis, float> lastHandAngleProjected = new Dictionary<Axis, float>
		{
			{Axis.X, 0.0f },
			{Axis.Y,  0.0f },
			{Axis.Z,  0.0f }
		};

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
		private Vector3 frozenAngle = new Vector3();
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

			if (transform.parent)
			{
				worldPlaneNormals[Axis.X] = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormals[Axis.X]).normalized;
				worldPlaneNormals[Axis.Y] = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormals[Axis.Y]).normalized;
				worldPlaneNormals[Axis.Z] = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormals[Axis.Z]).normalized;
			}

			if (limited)
			{
				start = Quaternion.identity;
				outAngle = transform.localEulerAngles;

				if (forceStart)
				{
					outAngle.x = Mathf.Clamp(startXAngle, minAngle.x, maxAngle.x);
					outAngle.y = Mathf.Clamp(startYAngle, minAngle.y, maxAngle.y);
					outAngle.z = Mathf.Clamp(startZAngle, minAngle.z, maxAngle.z);
				}
			}
			else //for each selected axis, set absolute start angle
			{
				start = Quaternion.identity;
				switch (rotateX)
				{
					case Mode.Lever:
						start *= Quaternion.AngleAxis(transform.localEulerAngles.x, localPlaneNormals[Axis.X]);
						break;

					case Mode.Grip:
						break;
				}
				switch (rotateY)
				{
					case Mode.Lever:
						start *= Quaternion.AngleAxis(transform.localEulerAngles.y, localPlaneNormals[Axis.Y]);
						break;

					case Mode.Grip:
						break;
				}
				switch (rotateZ)
				{
					case Mode.Lever:
						start *= Quaternion.AngleAxis(transform.localEulerAngles.z, localPlaneNormals[Axis.Z]);
						break;

					case Mode.Grip:
						break;
				}

				//reset relative start angle
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
		private void OnDisable()
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
				lastHandVectorProjected[Axis.X] = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormals[Axis.X]);
				lastHandVectorProjected[Axis.Y] = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormals[Axis.Y]);
				lastHandVectorProjected[Axis.Z] = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormals[Axis.Z]);

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
				threeAxisMapping.values["X"] = (outAngle.x - minAngle.x) / (maxAngle.x - minAngle.x);
				threeAxisMapping.values["Y"] = (outAngle.y - minAngle.y) / (maxAngle.y - minAngle.y);
				threeAxisMapping.values["Z"] = (outAngle.z - minAngle.z) / (maxAngle.z - minAngle.z);
			}
			else
			{
				// Normalize to [0, 1] based on 360 degree windings
				Vector3 flTmp = outAngle / 360.0f;
				threeAxisMapping.values["X"] = flTmp.x - Mathf.Floor(flTmp.x);
				threeAxisMapping.values["Y"] = flTmp.y - Mathf.Floor(flTmp.y);
				threeAxisMapping.values["Z"] = flTmp.z - Mathf.Floor(flTmp.z);
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
				Quaternion rTemp = start * Quaternion.AngleAxis(outAngle.x, localPlaneNormals[Axis.X]);
				rTemp *= Quaternion.AngleAxis(outAngle.y, localPlaneNormals[Axis.Y]);
				rTemp *= Quaternion.AngleAxis(outAngle.z, localPlaneNormals[Axis.Z]);
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
				debugText.text = string.Format("Linear: {0}\nAngle:  {1}\n", threeAxisMapping.values.ToString(), new string(outAngle.ToString()));
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
			switch (rotateX)
			{
				case Mode.Lever:
					ComputeLeverAngle(hand, Axis.X, freezeXOnMax, onXMaxAngle);
					break;

				case Mode.Grip:
					ComputeGripAngle(hand, Axis.X, freezeXOnMax, onXMaxAngle);
					break;
			}

			switch (rotateY)
			{
				case Mode.Lever:
					ComputeLeverAngle(hand, Axis.Y, freezeYOnMax, onYMaxAngle);
					break;

				case Mode.Grip:
					ComputeGripAngle(hand, Axis.Y, freezeYOnMax, onYMaxAngle);
					break;
			}

			switch (rotateZ)
			{
				case Mode.Lever:
					ComputeLeverAngle(hand, Axis.Z, freezeZOnMax, onZMaxAngle);
					break;

				case Mode.Grip:
					ComputeGripAngle(hand, Axis.Z, freezeZOnMax, onZMaxAngle);
					break;
			}
		}

		//-------------------------------------------------
		// Computes the angle if the hand is more than zero units from the object
		//-------------------------------------------------
		private void ComputeLeverAngle(Hand hand, Axis axis, bool freezeOnMax, UnityEvent onMaxAngle) //todo problems locking to border
		{
			//get projection of hand position
			Vector3 toHandProjected = ComputeToTransformProjected(hand.hoverSphereTransform, worldPlaneNormals[axis]);

			if (!toHandProjected.Equals(lastHandVectorProjected[axis])) //has there been movement?
			{
				float absAngleDelta = Vector3.Angle(lastHandVectorProjected[axis], toHandProjected); //get time delta angle of projections

				if (absAngleDelta > 0.0f) //is angle positive? savety check
				{
					if (frozen) //is frozen but movement is attemptet -> indicate locked state
					{
						float frozenSqDist = (hand.hoverSphereTransform.position - frozenHandWorldPos).sqrMagnitude;
						if (frozenSqDist > frozenSqDistanceMinMaxThreshold.x)
						{
							outAngle[(int)axis] = frozenAngle[(int)axis] + Random.Range(-1.0f, 1.0f);

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
						Vector3 cross = Vector3.Cross(lastHandVectorProjected[axis], toHandProjected).normalized; //get normal to moved angle
						float dot = Vector3.Dot(worldPlaneNormals[axis], cross);

						float signedAngleDelta = (dot < 0.0f) ? -absAngleDelta : absAngleDelta; //assign direction to angle

						if (limited)
						{
							float angleTmp = Mathf.Clamp(outAngle[(int)axis] + signedAngleDelta, minAngle[(int)axis], maxAngle[(int)axis]); //clamp on axis

							if (outAngle[(int)axis] == minAngle[(int)axis]) //is min
							{
								if (angleTmp > minAngle[(int)axis] && absAngleDelta < minMaxAngularThreshold) //ignore small movement
								{
									outAngle[(int)axis] = angleTmp;
									lastHandVectorProjected[axis] = toHandProjected;
								}
							}
							else if (outAngle[(int)axis] == maxAngle[(int)axis]) //is max
							{
								if (angleTmp < maxAngle[(int)axis] && absAngleDelta < minMaxAngularThreshold) //ignore small movement
								{
									outAngle[(int)axis] = angleTmp;
									lastHandVectorProjected[axis] = toHandProjected;
								}
							}
							else if (angleTmp == minAngle[(int)axis])
							{
								outAngle[(int)axis] = angleTmp;
								lastHandVectorProjected[axis] = toHandProjected;
								onMaxAngle.Invoke();
								if (freezeOnMax)
								{
									Freeze(hand);
								}
							}
							else if (angleTmp == maxAngle[(int)axis])
							{
								outAngle[(int)axis] = angleTmp;
								lastHandVectorProjected[axis] = toHandProjected;
								onMaxAngle.Invoke();
								if (freezeOnMax)
								{
									Freeze(hand);
								}
							}
							else
							{
								outAngle[(int)axis] = angleTmp;
								lastHandVectorProjected[axis] = toHandProjected;
							}
						}
						else
						{
							outAngle[(int)axis] += signedAngleDelta;
							lastHandVectorProjected[axis] = toHandProjected;
						}
					}
				}
			}
		}

		//-------------------------------------------------
		// Computes the angle if the hand has a distance of zero to the object
		//-------------------------------------------------
		private void ComputeGripAngle(Hand hand, Axis axis, bool freezeOnMax, UnityEvent onMaxAngle) //todo ported from lever mode test
		{
			//get projection of hand position
			float currentHandAngle = hand.hoverSphereTransform.eulerAngles[(int)axis]; //todo get angle along global axis

			if (!currentHandAngle.Equals(lastHandAngleProjected[axis])) //has there been movement?
			{
				float absAngleDelta = lastHandAngleProjected[axis] - currentHandAngle; //get time delta angle of projections

				if (frozen) //is frozen but movement is attemptet -> indicate locked state
				{
					float frozenSqDist = (hand.hoverSphereTransform.position - frozenHandWorldPos).sqrMagnitude;
					if (frozenSqDist > frozenSqDistanceMinMaxThreshold.x)
					{
						outAngle[(int)axis] = frozenAngle[(int)axis] + Random.Range(-1.0f, 1.0f);

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
					else //is not frozen
					{
						//Vector3 cross = Vector3.Cross(lastHandVectorProjected[axis], currentHandAngle).normalized; //get normal to moved angle
						//float dot = Vector3.Dot(worldPlaneNormals[axis], cross);

						//float signedAngleDelta = (dot < 0.0f) ? -absAngleDelta : absAngleDelta; //assign direction to angle

						if (limited)
						{
							//float angleTmp = Mathf.Clamp(outAngle[(int)axis] + signedAngleDelta, minAngle[(int)axis], maxAngle[(int)axis]); //clamp on axis
							float angleTmp = Mathf.Clamp(outAngle[(int)axis] + currentHandAngle, minAngle[(int)axis], maxAngle[(int)axis]); //clamp on axis

							if (outAngle[(int)axis] == minAngle[(int)axis]) //is min
							{
								if (angleTmp > minAngle[(int)axis] && absAngleDelta < minMaxAngularThreshold) //ignore small movement
								{
									outAngle[(int)axis] = angleTmp;
									lastHandAngleProjected[axis] = currentHandAngle;
								}
							}
							else if (outAngle[(int)axis] == maxAngle[(int)axis]) //is max
							{
								if (angleTmp < maxAngle[(int)axis] && absAngleDelta < minMaxAngularThreshold) //ignore small movement
								{
									outAngle[(int)axis] = angleTmp;
									lastHandAngleProjected[axis] = currentHandAngle;
								}
							}
							else if (angleTmp == minAngle[(int)axis])
							{
								outAngle[(int)axis] = angleTmp;
								lastHandAngleProjected[axis] = currentHandAngle;
								onMaxAngle.Invoke();
								if (freezeOnMax)
								{
									Freeze(hand);
								}
							}
							else if (angleTmp == maxAngle[(int)axis])
							{
								outAngle[(int)axis] = angleTmp;
								lastHandAngleProjected[axis] = currentHandAngle;
								onMaxAngle.Invoke();
								if (freezeOnMax)
								{
									Freeze(hand);
								}
							}
							else
							{
								outAngle[(int)axis] = angleTmp;
								lastHandAngleProjected[axis] = currentHandAngle;
							}
						}
						else
						{
							//outAngle[(int)axis] += signedAngleDelta;
							outAngle[(int)axis] += currentHandAngle;
							lastHandAngleProjected[axis] = currentHandAngle;
						}
					}
				}
			}
		}
	}
}