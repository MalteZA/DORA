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
using System.Linq;

using Maes.Map;
using Maes.Map.MapGen;
using Maes.Statistics;
using Maes.Utilities;

using UnityEngine;

using Vector2 = UnityEngine.Vector2;

namespace Maes.Robot
{
    public readonly struct SensedObject<T>
    {
        public readonly float Distance;
        public readonly float Angle;
        public readonly T item;

        public SensedObject(float distance, float angle, T t)
        {
            Distance = distance;
            Angle = angle;
            item = t;
        }

        public Vector2 GetRelativePosition(Vector2 myPosition, float globalAngle)
        {
            var x = myPosition.x + (Distance * Mathf.Cos(Mathf.Deg2Rad * ((Angle + globalAngle) % 360)));
            var y = myPosition.y + (Distance * Mathf.Sin(Mathf.Deg2Rad * ((Angle + globalAngle) % 360)));
            return new Vector2(x, y);
        }
    }

    // Messages sent through this class will be subject to communication range and line of sight.
    // Communication is non-instantaneous. Messages will be received by other robots after one logic tick. 
    public class CommunicationManager : ISimulationUnit
    {
        private readonly RobotConstraints _robotConstraints;
        private readonly DebuggingVisualizer _visualizer;

        // Messages that will sent during the next logic update
        private readonly List<Message> _queuedMessages = new();

        // Messages that were sent last tick and can now be read 
        private readonly List<Message> _readableMessages = new();

        private readonly RayTracingMap<Tile> _rayTracingMap;
        private IReadOnlyList<MonaRobot> _robots = Array.Empty<MonaRobot>();

        // Map for storing and retrieving all tags deposited by robots
        private readonly EnvironmentTaggingMap _environmentTaggingMap;

        private int _localTickCounter;

        private Dictionary<(int, int), CommunicationInfo>? _adjacencyMatrix;

        private List<HashSet<int>>? _communicationGroups;

        private float _robotRelativeSize;

        public readonly CommunicationTracker CommunicationTracker;

        private readonly struct Message
        {
            public readonly object Contents;
            public readonly MonaRobot Sender;
            public readonly Vector2 BroadcastCenter;

            public Message(object contents, MonaRobot sender, Vector2 broadcastCenter)
            {
                Contents = contents;
                Sender = sender;
                BroadcastCenter = broadcastCenter;
            }
        }

        public readonly struct CommunicationInfo
        {
            public readonly float Distance;
            public readonly float Angle;
            public readonly int WallsCellsPassedThrough;
            public readonly int RegularCellsPassedThrough;
            public readonly bool TransmissionSuccessful;
            public readonly float SignalStrength;

            public CommunicationInfo(float distance, float angle, int wallsCellsPassedThrough, int regularCellsPassedThrough, bool transmissionSuccess, float signalStrength)
            {
                Distance = distance;
                Angle = angle;
                WallsCellsPassedThrough = wallsCellsPassedThrough;
                RegularCellsPassedThrough = regularCellsPassedThrough;
                TransmissionSuccessful = transmissionSuccess;
                SignalStrength = signalStrength;
            }

        }

        public CommunicationManager(SimulationMap<Tile> collisionMap, RobotConstraints robotConstraints,
            DebuggingVisualizer visualizer)
        {
            _robotConstraints = robotConstraints;
            _visualizer = visualizer;
            _rayTracingMap = new RayTracingMap<Tile>(collisionMap);
            _environmentTaggingMap = new EnvironmentTaggingMap(collisionMap);
            CommunicationTracker = new CommunicationTracker();
        }

        public void SetRobotRelativeSize(float robotRelativeSize)
        {
            _robotRelativeSize = robotRelativeSize;
        }

        // Adds a message to the broadcast queue
        public void BroadcastMessage(MonaRobot sender, in object messageContents)
        {
            _queuedMessages.Add(new Message(messageContents, sender, sender.transform.position));
        }

        // Returns a list of messages sent by other robots
        public List<object> ReadMessages(MonaRobot receiver)
        {
            PopulateAdjacencyMatrix();
            var messages = new List<object>();
            foreach (var message in _readableMessages)
            {
                // The robot will not receive its own messages
                if (message.Sender.id == receiver.id)
                {
                    continue;
                }

                var communicationTrace = _adjacencyMatrix![(message.Sender.id, receiver.id)];
                // If the transmission probability is above the specified threshold then the message will be sent
                // otherwise it is discarded
                if (communicationTrace.TransmissionSuccessful)
                {
                    messages.Add(message.Contents);
                    if (GlobalSettings.DrawCommunication)
                    {
                        _visualizer.AddCommunicationTrail(message.Sender, receiver);
                    }
                }
            }

            return messages;
        }

