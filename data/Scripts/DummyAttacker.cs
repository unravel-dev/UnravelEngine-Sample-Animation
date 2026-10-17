using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Events as hit frames. The rogue attacks the dummy in a loop; each attack clip carries a "Hit"
    /// event on the frame the blade connects (its float parameter is the blow's strength), and only
    /// then do the sparks, the sound and the dummy's reaction happen - whatever the clip's speed.
    /// </summary>
    [ScriptSourceFile]
    public class DummyAttacker : ScriptComponent
    {
        public Entity Dummy;

        [Tooltip("Particle emitter (world space) for the sparks.")]
        public Entity Sparks;

        [Tooltip("Seconds between attacks.")]
        public float Interval = 1.9f;

        public string LastAttack { get; private set; } = "";

        private AnimationComponent animation;
        private TrainingDummy dummy;
        private ParticleEmitterComponent sparks;
        private AudioClip hitSound;
        private AudioClip[] swingSounds;
        private float timer;
        private int attack;

        public override void OnStart()
        {
            animation = owner.GetComponent<AnimationComponent>();
            dummy = Dummy.IsValid() ? Dummy.GetComponent<TrainingDummy>() : null;
            sparks = Sparks.IsValid() ? Sparks.GetComponent<ParticleEmitterComponent>() : null;
            hitSound = Assets.GetAsset<AudioClip>("app:/data/Audio/hit.wav");
            swingSounds = new[] { Assets.GetAsset<AudioClip>("app:/data/Audio/swing_0.wav"), Assets.GetAsset<AudioClip>("app:/data/Audio/swing_1.wav") };
        }

        public override void OnUpdate()
        {
            timer += Time.deltaTime;
            if (timer >= Interval && animation.IsInState("Idle"))
            {
                timer = 0.0f;
                attack = (attack + 1) % 3;
                animation.SetInt("AttackKind", attack);
                animation.SetTrigger("Attack");
                Audio.PlayOneShot(swingSounds[attack % 2], owner.transform.position, 0.45f, 1.1f);
            }
        }

        public override void OnAnimationEvent(AnimationEvent animationEvent)
        {
            EventLog.Add("Rogue", animationEvent, owner.transform.position);
            if (animationEvent.name != "Hit" || dummy == null)
            {
                return;
            }
            LastAttack = animationEvent.stringParameter;
            Vector3 direction = StationRegistry.Flat(Dummy.transform.position - owner.transform.position).normalized;
            dummy.Impact(direction, Mathf.Max(animationEvent.floatParameter, 0.5f));
            Vector3 contact = Dummy.transform.position + new Vector3(0.0f, 1.1f, 0.0f) - direction * 0.25f;
            Audio.PlayOneShot(hitSound, contact, 0.8f, Random.Range(0.9f, 1.1f));
            if (sparks == null)
            {
                return;
            }
            for (int i = 0; i < 24; ++i)
            {
                var jitter = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-0.2f, 1.2f), Random.Range(-1.0f, 1.0f));
                sparks.Emit(1, new EmitParams
                {
                    position = contact,
                    velocity = (jitter - direction) * Random.Range(1.5f, 3.5f),
                    color = new Color(1.0f, 0.82f, 0.45f, 1.0f),
                    size = Random.Range(0.03f, 0.06f),
                    lifetime = Random.Range(0.25f, 0.6f),
                });
            }
        }
    }
}
