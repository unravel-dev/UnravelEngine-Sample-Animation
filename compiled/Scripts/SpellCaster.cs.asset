using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Curves and events driving gameplay. The mage's controller runs raise - charge - release -
    /// channel on a Cast trigger; every one of those clips carries a "Glow" curve, read each frame with
    /// GetCurve to drive the staff light and the orb's emission, and the release clip's "Cast" event
    /// fires the burst and the sound at the exact frame the staff swings.
    /// </summary>
    [ScriptSourceFile]
    public class SpellCaster : ScriptComponent
    {
        [Tooltip("Seconds between casts.")]
        public float Interval = 7.0f;

        [Tooltip("Light at the staff tip, driven by the Glow curve.")]
        public Entity StaffLight;

        [Tooltip("Glowing orb at the staff tip (emissive material instance).")]
        public Entity Orb;

        [Tooltip("Particle emitter (world space) for the cast burst.")]
        public Entity Burst;

        [Tooltip("Light intensity at Glow 1.")]
        public float MaxIntensity = 8.0f;

        /// <summary>Last Glow value, for the station's status line.</summary>
        public float Glow { get; private set; }

        private AnimationComponent animation;
        private LightComponent staffLight;
        private ModelComponent orbModel;
        private ParticleEmitterComponent burst;
        private AudioClip castSound;
        private float timer;

        public override void OnStart()
        {
            animation = owner.GetComponent<AnimationComponent>();
            staffLight = StaffLight.IsValid() ? StaffLight.GetComponent<LightComponent>() : null;
            orbModel = Orb.IsValid() ? Orb.GetComponent<ModelComponent>() : null;
            burst = Burst.IsValid() ? Burst.GetComponent<ParticleEmitterComponent>() : null;
            castSound = Assets.GetAsset<AudioClip>("app:/data/Audio/cast.wav");
            timer = Interval * 0.5f;
        }

        public override void OnUpdate()
        {
            timer += Time.deltaTime;
            if (timer >= Interval && animation.IsInState("Idle"))
            {
                timer = 0.0f;
                animation.SetTrigger("Cast");
            }
            float target = animation.GetCurve("Glow");
            Glow = Mathf.Lerp(Glow, target, 1.0f - Mathf.Exp(-Time.deltaTime * 12.0f));
            if (staffLight != null)
            {
                staffLight.intensity = Glow * MaxIntensity;
            }
            if (orbModel != null)
            {
                orbModel.SetFloat(MaterialProperty.EmissiveIntensity, 0.4f + Glow * 9.0f);
            }
        }

        public override void OnAnimationEvent(AnimationEvent animationEvent)
        {
            EventLog.Add("Mage", animationEvent, owner.transform.position);
            if (animationEvent.name != "Cast")
            {
                return;
            }
            Vector3 origin = Orb.IsValid() ? Orb.transform.position : owner.transform.position + new Vector3(0, 1.5f, 0);
            Audio.PlayOneShot(castSound, origin, 0.8f);
            if (burst == null)
            {
                return;
            }
            Vector3 forward = owner.transform.forward;
            for (int i = 0; i < 60; ++i)
            {
                var direction = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-0.6f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;
                burst.Emit(1, new EmitParams
                {
                    position = origin,
                    velocity = direction * Random.Range(1.0f, 3.5f) + forward * 2.5f,
                    color = new Color(0.55f, 0.75f, 1.0f, 1.0f),
                    size = Random.Range(0.06f, 0.14f),
                    lifetime = Random.Range(0.5f, 1.1f),
                });
            }
        }
    }
}
