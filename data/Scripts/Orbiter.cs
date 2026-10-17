using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Moves its entity on a figure-eight (or a circle) around where it starts, with an optional bob -
    /// moving targets for the IK exhibits and the swinging spring-bone collider.
    /// </summary>
    [ScriptSourceFile]
    public class Orbiter : ScriptComponent
    {
        [Tooltip("Half extents of the path along local X, Y and Z (m).")]
        public Vector3 Extents = new Vector3(0.6f, 0.25f, 0.4f);

        [Tooltip("Loops per second.")]
        public float Frequency = 0.15f;

        [Tooltip("Figure-eight instead of an ellipse.")]
        public bool FigureEight = true;

        [Tooltip("Phase offset (0..1).")]
        public float Phase;

        private Vector3 origin;
        private float time;

        public override void OnStart()
        {
            origin = owner.transform.localPosition;
        }

        public override void OnUpdate()
        {
            time += Time.deltaTime;
            float a = (time * Frequency + Phase) * Mathf.PI * 2.0f;
            float z = FigureEight ? Mathf.Sin(a * 2.0f) : Mathf.Sin(a);
            var offset = new Vector3(Mathf.Cos(a) * Extents.x, Mathf.Sin(a * 3.0f) * Extents.y, z * Extents.z);
            owner.transform.localPosition = origin + offset;
        }
    }
}
