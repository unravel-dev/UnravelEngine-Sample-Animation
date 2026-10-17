using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Footstep and landing feedback from animation events: a puff of dust under the foot the event
    /// names and a footstep sound, both scaled by the event's weight (a fading clip steps softer).
    /// Put it on any character whose clips carry Footstep / Land events.
    /// </summary>
    [ScriptSourceFile]
    public class FootstepEffects : ScriptComponent
    {
        [Tooltip("Name shown in the HUD event feed.")]
        public string DisplayName = "Character";

        [Tooltip("Bone the 'left' footstep puffs at.")]
        public string LeftFootBone = "foot_l";

        [Tooltip("Bone the 'right' footstep puffs at.")]
        public string RightFootBone = "foot_r";

        [Tooltip("Particle emitter (world space) the dust is emitted from.")]
        public Entity Dust;

        [Tooltip("Footstep volume at full weight.")]
        public float Volume = 0.55f;

        [Tooltip("List the events in the HUD feed.")]
        public bool LogEvents = true;

        private static readonly string[] StepSounds =
        {
            "app:/data/Audio/footstep_0.wav", "app:/data/Audio/footstep_1.wav",
            "app:/data/Audio/footstep_2.wav", "app:/data/Audio/footstep_3.wav",
        };

        private Entity leftFoot;
        private Entity rightFoot;
        private ParticleEmitterComponent dustEmitter;
        private AudioClip[] steps;
        private AudioClip landing;
        private int stepIndex;

        public override void OnStart()
        {
            leftFoot = owner.transform.FindChild(LeftFootBone, true);
            rightFoot = owner.transform.FindChild(RightFootBone, true);
            if (Dust.IsValid())
            {
                dustEmitter = Dust.GetComponent<ParticleEmitterComponent>();
            }
            steps = new AudioClip[StepSounds.Length];
            for (int i = 0; i < StepSounds.Length; ++i)
            {
                steps[i] = Assets.GetAsset<AudioClip>(StepSounds[i]);
            }
            landing = Assets.GetAsset<AudioClip>("app:/data/Audio/land.wav");
        }

        public override void OnAnimationEvent(AnimationEvent animationEvent)
        {
            if (animationEvent.name == "Footstep")
            {
                Step(animationEvent.stringParameter, animationEvent.weight, false);
            }
            else if (animationEvent.name == "Land")
            {
                Step("both", 1.0f, true);
            }
            else
            {
                return;
            }
            if (LogEvents)
            {
                EventLog.Add(DisplayName, animationEvent, owner.transform.position);
            }
        }

        private void Step(string side, float weight, bool heavy)
        {
            Vector3 position = owner.transform.position;
            if (side == "left" && leftFoot.IsValid())
            {
                position = leftFoot.transform.position;
            }
            else if (side == "right" && rightFoot.IsValid())
            {
                position = rightFoot.transform.position;
            }
            position.y = owner.transform.position.y + 0.03f;
            if (dustEmitter != null)
            {
                int count = heavy ? 18 : 6;
                for (int i = 0; i < count; ++i)
                {
                    var spread = new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
                    dustEmitter.Emit(1, new EmitParams
                    {
                        position = position + spread * 0.06f,
                        velocity = spread * (heavy ? 0.9f : 0.45f) + new Vector3(0.0f, Random.Range(0.15f, 0.4f), 0.0f),
                        color = new Color(0.78f, 0.74f, 0.66f, 0.55f * weight),
                        size = heavy ? 0.32f : 0.2f,
                        lifetime = Random.Range(0.6f, 1.0f),
                    });
                }
            }
            AudioClip clip = heavy ? landing : steps[stepIndex++ % steps.Length];
            Audio.PlayOneShot(clip, position, Volume * Mathf.Clamp01(weight) * (heavy ? 1.4f : 1.0f), Random.Range(0.92f, 1.08f));
        }
    }
}
