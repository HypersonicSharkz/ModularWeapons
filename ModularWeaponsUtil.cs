using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace ModularWeapons
{
    public static class ModularWeaponsUtil
    {
        /// <summary>
        /// Connect two points by aligning the two parts and creating a new ModularWeapon
        /// </summary>
        /// <param name="point1">Source ConnectionPoint</param>
        /// <param name="point2">Target ConnectionPoint</param>
        /// <param name="force">If true will connect parts even if they aren't compatible</param>
        /// <returns>The instantiated ModularWeapon, which contains each connected part as children</returns>
        public static ModularWeapon Connect(ConnectionPoint point1, ConnectionPoint point2, float angle, bool force = false)
        {
            //Can't connect points which are already connected to something
            if (point1.Connected ||point2.Connected) 
                return null;

            if (!force && !point1.connectableTypes.HasFlag(point2.partType))
                return null;

            if (!force && !point2.connectableTypes.HasFlag(point1.partType))
                return null;

            Item i1 = point1.part.Item;
            Item i2 = point2.part.Item;

            ModularWeapon modularWeapon1 = point1.GetComponentInParent<ModularWeapon>();
            ModularWeapon modularWeapon2 = point2.GetComponentInParent<ModularWeapon>();
            ModularWeapon modularWeapon = null;

            Item child = modularWeapon1 ?? i1;
            Item parent = modularWeapon2 ?? i2;

            //Needed while connecting to avoid weird collisions
            foreach (Collider collider1 in child.GetComponentsInChildren<Collider>())
            {
                foreach (Collider collider2 in parent.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(collider1, collider2);
                }
            }

            //Creates a new instance of a ModularWeapon each time new parts are connected
            //Done to keep it consistent for each connection
            GameObject mw = new GameObject("Modular Weapon");
            modularWeapon = mw.AddComponent<ModularWeapon>();
            mw.transform.position = parent.transform.position;

            //Align the two parts
            MoveAlign(child.transform, point1.transform, point2.transform);

            //Apply the angle
            Quaternion snapQuat = Quaternion.AngleAxis(angle, point2.transform.forward);
            child.transform.rotation = snapQuat * child.transform.rotation;


            child.transform.parent = modularWeapon.transform;
            parent.transform.parent = modularWeapon.transform;

            point1.ConnectedTo = point2;
            point2.ConnectedTo = point1;

            GameObject.DestroyImmediate(child.gameObject.GetComponent<Rigidbody>());
            GameObject.DestroyImmediate(parent.gameObject.GetComponent<Rigidbody>());

            //Move parts and despawn old modular weapons if they exist
            if (modularWeapon1 != null)
            {
                modularWeapon1.ForceUngrabAll();
                foreach (ModularPart modularPart in modularWeapon1.parts)
                {
                    modularPart.transform.parent = modularWeapon.transform;
                }

                GameObject.DestroyImmediate(modularWeapon1.gameObject);
            }

            if (modularWeapon2 != null)
            {
                modularWeapon2.ForceUngrabAll();
                foreach (ModularPart modularPart in modularWeapon2.parts)
                {
                    modularPart.transform.parent = modularWeapon.transform;
                }

                GameObject.DestroyImmediate(modularWeapon2.gameObject);
            }

            InitializeModularWeapon(modularWeapon);

            i1.enabled = false;
            i2.enabled = false;

            return modularWeapon;
        }

        /// <summary>
        /// Will disconnect a modular weapon at the specified ConnectionPoint. Will result in two ModularWeapons, 
        /// unless only a single part is disconnected, where the part will become a normal item again.
        /// </summary>
        /// <param name="point"></param>
        public static void DisconnectConnection(ConnectionPoint point)
        {
            if (!point.Connected)
                return;

            ModularWeapon modularWeapon = point.GetComponentInParent<ModularWeapon>();

            ConnectionPoint sourcePoint = point;
            ConnectionPoint targetPoint = point.ConnectedTo;

            List<ModularPart> partsConnectedToTarget = new List<ModularPart>();
            GetConnectedParts(targetPoint, partsConnectedToTarget);
            List<ModularPart> partsConnectedToSource = new List<ModularPart>();
            GetConnectedParts(sourcePoint, partsConnectedToSource);

            DisconnectParts(partsConnectedToSource);
            DisconnectParts(partsConnectedToTarget);

            sourcePoint.ConnectedTo = null;
            targetPoint.ConnectedTo = null;

            if (modularWeapon != null)
                GameObject.Destroy(modularWeapon.gameObject);
        }

        public static void DisconnectParts(List<ModularPart> parts)
        {
            if (parts.Count == 0) return;

            if (parts.Count == 1)
            {
                ModularPart part = parts[0];

                part.transform.parent = null;
                part.Item.enabled = true;

                Rigidbody rigidbody = part.gameObject.AddComponent<Rigidbody>();
                rigidbody.centerOfMass = part.centerOfMass;
                rigidbody.mass = part.mass;

                return;
            }

            GameObject mw = new GameObject("Modular Weapon");
            ModularWeapon modularWeapon = mw.AddComponent<ModularWeapon>();

            foreach (ModularPart part in parts)
            {
                part.transform.SetParent(modularWeapon.transform, true);
                modularWeapon.parts.Add(part);
            }

            InitializeModularWeapon(modularWeapon);
        }

        //Recursive method for finding all connected parts
        public static void GetConnectedParts(ConnectionPoint point, List<ModularPart> parts)
        {
            parts.Add(point.part);

            //Look for any other connection that is not the parameter
            foreach (ConnectionPoint otherPoint in point.part.connections)
            {
                if (otherPoint == point)
                    continue;

                if (!otherPoint.Connected)
                    continue;

                if (parts.Contains(otherPoint.ConnectedTo.part))
                    continue;

                GetConnectedParts(otherPoint.ConnectedTo, parts);
            }
        }

        public static void InitializeModularWeapon(ModularWeapon weapon)
        {
            weapon.parts = weapon.GetComponentsInChildren<ModularPart>().ToList();
            Rigidbody rb = weapon.gameObject.GetOrAddComponent<Rigidbody>();
            Vector3 worldCenterOfMass = Vector3.zero;
            Vector3 parryPointWorld = Vector3.zero;
            float mass = 0;

            //Items only support CollisionHandlers on the same object as rigidbody :(
            CollisionHandler mainCollisionHandler = weapon.GetOrAddComponent<CollisionHandler>();

            mainCollisionHandler.physicBody = rb.GetPhysicBody();
            weapon.collisionHandlers.Add(mainCollisionHandler);
            weapon.mainCollisionHandler = mainCollisionHandler;

            //Should only disable handles of blade n such if an actual handle is connected
            bool disableOtherHandles = weapon.parts.Any(p => p.keepHandles);

            foreach (ModularPart modularPart in weapon.parts)
            {
                //Weighted average for center of mass
                worldCenterOfMass += modularPart.transform.TransformPoint(modularPart.centerOfMass) * modularPart.mass;
                mass += modularPart.mass;

                Item partItem = modularPart.Item;

                if (modularPart.isWeapon)
                {
                    parryPointWorld += partItem.parryPoint.position;
                }

                //Items only support single collisionHandler, so need to update damager references
                foreach (CollisionHandler collisionHandler in partItem.collisionHandlers)
                {
                    foreach (Damager damager in collisionHandler.damagers)
                    {
                        damager.collisionHandler = mainCollisionHandler;
                    }

                    collisionHandler.physicBody = rb.GetPhysicBody();
                    collisionHandler.item = weapon;
                }

                //Might cause a lot of noise with more parts...
                foreach (WhooshPoint whooshPoint in partItem.whooshPoints)
                {
                    whooshPoint.SendMessage("Awake");
                }

                //Need to update handles of parts to the modular weapon
                foreach (Handle handle in partItem.handles)
                {
                    if (disableOtherHandles && !modularPart.keepHandles)
                    {
                        handle.gameObject.SetActive(false);
                    }
                    else
                    {
                        handle.gameObject.SetActive(true);
                        handle.item = weapon;
                        handle.physicBody = rb.GetPhysicBody();
                    }
                }
            }

            worldCenterOfMass /= mass;
            parryPointWorld /= Mathf.Max(1f, weapon.parts.Where(p => p.isWeapon).Count());

            rb.centerOfMass = rb.transform.InverseTransformPoint(worldCenterOfMass);
            rb.mass = mass;

            //Holder and Spawnpoint
            Transform holderPoint = weapon.holderPoint;
            if (holderPoint == null)
                holderPoint = new GameObject("HolderPoint").transform;

            holderPoint.position = worldCenterOfMass;
            holderPoint.parent = weapon.transform;
            weapon.holderPoint = holderPoint;
            weapon.spawnPoint = holderPoint;
            weapon.flyDirRef = holderPoint;

            //Parry point
            Transform parryPoint = weapon.parryPoint;
            if (parryPoint == null)
                parryPoint = new GameObject("ParryPoint").transform;

            parryPoint.position = parryPointWorld;
            parryPoint.parent = weapon.transform;
            weapon.parryPoint = parryPoint;

            Debug.Log("INITIALIZE FINISHED");
        }

        public static void MoveAlign(Transform transform, Transform child, Transform target)
        {
            MoveAlign(transform, child, target.position, Quaternion.LookRotation(-target.forward, target.up));
        }

        public static void MoveAlign(Transform transform, Transform child, Vector3 targetPosition, Quaternion targetRotation)
        {
            Quaternion quaternion = targetRotation * Quaternion.Inverse(child.rotation);
            transform.transform.rotation = quaternion * transform.transform.rotation;
            Vector3 vector = targetPosition - child.position;
            transform.transform.position += vector;
        }
    }

    public enum PartType
    {
        Blade = 1,
        Handle = 2,
        Hilt = 4,
        Pummel = 8,
        Crystal = 16,
        Socket = 32,
        Spike = 64
    }
}
