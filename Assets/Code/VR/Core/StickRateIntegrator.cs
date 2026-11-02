using UnityEngine;

namespace Code.VR
{
    /// <summary>
    /// Turns a spring-loaded stick axis into a value that holds its position: deflection moves
    /// the value at a fixed rate and releasing the stick leaves it where it is. The Object 1
    /// puzzle uses it so the stick behaves like keeping a rotated phone in place.
    /// </summary>
    public sealed class StickRateIntegrator
    {
        public float Value { get; private set; }

        /// <param name="input">Stick axis in [-1, 1], deadzone already applied.</param>
        /// <param name="ratePerSecond">Change of the value per second at full deflection.</param>
        /// <param name="deltaTime">Frame time in seconds.</param>
        public float Step(float input, float ratePerSecond, float deltaTime)
        {
            Value = Mathf.Clamp(Value + input * ratePerSecond * deltaTime, -1f, 1f);
            return Value;
        }

        public void Reset()
        {
            Value = 0f;
        }
    }
}
