using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace ModularWeapons
{
    public class ConnectionHandler : MonoBehaviour
    {
        private GameObject previewRenderer;
        private List<ConnectionPoint> connections = new List<ConnectionPoint>();
        private Item item;
        private bool updating = true;

        private static bool SkipUngrab;

        private ConnectionPoint closestTargetPoint = null;
        private ConnectionPoint closestSourcePoint = null;
        private float closestDist = float.PositiveInfinity;

        public float Angle { get; private set; }

        private void Start()
        {
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

            connections = GetComponentsInChildren<ConnectionPoint>().ToList();
            item = GetComponent<Item>();


            //Only called when there are 0 handlers
            item.OnUngrabEvent += Item_OnUngrabEvent;
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (SkipUngrab || !item.enabled)
            {
                return;
            }
            StartCoroutine(PartUngrabbedCoroutine(ragdollHand));
        }

        private void Update()
        {
            if (previewRenderer == null || !updating)
                return;

            closestTargetPoint = null;
            closestSourcePoint = null;
            closestDist = float.PositiveInfinity;

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

                Angle = Vector3.SignedAngle(previewRenderer.transform.TransformDirection(localSourceUp), closestTargetPoint.transform.up, closestSourcePoint.transform.forward);
                if (Vector3.Dot(closestSourcePoint.transform.forward, closestTargetPoint.transform.forward) > 0)
                    Angle = -Angle;

                float snapped = SnapToStep(Angle, 180);
                float diffAngle = snapped - Angle;

                Angle = snapped;

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

        private void OnDestroy()
        {
            if (previewRenderer != null)
                Destroy(previewRenderer);
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
            //Disable previewer to save angle
            updating = false;

            //Wait for ungrab to finish
            yield return new WaitForEndOfFrame();

            if (closestDist < 0.2f)
            {
                Handle handle = null;
                Handle.GripInfo gripInfo = null;

                SkipUngrab = true;

                //Ungrab other item if it is held
                if (ragdollHand.otherHand.grabbedHandle)
                {
                    handle = ragdollHand.otherHand.grabbedHandle;
                    gripInfo = ragdollHand.otherHand.gripInfo;
                    ragdollHand.otherHand.UnGrab(false);
                }

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                ModularWeapon modularWeapon = ModularWeaponsUtil.Connect(closestSourcePoint, closestTargetPoint, Angle, true);

                SkipUngrab = false;

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                //Regrab handle with same info as before
                if (handle != null)
                {
                    if (handle.isActiveAndEnabled)
                        ragdollHand.otherHand.Grab(handle, gripInfo.orientation, gripInfo.axisPosition);
                }

                //Should destroy the previewer, or else it will keep showing the preview
                if (modularWeapon == null)
                    updating = true;
                else
                    Destroy(this);
            }
            else
            {
                updating = true;
            }
        }
    }
}
