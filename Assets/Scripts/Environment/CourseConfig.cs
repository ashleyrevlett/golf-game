using UnityEngine;

namespace GolfGame.Environment
{
    /// <summary>
    /// ScriptableObject holding course layout dimensions, pin settings,
    /// and physics material references.
    /// </summary>
    [CreateAssetMenu(fileName = "CourseConfig", menuName = "Golf/Course Config")]
    public class CourseConfig : ScriptableObject
    {
        [Header("Course Dimensions")]
        [SerializeField] private float courseLength = 114f;
        [SerializeField] private float fairwayWidth = 18f;
        [SerializeField] private float roughWidth = 9f;
        [SerializeField] private float greenRadius = 14f;
        [SerializeField] private float teeBoxSize = 4f;

        [Header("Pin")]
        [SerializeField] private float pinHeight = 2.5f;
        [SerializeField] private float pinDiameter = 0.05f;
        [SerializeField] private float flagWidth = 0.3f;
        [SerializeField] private float flagHeight = 0.2f;

        [Header("OB Markers")]
        [SerializeField] private float obDistanceBeyondRough = 5f;
        [SerializeField] private float obMarkerHeight = 1.5f;
        [SerializeField] private float obMarkerSpacing = 15f;

        public float CourseLength => courseLength;
        public float FairwayWidth => fairwayWidth;
        public float RoughWidth => roughWidth;
        public float GreenRadius => greenRadius;
        public float TeeBoxSize => teeBoxSize;
        public float PinHeight => pinHeight;
        public float PinDiameter => pinDiameter;
        public float FlagWidth => flagWidth;
        public float FlagHeight => flagHeight;
        public float ObDistanceBeyondRough => obDistanceBeyondRough;
        public float ObMarkerHeight => obMarkerHeight;
        public float ObMarkerSpacing => obMarkerSpacing;
    }
}
