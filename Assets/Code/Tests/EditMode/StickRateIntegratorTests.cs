using System;
using Code.VR;
using NUnit.Framework;

namespace Code.Tests
{
    public class StickRateIntegratorTests
    {
        private const float Dt = 0.01f;

        private static void Hold(StickRateIntegrator integrator, float input, float rate, float seconds)
        {
            var steps = (int)Math.Round(seconds / Dt);
            for (var i = 0; i < steps; i++)
            {
                integrator.Step(input, rate, Dt);
            }
        }

        [Test]
        public void FullDeflection_MovesAtTheGivenRate()
        {
            var integrator = new StickRateIntegrator();
            Hold(integrator, 1f, 0.5f, 1f);

            Assert.That(integrator.Value, Is.EqualTo(0.5f).Within(1e-3f));
        }

        [Test]
        public void ReleasingTheStick_HoldsTheValue()
        {
            var integrator = new StickRateIntegrator();
            Hold(integrator, 1f, 0.5f, 1f);
            Hold(integrator, 0f, 0.5f, 2f);

            Assert.That(integrator.Value, Is.EqualTo(0.5f).Within(1e-3f));
        }

        [Test]
        public void Value_IsClampedToPlusMinusOne()
        {
            var integrator = new StickRateIntegrator();
            Hold(integrator, 1f, 0.5f, 5f);
            Assert.That(integrator.Value, Is.EqualTo(1f));

            Hold(integrator, -1f, 0.5f, 10f);
            Assert.That(integrator.Value, Is.EqualTo(-1f));
        }

        [Test]
        public void Reset_ReturnsToZero()
        {
            var integrator = new StickRateIntegrator();
            Hold(integrator, -1f, 0.5f, 1f);
            integrator.Reset();

            Assert.That(integrator.Value, Is.Zero);
        }
    }
}
