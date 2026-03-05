using NUnit.Framework;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    public class MeterOscillatorTests
    {
        [Test]
        public void Constructor_InitializesToMin()
        {
            var osc = new MeterOscillator(0f, 1f);
            Assert.AreEqual(0f, osc.Value);
            Assert.IsTrue(osc.Rising);
        }

        [Test]
        public void Tick_Rises_FromMin()
        {
            var osc = new MeterOscillator(0f, 1f);
            osc.Tick(1f, 0.5f);
            Assert.AreEqual(0.5f, osc.Value, 0.001f);
            Assert.IsTrue(osc.Rising);
        }

        [Test]
        public void Tick_ClampsAtMax_AndReverses()
        {
            var osc = new MeterOscillator(0f, 1f);
            osc.Tick(1f, 1.5f); // overshoot
            Assert.AreEqual(1f, osc.Value);
            Assert.IsFalse(osc.Rising);
        }

        [Test]
        public void Tick_Falls_AfterMax()
        {
            var osc = new MeterOscillator(0f, 1f);
            osc.Tick(1f, 1.5f); // hit max
            osc.Tick(1f, 0.3f); // fall
            Assert.AreEqual(0.7f, osc.Value, 0.001f);
            Assert.IsFalse(osc.Rising);
        }

        [Test]
        public void Tick_ClampsAtMin_AndReverses()
        {
            var osc = new MeterOscillator(0f, 1f);
            osc.Tick(1f, 1.5f); // hit max
            osc.Tick(1f, 1.5f); // hit min
            Assert.AreEqual(0f, osc.Value);
            Assert.IsTrue(osc.Rising);
        }

        [Test]
        public void NegativeRange_Works()
        {
            var osc = new MeterOscillator(-1f, 1f);
            Assert.AreEqual(-1f, osc.Value);
            osc.Tick(2f, 1.5f); // -1 + 3 = overshoot 1
            Assert.AreEqual(1f, osc.Value);
            Assert.IsFalse(osc.Rising);
        }

        [Test]
        public void Reset_RestoresToMin()
        {
            var osc = new MeterOscillator(0f, 1f);
            osc.Tick(1f, 0.5f);
            osc.Reset();
            Assert.AreEqual(0f, osc.Value);
            Assert.IsTrue(osc.Rising);
        }
    }
}
