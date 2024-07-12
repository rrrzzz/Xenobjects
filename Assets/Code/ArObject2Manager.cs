using System.Collections;
using System.Collections.Generic;
using Code.Effects;
using DG.Tweening;
using EasyButtons;
using SplineMesh;
using UnityEngine;

namespace Code
{
    public class ArObject2Manager : MonoBehaviour
    {
        public float orbDecolorationTime = 1;
        // public float orbScaleDelay = 0.2f;
        public float orbScaleTime = 1.5f;
        public float firstTentacleScaleToFullTime = 2;
        public float firstTentacleScaleDelayTime = .1f; 
        public float idleDurationThreshold = 3;
        public float oscillationSpeed = 1;
        public MovementInteractionProviderBase dataProvider;

        public SplineMeshTiling firstTentacleSpline;
        public SplineMeshTiling secondTentacleSpline;
        public SplineMeshTiling thirdTentacleSpline;
        
        public GameObject steamObj;
        public Transform orbObj;
        public GameObject inkLinesObj;
        public GameObject capsuleObj;
        
        public Vector3 orbStartPos;
        public Vector3 orbEndPos = new Vector3(-5.606459f, 0.6333609f, -0.2901777f);
        
        private SplineMeshTiling _firstSpline;
        private ExampleTentacle _exampleTentacle;
        
        private OrbGlowingEffect _orbGlow = new OrbGlowingEffect();
        private DistortionEffect _distortion = new DistortionEffect();
        private RisingSteamEffect _risingSteamEffect = new RisingSteamEffect();
        private InkLinesEffect _inkLinesEffect = new InkLinesEffect();
        
        private bool _isTouchToggleOn;
        private float _idleT;
        private float _movingT;
        private bool _isArObjDisabled;
        private Vector3 _orbScale;

        private void Start()
        {
            SetMaterials();
            orbStartPos = orbObj.position;
            _orbScale = orbObj.localScale;

            // orbEndPos = orbObj.position;
            // dataProvider.DoubleTouchEvent.AddListener(OnDoubleTouch);
            // dataProvider.ArObjectSetEvent.AddListener(SetTentacleComponents);
            // dataProvider.ShakeEvent.AddListener(ResetScaleAndTime);
        }
        
        [Button]
        private void ShrinkOrb()
        {
            orbObj.DOScale(Vector3.zero, orbScaleTime);
            orbObj.DOMove(orbEndPos, orbScaleTime);
        }
        
        [Button]
        private void GrowOrb()
        {
            orbObj.DOScale(_orbScale, orbScaleTime);
            orbObj.DOMove(orbStartPos, orbScaleTime);
        }

        [Button]
        private void SetOrbEndPos()
        { 
            orbEndPos = orbObj.position;
            print(orbEndPos);
        }
        
        [Button]
        private void ResetOrb()
        {
            orbObj.position = orbStartPos;
            orbObj.localScale = _orbScale;
        }
        
        [Button]
        private void SaveOrbInitPosition()
        {
            _orbScale = orbObj.localScale;
            orbStartPos = orbObj.position;
            print(orbStartPos);
        }
        
        private void Update()
        {
            // if (dataProvider.IdleDuration > idleDurationThreshold && !_isArObjDisabled)
            // {
            //     _isArObjDisabled = true;
            //     FadeEffects();
            // }
            //
            // if (_isArObjDisabled)
            // {
            //     return;
            // }
            // _orbGlow.SetEffectByNormalizedValue(dataProvider.DistanceToArObject01);   
            // _distortion.SetEffectByNormalizedValue(dataProvider.Tilt01);
            //
            // if (_isTouchToggleOn)
            // {
            //     _distortion.SetOscillatingEffect(oscillationSpeed);
            // }

            // SetMovementChangingDissolution();
        }

        private void ResetScaleAndTime()
        {
            if (_isArObjDisabled)
            {
                return;
            }
            _firstSpline.ResetScalingAndPositions();
            // _exampleTentacle.startScale += 0.001f;
            // _exampleTentacle.ReapplyScaleAndRoll();
        }
        
        private void FadeOutEffects()
        {
            _risingSteamEffect.FadeAlpha(orbDecolorationTime, true);
            _distortion.FadeEffects(orbDecolorationTime);
            _inkLinesEffect.FadeAlpha(orbDecolorationTime, true);
        }
        
        private void FadeInEffects()
        {
            _risingSteamEffect.FadeAlpha(firstTentacleScaleToFullTime, false);
            _inkLinesEffect.FadeAlpha(firstTentacleScaleToFullTime, false);
        }
        
        [Button]
        private void PlayFirstStage()
        {
            StartCoroutine(FirstStageCoroutine());
        }

        private IEnumerator FirstStageCoroutine()
        {
            orbObj.position = orbStartPos;
            orbObj.localScale = _orbScale;
            _orbGlow.FadeColor(0, false);
            _orbGlow.FadeColor(orbDecolorationTime, true);
            FadeOutEffects();

            yield return new WaitForSeconds(orbDecolorationTime);
            
            firstTentacleSpline.ScaleToFull(firstTentacleScaleToFullTime, firstTentacleScaleDelayTime);
            ShrinkOrb();
        }

        private void SetMaterials()
        {
            _risingSteamEffect.SetMaterial(steamObj);
            _orbGlow.SetMaterial(orbObj.gameObject);
            _distortion.SetMaterial(capsuleObj);
            _inkLinesEffect.SetMaterial(inkLinesObj);
        }

        private void SetMovementChangingDissolution()
        {
            // if (!dataProvider.IsMoving)
            // {
            //     _idleT = 0;
            //     if (_movingT > 0)
            //     {
            //         var elapsedDissolvingTime = dissolvingDuration * _movingT;
            //
            //         _idleT = Mathf.InverseLerp(0, dissolvingDuration, 
            //             dataProvider.IdleDuration + elapsedDissolvingTime);
            //     }
            //     else if (dataProvider.IdleDuration > idleDurationThreshold)
            //     {
            //         _idleT = Mathf.InverseLerp(idleDurationThreshold, idleDurationThreshold + dissolvingDuration, 
            //             dataProvider.IdleDuration);
            //     }
            //     
            //     _dissolveEffect.SetEffectByNormalizedValue(_idleT);
            // }
            // else
            // {
            //     var elapsedAssemblyTime = dissolvingDuration * (1 - _idleT);
            //     
            //     _movingT = 1 - Mathf.InverseLerp(0, dissolvingDuration, 
            //         dataProvider.MovementDuration + elapsedAssemblyTime);
            //
            //     _dissolveEffect.SetEffectByNormalizedValue(_movingT);
            // }
        }

        private void OnDoubleTouch()
        {
            if (_isArObjDisabled)
            {
                return;
            }
            
            _isTouchToggleOn = !_isTouchToggleOn;
            _distortion.ToggleOscillatingEffect();
        }

        private void OnDestroy()
        {
            dataProvider.DoubleTouchEvent.RemoveListener(OnDoubleTouch);
            dataProvider.ArObjectSetEvent.RemoveListener(SetMaterials);
        }
    }
}