using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Audio;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for audio config and manager.
    /// </summary>
    public class AudioTests
    {
        [SetUp]
        public void SetUp()
        {
            // Suppress DontDestroyOnLoad / Destroy errors in edit mode
            LogAssert.ignoreFailingMessages = true;

            // Clean up any lingering singleton
            if (AudioManager.Instance != null)
            {
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (AudioManager.Instance != null)
            {
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
            }
            LogAssert.ignoreFailingMessages = false;
        }

        /// <summary>
        /// Create an AudioManager and invoke Awake (doesn't auto-fire in edit mode).
        /// </summary>
        private AudioManager CreateManager(string name = "AudioManager")
        {
            var obj = new GameObject(name);
            var manager = obj.AddComponent<AudioManager>();
            manager.SendMessage("Awake");
            return manager;
        }

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
            var manager = CreateManager();
            Assert.AreEqual(8, manager.PoolSize);
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_SetMasterVolume_UpdatesListener()
        {
            var manager = CreateManager();

            manager.SetMasterVolume(0.5f);
            Assert.AreEqual(0.5f, AudioListener.volume, 0.001f);

            // Reset
            AudioListener.volume = 1f;
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_PlaySFX_NullClip_ReturnsNull()
        {
            var manager = CreateManager();

            var result = manager.PlaySFX(null);
            Assert.IsNull(result);

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_PlayLoop_NullClip_ReturnsNull()
        {
            var manager = CreateManager();

            var result = manager.PlayLoop(null);
            Assert.IsNull(result);

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_Singleton_SetsInstance()
        {
            var manager = CreateManager("AudioManager_Singleton");
            Assert.AreEqual(manager, AudioManager.Instance);
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_Singleton_DestroysSecondInstance()
        {
            var first = CreateManager("AudioManager_First");

            var secondObj = new GameObject("AudioManager_Second");
            var second = secondObj.AddComponent<AudioManager>();
            second.SendMessage("Awake"); // Detects existing, calls Destroy (suppressed)

            Assert.AreEqual(first, AudioManager.Instance);

            Object.DestroyImmediate(secondObj);
            Object.DestroyImmediate(first.gameObject);
        }

        [Test]
        public void AudioManager_OnDestroy_ClearsInstance()
        {
            var manager = CreateManager("AudioManager_Destroy");

            Assert.IsNotNull(AudioManager.Instance);
            Object.DestroyImmediate(manager.gameObject);
            Assert.IsNull(AudioManager.Instance);
        }

        [Test]
        public void AudioManager_PlaySFX_WithClip_ReturnsAudioSource()
        {
            var manager = CreateManager("AudioManager_SFX");

            var clip = AudioClip.Create("test", 44100, 1, 44100, false);
            var source = manager.PlaySFX(clip, 0.5f, 1.2f);

            Assert.IsNotNull(source);
            Assert.AreEqual(clip, source.clip);
            Assert.AreEqual(1.2f, source.pitch, 0.001f);

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_PlayLoop_WithClip_ReturnsLoopingSource()
        {
            var manager = CreateManager("AudioManager_Loop");

            var clip = AudioClip.Create("loop_test", 44100, 1, 44100, false);
            var source = manager.PlayLoop(clip, 0.8f);

            Assert.IsNotNull(source);
            Assert.IsTrue(source.loop);
            Assert.AreEqual(clip, source.clip);

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_StopSource_StopsAndClearsClip()
        {
            var manager = CreateManager("AudioManager_Stop");

            var clip = AudioClip.Create("stop_test", 44100, 1, 44100, false);
            var source = manager.PlaySFX(clip);

            manager.StopSource(source);
            Assert.IsNull(source.clip);

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_Config_ExposesAssignedConfig()
        {
            var manager = CreateManager("AudioManager_Config");

            // Config is null when not assigned via SerializeField
            Assert.IsNull(manager.Config);

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_PlaySFX_WithoutConfig_UsesFallbackVolume()
        {
            var manager = CreateManager("AudioManager_Fallback");

            var clip = AudioClip.Create("fallback_test", 44100, 1, 44100, false);
            var source = manager.PlaySFX(clip, 1f, 1f);

            // Without config, fallback sfxVol is 0.8f, so volume = 1 * 0.8 = 0.8
            Assert.AreEqual(0.8f, source.volume, 0.001f);

            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_OnFirstUserGesture_SetsVolume()
        {
            var manager = CreateManager("AudioManager_Gesture");

            PlayerPrefs.SetFloat("SoundVolume", 0.6f);
            manager.OnFirstUserGesture();
            Assert.AreEqual(0.6f, AudioListener.volume, 0.001f);

            // Second call is no-op
            PlayerPrefs.SetFloat("SoundVolume", 0.9f);
            manager.OnFirstUserGesture();
            Assert.AreEqual(0.6f, AudioListener.volume, 0.001f);

            AudioListener.volume = 1f;
            PlayerPrefs.DeleteKey("SoundVolume");
            Object.DestroyImmediate(manager.gameObject);
        }

        // BallAudioController Tests

        [Test]
        public void BallAudioController_SetLaunchPower_StoresClamped()
        {
            var obj = new GameObject("BallAudio_Store");
            var controller = obj.AddComponent<BallAudioController>();

            controller.SetLaunchPower(0.75f);

            // Verify stored value via reflection (lastLaunchPower is private)
            var field = typeof(BallAudioController).GetField("lastLaunchPower",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "lastLaunchPower field should exist");
            Assert.AreEqual(0.75f, (float)field.GetValue(controller), 0.001f);

            // Verify clamping for out-of-range values
            controller.SetLaunchPower(5f);
            Assert.AreEqual(1f, (float)field.GetValue(controller), 0.001f);

            controller.SetLaunchPower(-3f);
            Assert.AreEqual(0f, (float)field.GetValue(controller), 0.001f);

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void BallAudioController_Config_HasBounceRoughClipField()
        {
            // Verify AudioConfig exposes BallBounceRough for surface-specific bounce
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            // BallBounceRough is null by default (no asset assigned)
            Assert.IsNull(config.BallBounceRough);
            // But the field exists for surface-specific selection
            Assert.IsNotNull(typeof(AudioConfig).GetProperty("BallBounceRough"));
            Object.DestroyImmediate(config);
        }

        // AmbientAudioController Tests

        [Test]
        public void CrowdReaction_ThresholdMatchesConfigDefault()
        {
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            Assert.AreEqual(3f, config.CrowdReactionDistanceThreshold, 0.001f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AmbientAudioController_GetAmbientVolume_FallbackWithoutManager()
        {
            if (AudioManager.Instance != null)
                Object.DestroyImmediate(AudioManager.Instance.gameObject);

            var obj = new GameObject("AmbientAudio_Fallback");
            var controller = obj.AddComponent<AmbientAudioController>();

            var method = typeof(AmbientAudioController).GetMethod("GetAmbientVolume",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, "GetAmbientVolume method should exist");
            float vol = (float)method.Invoke(controller, null);
            Assert.AreEqual(0.5f, vol, 0.001f, "Fallback ambient volume should be 0.5");

            Object.DestroyImmediate(obj);
        }

    }
}
