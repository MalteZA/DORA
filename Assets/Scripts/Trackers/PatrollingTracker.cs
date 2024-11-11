using System.Collections.Generic;
using System.Linq;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.Visualization;
using Maes.Robot;
using Maes.Statistics;
using MAES.Trackers;

using UnityEngine;

using XCharts.Runtime;

namespace Maes.Trackers
{
    // TODO: Change Tile to another type, Implemented in the next PR
    public class PatrollingTracker : Tracker<PatrollingCell, PatrollingVisualizer, IPatrollingVisualizationMode>
    {
        private PatrollingSimulation PatrollingSimulation { get; }
        private PatrollingMap Map { get;}
        private Dictionary<Vector2Int, VertexDetails> Vertices { get; }
        private int PlottedCycles = 0;

        public int WorstGraphIdleness { get; private set; }
        // TODO: TotalDistanceTraveled is not set any where in the code, don't know how to calculate it yet
        public float TotalDistanceTraveled { get; private set; } = 0;
        public float CurrentGraphIdleness { get; private set; } = 0;
        public float AverageGraphIdleness => GraphIdlenessList.Count != 0 ? GraphIdlenessList.Average() : 0;
        public int CompletedCycles { get; private set; } = 0;
        public float? AverageGraphDiffLastTwoCyclesProportion => GraphIdlenessList.Count >= 2 ? Mathf.Abs(GraphIdlenessList[^1] - GraphIdlenessList[^2]) / GraphIdlenessList[^2] : null;

        public ScatterChart Chart { get; set; }
        
        private List<float> GraphIdlenessList { get; } = new();
        //TODO: TotalCycles is not set any where in the code
        public int TotalCycles { get; set; } = 10;

        public PatrollingTracker(SimulationMap<Tile> collisionMap, PatrollingVisualizer visualizer, PatrollingSimulation patrollingSimulation, RobotConstraints constraints,
            PatrollingMap map) : base(collisionMap, visualizer, constraints, tile => new PatrollingCell(isExplorable: !Tile.IsWall(tile.Type)))
        {
            PatrollingSimulation = patrollingSimulation;
            Map = map;
            Vertices = map.Verticies.ToDictionary(vertex => vertex.Position, vertex => new VertexDetails(vertex));
            
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
            // Example: How to plot the data
            // TODO: Plot the correct data.
            if (_currentTick % 100 == 0)
            {
                PlottedCycles++;
                Chart.AddXAxisData("" + _currentTick);
                Chart.AddData(0, CurrentGraphIdleness);
            }
            // TODO: Remove this when the code UI is set up, just for showing that it works
            Debug.Log($"Worst graph idleness: {WorstGraphIdleness}, Current graph idleness: {CurrentGraphIdleness}, Average graph idleness: {AverageGraphIdleness}");
        }

        public override void SetVisualizedRobot(MonaRobot robot)
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
        
        public void ShowWaypointHeatMap()
        {
            SetVisualizationMode(new WaypointHeatMapVisualizationMode());
        }
        private void SetCompletedCycles()
        {
            CompletedCycles = Vertices.Values.Select(v => v.NumberOfVisits).Min();
        }
    }
}