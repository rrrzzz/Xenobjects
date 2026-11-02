namespace Code.VR
{
    /// <summary>
    /// Decides which exhibit the visitor is standing at from their distance to each one, with
    /// hysteresis so standing between two podiums doesn't flicker. -1 means none.
    /// </summary>
    public sealed class ExhibitFocusSelector
    {
        public ExhibitFocusSelector(float focusRadius, float releaseRadius, float switchMargin)
        {
            FocusRadius = focusRadius;
            ReleaseRadius = releaseRadius;
            SwitchMargin = switchMargin;
        }

        /// <summary>Distance at which an exhibit gains focus.</summary>
        public float FocusRadius { get; set; }

        /// <summary>Distance at which the focused exhibit is released (keep it above FocusRadius).</summary>
        public float ReleaseRadius { get; set; }

        /// <summary>How much closer another exhibit must be to take focus over the current one.</summary>
        public float SwitchMargin { get; set; }

        public int Current { get; private set; } = -1;

        /// <summary>
        /// Updates the focus from this frame's distances. Returns true when it changed.
        /// </summary>
        public bool Evaluate(float[] distances)
        {
            var nearest = -1;
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < distances.Length; i++)
            {
                if (distances[i] < nearestDistance)
                {
                    nearestDistance = distances[i];
                    nearest = i;
                }
            }

            var isNearestInRange = nearest >= 0 && nearestDistance <= FocusRadius;
            var next = Current;

            if (Current < 0 || Current >= distances.Length)
            {
                next = isNearestInRange ? nearest : -1;
            }
            else if (distances[Current] > ReleaseRadius)
            {
                next = isNearestInRange ? nearest : -1;
            }
            else if (isNearestInRange && nearest != Current && nearestDistance + SwitchMargin < distances[Current])
            {
                next = nearest;
            }

            if (next == Current)
                return false;

            Current = next;
            return true;
        }

        public void Reset()
        {
            Current = -1;
        }
    }
}
