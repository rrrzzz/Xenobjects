using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Code
{
    public class MovementPathVisualizer : MonoBehaviour
    {
        public float recordInterval = 0.2f;
        public float positionDifThreshold = 0.1f;
        public float visualizationDelay = 7;
        
        [HideInInspector] public bool isManualVizInvoke;

        private LineRenderer _lineRenderer;
        private float _intervalTimer;
        private float _globalTimer;
        private Transform _camTransform;
        private readonly List<Vector3> _recordedPositions = new List<Vector3>();
        private Vector3 _lastPosition;
        private bool _isRecordingPath;
    
        public void Initialize()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _camTransform = Camera.main.transform;
        }
    
        private void Update()
        {
            if (!_isRecordingPath)
                return;
            
            // if (!isManualVizInvoke)
            // {
            //     if (_globalTimer >= visualizationDelay)
            //     {
            //         return;
            //     }
            // }
            //
            // _globalTimer += Time.deltaTime;
            //
            // if (!isManualVizInvoke)
            // {
            //     if (_globalTimer >= visualizationDelay)
            //     {
            //         AddRecordedPointsToLine();
            //         return;
            //     }
            // }

            _intervalTimer += Time.deltaTime;
            
            if (_intervalTimer < recordInterval) 
                return;
            
            Vector3 currentPos = _camTransform.position;
            if (_recordedPositions.Count == 0)
            {
                _lastPosition = currentPos;
                _recordedPositions.Add(currentPos);
            }
            else
            {
                var difVector = _lastPosition - _recordedPositions[^1];
                var maxDif = Mathf.Max(Mathf.Abs(difVector.x), Mathf.Abs(difVector.y), Mathf.Abs(difVector.z));
                if (maxDif > positionDifThreshold)
                    _recordedPositions.Add(currentPos);
            }
            
            _intervalTimer = 0f;
        }

        public IEnumerator ShowPathAfterDelay()
        {
            yield return new WaitForSeconds(visualizationDelay);
            
            _lineRenderer.positionCount = _recordedPositions.Count;
            for (int i = 0; i < _recordedPositions.Count; i++)
            {
                _lineRenderer.SetPosition(i, _recordedPositions[i]);
            }

            _isRecordingPath = false;
        }
    
        public void RestartTracking()
        {
            _recordedPositions.Clear();
            _lineRenderer.positionCount = 0;
            _intervalTimer = 0;
            _globalTimer = 0;
            _isRecordingPath = true;
        }
    }
}
