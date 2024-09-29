using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;

namespace ModularWeapons
{
    /*
    public class ModularWeapon
    {
        public List<PartData> parts = new List<PartData>();
        public List<Connection> connections = new List<Connection>();
    }

    public class PartData
    {
        public string itemID;
    }

    public class Connection
    {
        public int part1;
        public int part2;
        public string transform1;
        public string transform2;
    }
    */

    /*
    public static class ModularWeaponsManager
    {
        public static List<ConnectionPoint> activeConnectionPoints = new List<ConnectionPoint>();

        /// <summary>
        /// Connects part1 to part2
        /// </summary>
        /// <param name="part1"></param>
        /// <param name="part2"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        public static void ConnectParts(ModularPart part1, ModularPart part2, ConnectionPoint point1, ConnectionPoint point2)
        {
            //Moves the item to the connectionpoint
            part1.transform.MoveAlign(point1.transform, point2.transform);
            part1.transform.parent = part2.transform;

            //Add new renderes
            part2.item.renderers.AddRange(part1.item.renderers);

            //Colliders and Damagers
            part2.item.colliderGroups.AddRange(part1.GetComponentsInChildren<ColliderGroup>());

            foreach (CollisionHandler collisionHandler in part1.GetComponentsInChildren<CollisionHandler>())
            {
                if (collisionHandler.isItem)
                {
                    foreach (Damager damager in collisionHandler.damagers)
                    {
                        damager.UnPenetrateAll();

                        if (collisionHandler == part1.item.mainCollisionHandler)
                        {
                            damager.collisionHandler = part2.item.mainCollisionHandler;

                            if (damager.colliderGroup != null)
                            {
                                damager.colliderGroup.collisionHandler = part2.item.mainCollisionHandler;
                            }

                            part2.item.mainCollisionHandler.damagers.Add(damager);
                            part2.item.mainCollisionHandler.SortDamagers();
                        } 
                    }

                    if (collisionHandler == part1.item.mainCollisionHandler)
                    {
                        UnityEngine.Object.Destroy(collisionHandler);
                    }
                    else
                    {
                        //not main, so add it as new
                        collisionHandler.item = part2.item;
                        part2.item.collisionHandlers.Add(collisionHandler);
                    }
                }
            }

            //Whooshs
            /*
            if (!part1.isWeapon)
            {
                foreach (WhooshPoint whoosh in part1.GetComponentsInChildren<WhooshPoint>())
                {
                    UnityEngine.Object.Destroy(whoosh.gameObject);
                }
            }*/
    /*
            //Handles
            foreach (Handle handle in part1.GetComponentsInChildren<Handle>())
            {
                if (part1.keepHandles)
                {
                    handle.item = part2.item;
                    if (!handle.customRigidBody)
                    {
                        handle.rb = part2.item.rb;
                    }
                    part2.item.handles.Add(handle);
                } else
                {
                    UnityEngine.Object.Destroy(handle);
                }
            }

            //Parry
            if (part1.isWeapon)
            {
                UnityEngine.Object.Destroy(part2.item.parryPoint.gameObject);
                part2.item.parryPoint = part1.item.parryPoint;
            }
            Debug.Log("Parry");

            //Reset Physics
            part2.item.ResetCenterOfMass();

            float mass = (part1.item.rb.mass + part2.item.rb.mass);
            part2.item.rb.mass = mass;

            Vector3 com1 = part1.item.rb.centerOfMass;
            Vector3 com2 = part2.item.rb.centerOfMass;

            float sumX = (com1.x * part1.item.rb.mass + com2.x * part2.item.rb.mass) / mass;
            float sumY = (com1.y * part1.item.rb.mass + com2.y * part2.item.rb.mass) / mass;
            float sumZ = (com1.z * part1.item.rb.mass + com2.z * part2.item.rb.mass) / mass;

            part2.item.rb.centerOfMass = new Vector3(sumX, sumY, sumZ);

            Debug.Log("Physics");

            //Update connectionpoints
            foreach (ConnectionPoint connectionPoint in part1.connectionPoints)
            {
                connectionPoint.item = part2.item;
            }

            Debug.Log("Points");

            //Remove used connectionpoints from list
            activeConnectionPoints.Remove(point1);
            part1.connectionPoints.Remove(point1);
 
            activeConnectionPoints.Remove(point2);
            part2.connectionPoints.Remove(point2);
            Debug.Log("Remove Points");


            //Create or update modular weapon component
            ModularWeapon mainMW = part2.item.GetComponent<ModularWeapon>();
            if (mainMW == null)
            {
                mainMW = part2.item.gameObject.AddComponent<ModularWeapon>();
                mainMW.data.parts.Add(new PartData() { id = part2.PartGUID, itemID = part2.item.itemId });
            }

            ModularWeapon secondMW = part2.item.GetComponent<ModularWeapon>();
            if (secondMW != null)
            {
                mainMW.data.parts.AddRange(secondMW.data.parts);
                mainMW.data.connections.AddRange(secondMW.data.connections);
            }

            mainMW.data.parts.Add(new PartData() { id = part1.PartGUID, itemID = part1.item.itemId });
            
            mainMW.data.connections.Add(new ConnectionData()
            {
                part1 = part1.PartGUID,
                part2 = part2.PartGUID,
                part1Point = point1.ConnectionGUID,
                part2Point = point2.ConnectionGUID
            });

            //Destroy item
            if (part1.item.currentRoom != null)
                part1.item.currentRoom.UnRegisterItem(part1.item);

            Rigidbody rb = part1.item.rb;

            UnityEngine.Object.Destroy(part1.item);

            if (part1.isFlail)
            {
                ConfigurableJoint joint = part2.item.gameObject.AddComponent<ConfigurableJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedBody = point1.flailConnection;
                joint.xMotion = ConfigurableJointMotion.Limited;
                joint.yMotion = ConfigurableJointMotion.Limited;
                joint.zMotion = ConfigurableJointMotion.Limited;
                SoftJointLimit l = joint.linearLimit;
                l.limit = 0.3f;
                SoftJointLimit a = joint.lowAngularXLimit;
                a.limit = -35;
                SoftJointLimit hx = joint.highAngularXLimit;
                hx.limit = -35;
                SoftJointLimit aa = joint.angularYLimit;
                aa.limit = 90;
                SoftJointLimit aaz = joint.angularZLimit;
                aaz.limit = 90;
                joint.projectionDistance = 0.01f;
                joint.enableCollision = false;
                joint.enablePreprocessing = true;

                joint.anchor = part2.item.transform.InverseTransformPoint(point1.transform.position);
                joint.connectedAnchor = point1.flailConnection.transform.InverseTransformPoint(point2.transform.position);
                joint.axis = new Vector3(-1, 0, 0);
                joint.secondaryAxis = new Vector3(0, 1, 0);
            } 
            
            UnityEngine.Object.Destroy(rb);

            part1.item = part2.item;

            Debug.Log("Destroy Item");

            //Update new item
            part2.item.RefreshColliderStateList();
            part2.item.RefreshCollision();

            //part2.item.ResetInertiaTensor();

            part2.item.SetColliderAndMeshLayer(GameManager.GetLayer(LayerName.MovingItem));
            Debug.Log("Update Item");
        }

        public static Task<Item> AsyncItemSpawn(string itemID)
        {
            var tcs = new TaskCompletionSource<Item>();

            Catalog.GetData<ItemData>(itemID).SpawnAsync( part =>
            {
                tcs.TrySetResult(part);
            });

            return tcs.Task;
        }
    }



    public class ModularWeaponData
    {
        public List<PartData> parts { get; set; }
        public List<ConnectionData> connections { get; set; }
    }

    public class PartData
    {
        public string itemID { get; set; }
        public string id { get; set; }
    }

    public class ConnectionData
    {
        public string part1;
        public string part2;
        public string part1Point;
        public string part2Point;
    }

    public class ModularWeapon : MonoBehaviour
    {
        public ModularWeaponData data = new ModularWeaponData();
    }
    /*
    public class ModularPart : MonoBehaviour
    {
        public List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();
        public bool isWeapon;
        public bool keepHandles;
        public bool isFlail;

        public Item item;

        public string PartGUID { get; private set; }

        void Awake()
        {
            //Give the part an unique id
            PartGUID = Guid.NewGuid().ToString();

            this.item = GetComponent<Item>();
            connectionPoints = GetComponentsInChildren<ConnectionPoint>().ToList();

            for (int i = 0; i < connectionPoints.Count; i++)
            {
                ConnectionPoint point = connectionPoints[i];

                //setup ids based on the parts id and number!!
                point.ConnectionGUID = PartGUID + "_" + i;
            }
        }

        /*
        public List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();
        public Item item;

        public ConnectionPoint closestValidPoint;
        public bool isWeapon;
        public bool keepHandles;
        public bool isFlail;

        public string origItemID;

        public bool isConnected;

        public List<Handle> handles = new List<Handle>();

        void Awake()
        {
            item = GetComponent<Item>();
            origItemID = item.itemId;

            connectionPoints.AddRange(gameObject.GetComponentsInChildren<ConnectionPoint>());
            ModularWeaponsManager.activeConnectionPoints.AddRange(connectionPoints);

            foreach (Handle handle in item.handles)
            {
                handle.UnGrabbed += PartUngrabbed;
                handles.Add(handle);
            }
        }

        public void UpdateHandles()
        {
            foreach (Handle handle1 in handles)
            {
                handle1.UnGrabbed -= PartUngrabbed;
            }

            foreach (Handle handle in item.handles)
            {
                handle.UnGrabbed += PartUngrabbed;
            }
        }

        private void PartUngrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            if (closestValidPoint?.closestValidConnectionPoint != null && eventTime == EventTime.OnStart)
            {
                StartCoroutine(PartUngrabbedCoroutine());
            }
        }

        public IEnumerator PartUngrabbedCoroutine()
        {
            //Wait for ungrab to finish
            yield return new WaitForEndOfFrame();

            ModularWeaponsManager.ConnectParts(this, closestValidPoint.closestValidConnectionPoint.owner, closestValidPoint, closestValidPoint.closestValidConnectionPoint);
        }

        void Update()
        {
            foreach (ConnectionPoint point in connectionPoints)
            {
                point.StopVFX();
            }

            if (item.IsHanded() && !isConnected) //Is held, and not already part of modular weapon
            {
                foreach (ConnectionPoint point in connectionPoints)
                {
                    point.GetClosestValidPoint();
                }

                closestValidPoint = connectionPoints.OrderBy(p => p.closestDistanceSqr).First();

                if (closestValidPoint.closestValidConnectionPoint != null)
                {
                    closestValidPoint.StartVFX();
                }
            }
        }*/
    //}


    /*
    public class ConnectionPoint : MonoBehaviour 
    {
        public VisualEffect vfx;
        public PartType partType;
        public PartType connectableTypes;
        public Rigidbody flailConnection;

        public string ConnectionGUID { get; internal set; }
    }*/
}
