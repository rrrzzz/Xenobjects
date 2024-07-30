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
        public float puzzleXOffset;
        public float puzzleYOffset;
        public float puzzleAppearingTime = 1f;
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
        public SplineMeshTiling puzzleTentacleSpline;
        public SplineMeshTiling puzzleEndTentacleSpline;
        
        public GameObject steamObj;
        public Transform orbObj;
        public GameObject inkLinesObj;
        public GameObject capsuleObj;
        
        public Vector3 orbStartPos;
        public Vector3 orbEndPos = new Vector3(-5.606459f, 0.6333609f, -0.2901777f);
        
        private ExampleTentacle _exampleTentacle;
        
        private OrbGlowingEffect _orbGlow = new OrbGlowingEffect();
        private OrbGlowingEffect _puzzleGlow = new OrbGlowingEffect();
        private DistortionEffect _distortion = new DistortionEffect();
        private RisingSteamEffect _risingSteamEffect = new RisingSteamEffect();
        private InkLinesEffect _inkLinesEffect = new InkLinesEffect();
        
        private bool _isTouchToggleOn;
        private float _idleT;
        private float _movingT;
        private bool _isArObjDisabled;
        private bool _isInitialized;
        private Vector3 _orbScale;
        private MovementInteractionProviderBase _dataProvider;
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
        private bool isMateralsSet;
        
        private readonly Color _puzzleSolvedColor = new Color(2.27060f, 1.69304f, 0.71760f, 1.00000f);
        private readonly Color _originalColor = new Color(2.31267f, 1.72440f, 0.73089f, 1.00000f);
        private readonly Color _fadedPuzzleColor = new Color(1.39772f, 1.04183f, 0.44173f, 1.00000f);

        public void Initialize(MovementInteractionProviderBase dataProvider)
        {
            _dataProvider = dataProvider;
            
            SetMaterials();
            
            orbStartPos = orbObj.position;
            _orbScale = orbObj.localScale;
            
            // _dataProvider.DoubleTouchEvent.AddListener(OnDoubleTouch);
            _dataProvider.SingleTouchEvent.AddListener(OnDoubleTouch);
            _dataProvider.ShakeEvent.AddListener(ResetScale);
            
            highResMiddleTentacleSpline.PathEndReachedEvent.AddListener(OnPathEndReached);
            
            _isInitialized = true;

            // StartPuzzleTest();
        }
        
        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }
            //
            // if (Input.GetKey(KeyCode.N))
            // {
            //     var currentRot = puzzleTentacleSpline.Rotation;
            //     currentRot.y += rotationSpeed * Time.deltaTime;
            //     puzzleTentacleSpline.rotation = currentRot;
            //     puzzleTentacleSpline.SetRotation();
            //     Debug.Log(currentRot.y);
            // }
            //
            // if (Input.GetKey(KeyCode.M))
            // {
            //     var currentRot = puzzleTentacleSpline.Rotation;
            //     currentRot.y -= rotationSpeed * Time.deltaTime;
            //     puzzleTentacleSpline.rotation = currentRot;
            //     puzzleTentacleSpline.SetRotation();
            //     Debug.Log(currentRot.y);
            // }
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
            
            _puzzleGlow.IncreaseAlphaToOne(puzzleAppearingTime);
            yield return new WaitForSeconds(puzzleAppearingTime);


            
            print("Morph into glowing orb");
            
            FadeInEffects();
            yield return new WaitForSeconds(orbDecolorationTime);
            
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
            puzzleTentacleSpline.SetSegmentsRotationToZero();
            _puzzleGlow.SetAlphaZero();
            
            _puzzleGlow.IncreaseAlphaToOne(puzzleAppearingTime);
            yield return new WaitForSeconds(puzzleAppearingTime);

            SetRandomPuzzleRotation();
            
            var errorX = Mathf.Abs(_startPuzzleXRotation);
            var errorY = Mathf.Abs(_startPuzzleYRotation);
            var totalError = Mathf.Clamp01((errorX + errorY) / 360);
                    
            var tColor = 1 - totalError;
            InterpolatePuzzleColor(tColor);


            while (!_isPuzzleCompleted)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Debug.Log($"StartX: {_startPuzzleXRotation}, StartY: {_startPuzzleYRotation}");
                }
                // if (!_isFirstHit || (_dataProvider.GetForwardRayHit(out var hit) && hit.transform == puzzleTentacleSpline.transform))
                if (!_isFirstHit || (_dataProvider.GetForwardRayHit(out var hit) && hit.transform == puzzleTentacleSpline.transform))
                {
                    if (_isFirstHit)
                    {
                        Debug.Log("first hit");
                        _isFirstHit = false;
                        _dataProvider.SetPuzzleEnteredRotation();
                    }
                    
                    var rotationX = _dataProvider.SignedTiltZ01 * 180 + _startPuzzleXRotation;
                    var rotationY = _dataProvider.SignedTiltY01 * 180 + _startPuzzleYRotation;

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

            puzzleEndTentacleSpline.enabled = true;
            puzzleTentacleSpline.enabled = false;
            puzzleEndTentacleSpline.InterpolatePosDir();
            yield return new WaitForSeconds(puzzleEndTentacleSpline.interpolationDuration);
            
            puzzleEndTentacleSpline.InverseVanish();
            var vanishDuration = puzzleEndTentacleSpline.GetTotalVanishingDuration();
            yield return new WaitForSeconds(vanishDuration / 2);
            
            _orbGlow.FadeColor(0, false);
            
            GrowOrb();
            yield return new WaitForSeconds(orbScaleTime);
            FadeInEffects();
        }
        
        private void InterpolatePuzzleColor(float t)
        {
            if (!isMateralsSet)
            {
                isMateralsSet = true;
                _puzzleSegmentMaterials = puzzleTentacleSpline.GetSegmentMaterials();
            }
            
            var interpolatedColor = Vector4.Lerp(_fadedPuzzleColor, _puzzleSolvedColor, t);
            
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
            _risingSteamEffect.FadeAlpha(orbDecolorationTime, false);
            _inkLinesEffect.FadeAlpha(orbDecolorationTime, false);
        }
        
        private void SetMaterials()
        {
            _risingSteamEffect.SetMaterial(steamObj);
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
        
        private void OnDoubleTouch()
        {
            if (_isArObjDisabled)
            {
                return;
            }

            // StartPuzzleTest();
            // TogglePulseMidTentacle();
            // _isTouchToggleOn = !_isTouchToggleOn;
            // _distortion.ToggleOscillatingEffect();
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