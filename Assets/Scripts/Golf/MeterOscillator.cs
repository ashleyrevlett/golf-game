namespace GolfGame.Golf
{
    /// <summary>
    /// Reusable oscillation logic for meter values.
    /// Bounces between min and max at a given speed.
    /// </summary>
    public struct MeterOscillator
    {
        public float Value;
        public bool Rising;
        public float Min;
        public float Max;

        public MeterOscillator(float min, float max)
        {
            Min = min;
            Max = max;
            Value = min;
            Rising = true;
        }

        public void Reset()
        {
            Value = Min;
            Rising = true;
        }

        public float Tick(float speed, float deltaTime)
        {
            float delta = speed * deltaTime;
            if (Rising)
            {
                Value += delta;
                if (Value >= Max)
                {
                    Value = Max;
                    Rising = false;
                }
            }
            else
            {
                Value -= delta;
                if (Value <= Min)
                {
                    Value = Min;
                    Rising = true;
                }
            }
            return Value;
        }
    }
}
