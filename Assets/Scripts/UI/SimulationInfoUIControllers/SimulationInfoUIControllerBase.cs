#nullable enable
// Copyright 2022 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
// 
// Original repository: https://github.com/MalteZA/MAES

using Maes;
using Maes.Algorithms;

using MAES.Simulation;
using MAES.Simulation.SimulationScenarios;

using Maes.UI;

using UnityEngine;
using UnityEngine.UI;

namespace MAES.UI.SimulationInfoUIControllers {
    public abstract class SimulationInfoUIControllerBase<TSimulation, TAlgorithm, TScenario> : MonoBehaviour, ISimulationInfoUIController
    where TSimulation : class, ISimulation<TSimulation, TAlgorithm, TScenario>
    where TAlgorithm : IAlgorithm
    where TScenario : SimulationScenario<TSimulation, TAlgorithm>
    {

        public Text AlgorithmDebugText;
        public Text ControllerDebugText;
        public Text TagDebugText;

        public Text MouseCoordinateText;
        
        
        public Button AllVisualizeTagsButton;
        private bool _visualizingAllTags = false;
        public Button VisualizeTagsButton;
        private bool _visualizingSelectedTags = false;

        public Button StickyCameraButton;

        public TSimulation? Simulation => simulationManager.CurrentSimulation;
        

        public SimulationManager<TSimulation, TAlgorithm, TScenario> simulationManager;
        // Represents a function that modifies the given simulation in some way
        // (for example by changing map visualization mode)
        protected delegate void SimulationModification(TSimulation? simulation);

        protected SimulationModification? _mostRecentMapVisualizationModification;
        
        protected readonly Color _mapVisualizationColor = Color.white;
        protected readonly Color _mapVisualizationSelectedColor = new Color(150 / 255f, 200 / 255f, 150 / 255f);

        protected abstract void AfterStart();

        private void Start()
        {
            /*// Set listeners for Tag visualization buttons 
            AllVisualizeTagsButton.onClick.AddListener(() => {
                ExecuteAndRememberTagVisualization(sim => {
                    if (sim != null) {
                        ToggleVisualizeTagsButtons(AllVisualizeTagsButton);
                    }
                });
            });
            
            VisualizeTagsButton.onClick.AddListener(() => {
                ExecuteAndRememberTagVisualization(sim => {
                    if (sim != null) {
                        if (sim.HasSelectedRobot()) {
                            ToggleVisualizeTagsButtons(VisualizeTagsButton);
                        }
                    }
                });
            });*/
            
            StickyCameraButton.onClick.AddListener(() => {
                CameraController.singletonInstance.stickyCam = !CameraController.singletonInstance.stickyCam;
                StickyCameraButton.image.color = CameraController.singletonInstance.stickyCam ? _mapVisualizationSelectedColor : _mapVisualizationColor;
            });
            
            AfterStart();
        }

        public void Update() {
            if (Simulation is not null) {
                if (_visualizingAllTags) {
                    Simulation.ShowAllTags();
                }
                else if (_visualizingSelectedTags) {
                    Simulation.ShowSelectedTags();
                }
                Simulation.RenderCommunicationLines();
            }
        }

        public void ClearSelectedRobot() {
            _visualizingSelectedTags = false;
            CameraController.singletonInstance.stickyCam = false;
            StickyCameraButton.image.color = _mapVisualizationColor;
            VisualizeTagsButton.image.color = _mapVisualizationColor;
        }

        public void UpdateMouseCoordinates(Vector2 mousePosition) {
            var xNumberString = $"{mousePosition.x:00.00}".PadLeft(6);
            var yNumberString = $"{mousePosition.y:00.00}".PadLeft(6);
            MouseCoordinateText.text = $"(x: {xNumberString}, y: {yNumberString})";
        }

        private void ToggleVisualizeTagsButtons(Button button) {
            simulationManager.CurrentSimulation.ClearVisualTags();
            if (button.name == "AllVisualizeTags") {
                _visualizingSelectedTags = false;
                VisualizeTagsButton.image.color = _mapVisualizationColor;
                _visualizingAllTags = !_visualizingAllTags;
                button.image.color = _visualizingAllTags ? _mapVisualizationSelectedColor : _mapVisualizationColor;
            }
            else {
                _visualizingAllTags = false;
                AllVisualizeTagsButton.image.color = _mapVisualizationColor;
                _visualizingSelectedTags = !_visualizingSelectedTags;
                button.image.color = _visualizingSelectedTags ? _mapVisualizationSelectedColor : _mapVisualizationColor;
            }
        }

        // This function executes the given map visualization change and remembers it.
        // Whenever the simulator creates a new simulation the most recent visualization change is repeated 
        protected void ExecuteAndRememberMapVisualizationModification(SimulationModification modificationFunc) {
            _mostRecentMapVisualizationModification = modificationFunc;
            modificationFunc(Simulation);
        }

        private void ExecuteAndRememberTagVisualization(SimulationModification modificationFunc) {
            modificationFunc(Simulation);
        }

        public void UpdateAlgorithmDebugInfo(string info) {
            AlgorithmDebugText.text = info;
        }


        public void UpdateControllerDebugInfo(string info) {
            ControllerDebugText.text = info;
        }

        public void UpdateTagDebugInfo(string info) {
            TagDebugText.text = info;
        }

        // Called whenever the simulator instantiates a new simulation object 
        protected abstract void NotifyNewSimulation(TSimulation? newSimulation);

        public void NotifyNewSimulation(ISimulation? simulation)
        {
            NotifyNewSimulation((TSimulation?) simulation);
        }

        protected abstract void UpdateStatistics(TSimulation? simulation);

        public void UpdateStatistics(ISimulation? simulation)
        { 
            UpdateStatistics((TSimulation?) simulation);
        }
    }
}