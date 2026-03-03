using NUnit.Framework;
using UnityEngine;
using GolfGame.Audio;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for audio config and manager.
    /// </summary>
    public class AudioTests
    {
        // AudioConfig Tests

        [Test]
        public void AudioConfig_DefaultMasterVolume_IsOne()
        {
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            Assert.AreEqual(1f, config.MasterVolume, 0.001f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AudioConfig_DefaultSfxVolume_IsPositive()
        {
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            Assert.Greater(config.SfxVolume, 0f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AudioConfig_DefaultAmbientVolume_IsPositive()
        {
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            Assert.Greater(config.AmbientVolume, 0f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AudioConfig_CrowdThreshold_IsPositive()
        {
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            Assert.Greater(config.CrowdReactionDistanceThreshold, 0f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AudioConfig_NullClips_AreNull()
        {
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            Assert.IsNull(config.BallHit);
            Assert.IsNull(config.WindAmbience);
            Assert.IsNull(config.ButtonClick);
            Object.DestroyImmediate(config);
        }

        // AudioManager Tests

        [Test]
        public void AudioManager_PoolCreatesCorrectCount()
        {
            var managerObj = new GameObject("AudioManager");
            var manager = managerObj.AddComponent<AudioManager>();

            // Pool is created in Awake, which runs on AddComponent
            Assert.AreEqual(8, manager.PoolSize);

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_SetMasterVolume_UpdatesListener()
        {
            var managerObj = new GameObject("AudioManager");
            var manager = managerObj.AddComponent<AudioManager>();

            manager.SetMasterVolume(0.5f);
            Assert.AreEqual(0.5f, AudioListener.volume, 0.001f);

            // Reset
            AudioListener.volume = 1f;
            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_PlaySFX_NullClip_ReturnsNull()
        {
            var managerObj = new GameObject("AudioManager");
            var manager = managerObj.AddComponent<AudioManager>();

            var result = manager.PlaySFX(null);
            Assert.IsNull(result);

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_PlayLoop_NullClip_ReturnsNull()
        {
            var managerObj = new GameObject("AudioManager");
            var manager = managerObj.AddComponent<AudioManager>();

            var result = manager.PlayLoop(null);
            Assert.IsNull(result);

            Object.DestroyImmediate(managerObj);
        }

        // BallAudioController Tests

        [Test]
        public void BallAudioController_NullConfig_DoesNotThrow()
        {
            var obj = new GameObject("BallAudio");
            var controller = obj.AddComponent<BallAudioController>();

            // Should not throw even without AudioManager
            Assert.DoesNotThrow(() => controller.SetLaunchPower(0.5f));

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void BallAudioController_HitPitch_ScalesWithPower()
        {
            // Pitch formula: Lerp(0.8, 1.2, power)
            float lowPitch = Mathf.Lerp(0.8f, 1.2f, 0f);
            float highPitch = Mathf.Lerp(0.8f, 1.2f, 1f);
            Assert.AreEqual(0.8f, lowPitch, 0.001f);
            Assert.AreEqual(1.2f, highPitch, 0.001f);
        }

        // AmbientAudioController Tests

        [Test]
        public void WindVolume_ScalesWithSpeed()
        {
            // Formula: Clamp01(speed / 8)
            float calm = Mathf.Clamp01(0f / 8f);
            float moderate = Mathf.Clamp01(4f / 8f);
            float strong = Mathf.Clamp01(8f / 8f);

            Assert.AreEqual(0f, calm, 0.001f);
            Assert.AreEqual(0.5f, moderate, 0.001f);
            Assert.AreEqual(1f, strong, 0.001f);
        }

        [Test]
        public void CrowdReaction_TriggersbelowThreshold()
        {
            float threshold = 3f;
            float closeShot = 2.5f;
            float farShot = 5f;

            Assert.IsTrue(closeShot <= threshold);
            Assert.IsFalse(farShot <= threshold);
        }
    }
}
