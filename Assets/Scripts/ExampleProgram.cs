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

using System;
using Maes.ExplorationAlgorithm.Minotaur;
using System.Collections;
using Maes.ExplorationAlgorithm.TheNextFrontier;
using Maes.Map;
using Maes.Map.MapGen;
using Maes.Robot;
using Maes.Utilities.Files;
using UnityEngine;
using Maes.Robot;
using Maes.ExplorationAlgorithm.Movement;
using System.Collections.Generic;
using Maes.UI;
using UnityEditor;
using System.Linq;

namespace Maes
{
    internal class ExampleProgram : MonoBehaviour
    {
        private Simulator _simulator;
        private void Start()
        {
            const int randomSeed = 12345;

            var constraintsList = new List<RobotConstraints>();

            //var constraintsGlobalCommunication = new RobotConstraints(
            constraintsList.Add(new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (distanceTravelled, distanceThroughWalls) =>
                {
                    return true;
                }
            ));

            //var constraintsMaterials = new RobotConstraints(
            constraintsList.Add(new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                materialCommunication: true
            ));

            //var constraintsLOS = new RobotConstraints(
            constraintsList.Add(new RobotConstraints(
                senseNearbyAgentsRange: 5f,
                senseNearbyAgentsBlockedByWalls: true,
                automaticallyUpdateSlam: true,
                slamUpdateIntervalInTicks: 1,
                slamSynchronizeIntervalInTicks: 10,
                slamPositionInaccuracy: 0.2f,
                distributeSlam: false,
                environmentTagReadRange: 4.0f,
                slamRayTraceRange: 7f,
                relativeMoveSpeed: 1f,
                agentRelativeSize: 0.6f,
                calculateSignalTransmissionProbability: (distanceTravelled, distanceThroughWalls) =>
                {
                    // Blocked by walls
                    if (distanceThroughWalls > 0)
                    {
                        return false;
                    }
                    return true;
                }
            ));

            var simulator = Simulator.GetInstance();
            RobotSpawner.CreateAlgorithmDelegate tnf = seed => new TnfExplorationAlgorithm(1, 10, seed);
            RobotSpawner.CreateAlgorithmDelegate minos = seed => new MinotaurAlgorithm(constraints, seed, 6);


            var random = new System.Random(1234);
            List<int> rand_numbers = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                var val = random.Next(0, 1000000);
                rand_numbers.Add(val);
            }

            var buildingConfigList50 = new List<BuildingMapConfig>();
            var buildingConfigList75 = new List<BuildingMapConfig>();
            var buildingConfigList100 = new List<BuildingMapConfig>();
            foreach (int val in rand_numbers)
            {
                buildingConfigList50.Add(new BuildingMapConfig(val, widthInTiles: 50, heightInTiles: 50, doorWidth: 6, minRoomSideLength: 11));
                buildingConfigList75.Add(new BuildingMapConfig(val, widthInTiles: 75, heightInTiles: 75));
                buildingConfigList100.Add(new BuildingMapConfig(val, widthInTiles: 100, heightInTiles: 100));
            }

            var constraintIterator = 0;
            var mapSizes = new List<int>{50, 75, 100};
            foreach (var constraint in constraintsList){
                var constraintName = '';
                switch (constraintIterator)
                {
                    case 0:
                        constraintName = "Global";
                        break;
                    case 1:
                        constraintName = "Material";
                        break;
                    default:
                        constraintName = "LOS";
                }
                constraintIterator++;
                var buildingMaps = buildingConfigList50.Union(buildingConfigList75.Union(buildingConfigList100));
                foreach (var mapConfig in buildingMaps)
                {
                    for (var amountOfRobots = 1; amountOfRobots < 10; amountOfRobots += 2)
                    {

                        foreach(var size in mapSizes)
                        {
                            simulator.EnqueueScenario(new SimulationScenario(seed: 123,
                                                                             mapSpawner: generator => generator.GenerateMap(mapConfig),
                                                                             robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                                 buildingConfig,
                                                                                 seed: 123,
                                                                                 numberOfRobots: amountOfRobots,
                                                                                 suggestedStartingPoint: new Vector2Int(random.Next(0, size), random.Next(0, size)),
                                                                                 createAlgorithmDelegate: minos),
                                                                             statisticsFileName: $"minotaur-seed-{map.RandomSeed}-size-{size}-comms-{constraintName}-robots-{amountOfRobots}-SpawnTogether",
                                                                             robotConstraints: constraint)
                            );

                            var spawningPosList = new List<Vector2Int>{}();
                            for (var amountOfSpawns = 0; amountOfSpawns <= amountOfRobots; amountOfSpawns++)
                                {
                                    spawningPosList.Add( new Vector2Int(random.Next(0, size), random.Next(0, size)));
                                }

                            simulator.EnqueueScenario(new SimulationScenario(seed: 123,
                                                                             mapSpawner: generator => generator.GenerateMap(mapConfig),
                                                                             robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsAtPositions(
                                                                                 collisionMap: buildingConfig,
                                                                                 seed: 123,
                                                                                 numberOfRobots: amountOfRobots,
                                                                                 spawnPositions: spawningPosList,
                                                                                 createAlgorithmDelegate: minos),
                                                                             statisticsFileName: $"minotaur-seed-{map.RandomSeed}-size-{size}-comms-{constraintName}-robots-{amountOfRobots}-SpawnApart",
                                                                             robotConstraints: constraint)
                            );
                        }
                    }
                }
            }

            //Just code to make sure we don't get too many maps of the last one in the experiment
            var dumpMap = new BuildingMapConfig(val, widthInTiles: 50, heightInTiles: 50)
            simulator.EnqueueScenario(new SimulationScenario(seed: 123,
                mapSpawner: generator => generator.GenerateMap(dumpMap),
                robotSpawner: (buildingConfig, spawner) => spawner.SpawnRobotsTogether(
                                                                 buildingConfig,
                                                                 seed: 123,
                                                                 numberOfRobots: 5,
                                                                 suggestedStartingPoint: Vector2Int.zero,
                                                                 createAlgorithmDelegate: minos),
                statisticsFileName: $"delete-me",
                robotConstraints: constraints)

        simulator.PressPlayButton(); // Instantly enter play mode

            //simulator.GetSimulationManager().AttemptSetPlayState(SimulationPlayState.FastAsPossible);
        }
    }
}
