using HarmonyLib;
using KMod;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WireExtensions
{
    public static class WireExtensions
    {
        public static List<int> GetConnectionIds(this Wire wire)
        {
            var cellId = wire.GetNetworkCell();
            var connections = wire.GetWireConnections();
            var destinations = new List<int>();

            // Coordinate system starts in lower left corner at (0,0) and has transform cell = x + y*GridWidth
            if ((connections & UtilityConnections.Left) != (UtilityConnections)0)
            {
                destinations.Add(cellId - 1);
            }
            if ((connections & UtilityConnections.Right) != (UtilityConnections)0)
            {
                destinations.Add(cellId + 1);
            }
            if ((connections & UtilityConnections.Up) != (UtilityConnections)0)
            {
                destinations.Add(cellId + Grid.WidthInCells);
            }
            if ((connections & UtilityConnections.Down) != (UtilityConnections)0)
            {
                destinations.Add(cellId - Grid.WidthInCells);
            }

            return destinations;
        }

        public static int GetCellValence(this Wire wire)
        {
            return wire.GetWireConnectionsString().Length;
        }

        public static WireBuildingProperties GetWireBuildingProperties(this Wire wire)
        {
            var game = Game.Instance;
            var circuitManager = game.circuitManager;

            var networkId = wire.NetworkID;
            var cellId = wire.GetNetworkCell();

            var batteries = circuitManager.GetBatteriesOnCircuit(networkId);
            var generators = circuitManager.GetGeneratorsOnCircuit(networkId);
            var consumers = circuitManager.GetConsumersOnCircuit(networkId);
            var transformers = circuitManager.GetTransformersOnCircuit(networkId);

            var generatorOnCell = generators.Where(g => g.PowerCell == cellId).FirstOrDefault();
            var consumerOnCell = consumers.Where(c => c.PowerCell == cellId).FirstOrDefault();
            var batteryOnCell = batteries.Where(b => b.PowerCell == cellId).FirstOrDefault();

            var hasBuilding = true;
            var powerGeneration = 0.0;
            var powerDraw = 0.0;

            if (generatorOnCell != null)
            {
                powerGeneration = generatorOnCell.WattageRating;
                powerDraw = 0;
            }
            else if (consumerOnCell != null)
            {
                powerGeneration = 0;
                powerDraw = consumerOnCell.WattsUsed;
            }
            else if (batteryOnCell != null)
            {
                powerGeneration = batteryOnCell.chargeWattage;
                powerDraw = batteryOnCell.WattsUsed;
            }
            else
            {
                hasBuilding = false;
            }

            return new WireBuildingProperties(hasBuilding, powerDraw, powerGeneration);
        }
    }

    public class WireBuildingProperties
    {
        public bool HasBuilding { get; set; }
        public double PowerDraw { get; set; }
        public double PowerGeneration { get; set; }

        public WireBuildingProperties(bool hasBuilding, double powerDraw, double powerGeneration)
        {
            this.HasBuilding = hasBuilding;
            this.PowerDraw = powerDraw;
            this.PowerGeneration = powerGeneration;
        }
    }
}