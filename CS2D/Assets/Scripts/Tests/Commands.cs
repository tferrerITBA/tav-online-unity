using System;
using UnityEngine;

namespace Tests
{
    public class Commands
    {
        private int userID;
        private int seq;
        private float vertical;
        private float horizontal;

        public override string ToString()
        {
            return $"UserID {userID} {nameof(Seq)}: {Seq}, {nameof(Vertical)}: {Vertical}, {nameof(Horizontal)}: {Horizontal}";
        }

        public Commands(int seq, int userID, float vertical, float horizontal)
        {
            this.seq = seq;
            this.userID = userID;
            this.vertical = vertical;
            this.horizontal = horizontal;
        }

        public bool hasCommand()
        {
            return Math.Abs(vertical) > 0 || Math.Abs(horizontal) > 0;
        }

        public int Seq => seq;

        public int UserID => userID;
        
        public float Vertical => vertical;

        public float Horizontal => horizontal;
    }
}