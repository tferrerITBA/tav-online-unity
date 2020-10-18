namespace Tests
{
    public class Commands
    {
        private int userID;
        private int seq;
        private bool up;
        private bool down;
        private bool left;
        private bool right;
        private bool space;

        public Commands(int userID)
        {
            this.userID = userID;
            seq = 1;
        }

        public Commands(int seq, int userID, bool up, bool down, bool left, bool right, bool space)
        {
            this.seq = seq;
            this.userID = userID;
            this.up = up;
            this.down = down;
            this.left = left;
            this.right = right;
            this.space = space;
        }

        public Commands(Commands o)
        {
            seq = o.seq;
            userID = o.userID;
            up = o.up;
            down = o.down;
            left = o.left;
            right = o.right;
            space = o.space;
        }
        
        public bool HasCommand()
        {
            return up || down || left || right || space;
        }

        public int GetXDirection()
        {
            int leftDir = left ? -1 : 0;
            int rightDir = right ? 1 : 0;
            return leftDir + rightDir;
        }
        
        public int GetZDirection()
        {
            int downDir = down ? -1 : 0;
            int upDir = up ? 1 : 0;
            return downDir + upDir;
        }

        public override string ToString()
        {
            return $"{nameof(userID)}: {userID}, {nameof(seq)}: {seq}, " +
                   $"{nameof(up)}: {up}, {nameof(down)}: {down}, " +
                   $"{nameof(left)}: {left}, {nameof(right)}: {right}, {nameof(space)}: {space}";
        }

        public int UserID
        {
            get => userID;
            set => userID = value;
        }

        public int Seq
        {
            get => seq;
            set => seq = value;
        }

        public bool Up
        {
            get => up;
            set => up = value;
        }

        public bool Down
        {
            get => down;
            set => down = value;
        }

        public bool Left
        {
            get => left;
            set => left = value;
        }

        public bool Right
        {
            get => right;
            set => right = value;
        }

        public bool Space
        {
            get => space;
            set => space = value;
        }
    }
}