using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace Demo {

	// this class is a bit like the built-in CycleForceProducer but can only attach to rigidbodies within the containing atom
	// and force/torque is relative to the body itself by choosing apply axis rather than world axis
	public class PuppeteerForce : MVRScript {

		protected Rigidbody RB;
		protected void SyncReceiver(string receiver) {
			if (receiver != null) {
				ForceReceiver fr;
				if (receiverNameToForceReceiver.TryGetValue(receiver, out fr)) {
					RB = fr.GetComponent<Rigidbody>();
				} else {
					RB = null;
				}
			} else {
				RB = null;
			}
		}
		protected JSONStorableStringChooser receiverChoiceJSON;

        protected Rigidbody RBMaster;
        protected void SyncMaster(string master)
        {
            if (master != null)
            {
                ForceReceiver fr;
                if (receiverNameToForceReceiver.TryGetValue(master, out fr))
                {
                    RBMaster = fr.GetComponent<Rigidbody>();
                }
                else
                {
                    RBMaster = null;
                }
            }
            else
            {
                RBMaster = null;
            }
        }
        protected JSONStorableStringChooser masterChoiceJSON;

        protected ForceProducerV2.AxisName _masterAxis = ForceProducerV2.AxisName.Y;
        protected JSONStorableStringChooser masterAxisJSON;
        protected JSONStorableStringChooser masterAxisTypeJSON;

        protected ForceProducerV2.AxisName _puppetAxis = ForceProducerV2.AxisName.Y;
        protected JSONStorableStringChooser puppetAxisJSON;
        protected JSONStorableStringChooser puppetAxisTypeJSON;


        protected void SyncMasterAxis(string axisName)     {SyncAxis(ref _masterAxis  , axisName); }
        protected void SyncPuppetAxis(string axisName)     {SyncAxis(ref _puppetAxis  , axisName); }

        protected void SyncAxis(ref ForceProducerV2.AxisName axis, string axisName) {
			try {
				ForceProducerV2.AxisName an = (ForceProducerV2.AxisName)System.Enum.Parse(typeof(ForceProducerV2.AxisName), axisName);
                axis = an;
			}
			catch (System.ArgumentException) {
				Debug.LogError("Attempt to set axis to " + axisName + " which is not a valid axis name");
			}
		}

        protected JSONStorableFloat masterMultiplierJSON;

        protected JSONStorableFloat puppetMaxForceJSON;
        protected JSONStorableFloat pForceJSON;

		protected List<string> receiverChoices;
		protected Dictionary<string, ForceReceiver> receiverNameToForceReceiver;

        protected List<string> masterChoices;
        protected Dictionary<string, ForceReceiver> masterNameToForceMaster;

        public override void Init() {
			try {
                //SuperController.singleton.GetAllFreeControllers();
                masterChoices = new List<string>();
                masterNameToForceMaster = new Dictionary<string, ForceReceiver>();
                foreach (ForceReceiver fr in containingAtom.forceReceivers)
                {
                    masterChoices.Add(fr.name);
                    masterNameToForceMaster.Add(fr.name, fr);
                }
                masterChoiceJSON = new JSONStorableStringChooser("master", masterChoices, null, "Master", SyncMaster);
                masterChoiceJSON.storeType = JSONStorableParam.StoreType.Full;
                RegisterStringChooser(masterChoiceJSON);
                UIDynamicPopup dpMaster = CreateScrollablePopup(masterChoiceJSON);
                dpMaster.popupPanelHeight = 600f;
                dpMaster.popup.alwaysOpen = false;

                UIDynamic spacer = CreateSpacer(false);
                spacer.height = 300;


                string[] axisChoices = System.Enum.GetNames(typeof(ForceProducerV2.AxisName));
                List<string> axisChoicesList = new List<string>(axisChoices);
//                axisChoicesList.Insert(0, "None");

                masterAxisJSON = new JSONStorableStringChooser("MasterAxis", axisChoicesList, _masterAxis.ToString(), "Axis", SyncMasterAxis);
                masterAxisJSON.storeType = JSONStorableParam.StoreType.Full;
                RegisterStringChooser(masterAxisJSON);
                CreatePopup(masterAxisJSON, true);

                List<string> axisTypeList = new List<string>();
                axisTypeList.Add("Movement");  // Use velocity as input and force as output
                axisTypeList.Add("Rotation");  // Use angular velocity as input and torque as output
                                               // axisTypeList.Add("Placement"); // Use delta distance from starting point as direct output

                masterAxisTypeJSON = new JSONStorableStringChooser("MasterAxisType", axisTypeList, null, "Axis Type");
                masterAxisTypeJSON.storeType = JSONStorableParam.StoreType.Full;
                RegisterStringChooser(masterAxisTypeJSON);
                CreatePopup(masterAxisTypeJSON, true);

                masterMultiplierJSON = new JSONStorableFloat("Master Multiplier", 10.0f, 0.0f, 100f, false, true);
                masterMultiplierJSON.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(masterMultiplierJSON);
                CreateSlider(masterMultiplierJSON, true);

                CreateSpacer(true);


                receiverChoices = new List<string>();
				receiverNameToForceReceiver = new Dictionary<string, ForceReceiver>();
				foreach (ForceReceiver fr in containingAtom.forceReceivers) {
					receiverChoices.Add(fr.name);
					receiverNameToForceReceiver.Add(fr.name, fr);
				}
				receiverChoiceJSON = new JSONStorableStringChooser("receiver", receiverChoices, null, "Receiver", SyncReceiver);
				receiverChoiceJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterStringChooser(receiverChoiceJSON);
				UIDynamicPopup dp = CreateScrollablePopup(receiverChoiceJSON);
				dp.popupPanelHeight = 600f;
				dp.popup.alwaysOpen = false;

                puppetAxisJSON = new JSONStorableStringChooser("recieverAxis", axisChoicesList, _puppetAxis.ToString(), "Axis", SyncPuppetAxis);
				puppetAxisJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterStringChooser(puppetAxisJSON);
				CreatePopup(puppetAxisJSON,true);

                puppetAxisTypeJSON = new JSONStorableStringChooser("PuppetAxisType", axisTypeList, null, "Axis Type");
                puppetAxisTypeJSON.storeType = JSONStorableParam.StoreType.Full;
                RegisterStringChooser(puppetAxisTypeJSON);
                CreatePopup(puppetAxisTypeJSON, true);

                puppetMaxForceJSON = new JSONStorableFloat("Puppet Max Force", 0.0f, 0.0f, 500f, false, true);
                puppetMaxForceJSON.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(puppetMaxForceJSON);
                CreateSlider(puppetMaxForceJSON, true);

                pForceJSON = new JSONStorableFloat("pForce", 0.0f, -500.0f, 500.0f, false, true);
                pForceJSON.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(pForceJSON);
                CreateSlider(pForceJSON, true);

			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

        //protected float timer;
        //protected float forceTimer;
        //protected float flip;
        protected float pmForce;
		protected Vector3 target;
		protected Vector3 current;


		protected void Start() {
            SetTargets();
        }

		protected virtual Vector3 AxisToVector(ForceProducerV2.AxisName axis) {
			Vector3 result;
			if (RB != null) {
				switch (axis) {
					case ForceProducerV2.AxisName.X:
						result = RB.transform.right;
						break;
					case ForceProducerV2.AxisName.NegX:
						result = -RB.transform.right;
						break;
					case ForceProducerV2.AxisName.Y:
						result = RB.transform.up;
						break;
					case ForceProducerV2.AxisName.NegY:
						result = -RB.transform.up;
						break;
					case ForceProducerV2.AxisName.Z:
						result = RB.transform.forward;
						break;
					case ForceProducerV2.AxisName.NegZ:
						result = -RB.transform.forward;
						break;
					default:
						result = Vector3.zero;
						break;
				}
			} else {
				result = Vector3.zero;
			}
			return (result);
		}

        protected void SetTargets()
        {
            //SuperController.LogError(masterAxisTypeJSON.val);

            if (RBMaster && masterAxisTypeJSON.val == "Movement")
            {
                pForceJSON.val += getVelocity(RBMaster.velocity) * masterMultiplierJSON.val;
                if (pForceJSON.val > puppetMaxForceJSON.val)
                    pForceJSON.val = puppetMaxForceJSON.val;
                else if (pForceJSON.val < -puppetMaxForceJSON.val)
                    pForceJSON.val = -puppetMaxForceJSON.val;
            }
            else if (RBMaster && masterAxisTypeJSON.val == "Rotation")
            {
                //SuperController.LogError("Exception caught: " + getVelocity(RBMaster.angularVelocity));

                pForceJSON.val += getVelocity(RBMaster.angularVelocity) * masterMultiplierJSON.val;
                if (pForceJSON.val > puppetMaxForceJSON.val)
                    pForceJSON.val = puppetMaxForceJSON.val;
                else if (pForceJSON.val < -puppetMaxForceJSON.val)
                    pForceJSON.val = -puppetMaxForceJSON.val;
            }

            RegisterFloat(pForceJSON);
            target = AxisToVector(_puppetAxis) * pForceJSON.val;
         
		}

        protected float getVelocity(Vector3 velocity)
        {
            float result = 0.0f;
            switch (masterAxisJSON.val)
            {
                case "X":
                    result = velocity.x;
                    break;
                case "NegX":
                    result = -velocity.x;
                    break;
                case "Y":
                    result = velocity.y;
                    break;
                case "NegY":
                    result = -velocity.y;
                    break;
                case "Z":
                    result = velocity.z;
                    break;
                case "NegZ":
                    result = -velocity.z;
                    break;
                default:
                    result = 0.0f;
                    break;
            }

            return (float)Math.Round(result,5);
        }

		// Use Update for the timers since this can account for time scale
		protected void Update() {
            SetTargets();
        }

		// FixedUpdate is called with each physics simulation frame by Unity
		void FixedUpdate() {
			try {
				// apply forces here
				float timeFactor = Time.fixedDeltaTime;
				current = Vector3.Lerp(current, target, timeFactor);
				if (RB && (!SuperController.singleton || !SuperController.singleton.freezeAnimation)) {
                    if(puppetAxisTypeJSON.val == "Rotation")
                        RB.AddTorque(current, ForceMode.Force);
                    else
                        RB.AddForce(current, ForceMode.Force);
					
				}
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

	}
}