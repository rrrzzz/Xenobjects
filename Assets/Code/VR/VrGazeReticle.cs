using UnityEngine;

namespace Code.VR
{
    /// <summary>
    /// Shows a small dot where the head ray hits one of the target colliders, so the visitor can
    /// tell they're looking at the Object 1 puzzle. On the phone the screen centre did this job;
    /// the lock-on itself is MovementInteractionProviderBase.GetForwardRayHit, the same ray.
    /// </summary>
    public class VrGazeReticle : MonoBehaviour
    {
        [SerializeField] private Transform reticle;
        [SerializeField] private Collider[] targets;
        [SerializeField] private float maxDistance = 20f;
        [Tooltip("Pulls the dot towards the viewer so it doesn't sink into the surface.")]
        [SerializeField] private float surfaceOffset = 0.02f;

        private Transform _head;
        private bool _isShown;

        private void Start()
        {
            _head = Camera.main.transform;
            SetShown(false);
        }

        private void LateUpdate()
        {
            var isOnTarget = Physics.Raycast(_head.position, _head.forward, out var hit, maxDistance) &&
                             IsTarget(hit.collider);
            SetShown(isOnTarget);
            if (!isOnTarget)
                return;

            reticle.position = hit.point - _head.forward * surfaceOffset;
            reticle.rotation = Quaternion.LookRotation(_head.forward);
        }

        private bool IsTarget(Collider hitCollider)
        {
            foreach (var target in targets)
            {
                if (target == hitCollider)
                    return true;
            }

            return false;
        }

        private void SetShown(bool isShown)
        {
            if (_isShown == isShown || !reticle)
                return;

            _isShown = isShown;
            reticle.gameObject.SetActive(isShown);
        }
    }
}
