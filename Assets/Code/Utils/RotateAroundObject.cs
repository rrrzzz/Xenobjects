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
        private float _targetAngle;
        private bool _hasDetached;
        private bool _isDetaching;

        public void Init(Transform parentTr)
        {
            _parentPos = parentTr.position;
            _parentPos2D = new Vector2(_parentPos.x, _parentPos.z);
            _hasDetached = false;
            _isDetaching = false;
            isRotating = false;
            _currentAngle = Mathf.Deg2Rad * startAngle;
        }

        public void RotateToTargetAngle(Vector3 playerPos)
        {
            var newPlayerPos2D = new Vector2(playerPos.x, playerPos.z);
            if (Vector2.Distance(_playerPos2D, newPlayerPos2D) < 0.01f)
                return;

            _playerPos2D = newPlayerPos2D;

            var intersection = FindFurthestCircleRayIntersection(_playerPos2D);
            intersection -= _parentPos2D;
            _targetAngle = Mathf.Atan2(intersection.y, intersection.x);

            StartRotation();
        }

        [Button] 
        private void StartRotation()
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

            float difference = Mathf.DeltaAngle(
                _currentAngle * Mathf.Rad2Deg,
                _targetAngle * Mathf.Rad2Deg
            ) * Mathf.Deg2Rad;

            if (Mathf.Abs(difference) <= targetAngleThreshold)
            {
                isRotating = false;
                return;
            }

            float rotationSign = Mathf.Sign(difference);
            _currentAngle += angularSpeed * Time.deltaTime * rotationSign;
            _currentAngle %= Mathf.PI * 2;

            transform.position = new Vector3(
                _parentPos.x + Mathf.Cos(_currentAngle) * radius,
                transform.position.y,
                _parentPos.z + Mathf.Sin(_currentAngle) * radius
            );
        }

        private Vector2 FindFurthestCircleRayIntersection(Vector2 playerPos2D)
        {
            Vector2 dir = (_parentPos2D - playerPos2D).normalized;
            Vector2 f = playerPos2D - _parentPos2D;

            float a = Vector2.Dot(dir, dir);
            float b = 2f * Vector2.Dot(f, dir);
            float c = Vector2.Dot(f, f) - radius * radius;

            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0)
                return _parentPos2D + dir * radius;

            discriminant = Mathf.Sqrt(discriminant);

            float t1 = (-b - discriminant) / (2f * a);
            float t2 = (-b + discriminant) / (2f * a);

            float furthestT = Mathf.Max(t1, t2);

            return playerPos2D + dir * furthestT;
        }
    }
}