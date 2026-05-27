using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class VrRightTriggerTest : MonoBehaviour
{
    [SerializeField] private Transform cube;
    [SerializeField] private float pingPongDuration = 0.5f;
    [SerializeField] private InputActionReference rightTriggerAction;
    [SerializeField] private InputActionReference xLeftPrimaryButtonAction;

    private Tween _scaleTween;

    private void Start()
    {
        InitPingPong(cube);
    }

    private void OnEnable()
    {
        if (rightTriggerAction == null || rightTriggerAction.action == null) return;
        rightTriggerAction.action.performed += OnRightTriggerPressed;
        rightTriggerAction.action.Enable();
        
        if (xLeftPrimaryButtonAction == null || xLeftPrimaryButtonAction.action == null) return;
        xLeftPrimaryButtonAction.action.performed += OnXPrimaryButtonPressed;
        xLeftPrimaryButtonAction.action.Enable();
    }

    private void OnDisable()
    {
        if (rightTriggerAction == null || rightTriggerAction.action == null) return;
        rightTriggerAction.action.performed -= OnRightTriggerPressed;
        
        if (xLeftPrimaryButtonAction == null || xLeftPrimaryButtonAction.action == null) return;
        xLeftPrimaryButtonAction.action.performed -= OnXPrimaryButtonPressed;
    }

    private void OnRightTriggerPressed(InputAction.CallbackContext _) => TogglePingPong();
    private void OnXPrimaryButtonPressed(InputAction.CallbackContext _) => Debug.Log("X primary button pressed");

    private void InitPingPong(Transform target)
    {
        if (target == null) return;

        Vector3 baseScale = target.localScale;
        Vector3 targetScale = baseScale + new Vector3(0f, 1f, 0f);
        _scaleTween = target.DOScale(targetScale, pingPongDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .Pause();
    }

    private void TogglePingPong()
    {
        if (_scaleTween == null) return;
        if (_scaleTween.IsPlaying()) _scaleTween.Pause();
        else _scaleTween.Play();
    }

    private void OnDestroy()
    {
        _scaleTween?.Kill();
    }
}
