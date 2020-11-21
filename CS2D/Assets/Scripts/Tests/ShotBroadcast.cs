
public class ShotBroadcast
{
    private int shotId;
    private int userID;
    private int playerShotID;
    private bool playerDied;

    public ShotBroadcast()
    {
    }
    
    public int ShotId
    {
        get => shotId;
        set => shotId = value;
    }

    public int UserID
    {
        get => userID;
        set => userID = value;
    }

    public int PlayerShotID
    {
        get => playerShotID;
        set => playerShotID = value;
    }

    public bool PlayerDied
    {
        get => playerDied;
        set => playerDied = value;
    }
    
    public override bool Equals(object obj)
    {
        ShotBroadcast other = obj as ShotBroadcast;

        if (other == null)
        {
            return false;
        }

        return shotId == other.ShotId;
    }

    public override int GetHashCode()
    {
        int hash = 13;
        hash = (hash * 7) + shotId.GetHashCode();
        return hash;
    }
}

