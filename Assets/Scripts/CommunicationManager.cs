using System.Collections.Generic;
using System.Linq;
using Dora.MapGeneration;
using Dora.Robot;
using UnityEngine;

namespace Dora {
    // Messages sent through this class will be subject to communication range and line of sight.
    // Communication is non-instantaneous. Messages will be received by other robots after one logic tick. 
    public class CommunicationManager : ISimulationUnit {
        private RobotConstraints _robotConstraints;
        private DebuggingVisualizer _visualizer;

        // Messages that will sent during the next logic update
        private List<Message> _queuedMessages = new List<Message>();

        // Messages that were sent last tick and can now be read 
        private List<Message> _readableMessages = new List<Message>();

        private RayTracingMap<bool> _rayTracingMap;

        private List<MonaRobot> _robots;
        
        private int _localTickCounter = 0;

        private readonly struct Message {
            public readonly object Contents;
            public readonly MonaRobot Sender;
            public readonly Vector2 broadcastCenter;

            public Message(object contents, MonaRobot sender, Vector2 broadcastCenter) {
                Contents = contents;
                Sender = sender;
                this.broadcastCenter = broadcastCenter;
            }
        }

        public CommunicationManager(SimulationMap<bool> collisionMap, RobotConstraints robotConstraints,
            DebuggingVisualizer visualizer) {
            _robotConstraints = robotConstraints;
            _visualizer = visualizer;
            _rayTracingMap = new RayTracingMap<bool>(collisionMap);
        }

        // Adds a message to the broadcast queue
        public void BroadcastMessage(MonaRobot sender, in object messageContents) {
            _queuedMessages.Add(new Message(messageContents, sender, sender.transform.position));
        }

        // Returns a list of messages sent by other robots
        public List<object> ReadMessages(MonaRobot receiver) {
            List<object> messages = new List<object>();
            Vector2 receiverPosition = receiver.transform.position;
            foreach (var message in _readableMessages) {
                // The robot will not receive its own messages
                if (message.Sender.id == receiver.id) continue;

                if (CanSignalTravelBetween(message.broadcastCenter, receiverPosition)) {
                    messages.Add(message.Contents);
                    if (GlobalSettings.DrawCommunication)
                        _visualizer.AddCommunicationTrail(message.Sender, receiver);
                }
            }

            return messages;
        }

        private bool CanSignalTravelBetween(Vector2 pos1, Vector2 pos2) {
            if (Vector2.Distance(pos1, pos2) > _robotConstraints.BroadcastRange)
                return false;

            // If walls can be ignored, then simply continue
            if (!_robotConstraints.BroadcastBlockedByWalls)
                return true;

            // If walls cannot be ignored, perform a raycast to check line of sight between the given points
            var angle = Vector2.Angle(Vector2.right, pos2 - pos1);
            if (pos1.y > pos2.y) angle = 180 + (180 - angle);

            bool canTravel = true;
            _rayTracingMap.Raytrace(pos1, angle, _robotConstraints.BroadcastRange,
                (_, cellIsSolid) => {
                    if (cellIsSolid)
                        canTravel = false;
                    return canTravel;
                });
            return canTravel;
        }

        public void LogicUpdate() {
            // Move messages sent last tick into readable messages
            _readableMessages.Clear();
            _readableMessages.AddRange(_queuedMessages);
            _queuedMessages.Clear();
            _localTickCounter++;
            if (_robotConstraints.ShouldAutomaticallyUpdateSlam 
                && _localTickCounter % _robotConstraints.SlamUpdateIntervalInTicks == 0) {
                SynchronizeSlamMaps();
            }
                
        }

        private void SynchronizeSlamMaps() {
            var slamGroups = GetCommunicationGroups();

            foreach (var group in slamGroups) {
                var slamMaps = group
                    .Select(id => _robots.Find(r => r.id == id))
                    .Select(r => r.Controller.SlamMap)
                    .ToList();
                
                SlamMap.Combine(slamMaps);
            }
        }

        public void PhysicsUpdate() {
            // No physics update needed
        }

        public object SaveState() {
            throw new System.NotImplementedException();
        }

        public void RestoreState(object stateInfo) {
            throw new System.NotImplementedException();
        }

        public List<HashSet<int>> GetCommunicationGroups() {
            var canCommunicateMatrix = new Dictionary<(int, int), bool>();

            foreach (var r1 in _robots) {
                foreach (var r2 in _robots) {
                    if (r1.id != r2.id) {
                        var r1Position = r1.transform.position;
                        var r2Position = r2.transform.position;
                        var r1Vector2 = new Vector2(r1Position.x, r1Position.y);
                        var r2Vector2 = new Vector2(r2Position.x, r2Position.y);
                        canCommunicateMatrix[(r1.id, r2.id)] = CanSignalTravelBetween(r1Vector2, r2Vector2);
                    }
                        
                }
            }

            List<HashSet<int>> groups = new List<HashSet<int>>();
            foreach (var r1 in _robots) {
                if(!groups.Exists(g => g.Contains(r1.id))); {
                    groups.Add(GetCommunicationGroup(r1.id, canCommunicateMatrix));
                }
            }

            return groups;
        }

        private HashSet<int> GetCommunicationGroup(int robotId, Dictionary<(int, int), bool> adjacencyMatrix) {
            var keys = new Queue<int>();
            keys.Enqueue(robotId);
            var resultSet = new HashSet<int>(){robotId};

            while (keys.Count > 0) {
                var currentKey = keys.Dequeue();

                var inRange = adjacencyMatrix
                    .Where((kv) => kv.Key.Item1 == currentKey && kv.Value)
                    .Select((e) => e.Key.Item2);

                foreach (var rInRange in inRange) {
                    if (!resultSet.Contains(rInRange)) {
                        keys.Enqueue(rInRange);
                        resultSet.Add(rInRange);
                    }
                }
            }

            return resultSet;
        }

        public void SetRobotReferences(List<MonaRobot> robots) {
            this._robots = robots;
        }
    }
}