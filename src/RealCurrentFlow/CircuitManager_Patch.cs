using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using WireExtensions;
using static WireExtensions.WireExtensions;

namespace RealisticCurrentFlow
{
    public class CircuitManager_Patch
    {
        [HarmonyPatch(typeof(CircuitManager))]
        [HarmonyPatch("CheckCircuitOverloaded")]
        public class CircuitManager_CheckCircuitOverloaded_Patch
        {
            public static bool Prefix(CircuitManager __instance, ref float dt, ref int id, ref float watts_used)
            {
                ushort ushortId = Convert.ToUInt16(id);
                Debug.Log("Circuit Manager mod checking in!");
                UtilityNetwork network = Game.Instance.electricalConduitSystem.GetNetworkByID(id);

                if (network == null)
                {
                    return false;
                }
                // Subclass ElectricalUtilityNetwork and save the wire graph in this class, then cast to the subclass as needed.
                var electricalNetwork = (ElectricalUtilityNetworkGraph)network;

                var wires = electricalNetwork.allWires;

                Debug.Log(" === CurrentMod: Grid Width in Cells " + Grid.WidthInCells);
                Debug.Log(" === CurrentMod: Grid Height in Cells " + Grid.HeightInCells);
                Debug.Log(" === CurrentMod: ElectricalNetwork has ID " + id.ToString());
                Debug.Log(" === CurrentMod: Wires " + wires.Count);
                Debug.Log(" === CurrentMod: Generators " + __instance.GetGeneratorsOnCircuit(ushortId).Count);
                Debug.Log(" === CurrentMod: Consumers " + __instance.GetConsumersOnCircuit(ushortId).Count);
                Debug.Log(" === CurrentMod: Batteries " + __instance.GetBatteriesOnCircuit(ushortId).Count);

                var nodes = new List<Node>();
                var branches = new List<Branch>();

                var nodeIndex = 0;
                var branchIndex = 0;

                foreach (Wire wire in wires)
                {
                    var valence = wire.GetCellValence();
                    var cellId = wire.GetNetworkCell();

                    var destinations = wire.GetConnectionIds();
                    var wireBuildingProperties = wire.GetWireBuildingProperties();


                    if (valence != 2 || wireBuildingProperties.HasBuilding)
                    {
                        var node = new Node(cellId, valence, nodeIndex, wireBuildingProperties);
                        nodes.Add(node);
                        nodeIndex++;
                    }

                    foreach (var destination in destinations)
                    {
                        var branch = new Branch(new List<Wire>() { wire }, branchIndex, cellId, destination, 0, 0);
                        var neighbourBranch = branches.Where(b => b.SourceCellId == cellId || b.DestinationCellId == cellId).FirstOrDefault();

                        if (neighbourBranch != null)
                        {
                            neighbourBranch.Merge(branch);
                        }
                        else
                        {
                            branches.Add(branch);
                            branchIndex++;
                        }
                    }
                }

                foreach (var branch in branches)
                {
                    var sourceNode = nodes.Where(n => n.CellId == branch.SourceCellId).First();
                    var destinationNode = nodes.Where(n => n.CellId == branch.DestinationCellId).First();

                    branch.SourceNodeId = sourceNode.Id;
                    branch.DestinationNodeId = destinationNode.Id;
                }

                electricalNetwork.Nodes = nodes;
                electricalNetwork.Branches = branches;

                return false;
            }
        }
    }

    public class BranchComparer : EqualityComparer<Branch>
    {
        public override bool Equals(Branch x, Branch y)
        {
            return x.IsBranchEqual(y);
        }

        public override int GetHashCode(Branch obj)
        {
            return obj.GetHashCode();
        }
    }

    public class Node : CircuitNode
    {
        public int CellId { get; set; }
        public int Valence { get; set; }
        public bool HasBuilding { get; set; }

        public Node(int cellId, int valence, int index, WireBuildingProperties wireBuildingProperties) : base(index, wireBuildingProperties.PowerDraw, wireBuildingProperties.PowerGeneration)
        {
            this.CellId = cellId;
            this.Valence = valence;
            this.HasBuilding = wireBuildingProperties.HasBuilding;
        }
    }


    public class Branch : CircuitBranch
    {
        public float WattageRating { get; set; }
        public List<Wire> Wires { get; set; }
        public int SourceCellId { get; set; }
        public int DestinationCellId { get; set; }

        public Branch(List<Wire> wires, int index, int sourceCellId, int destinationCellId, int sourceNodeId, int destinationNodeId) : base(index, sourceNodeId, destinationNodeId, 1)
        {
            this.Wires = wires;
            this.WattageRating = wires.Min(w => Wire.GetMaxWattageAsFloat(w.GetMaxWattageRating()));
            this.SourceCellId = sourceCellId;
            this.DestinationCellId = destinationCellId;
        }

        public void Merge(Branch that)
        {
            if (this.SourceCellId == that.SourceCellId)
            {
                this.DestinationCellId = that.DestinationCellId;
            }
            else if (this.DestinationCellId == that.DestinationCellId)
            {
                this.DestinationCellId = that.SourceCellId;
            }
            else if (this.SourceCellId == that.DestinationCellId)
            {
                this.DestinationCellId = that.SourceCellId;
            }
            else if (this.DestinationCellId == that.SourceCellId)
            {
                this.DestinationCellId = that.DestinationCellId;
            }

            this.Wires.AddRange(that.Wires);
            this.Id = Math.Min(this.Id, that.Id);
        }

        public bool IsBranchEqual(Branch that)
        {
            return (this.SourceNodeId == that.SourceNodeId && this.DestinationNodeId == that.DestinationNodeId) || (this.SourceNodeId == that.DestinationNodeId && this.DestinationNodeId == that.SourceNodeId);
        }
    }
}
