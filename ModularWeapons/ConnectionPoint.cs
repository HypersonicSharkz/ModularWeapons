using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.VFX;
using UnityEngine;

namespace ModularWeapons
{
    public class ConnectionPoint : MonoBehaviour
    {
        public static HashSet<ConnectionPoint> AllConnectionPoints = new HashSet<ConnectionPoint>();

        public ModularPart part;
        public PartType partType;
        public PartType connectableTypes;

        private ConnectionPoint connectedTo;

        public ConnectionPoint ConnectedTo
        {
            get => connectedTo;
            set
            {
                connectedTo = value;
                if (connectedTo != null)
                    AllConnectionPoints.Remove(this);
                else
                    AllConnectionPoints.Add(this);
            }
        }

        public bool Connected
        {
            get => ConnectedTo != null;
        }

        private void Start()
        {
            AllConnectionPoints.Add(this);
            part = GetComponentInParent<ModularPart>();
        }

        private void OnDestroy()
        {
            AllConnectionPoints.Remove(this);
        }

        public void Disconnect()
        {
            ModularWeaponsUtil.DisconnectConnection(this);
        }

        public bool CanConnectToPoint(ConnectionPoint point2)
        {
            if (Connected || point2.Connected)
                return false;

            if (!connectableTypes.HasFlag(point2.partType))
                return false;

            if (!point2.connectableTypes.HasFlag(partType))
                return false;

            return true;
        }
    }
}
