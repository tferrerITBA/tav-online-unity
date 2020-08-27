using UnityEngine;

namespace Tests
{
    public class Commands
    {
        private static int _seq = 0;
        private int seq;
        private bool up;
        private bool down;
        private bool right;
        private bool left;
        private bool space;

        public Commands(bool up, bool down, bool right, bool left, bool space)
        {
            this.seq = _seq;
            this.up = up;
            this.down = down;
            this.right = right;
            this.left = left;
            this.space = space;
            if (hasCommand())
                _seq++;
        }

        public override string ToString()
        {
            return $"{nameof(Seq)}: {Seq}, {nameof(Up)}: {Up}, {nameof(Down)}: {Down}, {nameof(Right)}: {Right}, {nameof(Left)}: {Left}, {nameof(Space)}: {Space}";
        }

        public Commands(int seq, bool up, bool down, bool right, bool left, bool space)
        {
            this.seq = seq;
            this.up = up;
            this.down = down;
            this.right = right;
            this.left = left;
            this.space = space;
        }

        public bool hasCommand()
        {
            return up || down || right || left || space;
        }

        public int Seq => seq;
        
        public bool Up => up;

        public bool Down => down;

        public bool Right => right;

        public bool Left => left;

        public bool Space => space;
    }
}