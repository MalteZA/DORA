using Maes.Algorithms;
using Maes.Map.MapPatrollingGen;
using Maes.Map.RobotSpawners;
using Maes.Simulation.SimulationScenarios;
using Maes.Statistics;
using Maes.Trackers;
using Maes.UI.SimulationInfoUIControllers;

namespace Maes.Simulation
{
    public sealed class PatrollingSimulation : SimulationBase<PatrollingSimulation, PatrollingVisualizer, PatrollingCell, PatrollingTracker, PatrollingInfoUIController, IPatrollingAlgorithm, PatrollingSimulationScenario, PatrollingRobotSpawner>
    {
        public PatrollingVisualizer patrollingVisualizer = null!;

        // Set after AfterCollisionMapGenerated is called
        public PatrollingTracker PatrollingTracker = null!;

        public override PatrollingVisualizer Visualizer => patrollingVisualizer;

        public override PatrollingTracker Tracker => PatrollingTracker;
        
        protected override void AfterCollisionMapGenerated(PatrollingSimulationScenario scenario)
        {
            var patrollingMap = scenario.PatrollingMapFactory(new PatrollingMapSpawner(), _collisionMap);
            
            PatrollingTracker = new PatrollingTracker(_collisionMap, patrollingVisualizer, this, scenario, patrollingMap);
            
            patrollingVisualizer.SetPatrollingMap(patrollingMap);
            
            RobotSpawner.SetPatrolling(patrollingMap, PatrollingTracker);
        }

        public override bool HasFinishedSim()
        {
            if (_scenario.TotalCycles != PatrollingTracker.CompletedCycles) return false;

            if (!PatrollingTracker.StopAfterDiff) return true;
            return PatrollingTracker.AverageGraphDiffLastTwoCyclesProportion <= 0.025;
        }
    }
}