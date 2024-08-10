using System.Collections;
using Code.Effects;
using DG.Tweening;
using EasyButtons;
using SplineMesh;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code
{
    public class ArObject2Manager : ArObjectManagerBase
    {
        private const float FogDistanceMin = 0.7f;
        private const float FogDistanceMax = 2f;
        private const float FogMin = .5f;
        private const float FogMax = 10;
        
        public ParticleSystem swarmParticles;
        public ParticleSystem fogParticleSystem;
        public float puzzleXOffset;
        public float puzzleYOffset;
        public float puzzleAppearingTime = 1f;
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
        public SplineMeshTiling puzzleTentacleSpline;
        public SplineMeshTiling puzzleEndTentacleSpline;
        
        public Transform orbObj;
        public GameObject inkLinesObj;
        public GameObject capsuleObj;
        
        private readonly Vector3 _orbEndPos = new Vector3(15.581f, 5.037f, 7.488f);
        private readonly Vector3 _orbStartPos = new Vector3(13.357f, 3.300f, 5.976f);
        
        private ExampleTentacle _exampleTentacle;
        
        private OrbGlowingEffect _orbGlow = new OrbGlowingEffect();
        private OrbGlowingEffect _puzzleGlow = new OrbGlowingEffect();
        private DistortionEffect _distortion = new DistortionEffect();
        private InkLinesEffect _inkLinesEffect = new InkLinesEffect();
        
        private bool _isTouchToggleOn;
        private float _idleT;
        private float _movingT;
        private bool _isArObjDisabled;
        private Vector3 _orbScale;
        private bool _isMidPulsing;
        private bool _isFinishedPulsing;
        private bool _isPathEndReached;
        private bool _isPuzzleCompleted;
        private float _startPuzzleXRotation;
        private float _startPuzzleYRotation;
        private bool _wasObjectActivated;
        private bool _isFirstHit;
        private static int _puzzleTintId = Shader.PropertyToID("_TintColor");
        private Material[] _puzzleSegmentMaterials;
        private bool _isMaterialsSet;
        
        private readonly Color _puzzleSolvedColor = new Color(2.27060f, 1.69304f, 0.71760f, 1.00000f);
        private readonly Color _originalColor = new Color(2.31267f, 1.72440f, 0.73089f, 1.00000f);
        private readonly Color _fadedPuzzleColor = new Color(1.39772f, 1.04183f, 0.44173f, 1.00000f);

 

        public override void Initialize(MovementInteractionProviderBase dataProvider)
        {
            base.Initialize(dataProvider);
            SetEffectMaterials();
            
            _orbScale = orbObj.localScale;
            
            DataProvider.SingleTouchEvent.AddListener(OnSingleTouch);
            DataProvider.ShakeEvent.AddListener(PlaySwarm);
            
            highResMiddleTentacleSpline.PathEndReachedEvent.AddListener(OnPathEndReached);
            
            initialTentacleSpline.ScaleToZeroNow();
            highResMiddleTentacleSpline.ScaleToZeroNow();
        }
        
        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            SetFogByDistance();

            if (!_wasObjectActivated && DataProvider.IdleDuration > idleDurationThreshold && !_isArObjDisabled)
            {
                _isArObjDisabled = true;
                StartCoroutine(PathfindingCoroutine());
            }
            
            if (_isArObjDisabled)
            {
                return;
            }
            _orbGlow.SetEffectByNormalizedValue(DataProvider.DistanceToArObject01);   
            _distortion.SetEffectByNormalizedValue(DataProvider.TiltZ01);
            
            if (_isTouchToggleOn)
            {
                _distortion.SetOscillatingEffect(oscillationSpeed);
            }
        }

        private void SetFogByDistance()
        {
            var t = Mathf.InverseLerp(FogDistanceMin, FogDistanceMax, DataProvider.DistanceToArObjectRaw);
            var fogValue = Mathf.Lerp(FogMin, FogMax, t);
            var main = fogParticleSystem.main;
           
            var startSize = new ParticleSystem.MinMaxCurve(fogValue, fogValue);
            startSize.mode = ParticleSystemCurveMode.Constant;
            main.startSize = startSize;
        }

        [Button]
        private void PlayPathfindingSequence()
        {
            StartCoroutine(PathfindingCoroutine());
        }

        private IEnumerator PathfindingCoroutine()
        {
            orbObj.localPosition = _orbStartPos;
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

            while (Vector3.Distance(DataProvider.camTr.position, firstNodePos) > playerToPathNodeDistanceThreshold * 1.5f)
            {
                yield return null;
            }
            
            TogglePulseMidTentacle();

            while (!_isFinishedPulsing)
            {
                yield return null;
            }

            highResMiddleTentacleSpline.InitVanishing(DataProvider.camTr, playerToPathNodeDistanceThreshold);

            while (!_isPathEndReached)
            {
                yield return null;
            }
            
            highResMiddleTentacleSpline.ScaleToZeroNow();
            
            StopPuzzleCoroutine();
            
            yield return StartCoroutine(BeginPuzzleLoop());
            
            _isArObjDisabled = false;
            _wasObjectActivated = true;
        }
        
        [Button]
        private void StartPuzzleTest()
        {
            StopPuzzleCoroutine();
            StartCoroutine(BeginPuzzleLoop());
        }

        private IEnumerator BeginPuzzleLoop()
        {
            _isFirstHit = true;
            _isPuzzleCompleted = false;
            SetRandomPuzzleRotation();
            var errorX = Mathf.Abs(_startPuzzleXRotation);
            var errorY = Mathf.Abs(_startPuzzleYRotation);
            var totalError = Mathf.Clamp01((errorX + errorY) / 360);
                    
            var tColor = 1 - totalError;
            InterpolatePuzzleColor(tColor, true);
            
            _puzzleGlow.IncreaseAlphaToOne(puzzleAppearingTime);
            yield return new WaitForSeconds(puzzleAppearingTime);
            
            while (!_isPuzzleCompleted)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Debug.Log($"StartX: {_startPuzzleXRotation}, StartY: {_startPuzzleYRotation}");
                }
                // if (!_isFirstHit || (_dataProvider.GetForwardRayHit(out var hit) && hit.transform == puzzleTentacleSpline.transform))
                if (!_isFirstHit || (DataProvider.GetForwardRayHit(out var hit) && hit.transform == puzzleTentacleSpline.transform))
                {
                    if (_isFirstHit)
                    {
                        _isFirstHit = false;
                        DataProvider.SetPuzzleEnteredRotation();
                    }
                    
                    var rotationX = DataProvider.SignedTiltZ01 * 180 + _startPuzzleXRotation;
                    var rotationY = DataProvider.SignedTiltY01 * 180 + _startPuzzleYRotation;
            
                    puzzleTentacleSpline.RotateSegments(rotationX, rotationY);
            
                    errorX = Mathf.Abs(rotationX);
                    errorY = Mathf.Abs(rotationY);
                    totalError = Mathf.Clamp01((errorX + errorY) / 360);
                    
                    tColor = 1 - totalError;
                    
                    InterpolatePuzzleColor(tColor);
                    
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        Debug.Log($"rotationX: {rotationX}, rotationY: {rotationY}");
                        Debug.Log($"errorX: {errorX}, errorY: {errorY}");
                        Debug.Log($"tColor: {tColor}");
                    }
                    
                    if (tColor >= 0.995)
                    {
                        _isPuzzleCompleted = true;
                        puzzleTentacleSpline.RotateSegments(0, 0);
                        foreach (var mat in _puzzleSegmentMaterials)
                        {
                            mat.SetColor(_puzzleTintId, _originalColor);
                        } 
                    }
                }
                yield return null;
            }

            puzzleTentacleSpline.enabled = false;
            puzzleTentacleSpline.gameObject.SetActive(false);
            puzzleEndTentacleSpline.gameObject.SetActive(true);
            puzzleEndTentacleSpline.InterpolatePosDir();
            yield return new WaitForSeconds(puzzleEndTentacleSpline.interpolationDuration);
            
            puzzleEndTentacleSpline.InverseVanish();
            var vanishDuration = puzzleEndTentacleSpline.GetTotalVanishingDuration();
            yield return new WaitForSeconds(vanishDuration / 1.4f);
            
            _orbGlow.FadeColor(0, false);
            
            GrowOrb();
            yield return new WaitForSeconds(orbScaleTime);
            FadeInEffects();
        }
        
        private void InterpolatePuzzleColor(float t, bool isAlphaZero = false)
        {
            if (!_isMaterialsSet)
            {
                _isMaterialsSet = true;
                _puzzleSegmentMaterials = puzzleTentacleSpline.GetSegmentMaterials();
            }
            
            var interpolatedColor = Vector4.Lerp(_fadedPuzzleColor, _puzzleSolvedColor, t);
            if (isAlphaZero)
            {
                interpolatedColor.w = 0;
            }
            foreach (var mat in _puzzleSegmentMaterials)
            {
                mat.SetColor(_puzzleTintId, interpolatedColor);
            } 
        }

        [Button]
        private void StopPuzzleCoroutine()
        {
            StopCoroutine(nameof(BeginPuzzleLoop));    
        }

        private void SetRandomPuzzleRotation()
        {
            puzzleYOffset = puzzleXOffset;
            _startPuzzleXRotation = Random.Range(puzzleXOffset, 180);
            var coinFlip = Random.Range(0, 2);
            _startPuzzleXRotation *= coinFlip == 0 ? 1 : -1;
            _startPuzzleYRotation = Random.Range(puzzleYOffset, 180);
            coinFlip = Random.Range(0, 2);
            _startPuzzleYRotation *= coinFlip == 0 ? 1 : -1;
            puzzleTentacleSpline.RotateSegments(_startPuzzleXRotation, _startPuzzleYRotation);
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

        private void PlaySwarm()
        {
            if (_isArObjDisabled)
            {
                return;
            }

            if (!swarmParticles.gameObject.activeInHierarchy)
            {
                swarmParticles.gameObject.SetActive(true);
            }
            
            swarmParticles.Play();
        }
        
        private void FadeOutEffects()
        {
            _distortion.FadeEffects(orbDecolorationTime);
            _inkLinesEffect.FadeAlpha(orbDecolorationTime, true);
        }
        
        private void FadeInEffects()
        {
            _inkLinesEffect.FadeAlpha(orbDecolorationTime, false);
        }
        
        private void SetEffectMaterials()
        {
            _orbGlow.SetMaterial(orbObj.gameObject);
            _distortion.SetMaterial(capsuleObj);
            _inkLinesEffect.SetMaterial(inkLinesObj);
            _puzzleGlow.SetMaterial(puzzleTentacleSpline.gameObject);
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
        
        private void OnSingleTouch()
        {
            if (_isArObjDisabled)
            {
                return;
            }

            _isTouchToggleOn = !_isTouchToggleOn;
            _distortion.ToggleOscillatingEffect();
        }
        
        [Button]
        private void ShrinkOrb()
        {
            orbObj.DOScale(Vector3.zero, orbScaleTime);
            orbObj.DOLocalMove(_orbEndPos, orbScaleTime);
        }
        
        [Button]
        private void GrowOrb()
        {
            orbObj.DOScale(_orbScale, orbScaleTime);
            orbObj.DOLocalMove(_orbStartPos, orbScaleTime);
        }

        private void OnDestroy()
        {
            if (highResMiddleTentacleSpline)
            {
                highResMiddleTentacleSpline.PathEndReachedEvent.RemoveListener(OnPathEndReached);
            }
            
            if (DataProvider)
            {
                DataProvider.DoubleTouchEvent.RemoveListener(OnSingleTouch);
                DataProvider.ShakeEvent.RemoveListener(PlaySwarm);
            }
        }
    }
}