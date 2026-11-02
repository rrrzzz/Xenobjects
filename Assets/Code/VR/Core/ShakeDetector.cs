using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.VR
{
    /// <summary>
    /// Detects a controller shake from per-hand velocity samples: several short velocity bursts
    /// inside a sliding window. Plain C# so it can be tested without a headset; the VR provider
    /// feeds it one sample per hand per frame.
    /// </summary>
    /// <remarks>
    /// Per hand: framerate-independent low-pass of the velocity, high-pass by subtraction,
    /// hysteresis burst counting, sliding window of burst timestamps. A shake fires when either
    /// hand has enough bursts in the window; both windows are then cleared and a cooldown starts.
    /// Every stage is load-bearing — a single threshold would fire on any sharp motion.
    /// </remarks>
    public sealed class ShakeDetector
    {
        [Serializable]
        public struct Settings
        {
            // Squared velocity deviation (m²/s²) that registers a burst.
            public float burstEnterThreshold;
            // Squared velocity deviation below which the burst is released.
            public float burstExitThreshold;
            // Sliding window in seconds.
            public float burstWindow;
            public int minBurstsForShake;
            public float cooldown;
            public float lowPassTimeConstant;

            public static Settings Default => new Settings
            {
                burstEnterThreshold = 4f,
                burstExitThreshold = 1f,
                burstWindow = 0.7f,
                minBurstsForShake = 3,
                cooldown = 0.35f,
                lowPassTimeConstant = 0.5f
            };
        }

        private readonly Hand _left = new Hand();
        private readonly Hand _right = new Hand();
        private float _lastShakeTime = float.NegativeInfinity;

        public ShakeDetector(Settings settings)
        {
            Config = settings;
        }

        public Settings Config { get; set; }

        public float LeftDeltaSq => _left.LastDeltaSq;
        public float RightDeltaSq => _right.LastDeltaSq;
        public int LeftBurstCount => _left.BurstCount;
        public int RightBurstCount => _right.BurstCount;

        /// <summary>
        /// Feeds one frame of samples. Pass hasLeft/hasRight = false for a hand without a
        /// velocity this frame. Returns true on the frame a shake is detected.
        /// </summary>
        public bool Step(float time, float deltaTime, bool hasLeft, Vector3 leftVelocity, bool hasRight, Vector3 rightVelocity)
        {
            var config = Config;
            var alpha = 1f - Mathf.Exp(-deltaTime / Mathf.Max(config.lowPassTimeConstant, 1e-4f));

            _left.Step(hasLeft, leftVelocity, alpha, time, config);
            _right.Step(hasRight, rightVelocity, alpha, time, config);

            var isShaking = _left.BurstCount >= config.minBurstsForShake ||
                            _right.BurstCount >= config.minBurstsForShake;
            if (!isShaking)
                return false;

            if (time - _lastShakeTime < config.cooldown)
                return false;

            _lastShakeTime = time;

            // Clear histories so we don't immediately re-fire on the tail end of the same shake.
            _left.ClearBursts();
            _right.ClearBursts();
            return true;
        }

        /// <summary>
        /// Re-primes one hand's filter, e.g. when its controller was (re)acquired.
        /// </summary>
        public void ResetHand(bool isLeft)
        {
            if (isLeft)
            {
                _left.Reset();
            }
            else
            {
                _right.Reset();
            }
        }

        public void Reset()
        {
            _left.Reset();
            _right.Reset();
            _lastShakeTime = float.NegativeInfinity;
        }

        private sealed class Hand
        {
            private readonly Queue<float> _burstTimes = new Queue<float>();
            private Vector3 _lowPass;
            private bool _isPrimed;
            private bool _isBurstActive;

            public int BurstCount => _burstTimes.Count;
            public float LastDeltaSq { get; private set; }

            public void Step(bool hasSample, Vector3 velocity, float alpha, float time, Settings config)
            {
                var deltaSq = 0f;
                if (hasSample)
                {
                    // Prime on the first sample, or the first frame produces a phantom spike.
                    if (!_isPrimed)
                    {
                        _lowPass = velocity;
                        _isPrimed = true;
                    }
                    else
                    {
                        _lowPass = Vector3.Lerp(_lowPass, velocity, alpha);
                    }

                    deltaSq = (velocity - _lowPass).sqrMagnitude;
                }

                LastDeltaSq = deltaSq;

                while (_burstTimes.Count > 0 && time - _burstTimes.Peek() > config.burstWindow)
                {
                    _burstTimes.Dequeue();
                }

                if (!_isBurstActive && deltaSq > config.burstEnterThreshold)
                {
                    _isBurstActive = true;
                    _burstTimes.Enqueue(time);
                }
                else if (_isBurstActive && deltaSq < config.burstExitThreshold)
                {
                    _isBurstActive = false;
                }
            }

            public void ClearBursts()
            {
                _burstTimes.Clear();
            }

            public void Reset()
            {
                _burstTimes.Clear();
                _isPrimed = false;
                _isBurstActive = false;
                LastDeltaSq = 0f;
            }
        }
    }
}
