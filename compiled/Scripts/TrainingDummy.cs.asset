using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// A training dummy that rocks back when struck: a damped spring on its pivot's tilt, pushed by
    /// <see cref="Impact"/>.
    /// </summary>
    [ScriptSourceFile]
    public class TrainingDummy : ScriptComponent
    {
        [Tooltip("Spring stiffness of the rocking (1/s^2).")]
        public float Stiffness = 90.0f;

        [Tooltip("Damping of the rocking (1/s).")]
        public float Damping = 5.0f;

        public int Hits { get; private set; }

        private Vector2 tilt;
        private Vector2 tiltVelocity;

        /// <summary>Strikes the dummy from <paramref name="direction"/> (world, horizontal).</summary>
        public void Impact(Vector3 direction, float strength)
        {
            Vector3 local = owner.transform.InverseTransformDirection(direction);
            tiltVelocity += new Vector2(local.z, -local.x) * 160.0f * strength;
            Hits++;
        }

        public override void OnUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            Vector2 acceleration = -tilt * Stiffness - tiltVelocity * Damping;
            tiltVelocity += acceleration * dt;
            tilt += tiltVelocity * dt;
            tilt = Vector2.ClampMagnitude(tilt, 35.0f);
            owner.transform.localRotation = Quaternion.Euler(tilt.x, 0.0f, tilt.y);
        }
    }
}
