using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.AI.Condition.TargetWithinRange;

namespace ModularWeapons
{
    public class ModularPart : MonoBehaviour
    {
        public bool isWeapon;
        public bool keepHandles;
        public bool isFlail;

        [NonSerialized]
        public List<ConnectionPoint> connections;

        [NonSerialized]
        public Vector3 centerOfMass;

        [NonSerialized]
        public float mass;

        [NonSerialized]
        public Item Item;

        public bool SkipUngrab;

        private GameObject previewRenderer;
        private float angle;

        private void Start()
        {
            Item = GetComponent<Item>();
            connections = GetComponentsInChildren<ConnectionPoint>().ToList();
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null )
            {
                centerOfMass = rb.centerOfMass;
                mass = rb.mass;

                foreach (Handle handle in Item.handles)
                {
                    handle.UnGrabbed += Handle_UnGrabbed;
                }
            }

            previewRenderer = new GameObject("Preview Renderer");
            previewRenderer.transform.position = transform.position;
            previewRenderer.transform.rotation = transform.rotation;
            foreach (Renderer renderer in GetComponentsInChildren<MeshRenderer>())
            {
                GameObject obj = GameObject.Instantiate(renderer.gameObject, previewRenderer.transform, true);
            }
            previewRenderer.SetActive(false);

            StartCoroutine(Catalog.LoadAssetCoroutine<Material>("MW.PreviewMaterial", (mat) =>
            {
                foreach (Renderer rend in previewRenderer.GetComponentsInChildren<Renderer>())
                {
                    rend.material = mat;
                }
            }, name));

            //Setup unique names, for saving and loader later on

            //Part name: 
            //c70680b4-0660-4c68-8b4b-149e2277fae1

            //Connection names:
            //c70680b4-0660-4c68-8b4b-149e2277fae1_0
            //c70680b4-0660-4c68-8b4b-149e2277fae1_1
            //c70680b4-0660-4c68-8b4b-149e2277fae1_2

            name = Guid.NewGuid().ToString();
            for (int i = 0; i < connections.Count; i++)
            {
                connections[i].name = name + "_" + i;
            }
        }

