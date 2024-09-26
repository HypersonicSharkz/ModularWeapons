using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.AI.Condition.TargetWithinRange;
using static ThunderRoad.CreatureData;

namespace ModularWeapons
{
    public class ModularWeapon : Item
    {
        public List<ModularPart> parts = new List<ModularPart>();

        private List<ConnectionPoint> allConnectionPoints = new List<ConnectionPoint>();

        private GameObject previewRenderer;

        protected override void Awake()
        {
            allConnectionPoints = parts.SelectMany(p => p.connections).ToList();
        }

        protected override void Start()
        {
            data = new ItemData();
            data.id = "MWData";
            data.localizationId = "";
            
            base.Awake();

            data.entityModules = new List<string>();
            LoadData(data);

            base.Start();

            SetupPreview();

            ModularCustomData modularCustomData = new ModularCustomData();

            List<ConnectionPoint> connections = new List<ConnectionPoint>();

            foreach (ModularPart part in parts)
            {
                //Add the part data
                modularCustomData.parts.Add(new ModularCustomData.MoudlarPartData(part.Item.itemId, part.name));

                foreach (ConnectionPoint point in part.connections)
                {
                    if (connections.Contains(point))
                        continue;

                    if (point.Connected)
                    {
                        connections.Add(point);
                        connections.Add(point.ConnectedTo);

                        modularCustomData.connections.Add(
                            new ModularCustomData.ConnectionData
                            (
                                point.part.name,
                                point.ConnectedTo.name,
                                point.name,
                                point.ConnectedTo.name
                            )
                        );
                    }
                }
            }

            AddCustomData<ModularCustomData>(modularCustomData);
        }

        private void Update()
        {
            if (previewRenderer == null)
                return;

            ConnectionPoint closestTargetPoint = null;
            ConnectionPoint closestSourcePoint = null;
            float closestDist = float.PositiveInfinity;

            foreach (ConnectionPoint targetPoint in ConnectionPoint.AllConnectionPoints)
            {
                if (this.allConnectionPoints.Contains(targetPoint))
                    continue;

                foreach (ConnectionPoint sourcePoint in this.allConnectionPoints)
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

        private void SetupPreview()
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
        }
    }

    public class ModularCustomData : ContentCustomData
    {
        public List<MoudlarPartData> parts { get; set; } = new List<MoudlarPartData>();
        public List<ConnectionData> connections { get; set; } = new List<ConnectionData>();

        public class MoudlarPartData
        {
            public MoudlarPartData(string itemID, string id)
            {
                this.itemID = itemID;
                this.id = id;
            }

            public string itemID { get; set; }
            public string id { get; set; }
        }

        public class ConnectionData
        {
            public ConnectionData(string part1, string part2, string part1Point, string part2Point)
            {
                this.part1 = part1;
                this.part2 = part2;
                this.part1Point = part1Point;
                this.part2Point = part2Point;
            }

            public string part1 { get; set; }
            public string part2 { get; set; }
            public string part1Point { get; set; }
            public string part2Point { get; set; }
        }
    }
}
