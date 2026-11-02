using Code.VR;
using NUnit.Framework;

namespace Code.Tests
{
    public class ExhibitFocusSelectorTests
    {
        private static ExhibitFocusSelector NewSelector()
        {
            return new ExhibitFocusSelector(focusRadius: 2.5f, releaseRadius: 3.5f, switchMargin: 0.5f);
        }

        [Test]
        public void NothingInRange_NoFocus()
        {
            var selector = NewSelector();
            var changed = selector.Evaluate(new[] { 4f, 5f, 9f });

            Assert.That(changed, Is.False);
            Assert.That(selector.Current, Is.EqualTo(-1));
        }

        [Test]
        public void WalkingUpToAPodium_FocusesIt()
        {
            var selector = NewSelector();
            var changed = selector.Evaluate(new[] { 4f, 2f, 9f });

            Assert.That(changed, Is.True);
            Assert.That(selector.Current, Is.EqualTo(1));
        }

        [Test]
        public void SlightlyCloserNeighbour_DoesNotStealFocus()
        {
            var selector = NewSelector();
            selector.Evaluate(new[] { 2f, 2.4f, 9f });
            var changed = selector.Evaluate(new[] { 2.2f, 1.9f, 9f });

            Assert.That(changed, Is.False);
            Assert.That(selector.Current, Is.EqualTo(0));
        }

        [Test]
        public void ClearlyCloserNeighbour_TakesFocus()
        {
            var selector = NewSelector();
            selector.Evaluate(new[] { 2f, 2.4f, 9f });
            var changed = selector.Evaluate(new[] { 2.4f, 1.5f, 9f });

            Assert.That(changed, Is.True);
            Assert.That(selector.Current, Is.EqualTo(1));
        }

        [Test]
        public void FocusIsKept_BetweenFocusAndReleaseRadius()
        {
            var selector = NewSelector();
            selector.Evaluate(new[] { 2f, 9f, 9f });
            var changed = selector.Evaluate(new[] { 3.2f, 9f, 9f });

            Assert.That(changed, Is.False);
            Assert.That(selector.Current, Is.EqualTo(0));
        }

        [Test]
        public void WalkingAway_ReleasesFocus()
        {
            var selector = NewSelector();
            selector.Evaluate(new[] { 2f, 9f, 9f });
            var changed = selector.Evaluate(new[] { 3.6f, 9f, 9f });

            Assert.That(changed, Is.True);
            Assert.That(selector.Current, Is.EqualTo(-1));
        }

        [Test]
        public void EqualDistances_KeepTheCurrentExhibit()
        {
            var selector = NewSelector();
            selector.Evaluate(new[] { 2f, 2.1f, 9f });
            var changed = selector.Evaluate(new[] { 2f, 2f, 9f });

            Assert.That(changed, Is.False);
            Assert.That(selector.Current, Is.EqualTo(0));
        }
    }
}
