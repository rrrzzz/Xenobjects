using System;
using DG.Tweening;
using EasyButtons;
using UnityEngine;

namespace Code.Utils
{
    [ExecuteAlways]
    public class RotateAroundObject : MonoBehaviour
    {
        public float startAngle = 270;
        public float heightOffset = .14f;
        public float radius;
        public bool isRotating;
        public float angularSpeed = 1.0f;
        public float targetAngleThreshold = 0.01f;
        
        private float _currentAngle;
        private Vector3 _parentPos;
        private Vector2 _parentPos2D;
        private Vector2 _playerPos2D;
        private Vector2 _prevPlayerPos2D;
        private float _targetAngle;
        private bool _hasDetached;
        private bool _isDetaching;
        
        public void Init(Transform parentTr)
        {
            _parentPos = parentTr.position;
            _parentPos2D = new Vector2(_parentPos.x, _parentPos.z);
            _hasDetached = false;
            _isDetaching = false;
            _currentAngle = Mathf.Deg2Rad * startAngle;
        }

        public void RotateToTargetAngle(Vector3 playerPos)
        {
            var newPlayerPos2D = new Vector2(playerPos.x, playerPos.z);
            if (Vector2.Distance(_playerPos2D, newPlayerPos2D) < 0.01f)
            {
                return;
            }

            _playerPos2D = newPlayerPos2D;
            var intersection = FindFurthestCircleRayIntersection(_playerPos2D); 
            intersection -= _parentPos2D;
            _targetAngle = Mathf.Atan2(intersection.y, intersection.x);
            StartRotation();
        }
        
        [Button] private void StartRotation()
        {
            if (!isRotating && !_hasDetached)
            {
                if (_isDetaching)
                {
                    return;
                }

                _isDetaching = true;
                float x = _parentPos.x + Mathf.Cos(_currentAngle) * radius;
                float z = _parentPos.z + Mathf.Sin(_currentAngle) * radius;
                var target = new Vector3(x, transform.position.y - heightOffset, z);
                
                transform.DOMove(target, 1).OnComplete(() =>
                {
                    isRotating = true;
                    _isDetaching = false;
                    _hasDetached = true;
                });
            }
            else
            {
                isRotating = true;
            }
        }
        
        void Update()
        {
            if (!isRotating) return;
            
            float difference = _targetAngle - _currentAngle;
            if (Mathf.Abs(difference) <= targetAngleThreshold)
            {
                isRotating = false;
                return;
            }
                
            if (difference < 0)
            {
                difference += 2 * Mathf.PI;
            }
                
            var rotationSign = difference > Mathf.PI ? -1 : 1;
            float x = _parentPos.x + Mathf.Cos(_currentAngle) * radius;
            float z = _parentPos.z + Mathf.Sin(_currentAngle) * radius;
      
            transform.position = new Vector3(x, transform.position.y, z);

            _currentAngle += angularSpeed * Time.deltaTime * rotationSign;
            _currentAngle %= Mathf.PI * 2;
                
            if (MathF.Abs(_currentAngle - _targetAngle) <= targetAngleThreshold)
            {
                isRotating = false;
            }
        }
        
        private Vector2 FindFurthestCircleRayIntersection(Vector2 playerPos2D)
        {
            var direction = (_parentPos2D - playerPos2D).normalized;
            playerPos2D += -direction * 10;

            // Quadratic coefficients
            Vector2 oc = playerPos2D - _parentPos2D;
            float a = Vector2.Dot(direction, direction);
            float b = 2f * Vector2.Dot(oc, direction);
            float c = Vector2.Dot(oc, oc) - 1;

            // Discriminant
            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0)
            {
                return Vector2.zero;
            }
            
            // Calculate the two points of intersection
            float sqrtDiscriminant = Mathf.Sqrt(discriminant);

            float t1 = (-b + sqrtDiscriminant) / (2f * a);
            float t2 = (-b - sqrtDiscriminant) / (2f * a);

            var intersection1 = playerPos2D + t1 * direction;
            var intersection2 = playerPos2D + t2 * direction;
            var distToIntersection1 = Vector2.Distance(playerPos2D, intersection1);
            var distToIntersection2 = Vector2.Distance(playerPos2D, intersection2);
            return distToIntersection1 < distToIntersection2 ? intersection2 : intersection1;
        }
    }
}