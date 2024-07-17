using System.Collections;
using Code.Effects;
using DG.Tweening;
using EasyButtons;
using SplineMesh;
using UnityEngine;

namespace Code
{
    public class ArObject2Manager : MonoBehaviour
    {
        public Transform playerTr;
        public float delayBeforeInitShrinking = 1;
        public float delayBeforeMidGrowing = 1;
        public float playerToPathNodeDistanceThreshold = 0.3f;
        public float orbDecolorationTime = 1;
        public float orbScaleTime = 1.5f;
        public float idleDurationThreshold = 3;
        public float oscillationSpeed = 1;
        public float splineScalingPause = 1;
        
        public SplineMeshTiling initialTentacleSpline;
        public SplineMeshTiling highResMiddleTentacleSpline;
        public SplineMeshTiling endTentacleSpline;
        
        public GameObject steamObj;
        public Transform orbObj;
        public GameObject inkLinesObj;
        public GameObject capsuleObj;
        
        public Vector3 orbStartPos;
        public Vector3 orbEndPos = new Vector3(-5.606459f, 0.6333609f, -0.2901777f);
        
        private ExampleTentacle _exampleTentacle;
        
        private OrbGlowingEffect _orbGlow = new OrbGlowingEffect();
        private DistortionEffect _distortion = new DistortionEffect();
        private RisingSteamEffect _risingSteamEffect = new RisingSteamEffect();
        private InkLinesEffect _inkLinesEffect = new InkLinesEffect();
        
        private bool _isTouchToggleOn;
        private float _idleT;
        private float _movingT;
        private bool _isArObjDisabled = false;
        private bool _isInitialized;
        private Vector3 _orbScale;
        private MovementInteractionProviderBase _dataProvider;
        private bool _isMidPulsing;
        private bool _isFinishedPulsing;
        private bool _isPathEndReached;

        public void Initialize(MovementInteractionProviderBase dataProvider)
        {
            _dataProvider = dataProvider;
            
            SetMaterials();
            
            orbStartPos = orbObj.position;
            _orbScale = orbObj.localScale;
            
            _dataProvider.DoubleTouchEvent.AddListener(OnDoubleTouch);
            _dataProvider.ShakeEvent.AddListener(ResetScale);
            
            highResMiddleTentacleSpline.PathEndReachedEvent.AddListener(OnPathEndReached);
            
            _isInitialized = true;
        }
        
        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }
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
        
        [Button]
        private void PlayPathfindingSequence()
        {
            StartCoroutine(PathfindingCoroutine());
        }

        private IEnumerator PathfindingCoroutine()
        {
            orbObj.position = orbStartPos;
            orbObj.localScale = _orbScale;
            _orbGlow.FadeColor(0, false);
            _orbGlow.FadeColor(orbDecolorationTime, true);
            FadeOutEffects();

            yield return new WaitForSeconds(orbDecolorationTime);
            
            initialTentacleSpline.ScaleToFull();
            ShrinkOrb();

            yield return new WaitForSeconds(initialTentacleSpline.GetTotalScaleToFullDuration() + delayBeforeInitShrinking);
            initialTentacleSpline.ScaleToZero();
            yield return new WaitForSeconds(delayBeforeMidGrowing);
            TogglePulseMidTentacle();

            var firstNodePos = highResMiddleTentacleSpline.GetGlobalNodePos(0);

            while (Vector3.Distance(playerTr.position, firstNodePos) > playerToPathNodeDistanceThreshold * 1.5f)
            {
                yield return null;
            }
            
            TogglePulseMidTentacle();

            while (!_isFinishedPulsing)
            {
                yield return null;
            }

            highResMiddleTentacleSpline.InitVanishing(playerTr, playerToPathNodeDistanceThreshold);

            while (!_isPathEndReached)
            {
                yield return null;
            }
            
            highResMiddleTentacleSpline.ScaleToZeroNow();
            print("Start creating puzzle!");
        }

        [Button]
        private void TogglePulseMidTentacle()
        {
            _isMidPulsing = !_isMidPulsing;
            if (!_isMidPulsing)
            {
                return;
            }

            StartCoroutine(PulseCoroutine());
        }

        private IEnumerator PulseCoroutine()
        {
            var splineToPulse = highResMiddleTentacleSpline;
            var isGrowing = true;

            var pauseAfterGrowing = new WaitForSeconds(splineToPulse.GetTotalScaleToFullDuration() + splineScalingPause);
            var pauseAfterShrinking = new WaitForSeconds(splineToPulse.GetTotalScaleToZeroDuration());
            
            while (true)
            {
                if (isGrowing)
                {
                    splineToPulse.ScaleToFull();
                }
                else
                {
                    if (!_isMidPulsing)
                    {
                        _isFinishedPulsing = true;
                        break;
                    }
                    splineToPulse.ScaleToZero();
                }
                
                yield return isGrowing ? pauseAfterGrowing : pauseAfterShrinking;
                
                isGrowing = !isGrowing;
            }
        }

        private void ResetScale()
        {
            if (_isArObjDisabled)
            {
                return;
            }
            
            highResMiddleTentacleSpline.ScaleToZeroNow();
        }
        
        private void FadeOutEffects()
        {
            _risingSteamEffect.FadeAlpha(orbDecolorationTime, true);
            _distortion.FadeEffects(orbDecolorationTime);
            _inkLinesEffect.FadeAlpha(orbDecolorationTime, true);
        }
        
        private void FadeInEffects()
        {
            _risingSteamEffect.FadeAlpha(initialTentacleSpline.scaleToFullDuration, false);
            _inkLinesEffect.FadeAlpha(initialTentacleSpline.scaleToFullDuration, false);
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

        private void OnPathEndReached()
        {
            _isPathEndReached = true;
        }
        
        private void OnDoubleTouch()
        {
            if (_isArObjDisabled)
            {
                return;
            }
           
            TogglePulseMidTentacle();
            // _isTouchToggleOn = !_isTouchToggleOn;
            // _distortion.ToggleOscillatingEffect();
        }
        
        // [Button]
        private void ShrinkOrb()
        {
            orbObj.DOScale(Vector3.zero, orbScaleTime);
            orbObj.DOMove(orbEndPos, orbScaleTime);
        }
        
        // [Button]
        private void GrowOrb()
        {
            orbObj.DOScale(_orbScale, orbScaleTime);
            orbObj.DOMove(orbStartPos, orbScaleTime);
        }

        // [Button]
        private void SetOrbEndPos()
        { 
            orbEndPos = orbObj.position;
            print(orbEndPos);
        }
        
        // [Button]
        private void ResetOrb()
        {
            orbObj.position = orbStartPos;
            orbObj.localScale = _orbScale;
        }
        
        // [Button]
        private void SaveOrbInitPosition()
        {
            _orbScale = orbObj.localScale;
            orbStartPos = orbObj.position;
            print(orbStartPos);
        }

        private void OnDestroy()
        {
            if (highResMiddleTentacleSpline)
            {
                highResMiddleTentacleSpline.PathEndReachedEvent.RemoveListener(OnPathEndReached);
            }
            
            if (_dataProvider)
            {
                _dataProvider.DoubleTouchEvent.RemoveListener(OnDoubleTouch);
            }
        }
    }
}