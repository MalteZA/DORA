using Maes;
using Maes.Algorithms;
using MAES.UI.RestartRemakeContollers;
using UnityEngine;

namespace MAES.Simulation
{
    public class ExplorationSimulationManager : SimulationManager<ExplorationSimulation, IExplorationAlgorithm>
    {
        public override void AddRestartRemakeController(GameObject restartRemakePanel)
        {
            restartRemakePanel.AddComponent<ExplorationRestartRemakeController>();
        }

        protected override ExplorationSimulation AddSimulation(GameObject gameObject)
        {
            return gameObject.AddComponent<ExplorationSimulation>();
        }
    }
}