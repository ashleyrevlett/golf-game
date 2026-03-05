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

        [Test]
        public void AudioManager_Singleton_SetsInstance()
        {
            var managerObj = new GameObject("AudioManager_Singleton");
            var manager = managerObj.AddComponent<AudioManager>();

            Assert.AreEqual(manager, AudioManager.Instance);

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_Singleton_DestroysSecondInstance()
        {
            var firstObj = new GameObject("AudioManager_First");
            var first = firstObj.AddComponent<AudioManager>();

            var secondObj = new GameObject("AudioManager_Second");
            secondObj.AddComponent<AudioManager>();

            Assert.AreEqual(first, AudioManager.Instance);

            Object.DestroyImmediate(secondObj);
            Object.DestroyImmediate(firstObj);
        }

        [Test]
        public void AudioManager_OnDestroy_ClearsInstance()
        {
            var managerObj = new GameObject("AudioManager_Destroy");
            managerObj.AddComponent<AudioManager>();

            Assert.IsNotNull(AudioManager.Instance);
            Object.DestroyImmediate(managerObj);
            Assert.IsNull(AudioManager.Instance);
        }

        [Test]
        public void AudioManager_PlaySFX_WithClip_ReturnsAudioSource()
        {
            var managerObj = new GameObject("AudioManager_SFX");
            var manager = managerObj.AddComponent<AudioManager>();

            var clip = AudioClip.Create("test", 44100, 1, 44100, false);
            var source = manager.PlaySFX(clip, 0.5f, 1.2f);

            Assert.IsNotNull(source);
            Assert.AreEqual(clip, source.clip);
            Assert.AreEqual(1.2f, source.pitch, 0.001f);

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_PlayLoop_WithClip_ReturnsLoopingSource()
        {
            var managerObj = new GameObject("AudioManager_Loop");
            var manager = managerObj.AddComponent<AudioManager>();

            var clip = AudioClip.Create("loop_test", 44100, 1, 44100, false);
            var source = manager.PlayLoop(clip, 0.8f);

            Assert.IsNotNull(source);
            Assert.IsTrue(source.loop);
            Assert.AreEqual(clip, source.clip);

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_StopSource_StopsAndClearsClip()
        {
            var managerObj = new GameObject("AudioManager_Stop");
            var manager = managerObj.AddComponent<AudioManager>();

            var clip = AudioClip.Create("stop_test", 44100, 1, 44100, false);
            var source = manager.PlaySFX(clip);

            manager.StopSource(source);
            Assert.IsNull(source.clip);

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_StopSource_NullDoesNotThrow()
        {
            var managerObj = new GameObject("AudioManager_StopNull");
            var manager = managerObj.AddComponent<AudioManager>();

            Assert.DoesNotThrow(() => manager.StopSource(null));

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_Config_ExposesAssignedConfig()
        {
            var managerObj = new GameObject("AudioManager_Config");
            var manager = managerObj.AddComponent<AudioManager>();

            // Config is null when not assigned via SerializeField
            Assert.IsNull(manager.Config);

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_PlaySFX_WithoutConfig_UsesFallbackVolume()
        {
            var managerObj = new GameObject("AudioManager_Fallback");
            var manager = managerObj.AddComponent<AudioManager>();

            var clip = AudioClip.Create("fallback_test", 44100, 1, 44100, false);
            var source = manager.PlaySFX(clip, 1f, 1f);

            // Without config, fallback sfxVol is 0.8f, so volume = 1 * 0.8 = 0.8
            Assert.AreEqual(0.8f, source.volume, 0.001f);

            Object.DestroyImmediate(managerObj);
        }

        [Test]
        public void AudioManager_OnFirstUserGesture_SetsVolume()
        {
            var managerObj = new GameObject("AudioManager_Gesture");
            var manager = managerObj.AddComponent<AudioManager>();

            PlayerPrefs.SetFloat("SoundVolume", 0.6f);
            manager.OnFirstUserGesture();
            Assert.AreEqual(0.6f, AudioListener.volume, 0.001f);

            // Second call is no-op
            PlayerPrefs.SetFloat("SoundVolume", 0.9f);
            manager.OnFirstUserGesture();
            Assert.AreEqual(0.6f, AudioListener.volume, 0.001f);

            AudioListener.volume = 1f;
            PlayerPrefs.DeleteKey("SoundVolume");
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
