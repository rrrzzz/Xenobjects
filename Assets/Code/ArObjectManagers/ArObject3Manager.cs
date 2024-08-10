using System.Diagnostics;
using Code;
using Code.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ArObject3Manager : ArObjectManagerBase
{
    private const float ColorFromWhiteThreshold = 310f;
    private const float ColorToWhiteHue = 260f;
    private const float SnowMinParticles = 50;
    private const float SnowMaxParticles = 10000;
    private const float SnowDefLifetime = 0.1f;
    private const float SnowMinLifetime = 0.7f;
    private const float SnowMaxLifetime = 2;
    private const float SnowDefStartSize = 0.051487f;
    private const float SnowMinStartSize = 1.0297f;
    private const float SnowMaxStartSize = 2f;
    
    public ParticleSystem objectCoreGlowPs;
    public ParticleSystem snowPs1;
    public ParticleSystem snowPs2;
    public ParticleSystem smokePs;
    
    public float snowDistanceMin = 0.7f;
    public float snowDistanceMax = 2.5f;
    public float oscillationSpeed = 1;
    public float idleDurationThreshold = 8;
    public float rotationDelay = 4;
    public float maxSmokeIntensity = 4;
    
    public RotateAroundObject rotationScript;

    private float _startTime;
    private bool _delayPassed;
    private Material _smokeMat;
    
    private readonly float _glowColorHMin = 360;
    private readonly float _glowColorHMax = 179;
    private readonly int _smokeTintColor = Shader.PropertyToID("_TintColor");
    private float _smokeAlpha;
    private readonly Stopwatch _oscillationTimer = new Stopwatch();
    private bool _isOscillating;
    private float _smokeIntensityMultiplier;

    public override void Initialize(MovementInteractionProviderBase dataProvider)
    {
        base.Initialize(dataProvider);
        DataProvider.SingleTouchEvent.AddListener(OnSingleTouch);
        rotationScript.Init(transform.parent);
        _startTime = Time.realtimeSinceStartup;
        _smokeMat = smokePs.GetComponent<Renderer>().material;
        _smokeAlpha = _smokeMat.GetColor(_smokeTintColor).a;
        _smokeIntensityMultiplier = Mathf.Pow(2, maxSmokeIntensity);
    }

    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        UpdateSmokeAndCoreGlow();
        UpdateSnowByDistance(snowPs1);
        UpdateSnowByDistance(snowPs2);
        
        if (_isOscillating)
        {
            SetOscillatingEffect();
        }

        if (!_delayPassed && Time.realtimeSinceStartup - _startTime > rotationDelay)
        {
            return;
        }

        _delayPassed = true;
        
        if (Time.realtimeSinceStartup - _startTime > rotationDelay && DataProvider.IdleDuration > idleDurationThreshold)
        {
            rotationScript.RotateToTargetAngle(DataProvider.camTr.position);
        }

        //TODO remove when set
        if (!DataProvider.TryGetParamValue(out var transformData)) return;
        if (transformData[0] == 0) return;
        rotationScript.radius = transformData[0];
    }

    private void UpdateSmokeAndCoreGlow()
    {
        var currentColor = Mathf.Lerp(_glowColorHMin, _glowColorHMax, DataProvider.TiltZ01);
        Debug.Log(currentColor);
        Color rgb;
        
        if (currentColor >= ColorFromWhiteThreshold)
        {
            var saturation = 1 - (_glowColorHMin - currentColor) / (_glowColorHMin - ColorFromWhiteThreshold);
            if (saturation <= 0.3)
            {
                saturation = 0;
            }
            rgb = Color.HSVToRGB(1, saturation, 1);
        }
        else if (currentColor is < ColorFromWhiteThreshold and >= ColorToWhiteHue)
        {
            var saturation = (ColorFromWhiteThreshold - currentColor) / (ColorFromWhiteThreshold - ColorToWhiteHue);
            Debug.Log("Sat " + saturation);

            rgb = Color.HSVToRGB(ColorToWhiteHue / 360f, saturation, 1);
        }
        else
        {
            rgb = Color.HSVToRGB(currentColor / 360f, 1, 1);
        }
        
        var main = objectCoreGlowPs.main;
        main.startColor = 
            new ParticleSystem.MinMaxGradient(rgb);
        rgb.a = _smokeAlpha;
        
        _smokeMat.SetColor(_smokeTintColor, rgb);
    }
    
    private void OnSingleTouch()
    {
        // var col = new Vector4(168.89700f, 0.00000f, 7.64637f, 0.10000f);
        // _smokeMat.SetColor(_smokeTintColor, col);
        // ToggleOscillatingEffect();
    }

    private void SetOscillatingEffect()
    {
        var t = _oscillationTimer.ElapsedMilliseconds / 1000f * oscillationSpeed;
        var currentMultiplier = Mathf.Sin(t) * _smokeIntensityMultiplier;
        var currentCol = _smokeMat.GetColor(_smokeTintColor);
        currentCol *= currentMultiplier;
        currentCol.a = _smokeAlpha;
        _smokeMat.SetColor(_smokeTintColor, currentCol);
    }
    
    private void UpdateSnowByDistance(ParticleSystem snowPs)
    {
        var t = Mathf.InverseLerp(snowDistanceMin, snowDistanceMax, DataProvider.DistanceToArObjectRaw);
        var lifetime = Mathf.Lerp(SnowMinLifetime, SnowMaxLifetime, t);
        var particleCount = Mathf.Lerp(SnowMinParticles, SnowMaxParticles, t);
        var size = Mathf.Lerp(SnowMinStartSize, SnowMaxStartSize, t);
        var main = snowPs.main;
           
        var startSize = new ParticleSystem.MinMaxCurve(SnowDefStartSize, size);
        var lifetimeCurve = new ParticleSystem.MinMaxCurve(SnowDefLifetime, lifetime);
        main.startSize = startSize;
        main.startLifetime = lifetimeCurve;
        main.maxParticles = Mathf.RoundToInt(particleCount);
    }

    private void ToggleOscillatingEffect()
    {
        _isOscillating = !_isOscillating;
        if (_isOscillating)
        {
            _oscillationTimer.Start();
        }
        else
        {
            _oscillationTimer.Stop();
        }
    }
}
