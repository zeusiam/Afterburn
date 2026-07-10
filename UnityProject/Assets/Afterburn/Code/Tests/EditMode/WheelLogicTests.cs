using Afterburn.UI;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// UIEnvSpec §4.3 acceptance: "input layer provably never emits boost+shield in the same
    /// frame" — the edit-mode test the spec demands, plus zone geometry and slide-to-switch.
    /// </summary>
    [TestFixture]
    public sealed class WheelLogicTests
    {
        [Test]
        public void NeverBoostAndShieldTogether_ExhaustiveSweep()
        {
            var wheel = new WheelLogic();
            for (float r = 0f; r <= 320f; r += 10f)
            {
                for (float deg = 0f; deg < 360f; deg += 5f)
                {
                    Vector2 rel = new Vector2(Mathf.Sin(deg * Mathf.Deg2Rad), Mathf.Cos(deg * Mathf.Deg2Rad)) * r;
                    WheelLogic.Intents intents = wheel.Update(true, rel);
                    Assert.That(intents.Boost && intents.Shield, Is.False,
                        $"boost+shield emitted together at r={r} deg={deg}");
                    int held = (intents.Boost ? 1 : 0) + (intents.Shield ? 1 : 0) + (intents.Fire ? 1 : 0);
                    Assert.That(held, Is.LessThanOrEqualTo(1), "at most one intent per position");
                }
            }
        }

        [Test]
        public void ZoneGeometry_HubArcsAndDeadSectors()
        {
            var wheel = new WheelLogic(hubRadius: 120f, outerRadius: 300f, arcDegrees: 100f);
            Assert.That(wheel.Classify(Vector2.zero), Is.EqualTo(WheelLogic.Zone.Hub));
            Assert.That(wheel.Classify(new Vector2(0f, 100f)), Is.EqualTo(WheelLogic.Zone.Hub));
            Assert.That(wheel.Classify(new Vector2(0f, 200f)), Is.EqualTo(WheelLogic.Zone.Boost), "straight up = boost arc");
            Assert.That(wheel.Classify(new Vector2(0f, -200f)), Is.EqualTo(WheelLogic.Zone.Shield), "straight down = shield arc");
            Assert.That(wheel.Classify(new Vector2(200f, 0f)), Is.EqualTo(WheelLogic.Zone.None), "right dead sector");
            Assert.That(wheel.Classify(new Vector2(-200f, 0f)), Is.EqualTo(WheelLogic.Zone.None), "left dead sector");
            Assert.That(wheel.Classify(new Vector2(0f, 320f)), Is.EqualTo(WheelLogic.Zone.None), "outside the wheel");
        }

        [Test]
        public void SlideToSwitch_ReleasesOldEngagesNew_SameFrame()
        {
            var wheel = new WheelLogic();
            WheelLogic.Intents boost = wheel.Update(true, new Vector2(0f, 200f));
            Assert.That(boost.Boost, Is.True);

            // Slide straight down through the hub into the shield arc: one frame, one intent.
            WheelLogic.Intents shield = wheel.Update(true, new Vector2(0f, -200f));
            Assert.That(shield.Shield, Is.True);
            Assert.That(shield.Boost, Is.False, "old intent released the same frame");
        }

        [Test]
        public void Release_DropsEverything()
        {
            var wheel = new WheelLogic();
            wheel.Update(true, new Vector2(0f, 200f));
            WheelLogic.Intents released = wheel.Update(false, Vector2.zero);
            Assert.That(released.Boost || released.Shield || released.Fire, Is.False);
            Assert.That(wheel.ActiveZone, Is.EqualTo(WheelLogic.Zone.None));
        }
    }
}
