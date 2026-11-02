using System.Globalization;
using UnityEngine;

namespace Code
{
    /// <summary>
    /// Shared helpers used by the interaction providers and the spawners.
    /// </summary>
    public static class InteractionUtils
    {
        /// <summary>
        /// Maps euler angles from [0, 360) to [-180, 180) and flips the sign,
        /// so a device tilted right/down produces negative values.
        /// </summary>
        public static Vector3 NormalizeRotationAngles(Vector3 rotation)
        {
            if (rotation.x > 180)
            {
                rotation.x -= 360;
            }

            if (rotation.y > 180)
            {
                rotation.y -= 360;
            }

            if (rotation.z > 180)
            {
                rotation.z -= 360;
            }

            return rotation * -1;
        }

        /// <summary>
        /// Parses a comma-separated float list (invariant culture) into <paramref name="target"/>.
        /// Unparsable or missing entries keep their existing value. Extra entries beyond the
        /// target length are ignored. Returns true if at least one value was parsed.
        /// </summary>
        public static bool TryParseFloats(string text, float[] target)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var parts = text.Split(',');
            var parsedAny = false;

            for (int i = 0; i < parts.Length && i < target.Length; i++)
            {
                if (float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    target[i] = value;
                    parsedAny = true;
                }
            }

            return parsedAny;
        }
    }
}
