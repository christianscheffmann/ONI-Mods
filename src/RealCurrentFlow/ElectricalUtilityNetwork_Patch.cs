using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using WireExtensions;

namespace RealisticCurrentFlow
{
    public class ElectricalUtilityNetwork_Patch
    {
        [HarmonyPatch(typeof(ElectricalUtilityNetwork))]
        [HarmonyPatch("AddItem")]
        public class ElectricalUtilityNetwork_AddItem_Patch
        {
            public static void Postfix(ElectricalUtilityNetwork __instance, object item)
            {
                var instance = (ElectricalUtilityNetworkGraph)__instance;

                if (item.GetType() == typeof(Wire))
                {
                    Wire wire = (Wire)item;
                    var wireBuildingProperties = wire.GetWireBuildingProperties();
                    var newNode = new Node(wire.GetNetworkCell(), wire.GetWireConnectionsString().Length, instance.Nodes.Count() + 1, wireBuildingProperties);

                    instance.Nodes.Add(newNode);
                }
            }
        }
    }
}