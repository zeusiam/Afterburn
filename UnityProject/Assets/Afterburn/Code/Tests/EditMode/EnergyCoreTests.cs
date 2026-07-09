using Afterburn.Core;
using NUnit.Framework;

namespace Afterburn.Tests
{
    /// <summary>
    /// BUILD §11 U2 tests: EnergyCore drain/regen ordering and the §2 accounting invariants.
    /// Pure Core — no scene.
    /// </summary>
    [TestFixture]
    public sealed class EnergyCoreTests
    {
        private const float Dt = 1f / 60f;

        private static EnergyCore MediumCore() => new EnergyCore(100f, 100f);   // max = 100

        [Test]
        public void StartsFull_AndScalesWithEnergyMaxScale()
        {
            Assert.That(MediumCore().Energy, Is.EqualTo(100f));
            Assert.That(new EnergyCore(80f, 100f).Max, Is.EqualTo(80f));    // Light
            Assert.That(new EnergyCore(130f, 100f).Max, Is.EqualTo(130f)); // Heavy
            Assert.That(new EnergyCore(100f, 50f).Max, Is.EqualTo(50f));   // scale = ×0.5
        }

        [Test]
        public void TrySpend_RefusesWhenShort_AndSpendsExactlyWhenNot()
        {
            EnergyCore core = MediumCore();
            Assert.That(core.TrySpend(20f), Is.True);
            Assert.That(core.Energy, Is.EqualTo(80f));

            EnergyCore poor = new EnergyCore(100f, 100f);
            poor.Damage(85f);                                   // 15 left
            Assert.That(poor.TrySpend(20f), Is.False, "fireCost 20 with 15 in the pool must refuse");
            Assert.That(poor.Energy, Is.EqualTo(15f), "a refused spend must not partially drain");
        }

        [Test]
        public void Drain_SelfCancelsAtZero_AndClampsExactly()
        {
            EnergyCore core = MediumCore();
            core.Damage(99.9f);                                 // 0.1 left
            // 25/s over one tick = ~0.4167 — pool empties mid-tick.
            bool alive = core.Drain(25f, Dt);
            Assert.That(alive, Is.False, "drain must report self-cancel when the pool empties");
            Assert.That(core.Energy, Is.EqualTo(0f), "prototype clamps at exactly 0");
        }

        [Test]
        public void Regen_UsesScaleBaseline8_AndClampsAtMax()
        {
            EnergyCore core = MediumCore();
            core.Damage(10f);                                   // 90
            core.Regen(8f, 8f, 1f);                             // 8 × (8/8) × 1s = +8
            Assert.That(core.Energy, Is.EqualTo(98f).Within(1e-4f));
            core.Regen(8f, 8f, 1f);                             // would be 106 → clamps
            Assert.That(core.Energy, Is.EqualTo(100f));

            EnergyCore light = new EnergyCore(80f, 100f);
            light.Damage(40f);
            light.Regen(11f, 8f, 1f);                           // Light regen 11 at baseline
            Assert.That(light.Energy, Is.EqualTo(51f).Within(1e-4f));
        }

        [Test]
        public void Grant_NeverExceedsMax()
        {
            EnergyCore core = MediumCore();
            core.Damage(5f);
            core.Grant(16f);                                    // bounty reward vs leader
            Assert.That(core.Energy, Is.EqualTo(100f), "grants clamp at max (PortSpec §7)");
        }

        [Test]
        public void Damage_ReturnsActualRemoved_ForSiphonRuling7()
        {
            EnergyCore core = MediumCore();
            core.Damage(90f);                                   // 10 left
            float removed = core.Damage(25f);                   // siphon steal attempt
            Assert.That(removed, Is.EqualTo(10f), "ruling #7: steal caps at the victim's remaining pool");
            Assert.That(core.Energy, Is.EqualTo(0f));
        }
    }
}
