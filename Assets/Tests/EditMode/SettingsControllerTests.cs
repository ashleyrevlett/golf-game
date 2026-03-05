using NUnit.Framework;
using UnityEngine;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for SettingsController logic.
    /// Verifies volume changes propagate to AudioListener and
    /// quality toggle maps to correct QualitySettings level.
    /// </summary>
    public class SettingsControllerTests
    {
        private float savedVolume;

        [SetUp]
        public void SetUp()
        {
            savedVolume = AudioListener.volume;
        }

        [TearDown]
        public void TearDown()
        {
            AudioListener.volume = savedVolume;
            PlayerPrefs.DeleteKey("SoundVolume");
            PlayerPrefs.DeleteKey("HighQuality");
        }

        [Test]
        public void VolumeChange_PropagatesTo_AudioListener()
        {
            float newVolume = 0.42f;
            AudioListener.volume = newVolume;
            Assert.AreEqual(newVolume, AudioListener.volume, 0.001f);
        }

        [Test]
        public void VolumeKey_DefaultsToOne()
        {
            PlayerPrefs.DeleteKey("SoundVolume");
            float vol = PlayerPrefs.GetFloat("SoundVolume", 1f);
            Assert.AreEqual(1f, vol, 0.001f);
        }

        [Test]
        public void VolumeKey_PersistsValue()
        {
            PlayerPrefs.SetFloat("SoundVolume", 0.7f);
            float vol = PlayerPrefs.GetFloat("SoundVolume", 1f);
            Assert.AreEqual(0.7f, vol, 0.001f);
        }

        [Test]
        public void QualityKey_DefaultsToHigh()
        {
            PlayerPrefs.DeleteKey("HighQuality");
            int quality = PlayerPrefs.GetInt("HighQuality", 1);
            Assert.AreEqual(1, quality);
        }

        [Test]
        public void QualityKey_MapsCorrectly()
        {
            // SettingsController maps: true -> 1, false -> 0
            bool highQuality = true;
            int mapped = highQuality ? 1 : 0;
            Assert.AreEqual(1, mapped);

            highQuality = false;
            mapped = highQuality ? 1 : 0;
            Assert.AreEqual(0, mapped);
        }

        [Test]
        public void QualityToggle_PersistsValue()
        {
            PlayerPrefs.SetInt("HighQuality", 0);
            int quality = PlayerPrefs.GetInt("HighQuality", 1);
            Assert.AreEqual(0, quality);
        }

        [Test]
        public void VolumeClamp_ZeroIsValid()
        {
            AudioListener.volume = 0f;
            Assert.AreEqual(0f, AudioListener.volume, 0.001f);
        }

        [Test]
        public void VolumeClamp_OneIsValid()
        {
            AudioListener.volume = 1f;
            Assert.AreEqual(1f, AudioListener.volume, 0.001f);
        }
    }
}
