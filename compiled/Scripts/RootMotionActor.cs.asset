using System.Collections;
using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Root motion. The animation component has Apply Root Motion on, so the clips' root bone motion
    /// moves the entity itself: the climb lifts the character onto the box, the dashes and the
    /// knockback carry it along the lane - no script moves it. This script only plays the states in
    /// turn (CrossFade) and puts the character back at its start between performances.
    /// </summary>
    [ScriptSourceFile]
    public class RootMotionActor : ScriptComponent
    {
        [Tooltip("States played in turn, separated by '|'.")]
        public string Sequence = "ClimbUp";

        [Tooltip("Rest between performances (s).")]
        public float Pause = 1.6f;

        [Tooltip("Particle emitter (world space) for the reset puff.")]
        public Entity Puff;

        public string Current { get; private set; } = "";
        public float Travelled { get; private set; }

        private AnimationComponent animation;
        private ParticleEmitterComponent puff;
        private Vector3 home;
        private Quaternion homeRotation;

        public override void OnStart()
        {
            animation = owner.GetComponent<AnimationComponent>();
            puff = Puff.IsValid() ? Puff.GetComponent<ParticleEmitterComponent>() : null;
            home = owner.transform.position;
            homeRotation = owner.transform.rotation;
            StartCoroutine(Perform());
        }

        private IEnumerator Perform()
        {
            string[] states = Sequence.Split('|');
            int index = 0;
            while (true)
            {
                yield return new WaitForSeconds(Pause);
                Current = states[index];
                Vector3 start = owner.transform.position;
                animation.CrossFade(Current, 0.15f);
                yield return new WaitForSeconds(0.3f);
                yield return new WaitUntil(() => animation.IsInState("Idle") && !animation.IsInTransition());
                Travelled = Vector3.Distance(start, owner.transform.position);
                yield return new WaitForSeconds(Pause);
                Reset();
                index = (index + 1) % states.Length;
            }
        }

        private void Reset()
        {
            if (puff != null)
            {
                for (int i = 0; i < 24; ++i)
                {
                    var direction = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                    puff.Emit(1, new EmitParams
                    {
                        position = owner.transform.position + new Vector3(0.0f, 0.8f, 0.0f),
                        velocity = direction * 1.2f,
                        color = new Color(0.85f, 0.85f, 0.95f, 0.6f),
                        size = 0.3f,
                        lifetime = 0.7f,
                    });
                }
            }
            owner.transform.position = home;
            owner.transform.rotation = homeRotation;
            Current = "";
        }
    }
}
