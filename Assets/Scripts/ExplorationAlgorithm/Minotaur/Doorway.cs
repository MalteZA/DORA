using Maes.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Maes.ExplorationAlgorithm.Minotaur
{
    public class Doorway
    {
        public (Vector2, Vector2) Position;
        public bool Explored;
        public CardinalDirection.RelativeDirection ApproachedDirection;

        public Doorway Doorway((Vector2, Vector2) Position, CardinalDirection.RelativeDirection ApproachedDirection)
        {
            this.Position = Position;
            Explored = false;
            this.ApproachedDirection = ApproachedDirection;
        }
        
        public float Distance(MinotaurAlgorithm minotaur)
        {
            throw new NotImplementedException();
        }
    }
}
