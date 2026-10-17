using Unravel.Core;

namespace AnimationSample
{
    /// <summary>Swings its entity around its local X or Z axis like a pendulum.</summary>
    [ScriptSourceFile]
    public class Pendulum : ScriptComponent
    {
        [Tooltip("Largest swing (degrees).")]
        public float Amplitude = 40.0f;

        [Tooltip("Swings per second.")]
        public float Frequency = 0.35f;

        [Tooltip("Swing around local Z instead of local X.")]
        public bool AroundZ;

        [Tooltip("Phase offset (0..1).")]
        public float Phase;

        private float time;

        public override void OnUpdate()
        {
            time += Time.deltaTime;
            float angle = Mathf.Sin((time * Frequency + Phase) * Mathf.PI * 2.0f) * Amplitude;
            owner.transform.localRotation = AroundZ ? Quaternion.Euler(0.0f, 0.0f, angle) : Quaternion.Euler(angle, 0.0f, 0.0f);
        }
    }
}
