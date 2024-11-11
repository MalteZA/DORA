using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.Visualization;
using Maes.Map.Visualization.Patrolling;

using Maes.Robot;
using Maes.Simulation;
using Maes.Simulation.SimulationScenarios;
using Maes.Statistics;

using UnityEngine;

namespace Maes.Trackers
{
    // TODO: Change Tile to another type, Implemented in the next PR
    public class PatrollingTracker : Tracker<PatrollingCell, PatrollingVisualizer, IPatrollingVisualizationMode>
    {
        private PatrollingSimulation PatrollingSimulation { get; }
        private PatrollingMap Map { get;}
        private Dictionary<Vector2Int, VertexDetails> Vertices { get; }

        public int WorstGraphIdleness { get; private set; }
        // TODO: TotalDistanceTraveled is not set any where in the code, don't know how to calculate it yet
        public float TotalDistanceTraveled { get; private set; }
        public float CurrentGraphIdleness { get; private set; }
        public float AverageGraphIdleness => GraphIdlenessList.Count != 0 ? GraphIdlenessList.Average() : 0;
        public int CompletedCycles { get; private set; }
        public float? AverageGraphDiffLastTwoCyclesProportion => GraphIdlenessList.Count >= 2 ? Mathf.Abs(GraphIdlenessList[^1] - GraphIdlenessList[^2]) / GraphIdlenessList[^2] : null;

        private List<float> GraphIdlenessList { get; } = new();
        //TODO: TotalCycles is not set any where in the code
        public int TotalCycles { get; }
        public bool StopAfterDiff { get; set; } 

        public PatrollingTracker(SimulationMap<Tile> collisionMap, PatrollingVisualizer visualizer, PatrollingSimulation patrollingSimulation, PatrollingSimulationScenario scenario,
            PatrollingMap map) : base(collisionMap, visualizer, scenario.RobotConstraints, tile => new PatrollingCell(isExplorable: !Tile.IsWall(tile.Type)))
        {
            PatrollingSimulation = patrollingSimulation;
            Map = map;
            Vertices = map.Vertices.ToDictionary(vertex => vertex.Position, vertex => new VertexDetails(vertex));
            TotalCycles = scenario.TotalCycles;
            StopAfterDiff = scenario.StopAfterDiff;
            
            _visualizer.meshRenderer.enabled = false;
            _currentVisualizationMode = new WaypointHeatMapVisualizationMode();
        }

        public void OnReachedVertex(Vertex vertex, int atTick)
        {
            if (!Vertices.TryGetValue(vertex.Position, out var vertexDetails)) return;

            var idleness = atTick - vertexDetails.LastTimeVisitedTick;
            vertexDetails.MaxIdleness = Mathf.Max(vertexDetails.MaxIdleness, idleness);
            vertexDetails.VisitedAtTick(atTick);
                
            WorstGraphIdleness = Mathf.Max(WorstGraphIdleness, vertexDetails.MaxIdleness);
            SetCompletedCycles();
        }
        
        protected override void OnLogicUpdate(IReadOnlyList<MonaRobot> robots)
        {
            var eachVertexIdleness = GetEachVertexIdleness();
            
            WorstGraphIdleness = Mathf.Max(WorstGraphIdleness, eachVertexIdleness.Max());
            CurrentGraphIdleness = eachVertexIdleness.Average(n => (float)n);
            GraphIdlenessList.Add(CurrentGraphIdleness);
            
            // TODO: Remove this when the code UI is set up, just for showing that it works
            Debug.Log($"Worst graph idleness: {WorstGraphIdleness}, Current graph idleness: {CurrentGraphIdleness}, Average graph idleness: {AverageGraphIdleness}");
        }

        public override void SetVisualizedRobot(MonaRobot? robot)
        {
            // TODO: Implement
        }

        protected override void CreateSnapShot()
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
        
        public void ShowWaypointHeatMap()
        {
            _visualizer.meshRenderer.enabled = false;
            SetVisualizationMode(new WaypointHeatMapVisualizationMode());
        }

        public void ShowAllRobotCoverageHeatMap() {
            _visualizer.meshRenderer.enabled = true;
            SetVisualizationMode(new PatrollingCoverageHeatMapVisualizationMode(_map));
        }

        public void ShowAllRobotPatrollingHeatMap() {
            _visualizer.meshRenderer.enabled = true;
            SetVisualizationMode(new PatrollingHeatMapVisualizationMode(_map));
        }
    }
}