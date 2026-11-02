using System;
using UnityEngine;

namespace Code.VR
{
    /// <summary>
    /// VR museum with several exhibits, one interaction provider each. Picks the exhibit the
    /// visitor is standing at (horizontal head distance, with hysteresis) and focuses only its
    /// provider, so head and controller input reach one object at a time — as on the phone,
    /// where only the scanned object exists. An object is initialized the first time its exhibit
    /// gains focus, the VR stand-in for scanning its marker.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class VrExhibitFocus : MonoBehaviour
    {
        [Serializable]
        public class Exhibit
        {
            public string label;
            public Transform objectRoot;
            public MovementInteractionProviderBase provider;
        }

        [SerializeField] private Exhibit[] exhibits = Array.Empty<Exhibit>();
        [SerializeField] private MovementPathVisualizer pathVisualizer;
        [Tooltip("Horizontal head distance (m) at which an exhibit gains focus.")]
        [SerializeField] private float focusRadius = 2.5f;
        [Tooltip("Horizontal head distance (m) at which the focused exhibit is released.")]
        [SerializeField] private float releaseRadius = 3.5f;
        [Tooltip("Another exhibit must be this much closer (m) to take focus.")]
        [SerializeField] private float switchMargin = 0.5f;

        private ExhibitFocusSelector _selector;
        private float[] _distances;
        private bool[] _isInitialized;
        private Transform _head;

        public event Action<int> FocusChanged;

        public int ExhibitCount => exhibits.Length;
        public int FocusedIndex => _selector != null ? _selector.Current : -1;
        public Exhibit FocusedExhibit => FocusedIndex >= 0 ? exhibits[FocusedIndex] : null;

        /// <summary>Horizontal head distance to the focused exhibit, or infinity when none.</summary>
        public float FocusedDistance => FocusedIndex >= 0 ? _distances[FocusedIndex] : float.PositiveInfinity;

        public Exhibit GetExhibit(int index)
        {
            return exhibits[index];
        }

        public bool IsInitialized(int index)
        {
            return _isInitialized != null && _isInitialized[index];
        }

        private void Start()
        {
            _head = Camera.main.transform;
            _selector = new ExhibitFocusSelector(focusRadius, releaseRadius, switchMargin);
            _distances = new float[exhibits.Length];
            _isInitialized = new bool[exhibits.Length];

            if (pathVisualizer)
            {
                pathVisualizer.Initialize();
                pathVisualizer.RestartTracking();
            }

            foreach (var exhibit in exhibits)
            {
                exhibit.provider.SetFocused(false);
            }

            UpdateFocus();
        }

        private void Update()
        {
            UpdateFocus();
        }

        private void UpdateFocus()
        {
            _selector.FocusRadius = focusRadius;
            _selector.ReleaseRadius = releaseRadius;
            _selector.SwitchMargin = switchMargin;

            var headPosition = _head.position;
            for (var i = 0; i < exhibits.Length; i++)
            {
                var offset = exhibits[i].objectRoot.position - headPosition;
                offset.y = 0;
                _distances[i] = offset.magnitude;
            }

            var previous = _selector.Current;
            if (!_selector.Evaluate(_distances))
                return;

            if (previous >= 0)
                exhibits[previous].provider.SetFocused(false);

            var current = _selector.Current;
            if (current >= 0)
            {
                var exhibit = exhibits[current];
                if (!_isInitialized[current])
                {
                    _isInitialized[current] = true;
                    exhibit.provider.SetArObjectTransform(exhibit.objectRoot, pathVisualizer);
                }

                exhibit.provider.SetFocused(true);
            }

            FocusChanged?.Invoke(current);
        }
    }
}
