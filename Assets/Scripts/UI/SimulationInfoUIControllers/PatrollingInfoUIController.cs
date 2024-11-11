using Maes;
using Maes.Algorithms;

using MAES.Simulation.SimulationScenarios;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using XCharts.Runtime;

namespace MAES.UI.SimulationInfoUIControllers
{
    public sealed class PatrollingInfoUIController : SimulationInfoUIControllerBase<PatrollingSimulation, IPatrollingAlgorithm, PatrollingSimulationScenario>
    {
        public ScatterChart Chart;
        public Image ProgressBarMask;
        public TextMeshProUGUI ProgressText;

        public Toggle StoppingCriteriaToggle;
        
        public TextMeshProUGUI DistanceTravelledText;
        public TextMeshProUGUI CurrentGraphIdlenessText;
        public TextMeshProUGUI WorstGraphIdlenessText;
        public TextMeshProUGUI AverageGraphIdlenessText;
        
        public Button WaypointHeatMapButton;
        public Button ToogleIdleGraphButton;
        
        protected override void AfterStart()
        {
            InitIdleGraph();
            StoppingCriteriaToggle.onValueChanged.AddListener(delegate {
                //TODO: when the stopping criteria is toggled
            });
            
            ToogleIdleGraphButton.onClick.AddListener(() => ToogleGraph());
            
            WaypointHeatMapButton.onClick.AddListener(() => {
                ExecuteAndRememberMapVisualizationModification(sim => {
                    if (sim != null) {
                        sim.PatrollingTracker.ShowWaypointHeatMap();
                    }
                });
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

        private void ToogleGraph()
        {
            Chart.gameObject.SetActive(!Chart.gameObject.activeSelf);
        }
        
        private void InitIdleGraph()
        {
            Chart.Init();
            var xAxis = Chart.EnsureChartComponent<XAxis>();
            xAxis.splitNumber = 10;
            xAxis.boundaryGap = true;
            xAxis.type =  Axis.AxisType.Category;

            var yAxis = Chart.EnsureChartComponent<YAxis>();
            yAxis.type =  Axis.AxisType.Value;
            Chart.RemoveData();
            var series = Chart.AddSerie<Scatter>("scatter");
            series.symbol.size = 4;
            
            Simulation!.PatrollingTracker.Chart = Chart;
        }
    }
}