using UnityEngine;

namespace GolfGame.Golf
{
    /// <summary>
    /// Adds TrailRenderer (airborne only) and ParticleSystem burst (on landing)
    /// to the ball GameObject. Subscribes to BallController events.
    /// </summary>
    public class BallVisualEffects : MonoBehaviour
    {
        private BallController ballController;
        private TrailRenderer trail;
        private ParticleSystem landingParticles;

        private void Awake()
        {
            ballController = GetComponent<BallController>();
            trail = CreateTrail();
            landingParticles = CreateLandingParticles();
        }

        private void Start()
        {
            if (ballController != null)
            {
                ballController.OnBallLaunched += HandleLaunched;
                ballController.OnBallLanded += HandleLanded;
            }
        }

        private void OnDestroy()
        {
            if (ballController != null)
            {
                ballController.OnBallLaunched -= HandleLaunched;
                ballController.OnBallLanded -= HandleLanded;
            }
        }

        private void HandleLaunched()
        {
            trail.Clear();
            trail.emitting = true;
        }

        private void HandleLanded(Vector3 pos)
        {
            trail.emitting = false;
            landingParticles.transform.position = pos;
            landingParticles.Play();
        }

        private TrailRenderer CreateTrail()
        {
            var tr = gameObject.AddComponent<TrailRenderer>();
            tr.time = 0.4f;
            tr.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(1f, 0f)
            );

            // White gradient: start alpha 0.7, end alpha 0.0
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            tr.colorGradient = gradient;

            // Unlit material
            tr.material = GetParticleMaterial();
            tr.emitting = false;

            return tr;
        }

        private ParticleSystem CreateLandingParticles()
        {
            var particleObj = new GameObject("LandingParticles");
            particleObj.transform.SetParent(transform.parent);
            particleObj.transform.position = transform.position;

            var ps = particleObj.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.gravityModifier = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;

            // Two-color random between grass green and dirt brown
            var grassColor = new Color(0.298f, 0.686f, 0.314f); // #4CAF50
            var dirtColor = new Color(0.553f, 0.431f, 0.388f);  // #8D6E63
            main.startColor = new ParticleSystem.MinMaxGradient(grassColor, dirtColor);

            // Emission: single burst of 20
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            // Shape: hemisphere
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.3f;

            // Color over lifetime: alpha fade
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var alphaGradient = new Gradient();
            alphaGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = alphaGradient;

            // Renderer: billboard, unlit material
            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetParticleMaterial();

            ps.Stop();
            return ps;
        }

        private static Material GetParticleMaterial()
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            return mat;
        }
    }
}