        private void Handle_UnGrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                if (SkipUngrab)
                {
                    return;
                }
                StartCoroutine(PartUngrabbedCoroutine(ragdollHand));
            }
        }

        void Update ()
        {
            if (previewRenderer == null)
                return;

            ConnectionPoint closestTargetPoint = null;
            ConnectionPoint closestSourcePoint = null;
            float closestDist = float.PositiveInfinity;

            foreach (ConnectionPoint targetPoint in ConnectionPoint.AllConnectionPoints)
            {
                if (this.connections.Contains(targetPoint))
                    continue;

                foreach (ConnectionPoint sourcePoint in this.connections)
                {
                    if (!targetPoint.CanConnectToPoint(sourcePoint))
                        continue;

                    float dist = Vector3.Distance(sourcePoint.transform.position, targetPoint.transform.position);
                    if (dist < closestDist)
                    {
                        closestTargetPoint = targetPoint;
                        closestSourcePoint = sourcePoint;
                        closestDist = dist;
                    }
                }
            }


            if (closestDist < 0.2f)
            {
                //Show preview of connection
                previewRenderer.SetActive(true);

                previewRenderer.transform.position = transform.position;
                previewRenderer.transform.rotation = transform.rotation;

                Vector3 targetPosition = closestTargetPoint.transform.position;
                
                Quaternion targetRotation = Quaternion.LookRotation(-closestTargetPoint.transform.forward, Vector3.up);
                Quaternion sourceRotation = Quaternion.LookRotation(closestSourcePoint.transform.forward, Vector3.up);

                Vector3 localSourcePosition = previewRenderer.transform.InverseTransformPoint(closestSourcePoint.transform.position);
                Vector3 localSourceUp = previewRenderer.transform.InverseTransformDirection(closestSourcePoint.transform.up);

                Quaternion deltaRotation = targetRotation * Quaternion.Inverse(sourceRotation);

                previewRenderer.transform.rotation = deltaRotation * previewRenderer.transform.rotation;

                angle = Vector3.SignedAngle(previewRenderer.transform.TransformDirection(localSourceUp), closestTargetPoint.transform.up, closestSourcePoint.transform.forward);
                if (Vector3.Dot(closestSourcePoint.transform.forward, closestTargetPoint.transform.forward) > 0)
                    angle = -angle;

                float snapped = SnapToStep(angle, 45);
                float diffAngle = snapped - angle;

                angle = snapped;

                deltaRotation *= Quaternion.AngleAxis(diffAngle, closestTargetPoint.transform.forward);

                previewRenderer.transform.rotation = Quaternion.AngleAxis(diffAngle, closestTargetPoint.transform.forward) * previewRenderer.transform.rotation;

                // Align connector positions
                Vector3 displacement = targetPosition - previewRenderer.transform.TransformPoint(localSourcePosition);
                previewRenderer.transform.position += displacement;
            }
            else
            {
                previewRenderer.SetActive(false);
            }
        }

        public float GetAngle(Transform from, Transform to)
        {
            float angle = Vector3.SignedAngle(from.up, to.up, from.forward);
            if (Vector3.Dot(from.forward, to.forward) > 0)
                angle = -angle;

            angle = SnapToStep(angle, 45) - angle;

            return angle;
        }

        public float SnapToStep(float value, float step)
        {
            float min = -180;
            float max = 180;

            // Ensure the value is within the min-max range
            if (value < min) return min;
            if (value > max) return max;

            // Calculate the closest step
            float steps = Mathf.Round((value - min) / step);

            // Snap the value to the closest step and clamp it within the range
            float snappedValue = min + steps * step;
            return Mathf.Clamp(snappedValue, min, max);
        }
        
        public IEnumerator PartUngrabbedCoroutine(RagdollHand ragdollHand)
        {
            //Wait for ungrab to finish
            yield return new WaitForEndOfFrame();

            ConnectionPoint closestTargetPoint = null;
            ConnectionPoint closestSourcePoint = null;
            float closestDist = float.PositiveInfinity;

            foreach (ConnectionPoint targetPoint in ConnectionPoint.AllConnectionPoints)
            {
                if (this.connections.Contains(targetPoint))
                    continue;

                foreach (ConnectionPoint sourcePoint in this.connections)
                {
                    if (!targetPoint.CanConnectToPoint(sourcePoint))
                        continue;

                    float dist = Vector3.Distance(sourcePoint.transform.position, targetPoint.transform.position);
                    if (dist < closestDist)
                    {
                        closestTargetPoint = targetPoint;
                        closestSourcePoint = sourcePoint;
                        closestDist = dist;
                    }
                }
            }
            

            if (closestDist < 0.2f)
            {
                Handle handle = null;
                Handle.GripInfo gripInfo = null;

                closestSourcePoint.part.SkipUngrab = true;
                closestTargetPoint.part.SkipUngrab = true;

                //Ungrab other item if it is held
                if (ragdollHand.otherHand.grabbedHandle)
                {
                    handle = ragdollHand.otherHand.grabbedHandle;
                    gripInfo = ragdollHand.otherHand.gripInfo;
                    ragdollHand.otherHand.UnGrab(false);
                }

                yield return new WaitForEndOfFrame();

                ModularWeapon modularWeapon = ModularWeaponsUtil.Connect(closestSourcePoint, closestTargetPoint, angle, true);
                closestSourcePoint.part.SkipUngrab = false;
                closestTargetPoint.part.SkipUngrab = false;

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                //Regrab handle with same info as before
                if (handle != null)
                {
                    if (handle.isActiveAndEnabled)
                        ragdollHand.otherHand.Grab(handle, gripInfo.orientation, gripInfo.axisPosition);
                }
            }
        }
    }
}
