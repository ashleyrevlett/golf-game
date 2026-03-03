namespace GolfGame.Multiplayer
{
    /// <summary>
    /// A single entry on the leaderboard.
    /// </summary>
    public struct LeaderboardEntry
    {
        public int Rank;
        public string PlayerId;
        public string DisplayName;
        public float Distance;
    }
}
