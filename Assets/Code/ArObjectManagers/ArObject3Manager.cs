using System.Diagnostics;
using Code;
using Code.Utils;
using EasyButtons;
using UnityEngine;

public class ArObject3Manager : ArObjectManagerBase
{
    private const float RibbonStartSizeDef = .1f;
    private const float RibbonLifetimeDef = 1;
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
    
    public ParticleSystem[] ribbons;
    public ParticleSystem objectCoreGlowPs;
    public ParticleSystem snowPs1;
    public ParticleSystem snowPs2;
    public ParticleSystem smokePs;

    public float ribbonStartSizeMax = .1f;
    public float ribbonLifetimeMax = 1;
    public float snowDistanceMin = 0.7f;
    public float snowDistanceMax = 2.5f;
    public float oscillationSpeed = 1;
    public float idleDurationThreshold = 8;
    public float rotationDelay = 4;
    
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
    private bool _isEndingOscillation;

    public override void Initialize(MovementInteractionProviderBase dataProvider)
    {
        base.Initialize(dataProvider);
        DataProvider.SingleTouchEvent.AddListener(OnSingleTouch);
        rotationScript.Init(transform.parent);
        _startTime = Time.realtimeSinceStartup;
        _smokeMat = smokePs.GetComponent<Renderer>().material;
        _smokeAlpha = _smokeMat.GetColor(_smokeTintColor).a;
    }

    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }
        
        if (_isOscillating || _isEndingOscillation)
        {
            SetOscillatingEffect();
        }

        UpdateSmokeAndCoreGlow();
        UpdateSnowByDistance(snowPs1);
        UpdateSnowByDistance(snowPs2);

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
        ToggleOscillatingEffect();
    }

    private void SetOscillatingEffect()
    {
        var t = _oscillationTimer.ElapsedMilliseconds / 1000f * oscillationSpeed;
        var currentT = Mathf.Abs(Mathf.Sin(t));
        
        if (_isEndingOscillation)
        {
            currentT = Mathf.Min(currentT, 1 - currentT);
            if (currentT < 0.05f)
            {
                _isEndingOscillation = false;
                _isOscillating = false;
                _oscillationTimer.Stop();
            }
        }
        
        var currentLifetime = Mathf.Lerp(RibbonLifetimeDef, ribbonLifetimeMax, currentT);
        var currentSize = Mathf.Lerp(RibbonStartSizeDef, ribbonStartSizeMax, currentT);
        foreach (var ps in ribbons)
        {
            var main = ps.main;
            main.startSize = currentSize;
            main.startLifetime = currentLifetime;
        }
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

    [Button]
    private void ToggleOscillatingEffect()
    {
        _isOscillating = !_isOscillating;
        if (_isOscillating)
        {
            _isEndingOscillation = false;
            _oscillationTimer.Start();
        }
        else
        {
            _isEndingOscillation = true;
        }
    }
}
