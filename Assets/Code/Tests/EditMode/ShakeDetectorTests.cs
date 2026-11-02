using System;
using System.Collections.Generic;
using Code.VR;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests
{
    public class ShakeDetectorTests
    {
        private const float Dt72 = 1f / 72f;

        // A hand moving back and forth: x(t) = -A cos(2πft), so the velocity starts at zero.
        private static Func<float, Vector3> Oscillation(float amplitude, float frequency)
        {
            return t => new Vector3(amplitude * 2f * Mathf.PI * frequency * Mathf.Sin(2f * Mathf.PI * frequency * t), 0f, 0f);
        }

        private static List<float> RunRightHand(ShakeDetector detector, float duration, float dt, Func<float, Vector3> velocityAt)
        {
            var shakeTimes = new List<float>();
            var steps = Mathf.RoundToInt(duration / dt);
            for (var i = 0; i < steps; i++)
            {
                var t = i * dt;
                if (detector.Step(t, dt, false, Vector3.zero, true, velocityAt(t)))
                    shakeTimes.Add(t);
            }

            return shakeTimes;
        }

        private static ShakeDetector NewDetector()
        {
            return new ShakeDetector(ShakeDetector.Settings.Default);
        }

        [Test]
        public void HalfSecondShake_FiresExactlyOnce()
        {
            // 10 cm at 4 Hz peaks at ~2.5 m/s, a brisk wrist shake.
            var shakes = RunRightHand(NewDetector(), 0.5f, Dt72, Oscillation(0.1f, 4f));

            Assert.That(shakes.Count, Is.EqualTo(1));
        }

        [TestCase(72)]
        [TestCase(90)]
        [TestCase(120)]
        public void SameShake_SameResult_AtQuestFrameRates(int fps)
        {
            var shakes = RunRightHand(NewDetector(), 0.5f, 1f / fps, Oscillation(0.1f, 4f));

            Assert.That(shakes.Count, Is.EqualTo(1), $"at {fps} fps");
        }

        [Test]
        public void SingleChop_DoesNotFire()
        {
            // One fast swing: 3 m/s for 0.1 s, then still.
            var shakes = RunRightHand(NewDetector(), 1f, Dt72, t => t < 0.1f ? new Vector3(3f, 0f, 0f) : Vector3.zero);

            Assert.That(shakes, Is.Empty);
        }

        [Test]
        public void TwoChops_DoNotFire()
        {
            // Out and back once: two bursts, one short of a shake.
            Func<float, Vector3> outAndBack = t =>
            {
                if (t < 0.1f)
                    return new Vector3(3f, 0f, 0f);

                if (t >= 0.25f && t < 0.35f)
                    return new Vector3(-3f, 0f, 0f);

                return Vector3.zero;
            };

            var shakes = RunRightHand(NewDetector(), 1f, Dt72, outAndBack);

            Assert.That(shakes, Is.Empty);
        }

        [Test]
        public void SlowSway_DoesNotFire()
        {
            var shakes = RunRightHand(NewDetector(), 4f, Dt72, Oscillation(0.15f, 0.5f));

            Assert.That(shakes, Is.Empty);
        }

        [Test]
        public void TrackingJitter_DoesNotFire()
        {
            var random = new System.Random(1234);
            var shakes = RunRightHand(NewDetector(), 4f, Dt72,
                _ => new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, 0f) * 0.6f);

            Assert.That(shakes, Is.Empty);
        }

        [Test]
        public void HandAlreadyMovingAtStart_DoesNotFakeABurst()
        {
            // Without priming, the first sample would read as a 3 m/s spike.
            var detector = NewDetector();
            RunRightHand(detector, 0.3f, Dt72, _ => new Vector3(3f, 0f, 0f));

            Assert.That(detector.RightBurstCount, Is.Zero);
        }

        [Test]
        public void ContinuousShaking_RespectsCooldown()
        {
            var settings = ShakeDetector.Settings.Default;
            var shakes = RunRightHand(new ShakeDetector(settings), 2f, Dt72, Oscillation(0.1f, 4f));

            Assert.That(shakes.Count, Is.InRange(3, 6));
            for (var i = 1; i < shakes.Count; i++)
            {
                Assert.That(shakes[i] - shakes[i - 1], Is.GreaterThanOrEqualTo(settings.cooldown));
            }
        }

        [Test]
        public void LeftHandShake_Fires()
        {
            var detector = NewDetector();
            var velocityAt = Oscillation(0.1f, 4f);
            var shakes = 0;
            for (var i = 0; i < 36; i++)
            {
                var t = i * Dt72;
                if (detector.Step(t, Dt72, true, velocityAt(t), false, Vector3.zero))
                    shakes++;
            }

            Assert.That(shakes, Is.EqualTo(1));
        }

        [Test]
        public void NoControllers_NeverFires()
        {
            var detector = NewDetector();
            var shakes = 0;
            for (var i = 0; i < 300; i++)
            {
                if (detector.Step(i * Dt72, Dt72, false, Vector3.zero, false, Vector3.zero))
                    shakes++;
            }

            Assert.That(shakes, Is.Zero);
        }
    }
}
