using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using System.Linq;

namespace RealisticCurrentFlow
{
    public static class CircuitFlowSolver
    {
        public static Vector<double> SolveForPowerFlows(List<CircuitNode> nodes, List<CircuitBranch> branches)
        {
            var P = CreatePowerInjectionVector(nodes);
            var B = CreateAdmittanceMatrix(nodes, branches);
            var theta = SolveVoltageAngleVector(B, P);

            var b = CreateSusceptanceMatrix(branches);
            var A = CreateIncidenceMatrixFromBranches(nodes, branches);

            return CalculateBranchFlowVector(b, A, theta);
        }

        private static Matrix<double> CreateIncidenceMatrixFromBranches(List<CircuitNode> nodes, List<CircuitBranch> branches)
        {
            var A = Matrix<double>.Build.Dense(branches.Count, nodes.Count);

            foreach (CircuitBranch branch in branches)
            {
                A[branch.Id, branch.SourceNodeId] = 1;
                A[branch.Id, branch.DestinationNodeId] = -1;
            }

            return A;
        }

        private static Matrix<double> CreateAdmittanceMatrix(List<CircuitNode> nodes, List<CircuitBranch> branches)
        {
            var B = Matrix<double>.Build.Dense(nodes.Count, nodes.Count);

            foreach (CircuitBranch branch in branches)
            {
                B[branch.SourceNodeId, branch.DestinationNodeId] += -1 / branch.Reactance;
                B[branch.DestinationNodeId, branch.SourceNodeId] += -1 / branch.Reactance;
            }

            foreach (CircuitNode node in nodes)
            {
                B[node.Id, node.Id] = branches.Where(b => b.DestinationNodeId == node.Id || b.SourceNodeId == node.Id).Select(e => 1 / e.Reactance).Sum();
            }

            return B;
        }

        private static Vector<double> CreatePowerInjectionVector(List<CircuitNode> nodes)
        {
            return Vector<double>.Build.Dense(nodes.Count, i => nodes[i].PowerGeneration - nodes[i].PowerDraw);
        }

        private static Matrix<double> CreateSusceptanceMatrix(List<CircuitBranch> branches)
        {
            var b = Matrix<double>.Build.Diagonal(branches.Count, branches.Count);
            foreach (CircuitBranch branch in branches)
            {
                b[branch.Id, branch.Id] = 1 / branch.Reactance;
            }
            return b;
        }

        private static Vector<double> SolveVoltageAngleVector(Matrix<double> B, Vector<double> P)
        {
            // Designate first bus as the slack bus and remove associated column and row
            var reducedB = B.RemoveColumn(0).RemoveRow(0);
            var noSlackTheta = reducedB.Solve(P.SubVector(1, P.Count - 1));

            // Add slack voltage angle to theta. Set to zero by definition.
            return Vector<double>.Build.Dense(1).ToColumnMatrix().Stack(noSlackTheta.ToColumnMatrix()).Column(0);
        }

        private static Vector<double> CalculateBranchFlowVector(Matrix<double> b, Matrix<double> A, Vector<double> theta)
        {
            return b * A * theta;
        }
    }

    public class CircuitNode
    {
        public int Id { get; set; }
        public double PowerDraw { get; set; }
        public double PowerGeneration { get; set; }

        public CircuitNode(int id, double powerDraw, double powerGeneration)
        {
            this.Id = id;
            this.PowerDraw = powerDraw;
            this.PowerGeneration = powerGeneration;
        }
    }

    public class CircuitBranch
    {
        public int Id { get; set; }
        public int SourceNodeId { get; set; }
        public int DestinationNodeId { get; set; }
        public double Reactance { get; set; }

        public CircuitBranch(int id, int sourceNodeId, int destinationNodeId, double reactance)
        {
            this.Id = id;
            this.SourceNodeId = sourceNodeId;
            this.DestinationNodeId = destinationNodeId;
            this.Reactance = reactance;
        }
    }
}
