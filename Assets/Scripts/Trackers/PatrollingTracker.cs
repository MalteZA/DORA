using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Robot;

using UnityEngine;

namespace Maes.Trackers
{
    public class PatrollingTracker : ITracker
    {
        private RobotConstraints Constraints { get; }
        private PatrollingSimulation PatrollingSimulation { get; }
        private PatrollingMap Map { get;}
        private Dictionary<Vector2Int, VertexDetails> Vertices { get; }
        
        public int WorstGraphIdleness { get; private set; }
        // TODO: TotalDistanceTraveled is not set any where in the code, don't know how to calculate it yet
        public float TotalDistanceTraveled { get; private set; } = 0;
        public float CurrentGraphIdleness { get; private set; } = 0;
        public float AverageGraphIdleness => GraphIdlenessList.Count != 0 ? GraphIdlenessList.Average() : 0;
        public int CompletedCycles { get; private set; } = 0;
        public float? AverageGraphDiffLastTwoCyclesProportion => GraphIdlenessList.Count >= 2 ? Mathf.Abs(GraphIdlenessList[^1] - GraphIdlenessList[^2]) / GraphIdlenessList[^2] : null;

        private List<float> GraphIdlenessList { get; } = new();
        
        public PatrollingTracker(PatrollingSimulation patrollingSimulation, RobotConstraints constraints,
            PatrollingMap map)
        {
            PatrollingSimulation = patrollingSimulation;
            Constraints = constraints;
            Map = map;
            Vertices = map.Verticies.ToDictionary(vertex => vertex.Position, vertex => new VertexDetails(vertex));
        }

        public void OnReachedVertex(Vertex vertex, int atTick)
        {
            if (!Vertices.TryGetValue(vertex.Position, out var vertexDetails)) return;

            var idleness = atTick - vertexDetails.LastTimeVisitedTick;
            vertexDetails.MaxIdleness = Mathf.Max(vertexDetails.MaxIdleness, idleness);
            vertexDetails.NumberOfVisits++;
            vertexDetails.VisitedAtTick(atTick);
                
            WorstGraphIdleness = Mathf.Max(WorstGraphIdleness, vertexDetails.MaxIdleness);
            SetCompletedCycles();
        }
        
        public void LogicUpdate(IReadOnlyList<MonaRobot> robots)
        {
            var eachVertexIdleness = GetEachVertexIdleness();
            
            WorstGraphIdleness = Mathf.Max(WorstGraphIdleness, eachVertexIdleness.Max());
            CurrentGraphIdleness = eachVertexIdleness.Average(n => (float)n);
            GraphIdlenessList.Add(CurrentGraphIdleness);
            
            // TODO: Remove this when the code UI is set up, just for showing that it works
            Debug.Log($"Worst graph idleness: {WorstGraphIdleness}, Current graph idleness: {CurrentGraphIdleness}, Average graph idleness: {AverageGraphIdleness}");
            
            if (Constraints.AutomaticallyUpdateSlam) {
                // Always update estimated robot position and rotation
                // regardless of whether the slam map was updated this tick
                foreach (var robot in robots) {
                    var slamMap = robot.Controller.SlamMap;
                    slamMap.UpdateApproxPosition(robot.transform.position);
                    slamMap.SetApproxRobotAngle(robot.Controller.GetForwardAngleRelativeToXAxis());
                }
            }
        }

        public void SetVisualizedRobot(MonaRobot robot)
        {
            // TODO: Implement
        }
        
        private IReadOnlyList<int> GetEachVertexIdleness()
        {
            var currentTick = PatrollingSimulation.SimulatedLogicTicks;
            return Vertices.Values.Select(vertex => currentTick - vertex.LastTimeVisitedTick).ToArray();
        }
        
        private void SetCompletedCycles()
        {
            CompletedCycles = Vertices.Values.Select(v => v.NumberOfVisits).Min();
        }
    }
}