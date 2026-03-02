using UnityEngine;

namespace GolfGame.Environment
{
    /// <summary>
    /// Provides pin position and distance calculation from any point to the pin.
    /// Attach to the pin root GameObject.
    /// </summary>
    public class PinController : MonoBehaviour
    {
        /// <summary>
        /// Pin position at ground level (Y = 0).
        /// </summary>
        public Vector3 PinBasePosition => new Vector3(
            transform.position.x, 0f, transform.position.z);

        /// <summary>
        /// Calculate flat distance (ignoring Y) from a position to the pin base.
        /// </summary>
        public float CalculateDistance(Vector3 fromPosition)
        {
            var flatFrom = new Vector3(fromPosition.x, 0f, fromPosition.z);
            return Vector3.Distance(flatFrom, PinBasePosition);
        }
    }
}
