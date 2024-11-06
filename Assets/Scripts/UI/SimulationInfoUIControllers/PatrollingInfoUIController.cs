using Maes;
using Maes.Algorithms;

using MAES.Simulation.SimulationScenarios;

using TMPro;

using UnityEngine.UI;

namespace MAES.UI.SimulationInfoUIControllers
{
    public sealed class PatrollingInfoUIController : SimulationInfoUIControllerBase<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        public Image ProgressBarMask;
        public TextMeshProUGUI ProgressText;

        public Toggle StoppingCriteriaToggle;
        
        public TextMeshProUGUI DistanceTravelledText;
        public TextMeshProUGUI CurrentGraphIdlenessText;
        public TextMeshProUGUI WorstGraphIdlenessText;
        public TextMeshProUGUI AverageGraphIdlenessText;
        
        
        protected override void AfterStart()
        {
            StoppingCriteriaToggle.onValueChanged.AddListener(delegate {
                //TODO: when the stopping criteria is toggled
            });
        }

        protected override void NotifyNewSimulation(PatrollingSimulation newSimulation)
        {
            //TODO: Implement
        }

        protected override void UpdateStatistics(PatrollingSimulation simulation)
        {
            if (!simulation) return;
            SetProgress(simulation.PatrollingTracker.CompletedCycles, simulation.PatrollingTracker.TotalCycles);
            SetDistanceTravelled(simulation.PatrollingTracker.TotalDistanceTraveled);
            SetCurrentGraphIdleness(simulation.PatrollingTracker.CurrentGraphIdleness);
            SetWorstGraphIdleness(simulation.PatrollingTracker.WorstGraphIdleness);
            SetAverageGraphIdleness(simulation.PatrollingTracker.AverageGraphIdleness);
        }
        
        private void SetProgress(int completed, int total)
        {
            ProgressBarMask.fillAmount = (float)completed / total;
            ProgressText.text = $"{completed}/{total}";
        }
        
        private void SetDistanceTravelled(float distance) => 
            DistanceTravelledText.text = $"The total patrolling distance traveled: {distance} meters";
        
        private void SetCurrentGraphIdleness(float idleness) => 
            CurrentGraphIdlenessText.text = $"Current graph idleness: {idleness} ticks";
        
        private void SetWorstGraphIdleness(float idleness) => 
            WorstGraphIdlenessText.text = $"Worst graph idleness: {idleness} ticks";
        
        private void SetAverageGraphIdleness(float idleness) => 
            AverageGraphIdlenessText.text = $"Average graph idleness: {idleness} ticks";
    }
}