// Copyright 2024 MAES
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
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard, Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen,
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;

using Maes.Algorithms;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Map.RobotSpawners;
using Maes.Robot;

namespace Maes.Simulation.SimulationScenarios
{
    
    // A function that generates, initializes and returns a world map
    public delegate SimulationMap<Tile> MapFactory(MapSpawner generator);
    // A function that spawns and returns a group of robots
    public delegate List<MonaRobot> RobotFactory<TAlgorithm>(SimulationMap<Tile> map, RobotSpawner<TAlgorithm> spawner) where TAlgorithm : IAlgorithm;
    
    // A function that returns true if the given simulation has been completed
    public delegate bool SimulationEndCriteriaDelegate<TSimulation>(TSimulation simulationBase)
        where TSimulation : ISimulation;
    
    // Contains all information needed for simulating a single simulation scenario
    // (One map, one type of robots)
    public abstract class SimulationScenario<TSimulation, TAlgorithm> : ISimulationScenario
    where TSimulation : ISimulation
    where TAlgorithm : IAlgorithm
    {
        protected readonly int Seed;
        public readonly SimulationEndCriteriaDelegate<TSimulation> HasFinishedSim; 
        
        public MapFactory MapSpawner { get; }
        public RobotFactory<TAlgorithm> RobotSpawner { get; }
        public RobotConstraints RobotConstraints { get; }
        public string StatisticsFileName { get; }

        protected SimulationScenario(
            int seed, 
            RobotFactory<TAlgorithm> robotSpawner, 
            SimulationEndCriteriaDelegate<TSimulation>? hasFinishedSim=null, 
            MapFactory? mapSpawner=null, 
            RobotConstraints? robotConstraints=null, 
            string? statisticsFileName=null
            )
        {
            Seed = seed;
            HasFinishedSim = hasFinishedSim ?? (simulation => simulation.HasFinishedSim());
            // Default to generating a cave map when no map generator is specified
            MapSpawner = mapSpawner ?? (generator => generator.GenerateMap(new CaveMapConfig(seed)));
            RobotSpawner = robotSpawner;
            RobotConstraints = robotConstraints ?? new RobotConstraints();
            StatisticsFileName = statisticsFileName ?? $"statistics_{DateTime.Now.ToShortDateString().Replace('/','-')}" +
                $"_{DateTime.Now.ToLongTimeString().Replace(' ','-').Replace(':','-')}";
        }
    }
}