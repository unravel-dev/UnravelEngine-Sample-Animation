using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// A sparring fighter driven only through its controller's parameters (Sparring.actrl): triggers for
    /// attacks, dodges and hits, a Block bool, a Dead bool. The state machine does the rest - Any State
    /// transitions into Hit and Death, a guard that turns hits into block reactions, exit times back to
    /// Idle. Damage lands on the attack clips' "Hit" events; the AI reads the opponent's state tags
    /// (Guard, Dodge, Down) to decide what the blow did.
    /// </summary>
    [ScriptSourceFile]
    public class Duelist : ScriptComponent
    {
        public Entity Opponent;

        [Tooltip("Name shown in the HUD.")]
        public string DisplayName = "Fighter";

        [Tooltip("Particle emitter (world space) for the sparks.")]
        public Entity Sparks;

        [Tooltip("Reach of a blow (m).")]
        public float Reach = 2.4f;

        public int Health { get; private set; } = MaxHealth;
        public string LastResult { get; private set; } = "";

        private const int MaxHealth = 100;

        private AnimationComponent animation;
        private Duelist rival;
        private ParticleEmitterComponent sparks;
        private AudioClip fleshSound;
        private AudioClip blockSound;
        private float thinkTimer;
        private float blockTimer;
        private float downTimer;

        public override void OnStart()
        {
            animation = owner.GetComponent<AnimationComponent>();
            rival = Opponent.IsValid() ? Opponent.GetComponent<Duelist>() : null;
            sparks = Sparks.IsValid() ? Sparks.GetComponent<ParticleEmitterComponent>() : null;
            fleshSound = Assets.GetAsset<AudioClip>("app:/data/Audio/hit.wav");
            blockSound = Assets.GetAsset<AudioClip>("app:/data/Audio/hit_metal.wav");
            thinkTimer = Random.Range(0.5f, 1.5f);
        }

        public override void OnUpdate()
        {
            float dt = Time.deltaTime;
            FaceOpponent(dt);
            if (animation.GetBool("Dead"))
            {
                downTimer += dt;
                if (downTimer > 3.5f)
                {
                    downTimer = 0.0f;
                    Health = MaxHealth;
                    animation.SetBool("Dead", false);
                }
                return;
            }
            if (blockTimer > 0.0f)
            {
                blockTimer -= dt;
                if (blockTimer <= 0.0f)
                {
                    animation.SetBool("Block", false);
                }
                return;
            }
            thinkTimer -= dt;
            if (thinkTimer > 0.0f || !animation.IsInState("Idle") || rival == null || rival.IsDown())
            {
                return;
            }
            thinkTimer = Random.Range(0.6f, 1.5f);
            float roll = Random.Range(0.0f, 1.0f);
            if (roll < 0.55f)
            {
                animation.SetInt("AttackKind", Random.Range(0, 3));
                animation.SetTrigger("Attack");
            }
            else if (roll < 0.8f)
            {
                animation.SetBool("Block", true);
                blockTimer = Random.Range(1.0f, 2.0f);
            }
            else
            {
                animation.SetInt("DodgeDir", Random.Range(0, 3));
                animation.SetTrigger("Dodge");
            }
        }

        public bool IsDown()
        {
            return animation.IsInTag("Down");
        }

        /// <summary>Damage after the guard and dodge checks; returns what the blow did.</summary>
        public string ReceiveBlow(int damage, Vector3 contact)
        {
            if (animation.IsInTag("Dodge"))
            {
                return "dodged";
            }
            if (animation.IsInTag("Guard"))
            {
                animation.SetTrigger("Hit");
                Audio.PlayOneShot(blockSound, contact, 0.8f, Random.Range(0.95f, 1.05f));
                Spark(contact, new Color(0.75f, 0.85f, 1.0f, 1.0f), 30);
                return "blocked";
            }
            if (IsDown())
            {
                return "missed";
            }
            Health -= damage;
            Audio.PlayOneShot(fleshSound, contact, 0.8f, Random.Range(0.9f, 1.1f));
            Spark(contact, new Color(1.0f, 0.6f, 0.3f, 1.0f), 16);
            if (Health <= 0)
            {
                Health = 0;
                animation.SetBool("Block", false);
                animation.SetBool("Dead", true);
                return "knocked out";
            }
            animation.SetTrigger("Hit");
            return "hit (" + Health + " hp left)";
        }

        public override void OnAnimationEvent(AnimationEvent animationEvent)
        {
            EventLog.Add(DisplayName, animationEvent, owner.transform.position);
            if (animationEvent.name != "Hit" || rival == null)
            {
                return;
            }
            Vector3 toRival = Opponent.transform.position - owner.transform.position;
            if (StationRegistry.Flat(toRival).magnitude > Reach)
            {
                LastResult = "out of reach";
                return;
            }
            Vector3 contact = Vector3.Lerp(owner.transform.position, Opponent.transform.position, 0.6f) + new Vector3(0.0f, 1.2f, 0.0f);
            int damage = (int)(18.0f * Mathf.Max(animationEvent.floatParameter, 0.5f));
            LastResult = animationEvent.stringParameter + ": " + rival.ReceiveBlow(damage, contact);
        }

        private void FaceOpponent(float dt)
        {
            if (!Opponent.IsValid() || IsDown())
            {
                return;
            }
            Vector3 to = StationRegistry.Flat(Opponent.transform.position - owner.transform.position);
            if (to.sqrMagnitude < 1e-4f)
            {
                return;
            }
            Quaternion wanted = Quaternion.LookRotation(to.normalized);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, wanted, 1.0f - Mathf.Exp(-dt * 6.0f));
        }

        private void Spark(Vector3 position, Color color, int count)
        {
            if (sparks == null)
            {
                return;
            }
            for (int i = 0; i < count; ++i)
            {
                var direction = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-0.3f, 1.0f), Random.Range(-1.0f, 1.0f));
                sparks.Emit(1, new EmitParams
                {
                    position = position,
                    velocity = direction * Random.Range(1.5f, 3.5f),
                    color = color,
                    size = Random.Range(0.03f, 0.06f),
                    lifetime = Random.Range(0.25f, 0.55f),
                });
            }
        }
    }
}