        private CommunicationInfo RayTraceCommunication(Vector2 pos1, Vector2 pos2)
        {
            var distance = Vector2.Distance(pos1, pos2);
            var angle = Vector2.Angle(Vector2.right, pos2 - pos1);
            // If p1.y > p2.y then angle should be 360 minus the angle difference between the vectors
            // to make the angle relative to the x axis. (Moving from oregon along the x axis is 0 degrees in out system)
            if (pos1.y > pos2.y)
            {
                angle = 360f - angle;
            }

            var angleMod = angle % 90f;
            if (angleMod <= 45.05f && angleMod >= 45f)
            {
                angle += 0.005f;
            }
            else if (angleMod >= 44.95f && angleMod <= 45f)
            {
                angle -= 0.005f;
            }

            var wallsTraveledThrough = 0;
            var regularCellsTraveledThrough = 0;
            var signalStrength = _robotConstraints.TransmitPower;

            _rayTracingMap.Raytrace(pos1, angle, distance, (_, tile) =>
            {
                if (Tile.IsWall(tile.Type))
                {
                    wallsTraveledThrough++;
                }
                else
                {
                    regularCellsTraveledThrough++;
                }

                if (_robotConstraints.MaterialCommunication)
                {
                    signalStrength -= _robotConstraints.AttenuationDictionary[_robotConstraints.Frequency][tile.Type];
                }

                return true;
            });
            return CreateCommunicationInfo(angle, wallsTraveledThrough, regularCellsTraveledThrough, distance, signalStrength);
        }

        private CommunicationInfo CreateCommunicationInfo(float angle, int wallsCellsPassedThrough, int regularCellsPassedThrough, float distance, float signalStrength)
        {
            var totalCells = wallsCellsPassedThrough + regularCellsPassedThrough;
            var distanceTraveledThroughWalls = ((float)wallsCellsPassedThrough / (float)totalCells) * distance;
            var transmissionSuccessful = _robotConstraints
                .IsTransmissionSuccessful(distance, distanceTraveledThroughWalls);
            if (_robotConstraints.MaterialCommunication)
            {
                transmissionSuccessful = _robotConstraints.ReceiverSensitivity <= signalStrength;
            }
            //Debug.Log($"strength: {signalStrength}, success: {transmissionSuccessful}");
            return new CommunicationInfo(distance, angle, wallsCellsPassedThrough, regularCellsPassedThrough, transmissionSuccessful, signalStrength);
        }

        public void LogicUpdate()
        {
            // Move messages sent last tick into readable messages
            _readableMessages.Clear();
            _readableMessages.AddRange(_queuedMessages);
            _queuedMessages.Clear();
            _localTickCounter++;

            if (GlobalSettings.PopulateAdjacencyAndComGroupsEveryTick)
            {
                PopulateAdjacencyMatrix();
                _communicationGroups = GetCommunicationGroups();
            }

            if (_robotConstraints.AutomaticallyUpdateSlam // Are we using slam?
                && _robotConstraints.DistributeSlam // Are we distributing slam?
                && _localTickCounter % _robotConstraints.SlamSynchronizeIntervalInTicks == 0)
            {
                SynchronizeSlamMaps();
            }

            if (GlobalSettings.ShouldWriteCsvResults && _localTickCounter % GlobalSettings.TicksPerStatsSnapShot == 0)
            {
                CommunicationTracker.AdjacencyMatrixRef = _adjacencyMatrix;
                if (_communicationGroups == null)
                {
                    _communicationGroups = GetCommunicationGroups();
                }

                CommunicationTracker.CommunicationGroups = _communicationGroups;
                CommunicationTracker.CreateSnapshot(_localTickCounter);
            }

            _adjacencyMatrix = null;
            _communicationGroups = null;
        }

        private void SynchronizeSlamMaps()
        {
            _communicationGroups = GetCommunicationGroups();

            foreach (var group in _communicationGroups)
            {
                var slamMaps = group
                    .Select(id => _robots.Single(r => r.id == id))
                    .Select(r => r.Controller.SlamMap)
                    .ToList();

                SlamMap.Synchronize(slamMaps);
            }
        }

        public void PhysicsUpdate()
        {
            // No physics update needed
        }

        private void PopulateAdjacencyMatrix()
        {
            if (_adjacencyMatrix != null)
            {
                return;
            }

            _adjacencyMatrix = new Dictionary<(int, int), CommunicationInfo>();

            foreach (var r1 in _robots)
            {
                foreach (var r2 in _robots)
                {
                    if (r1.id != r2.id)
                    {
                        var r1Position = r1.transform.position;
                        var r2Position = r2.transform.position;
                        var r1Vector2 = new Vector2(r1Position.x, r1Position.y);
                        var r2Vector2 = new Vector2(r2Position.x, r2Position.y);
                        // TODO: This fails 2 / 40.000.000.000 times. We need unit tests to eliminate the problems.
                        // TODO: Can't we improve performance by only going through half the matrix? - Philip
                        // They are caused by rays with angles of 45 or 90 degrees.
                        try
                        {
                            _adjacencyMatrix[(r1.id, r2.id)] = RayTraceCommunication(r1Vector2, r2Vector2);
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                            Debug.Log("Raytracing failed - Execution continued by providing a fake trace" +
                                      " with zero transmission probability");
                            _adjacencyMatrix[(r1.id, r2.id)] = new CommunicationInfo(float.MaxValue, 90, 1, 1, false, -int.MaxValue);
                        }

                    }
                }
            }
        }

