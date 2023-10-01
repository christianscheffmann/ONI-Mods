using System.Collections.Generic;

namespace RealisticCurrentFlow
{
    class ElectricalUtilityNetworkGraph : ElectricalUtilityNetwork
    {
        public List<Node> Nodes { get; set; }

        public List<Branch> Branches { get; set; }
    }
}
