namespace GolfGame.Golf
{
    /// <summary>
    /// Data class holding shot input parameters.
    /// Created by ShotInput, consumed by BallController.
    /// </summary>
    public class ShotParameters
    {
        /// <summary>
        /// Power level normalized 0-1 from power meter.
        /// </summary>
        public float PowerNormalized { get; set; }

        /// <summary>
        /// Horizontal aim offset in degrees. Negative = left, positive = right.
        /// Clamped to +-45.
        /// </summary>
        public float AimAngleDegrees { get; set; }

        /// <summary>
        /// Backspin in RPM. Higher values create more lift and reduce roll.
        /// </summary>
        public float BackspinRpm { get; set; }

        /// <summary>
        /// Sidespin in RPM. Negative = draw (curves left), positive = fade (curves right).
        /// </summary>
        public float SidespinRpm { get; set; }

        /// <summary>
        /// Calculate power in mph given the maximum power setting.
        /// </summary>
        public float PowerMph(float maxMph)
        {
            return PowerNormalized * maxMph;
        }
    }
}