        public List<HashSet<int>> GetCommunicationGroups()
        {
            PopulateAdjacencyMatrix();

            var groups = new List<HashSet<int>>();
            foreach (var r1 in _robots)
            {
                if (!groups.Exists(g => g.Contains(r1.id)))
                {
                    groups.Add(GetCommunicationGroup(r1.id));
                }
            }

            return groups;
        }

        private HashSet<int> GetCommunicationGroup(int robotId)
        {
            var keys = new Queue<int>();
            keys.Enqueue(robotId);
            var resultSet = new HashSet<int>() { robotId };

            while (keys.Count > 0)
            {
                var currentKey = keys.Dequeue();

                var inRange = _adjacencyMatrix!
                    .Where((kv) => kv.Key.Item1 == currentKey && kv.Value.TransmissionSuccessful)
                    .Select((e) => e.Key.Item2);

                foreach (var rInRange in inRange)
                {
                    if (!resultSet.Contains(rInRange))
                    {
                        keys.Enqueue(rInRange);
                        resultSet.Add(rInRange);
                    }
                }
            }

            return resultSet;
        }

        public void DepositTag(MonaRobot robot, string content)
        {
            var tag = _environmentTaggingMap.AddTag(robot.transform.position, new EnvironmentTag(robot.id, robot.ClaimTag(), content));
            _visualizer.AddEnvironmentTag(tag);
        }

        public List<EnvironmentTag> ReadNearbyTags(MonaRobot robot)
        {
            var tags = _environmentTaggingMap.GetTagsNear(robot.transform.position,
                _robotConstraints.EnvironmentTagReadRange);

            return tags;
        }

        public List<SensedObject<int>> SenseNearbyRobots(int id)
        {
            PopulateAdjacencyMatrix();
            var sensedObjects = new List<SensedObject<int>>();

            foreach (var robot in _robots)
            {
                if (robot.id == id)
                {
                    continue;
                }

                var comInfo = _adjacencyMatrix![(id, robot.id)];
                if ((comInfo.Distance > _robotConstraints.SenseNearbyAgentsRange && !_robotConstraints.MaterialCommunication) ||
                   (comInfo.WallsCellsPassedThrough > 0 && _robotConstraints.SenseNearbyAgentsBlockedByWalls) ||
                   (!comInfo.TransmissionSuccessful && _robotConstraints.MaterialCommunication))
                {
                    continue;
                }

                sensedObjects.Add(new SensedObject<int>(comInfo.Distance, comInfo.Angle, robot.id));
            }

            return sensedObjects;
        }

        public void SetRobotReferences(IReadOnlyList<MonaRobot> robots)
        {
            _robots = robots;
        }


        // Attempts to detect a wall in the given direction. If present, it will return the intersection point and the
        // global angle (relative to x-axis) in degrees of the intersecting line
        public (Vector2, float)? DetectWall(MonaRobot robot, float globalAngle)
        {
            var range = _robotConstraints.EnvironmentTagReadRange;
            // Perform 3 parallel traces from the robot to determine if
            // a wall will be encountered if the robot moves straight ahead

            var robotPosition = robot.transform.position;

            // Perform trace from the center of the robot
            var result1 = _rayTracingMap.FindIntersection(robotPosition, globalAngle, range, (_, tile) => !Tile.IsWall(tile.Type));
            var distance1 = result1 == null ? float.MaxValue : Vector2.Distance(robotPosition, result1.Value.Item1);
            var robotSize = _robotRelativeSize;

            // Perform trace from the left side perimeter of the robot
            var offsetLeft = Geometry.VectorFromDegreesAndMagnitude((globalAngle + 90) % 360, robotSize / 2f);
            var result2 = _rayTracingMap.FindIntersection((Vector2)robot.transform.position + offsetLeft, globalAngle, range, (_, tile) => !Tile.IsWall(tile.Type));
            var distance2 = result2 == null ? float.MaxValue : Vector2.Distance(robotPosition, result2.Value.Item1);

            // Finally perform trace from the right side perimeter of the robot
            var offsetRight = Geometry.VectorFromDegreesAndMagnitude((globalAngle + 270) % 360, robotSize / 2f);
            var result3 = _rayTracingMap.FindIntersection((Vector2)robot.transform.position + offsetRight, globalAngle, range, (_, tile) => !Tile.IsWall(tile.Type));
            var distance3 = result3 == null ? float.MaxValue : Vector2.Distance(robotPosition, result3.Value.Item1);

            // Return the detected wall that is closest to the robot
            var closestWall = result1;
            var closestWallDistance = distance1;

            if (distance2 < closestWallDistance)
            {
                closestWall = result2;
                closestWallDistance = distance2;
            }

            if (distance3 < closestWallDistance)
            {
                closestWall = result3;
            }

            return closestWall;
        }

    }
}