namespace GolfGame.Environment
{
    /// <summary>
    /// Statistics for a single completed shot.
    /// </summary>
    public struct ShotResult
    {
        /// <summary>Shot number (1-based).</summary>
        public int ShotNumber;

        /// <summary>Distance from ball landing position to pin (meters).</summary>
        public float DistanceToPin;

        /// <summary>Distance from tee to first bounce point (meters).</summary>
        public float CarryDistance;

        /// <summary>Total distance from tee to final resting position (meters).</summary>
        public float TotalDistance;

        /// <summary>Lateral deviation from center line (positive = right, meters).</summary>
        public float LateralDeviation;

        /// <summary>Ball speed at launch (m/s).</summary>
        public float BallSpeed;
    }
}
