using System.Collections.Generic;
using System.Linq;
using ThunderRoad;

namespace ModularWeapons
{
    public class ModularWeapon : Item
    {
        public List<ModularPart> parts = new List<ModularPart>();

        protected override void Awake()
        {
            gameObject.AddComponent<ConnectionHandler>();
        }

        protected override void Start()
        {
            data = new ItemData();
            data.id = "MWData";
            data.localizationId = "";
            data.slot = parts.Any(p => p.Item.data.slot == "Large") ? "Large" : parts.Any(p => p.Item.data.slot == "Medium") ? "Medium" : "Small";
            data.flags = ItemFlags.Spinnable | ItemFlags.Jabbing;

            base.Awake();

            InitializeAiModule();
            InitializeStatModule();

            //Main Handle for now
            Handle main = handles.FirstOrDefault(h => h.GetComponentInParent<ModularPart>(true).connections.All(c => c.partType == PartType.Handle));
            if (main != null)
            {
                mainHandleLeft = main;
                mainHandleRight = main;
            }

            data.entityModules = new List<string>();
            LoadData(data);

            base.Start();

            CreateCustomData();
        }

        private void CreateCustomData()
        {
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

        private void InitializeAiModule()
        {
            bool isDoubleHanded = handles.Any(h => h.axisLength > 0);
            ItemModuleAI moduleAI = new ItemModuleAI();

            moduleAI.primaryClass = ItemModuleAI.WeaponClass.Melee;

            if (isDoubleHanded)
            {
                moduleAI.weaponHandling = ItemModuleAI.WeaponHandling.TwoHanded;
                moduleAI.defaultStanceInfo = new ItemModuleAI.StanceInfo()
                {
                    offhand = ItemModuleAI.StanceInfo.Offhand.Anything,
                    grabAIHandleRadius = 0.0f,
                    stanceDataID = "HumanMelee1hStance"
                };
                moduleAI.stanceInfosByOffhand = new List<ItemModuleAI.StanceInfo>
                {
                    new ItemModuleAI.StanceInfo()
                    {
                        offhand = ItemModuleAI.StanceInfo.Offhand.SameItem,
                        grabAIHandleRadius = 0.0f,
                        stanceDataID = ""
                    }
                };
            }
            else
            {
                moduleAI.weaponHandling = ItemModuleAI.WeaponHandling.OneHanded;
                moduleAI.defaultStanceInfo = new ItemModuleAI.StanceInfo()
                {
                    offhand = ItemModuleAI.StanceInfo.Offhand.Anything,
                    grabAIHandleRadius = 0.0f,
                    stanceDataID = "HumanMelee1hStance"
                };
                moduleAI.stanceInfosByOffhand = new List<ItemModuleAI.StanceInfo>
                {
                    new ItemModuleAI.StanceInfo()
                    {
                        offhand = ItemModuleAI.StanceInfo.Offhand.Anything,
                        grabAIHandleRadius = 0.0f,
                        stanceDataID = "HumanMelee1hStance"
                    },
                    new ItemModuleAI.StanceInfo()
                    {
                        offhand = ItemModuleAI.StanceInfo.Offhand.AnyShield,
                        grabAIHandleRadius = 0.0f,
                        stanceDataID = "HumanMeleeShieldStance"
                    },
                    new ItemModuleAI.StanceInfo()
                    {
                        offhand = ItemModuleAI.StanceInfo.Offhand.AnyMelee,
                        grabAIHandleRadius = 0.0f,
                        stanceDataID = "HumanMeleeDualWieldStance"
                    }
                };
            }
        }

        private void InitializeStatModule()
        {
            //TODO
            return;
            foreach (Item item in parts.Select(p => p.Item))
            {
                ItemModuleStats stats = (ItemModuleStats)item.data.modules.FirstOrDefault(m => m is ItemModuleStats);

                if (stats == null)
                    continue;
            }

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
