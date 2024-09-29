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

        private void Start()
        {
            Item = GetComponent<Item>();
            connections = GetComponentsInChildren<ConnectionPoint>().ToList();
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null )
            {
                centerOfMass = rb.centerOfMass;
                mass = rb.mass;
            }

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

            //Attach the ConnectionPreview module
            gameObject.AddComponent<ConnectionHandler>();
        }
       
    }
}
