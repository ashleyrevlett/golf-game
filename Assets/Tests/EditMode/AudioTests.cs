using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Audio;
using GolfGame.Environment;

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

        // --- Reflection Helpers ---

        /// <summary>
        /// Inject an AudioConfig into AudioManager's private 'config' SerializeField.
        /// </summary>
        private void InjectConfig(AudioManager manager, AudioConfig config)
        {
            var field = typeof(AudioManager).GetField("config",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "AudioManager.config field should exist");
            field.SetValue(manager, config);
        }

        /// <summary>
        /// Inject an AudioClip into a private SerializeField on AudioConfig by name.
        /// </summary>
        private void InjectClipField(AudioConfig config, string fieldName, AudioClip clip)
        {
            var field = typeof(AudioConfig).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"AudioConfig.{fieldName} field should exist");
            field.SetValue(config, clip);
        }

        // --- AudioManager Config Volume Tests ---

        [Test]
        public void AudioManager_PlaySFX_WithConfig_AppliesSfxVolumeMultiplier()
        {
            var manager = CreateManager("AudioManager_SfxConfig");
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            InjectConfig(manager, config);

            var clip = AudioClip.Create("sfx_config_test", 44100, 1, 44100, false);
            var source = manager.PlaySFX(clip, 0.7f);

            // config.SfxVolume defaults to 0.8f, so volume = 0.7 * 0.8 = 0.56
            Assert.AreEqual(0.7f * config.SfxVolume, source.volume, 0.001f);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AudioManager_PlayLoop_WithConfig_AppliesAmbientVolumeMultiplier()
        {
            var manager = CreateManager("AudioManager_LoopConfig");
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            InjectConfig(manager, config);

            var clip = AudioClip.Create("loop_config_test", 44100, 1, 44100, false);
            var source = manager.PlayLoop(clip, 0.6f);

            // config.AmbientVolume defaults to 0.5f, so volume = 0.6 * 0.5 = 0.3
            Assert.AreEqual(0.6f * config.AmbientVolume, source.volume, 0.001f);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(manager.gameObject);
        }

        // --- BallAudioController Tests ---

        [Test]
        public void BallAudioController_HandleBallBounced_VolumeScalesWithSpeed()
        {
            // EditMode limitation: Start() doesn't fire, handler invoked directly via reflection.
            var manager = CreateManager("AudioManager_Bounce");
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            var bounceClip = AudioClip.Create("bounce_test", 44100, 1, 44100, false);
            InjectConfig(manager, config);
            InjectClipField(config, "ballBounce", bounceClip);

            var obj = new GameObject("BallAudio_Bounce");
            var controller = obj.AddComponent<BallAudioController>();

            var method = typeof(BallAudioController).GetMethod("HandleBallBounced",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "HandleBallBounced method should exist");

            // speed=10 => volume = Clamp01(10/20) * sfxVolume = 0.5 * 0.8 = 0.4
            method.Invoke(controller, new object[] { Vector3.zero, 10f });
            var pool = manager.transform;
            AudioSource found = null;
            for (int i = 0; i < pool.childCount; i++)
            {
                var src = pool.GetChild(i).GetComponent<AudioSource>();
                if (src != null && src.clip == bounceClip)
                {
                    found = src;
                    break;
                }
            }
            Assert.IsNotNull(found, "Should find a pool source with bounceClip");
            Assert.AreEqual(Mathf.Clamp01(10f / 20f) * config.SfxVolume, found.volume, 0.001f);

            // Reset for speed=0 test
            manager.StopSource(found);

            method.Invoke(controller, new object[] { Vector3.zero, 0f });
            found = null;
            for (int i = 0; i < pool.childCount; i++)
            {
                var src = pool.GetChild(i).GetComponent<AudioSource>();
                if (src != null && src.clip == bounceClip)
                {
                    found = src;
                    break;
                }
            }
            Assert.IsNotNull(found, "Should find a pool source for speed=0");
            Assert.AreEqual(0f, found.volume, 0.001f, "Volume should be 0 at speed=0");

            // Reset for speed=25 (above max) test
            manager.StopSource(found);

            method.Invoke(controller, new object[] { Vector3.zero, 25f });
            found = null;
            for (int i = 0; i < pool.childCount; i++)
            {
                var src = pool.GetChild(i).GetComponent<AudioSource>();
                if (src != null && src.clip == bounceClip)
                {
                    found = src;
                    break;
                }
            }
            Assert.IsNotNull(found, "Should find a pool source for speed=25");
            Assert.AreEqual(1f * config.SfxVolume, found.volume, 0.001f,
                "Volume should clamp to 1 * sfxVolume at speed > 20");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(manager.gameObject);
        }

        // --- AmbientAudioController Tests ---

        [Test]
        public void AmbientAudioController_HandleWindChanged_ScalesVolumeByWindMagnitude()
        {
            // EditMode limitation: Start() doesn't fire, handlers invoked directly via reflection.
            var manager = CreateManager("AudioManager_Wind");
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            var windClip = AudioClip.Create("wind_test", 44100, 1, 44100, false);
            InjectConfig(manager, config);
            InjectClipField(config, "windAmbience", windClip);

            var obj = new GameObject("AmbientAudio_Wind");
            var controller = obj.AddComponent<AmbientAudioController>();

            // Start wind to create windSource
            var startWind = typeof(AmbientAudioController).GetMethod("StartWind",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(startWind, "StartWind method should exist");
            startWind.Invoke(controller, null);

            // Verify windSource was created
            var windSourceField = typeof(AmbientAudioController).GetField("windSource",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(windSourceField, "windSource field should exist");
            var windSource = (AudioSource)windSourceField.GetValue(controller);
            Assert.IsNotNull(windSource, "windSource should be created after StartWind");

            // Invoke HandleWindChanged with magnitude 4 => normalized = 4/8 = 0.5
            var handleWind = typeof(AmbientAudioController).GetMethod("HandleWindChanged",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(handleWind, "HandleWindChanged method should exist");
            handleWind.Invoke(controller, new object[] { new Vector3(4f, 0f, 0f) });

            Assert.AreEqual(Mathf.Clamp01(4f / 8f) * config.AmbientVolume, windSource.volume, 0.001f);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AmbientAudioController_HandleShotScored_PlaysWhenWithinThreshold()
        {
            // EditMode limitation: Start() doesn't fire, handler invoked directly via reflection.
            var manager = CreateManager("AudioManager_CrowdPlay");
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            var crowdClip = AudioClip.Create("crowd_test", 44100, 1, 44100, false);
            InjectConfig(manager, config);
            InjectClipField(config, "crowdReaction", crowdClip);

            var obj = new GameObject("AmbientAudio_CrowdPlay");
            var controller = obj.AddComponent<AmbientAudioController>();

            var handleScored = typeof(AmbientAudioController).GetMethod("HandleShotScored",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(handleScored, "HandleShotScored method should exist");

            // DistanceToPin = 2f, threshold = 3f => should play
            var result = new ShotResult { DistanceToPin = 2f };
            handleScored.Invoke(controller, new object[] { result });

            bool foundCrowdClip = false;
            for (int i = 0; i < manager.transform.childCount; i++)
            {
                var src = manager.transform.GetChild(i).GetComponent<AudioSource>();
                if (src != null && src.clip == crowdClip)
                {
                    foundCrowdClip = true;
                    break;
                }
            }
            Assert.IsTrue(foundCrowdClip, "Crowd clip should play when within threshold");

            // Also test exact threshold boundary (DistanceToPin == 3f, uses <=)
            // Clear pool first
            for (int i = 0; i < manager.transform.childCount; i++)
            {
                var src = manager.transform.GetChild(i).GetComponent<AudioSource>();
                if (src != null)
                {
                    manager.StopSource(src);
                }
            }

            var boundaryResult = new ShotResult { DistanceToPin = 3f };
            handleScored.Invoke(controller, new object[] { boundaryResult });

            foundCrowdClip = false;
            for (int i = 0; i < manager.transform.childCount; i++)
            {
                var src = manager.transform.GetChild(i).GetComponent<AudioSource>();
                if (src != null && src.clip == crowdClip)
                {
                    foundCrowdClip = true;
                    break;
                }
            }
            Assert.IsTrue(foundCrowdClip, "Crowd clip should play at exact threshold (<=)");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AmbientAudioController_HandleShotScored_SkipsWhenBeyondThreshold()
        {
            // EditMode limitation: Start() doesn't fire, handler invoked directly via reflection.
            var manager = CreateManager("AudioManager_CrowdSkip");
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            var crowdClip = AudioClip.Create("crowd_skip_test", 44100, 1, 44100, false);
            InjectConfig(manager, config);
            InjectClipField(config, "crowdReaction", crowdClip);

            var obj = new GameObject("AmbientAudio_CrowdSkip");
            var controller = obj.AddComponent<AmbientAudioController>();

            var handleScored = typeof(AmbientAudioController).GetMethod("HandleShotScored",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(handleScored, "HandleShotScored method should exist");

            // DistanceToPin = 5f, threshold = 3f => should NOT play
            var result = new ShotResult { DistanceToPin = 5f };
            handleScored.Invoke(controller, new object[] { result });

            bool foundCrowdClip = false;
            for (int i = 0; i < manager.transform.childCount; i++)
            {
                var src = manager.transform.GetChild(i).GetComponent<AudioSource>();
                if (src != null && src.clip == crowdClip)
                {
                    foundCrowdClip = true;
                    break;
                }
            }
            Assert.IsFalse(foundCrowdClip,
                "Crowd clip should NOT play when beyond threshold");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void AmbientAudioController_GetAmbientVolume_ReturnsConfigValueWhenAvailable()
        {
            var manager = CreateManager("AudioManager_AmbientVol");
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            InjectConfig(manager, config);

            var obj = new GameObject("AmbientAudio_ConfigVol");
            var controller = obj.AddComponent<AmbientAudioController>();

            var method = typeof(AmbientAudioController).GetMethod("GetAmbientVolume",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "GetAmbientVolume method should exist");

            float vol = (float)method.Invoke(controller, null);
            Assert.AreEqual(config.AmbientVolume, vol, 0.001f,
                "GetAmbientVolume should return config.AmbientVolume when manager has config");

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(obj);
            Object.DestroyImmediate(manager.gameObject);
        }

    }
}
