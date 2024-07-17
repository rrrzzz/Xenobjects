using System;
using JetBrains.Annotations;
using UnityEngine;

namespace SplineMesh {
    /// <summary>
    /// Example of component to bend a mesh along a spline with some interpolation of scales and rolls. This component can be used as-is but will most likely be a base for your own component.
    /// 
    /// For explanations of the base component, <see cref="ExamplePipe"/>
    /// 
    /// In this component, we have added properties to make scale and roll vary between spline start and end.
    /// Intermediate scale and roll values are calculated at each spline node accordingly to the distance, then given to the MeshBenders component.
    /// MeshBender applies scales and rolls values by interpollation if they differ from strat to end of the curve.
    /// 
    /// You can easily imagine a list of scales to apply to each node independantly to create your own variation.
    /// </summary>
    ///
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ExampleTentacle : MonoBehaviour
    {
        public float startScale = 1, endScale = 1;
        public float startRoll = 0, endRoll = 0;
        public float time = .5f;
        public bool isSplitting;
        public bool isSplittingAllAtIntervals;

        private Spline _spline;

        private void OnEnable()
        {
            ReapplyScaleAndRoll();
            _spline = GetComponent<Spline>();
            _spline.NodeListChanged += ReapplyScaleAndRoll;
        }

        private void OnValidate()
        {
            ReapplyScaleAndRoll();
        }

        private void Update()
        {
            if (isSplitting)
            {
                _spline.SplitAtTime(time);
                ReapplyScaleAndRoll();
                isSplitting = false;
            }
            
            // Debug.LogError($"splitting is {isSplittingAllAtIntervals} at time: {DateTime.Now.Minute}:{DateTime.Now.Second}");
            if (isSplittingAllAtIntervals)
            {
                isSplittingAllAtIntervals = false;
                var nodeCount = _spline.nodes.Count;
                for (int i = 0; i < nodeCount - 1; i++)
                {
                    var t = i * 2 + .5f;
                    _spline.SplitAtTime(t);
                    ReapplyScaleAndRoll();
                }
            }
        }
        
        public void ReapplyScaleAndRoll(object sender = null, ListChangedEventArgs<SplineNode> args= null) 
        {
            if (_spline == null)
            {
                return;
            }
            // apply scale and roll at each node
            float currentLength = 0;
            foreach (CubicBezierCurve curve in _spline.GetCurves()) 
            {
                float startRate = currentLength / _spline.Length;
                currentLength += curve.Length;
                float endRate = currentLength / _spline.Length;

                curve.n1.Scale = Vector2.one * (startScale + (endScale - startScale) * startRate);
                curve.n2.Scale = Vector2.one * (startScale + (endScale - startScale) * endRate);

                curve.n1.Roll = startRoll + (endRoll - startRoll) * startRate;
                curve.n2.Roll = startRoll + (endRoll - startRoll) * endRate;
            }
        }

        private void OnDisable()
        {
            _spline.NodeListChanged -= ReapplyScaleAndRoll;
        }
    }
}
