using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Walks a character back and forth along waypoints (the children of Path) over uneven ground,
    /// keeping its root on the ground with a ray cast and feeding Speed to its walk blend space. Its
    /// Foot IK (when it has one and it is enabled) then plants each foot on the steps and the slope.
    /// </summary>
    [ScriptSourceFile]
    public class PathWalker : ScriptComponent
    {
        [Tooltip("Entity whose children are the waypoints, in order.")]
        public Entity Path;

        [Tooltip("Walking speed (m/s); matches the walk clip so the feet do not slide.")]
        public float Speed = 1.35f;

        [Tooltip("Pause at each end (s).")]
        public float Pause = 1.2f;

        private AnimationComponent animation;
        private Entity[] points;
        private int target = 1;
        private int step = 1;
        private float wait;
        private float currentSpeed;

        public override void OnStart()
        {
            animation = owner.GetComponent<AnimationComponent>();
            points = Path.IsValid() ? Path.transform.children : new Entity[0];
            if (points.Length > 0)
            {
                owner.transform.position = points[0].transform.position;
            }
        }

        public override void OnUpdate()
        {
            if (points.Length < 2)
            {
                return;
            }
            float dt = Time.deltaTime;
            Vector3 position = owner.transform.position;
            Vector3 goal = points[target].transform.position;
            Vector3 to = StationRegistry.Flat(goal - position);
            bool waiting = wait > 0.0f;
            if (waiting)
            {
                wait -= dt;
            }
            currentSpeed = Mathf.MoveTowards(currentSpeed, waiting ? 0.0f : Speed, dt * 2.5f);
            if (!waiting)
            {
                if (to.magnitude < 0.08f)
                {
                    if (target == points.Length - 1 || target == 0)
                    {
                        step = -step;
                        wait = Pause;
                    }
                    target += step;
                }
                else
                {
                    Quaternion wanted = Quaternion.LookRotation(to.normalized);
                    owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, wanted, 1.0f - Mathf.Exp(-dt * 5.0f));
                }
            }
            Vector3 forward = owner.transform.forward;
            position += StationRegistry.Flat(forward).normalized * currentSpeed * dt;
            RaycastHit? hit = Physics.Raycast(position + new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f), 3.0f);
            if (hit.HasValue)
            {
                position.y = Mathf.Lerp(position.y, hit.Value.point.y, 1.0f - Mathf.Exp(-dt * 12.0f));
            }
            owner.transform.position = position;
            animation?.SetFloat("Speed", currentSpeed * 1.6f / Speed);
        }
    }
}
