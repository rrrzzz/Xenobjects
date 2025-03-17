using System.Diagnostics;
using Code;
using Code.Utils;
using EasyButtons;
using UnityEngine;
using UnityEngine.Serialization;

public class ArObject3Manager : ArObjectManagerBase
{
    private const float RibbonStartSizeDef = .1f;
    private const float RibbonLifetimeDef = 1;
    private const float ColorFromWhiteThreshold = 310f;
    private const float ColorToWhiteHue = 260f;
    private const float SmokeMinParticles = 50;
    private const float SmokeMaxParticles = 10000;
    private const float SmokeDefLifetime = 0.1f;
    private const float SmokeMinLifetime = 0.7f;
    private const float SmokeMaxLifetime = 2;
    private const float SmokeDefStartSize = 0.051487f;
    private const float SmokeMinStartSize = 1.0297f;
    private const float SmokeMaxStartSize = 2f;
    
    public ParticleSystem[] ribbons;
    public ParticleSystem objectCoreGlowPs;
    [FormerlySerializedAs("smokePs1")] [FormerlySerializedAs("snowPs1")] 
    public ParticleSystem smokePs;
    [FormerlySerializedAs("smokePs")] 
    public ParticleSystem coloredFlamePs;

    public float ribbonStartSizeMax = .1f;
    public float ribbonLifetimeMax = 1;
    [FormerlySerializedAs("snowDistanceMin")] 
    public float smokeDistanceMin = 0.7f;
    [FormerlySerializedAs("snowDistanceMax")] 
    public float smokeDistanceMax = 2.5f;
    public float oscillationSpeed = 1;
    public float idleDurationThreshold = 8;
    public float rotationDelay = 4;
    public float rotationRadius = 1;
    
    public RotateAroundObjectToRayCircleIntersection rotationScript;

    private float _startTime;
    private Material _coloredFlameMat;
    
    private readonly float _glowColorHMin = 360;
    private readonly float _glowColorHMax = 179;
    private readonly int _smokeTintColor = Shader.PropertyToID("_TintColor");
    private float _coloredFlameAlpha;
    private readonly Stopwatch _oscillationTimer = new Stopwatch();
    private bool _isOscillating;
    private bool _isEndingOscillation;

    public override void Initialize(MovementInteractionProviderBase dataProvider)
    {
        base.Initialize(dataProvider);
        DataProvider.SingleTouchEvent.AddListener(OnSingleTouch);
        rotationScript.Init(transform.parent);
        rotationScript.radius = rotationRadius;
        _startTime = Time.realtimeSinceStartup;
        _coloredFlameMat = coloredFlamePs.GetComponent<Renderer>().material;
        _coloredFlameAlpha = _coloredFlameMat.GetColor(_smokeTintColor).a;
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

        UpdateColoredFlameAndCoreGlow();
        UpdateSmokeByDistance(smokePs);

        if (Time.realtimeSinceStartup - _startTime <= rotationDelay)
        {
            return;
        }
        
        if (DataProvider.IdleDuration > idleDurationThreshold)
        {
            rotationScript.RotateToTargetAngle(DataProvider.camTr.position);
        }

        //TODO remove when set
        // if (DataProvider.TryGetParamValue(out var transformData) && transformData[0] != 0)
        // {
        //     rotationScript.radius = transformData[0];
        // }
    }

    private void UpdateColoredFlameAndCoreGlow()
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
        rgb.a = _coloredFlameAlpha;
        
        _coloredFlameMat.SetColor(_smokeTintColor, rgb);
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
    
    private void UpdateSmokeByDistance(ParticleSystem snowPs)
    {
        var t = Mathf.InverseLerp(smokeDistanceMin, smokeDistanceMax, DataProvider.DistanceToArObjectRaw);
        var lifetime = Mathf.Lerp(SmokeMinLifetime, SmokeMaxLifetime, t);
        var particleCount = Mathf.Lerp(SmokeMinParticles, SmokeMaxParticles, t);
        var size = Mathf.Lerp(SmokeMinStartSize, SmokeMaxStartSize, t);
        var main = snowPs.main;
           
        var startSize = new ParticleSystem.MinMaxCurve(SmokeDefStartSize, size);
        var lifetimeCurve = new ParticleSystem.MinMaxCurve(SmokeDefLifetime, lifetime);
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
