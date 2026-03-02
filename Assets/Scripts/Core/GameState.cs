namespace GolfGame.Core
{
    /// <summary>
    /// Application-level states controlling screen flow.
    /// Managed by AppManager.
    /// </summary>
    public enum AppState
    {
        Title,
        Instructions,
        TransitionToGame,
        Playing,
        GameOver,
        Leaderboard
    }

    /// <summary>
    /// Gameplay-level states controlling the shot loop.
    /// Managed by GameManager.
    /// </summary>
    public enum ShotState
    {
        Ready,
        Flying,
        Landed
    }
}
