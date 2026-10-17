using System.Collections.Generic;
using Unravel.Core;

namespace AnimationSample
{
    /// <summary>The data model "crowd" of UI/CrowdHud.rhtml.</summary>
    public sealed class CrowdModel
    {
        public bool helpShown = true;
        public string count = "";
        public string frame = "";
        public string rigs = "";
    }

    /// <summary>
    /// The animation scaling benchmark. Spawns a grid of crowd characters, each on the full Hero
    /// controller (2D locomotion blend, masked upper-body layer, additive reactions) with foot and
    /// look-at IK, and keeps every one busy with its own speed, crouch, aim, sword combos, rolls,
    /// jumps and hit reactions, so the profiler's Animation and Rig scopes show what a character
    /// costs. Rolls and sword combos move a character by root motion; it walks back to its grid slot
    /// afterwards. Up / Down double or halve the crowd, Left / Right change it by CountStep, I turns the
    /// IK rigs on or off and F1 the help. SetCount and SetRigs do the same for tools.
    /// </summary>
    [ScriptSourceFile]
    public class CrowdBenchmark : ScriptComponent
    {
        [Tooltip("The crowd character: an animated model with Foot IK, and Look At IK on its head.")]
        public Prefab Character;

        [Tooltip("The camera that frames the crowd.")]
        public Entity Camera;

        [Tooltip("What every head looks at; it sweeps in front of the crowd.")]
        public Entity LookTarget;

        [Tooltip("Characters spawned on start.")]
        public int StartCount = 100;

        [Tooltip("Largest crowd.")]
        public int MaxCount = 1600;

        [Tooltip("Characters added or removed by Right / Left.")]
        public int CountStep = 25;

        [Tooltip("Distance between neighbours on the grid (m).")]
        public float Spacing = 1.6f;

        [Tooltip("Foot and look-at IK on every character.")]
        public bool RigsEnabled = true;

        [Tooltip("Seconds between a character's decisions: random between X and Y.")]
        public Vector2 DecisionInterval = new Vector2(1.5f, 4.5f);

        private const float MAX_SPEED = 6.5f;
        private const float MIN_MOVING_SPEED = 0.8f;
        private const float IDLE_CHANCE = 0.25f;
        /// Parameter change per second, so a new speed or crouch blends in instead of popping.
        private const float SPEED_RATE = 4.0f;
        private const float CROUCH_RATE = 2.5f;
        private const int MAX_COMBO_HITS = 4;
        private const float COMBO_INTERVAL = 0.45f;
        private const float MIN_DANCE_SECONDS = 3.0f;
        private const float MAX_DANCE_SECONDS = 6.0f;
        private const float YAW_JITTER_DEGREES = 20.0f;
        private const float CAMERA_PITCH_DEGREES = 40.0f;
        private const float MIN_CAMERA_DISTANCE = 7.0f;
        private const float CAMERA_MARGIN = 4.0f;
        private const float LOOK_HEIGHT = 1.6f;
        private const float LOOK_BOB = 0.5f;
        private const float LOOK_DISTANCE_AHEAD = 2.5f;
        private const float LOOK_SWEEP_RATE = 0.35f;
        private const float LOOK_BOB_RATE = 1.1f;
        private const float FRAME_SMOOTHING = 0.05f;
        private const float HUD_INTERVAL = 0.25f;
        /// A character a root-motion move (roll, sword combo) carried further than this walks back.
        private const float RETURN_DISTANCE = 0.5f;
        private const float RETURN_SPEED = 1.6f;
        private const float ARRIVE_DISTANCE = 0.2f;
        private const float TURN_RATE = 5.0f;
        private const float FACING_TOLERANCE_DEGREES = 3.0f;

        private sealed class Agent
        {
            public Entity entity;
            public AnimationComponent animation;
            public FootIKComponent foot;
            public LookAtIKComponent look;
            public float decisionTimer;
            public float speed;
            public float targetSpeed;
            public float crouch;
            public float targetCrouch;
            public int attacksQueued;
            public float attackTimer;
            public float danceTimer;
            public bool aiming;
            /// The grid slot and facing the character walks back to after a move carried it away.
            public Vector3 home;
            public Quaternion homeRotation;
            public bool returning;
        }

        private readonly List<Agent> agents = new List<Agent>();
        private readonly CrowdModel data = new CrowdModel();
        private UIDataModel model;
        private float gridWidth;
        private float lookTime;
        private float smoothedFrame;
        private float hudTimer;

        public override void OnStart()
        {
            var document = owner.GetComponent<UIDocumentComponent>();
            if (document != null)
            {
                model = document.GetModel("crowd");
                model.Bind(data);
            }
            SetCount(StartCount);
        }

        public override void OnUpdate()
        {
            float dt = Time.deltaTime;
            HandleKeys();
            foreach (var agent in agents)
            {
                UpdateAgent(agent, dt);
            }
            MoveLookTarget(dt);
            UpdateHud(dt);
        }

        /// <summary>Spawns or removes characters until the crowd has `count` (0 to MaxCount); returns the new size.</summary>
        public int SetCount(int count)
        {
            count = Mathf.Clamp(count, 0, MaxCount);
            while (agents.Count < count && Spawn())
            {
            }
            while (agents.Count > count)
            {
                Scene.DestroyEntity(agents[agents.Count - 1].entity);
                agents.RemoveAt(agents.Count - 1);
            }
            LayOut();
            RefreshHud();
            return agents.Count;
        }

        /// <summary>Turns foot and look-at IK on or off for the whole crowd.</summary>
        public bool SetRigs(bool enabled)
        {
            RigsEnabled = enabled;
            foreach (var agent in agents)
            {
                ApplyRig(agent);
            }
            RefreshHud();
            return RigsEnabled;
        }

        /// <summary>The crowd size, the rig state and the smoothed frame time, for tools.</summary>
        public string GetStatus()
        {
            return string.Format("{0} characters, rigs {1}, {2:0.00} ms per frame", agents.Count, RigsEnabled ? "on" : "off",
                smoothedFrame * 1000.0f);
        }

        private void HandleKeys()
        {
            if (Input.IsPressed(KeyCode.F1))
            {
                data.helpShown = !data.helpShown;
                model?.Dirty("helpShown");
            }
            if (Input.IsPressed(KeyCode.Up))
            {
                SetCount(agents.Count == 0 ? CountStep : agents.Count * 2);
            }
            if (Input.IsPressed(KeyCode.Down))
            {
                SetCount(agents.Count / 2);
            }
            if (Input.IsPressed(KeyCode.Right))
            {
                SetCount(agents.Count + CountStep);
            }
            if (Input.IsPressed(KeyCode.Left))
            {
                SetCount(agents.Count - CountStep);
            }
            if (Input.IsPressed(KeyCode.I))
            {
                SetRigs(!RigsEnabled);
            }
        }

        private bool Spawn()
        {
            Entity entity = Scene.Instantiate(Character);
            if (!entity.IsValid())
            {
                Log.Error("CrowdBenchmark: the Character prefab did not instantiate.");
                return false;
            }
            var agent = new Agent
            {
                entity = entity,
                animation = entity.GetComponent<AnimationComponent>(),
                foot = entity.GetComponent<FootIKComponent>(),
                look = entity.GetComponentInChildren<LookAtIKComponent>(),
                decisionTimer = Random.Range(0.0f, DecisionInterval.y),
            };
            if (agent.look != null)
            {
                agent.look.target = LookTarget;
            }
            ApplyRig(agent);
            agents.Add(agent);
            return true;
        }

        private void ApplyRig(Agent agent)
        {
            float weight = RigsEnabled ? 1.0f : 0.0f;
            if (agent.foot != null)
            {
                agent.foot.weight = weight;
            }
            if (agent.look != null)
            {
                agent.look.weight = weight;
            }
        }

        /// <summary>Places the crowd on a square grid facing the camera and frames it.</summary>
        private void LayOut()
        {
            int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(agents.Count)));
            int rows = Mathf.Max(1, Mathf.CeilToInt(agents.Count / (float)columns));
            for (int i = 0; i < agents.Count; ++i)
            {
                int column = i % columns;
                int row = i / columns;
                float x = (column - (columns - 1) * 0.5f) * Spacing;
                // A fixed spread per slot, so neighbours do not all face exactly the same way.
                float yaw = 180.0f + ((i * 37) % 41 - 20) / 20.0f * YAW_JITTER_DEGREES;
                var agent = agents[i];
                agent.home = new Vector3(x, 0.0f, row * Spacing);
                agent.homeRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
                agent.returning = false;
                agent.entity.transform.position = agent.home;
                agent.entity.transform.rotation = agent.homeRotation;
            }
            gridWidth = (columns - 1) * Spacing;
            FrameCamera(gridWidth, (rows - 1) * Spacing);
        }

        private void FrameCamera(float width, float depth)
        {
            if (!Camera.IsValid())
            {
                return;
            }
            float pitch = CAMERA_PITCH_DEGREES * Mathf.Deg2Rad;
            float distance = Mathf.Max(MIN_CAMERA_DISTANCE, Mathf.Max(width, depth) + CAMERA_MARGIN);
            var center = new Vector3(0.0f, 1.0f, depth * 0.5f);
            Camera.transform.position = center + new Vector3(0.0f, Mathf.Sin(pitch), -Mathf.Cos(pitch)) * distance;
            Camera.transform.rotation = Quaternion.Euler(CAMERA_PITCH_DEGREES, 0.0f, 0.0f);
        }

        private void UpdateAgent(Agent agent, float dt)
        {
            agent.decisionTimer -= dt;
            if (agent.decisionTimer <= 0.0f)
            {
                Think(agent);
                agent.decisionTimer = Random.Range(DecisionInterval.x, DecisionInterval.y);
            }
            if (agent.returning)
            {
                WalkHome(agent, dt);
            }
            if (agent.speed != agent.targetSpeed)
            {
                agent.speed = Mathf.MoveTowards(agent.speed, agent.targetSpeed, SPEED_RATE * dt);
                agent.animation.SetFloat("Speed", agent.speed);
            }
            if (agent.crouch != agent.targetCrouch)
            {
                agent.crouch = Mathf.MoveTowards(agent.crouch, agent.targetCrouch, CROUCH_RATE * dt);
                agent.animation.SetFloat("Crouch", agent.crouch);
            }
            if (agent.attacksQueued > 0)
            {
                agent.attackTimer -= dt;
                if (agent.attackTimer <= 0.0f)
                {
                    agent.animation.SetTrigger("Attack");
                    agent.attacksQueued--;
                    agent.attackTimer = COMBO_INTERVAL;
                }
            }
            if (agent.danceTimer > 0.0f)
            {
                agent.danceTimer -= dt;
                if (agent.danceTimer <= 0.0f)
                {
                    agent.animation.SetBool("Dance", false);
                }
            }
        }

        /// <summary>A character a move carried off its slot walks back once the move ended; else it decides anew.</summary>
        private void Think(Agent agent)
        {
            if (agent.returning)
            {
                return;
            }
            if (IsAwayFromHome(agent) && agent.animation.IsInState("Locomotion"))
            {
                StartReturning(agent);
                return;
            }
            Decide(agent);
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0.0f, v.z);
        }

        private static bool IsAwayFromHome(Agent agent)
        {
            return Flat(agent.home - agent.entity.transform.position).magnitude > RETURN_DISTANCE;
        }

        private void StartReturning(Agent agent)
        {
            agent.returning = true;
            agent.targetSpeed = RETURN_SPEED;
            agent.targetCrouch = 0.0f;
            agent.attacksQueued = 0;
            if (agent.aiming)
            {
                agent.aiming = false;
                agent.animation.SetBool("Aim", false);
            }
        }

        /// <summary>Turns toward the grid slot and walks there, then turns back to the slot's facing.</summary>
        private static void WalkHome(Agent agent, float dt)
        {
            var transform = agent.entity.transform;
            Vector3 to = Flat(agent.home - transform.position);
            float blend = 1.0f - Mathf.Exp(-dt * TURN_RATE);
            if (to.magnitude <= ARRIVE_DISTANCE)
            {
                agent.targetSpeed = 0.0f;
                transform.rotation = Quaternion.Slerp(transform.rotation, agent.homeRotation, blend);
                if (agent.speed <= 0.0f && Quaternion.Angle(transform.rotation, agent.homeRotation) < FACING_TOLERANCE_DEGREES)
                {
                    agent.returning = false;
                }
                return;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), blend);
            // In-place locomotion: the walk the Speed parameter shows is moved here.
            transform.position = transform.position + Flat(transform.forward).normalized * agent.speed * dt;
        }

        /// <summary>What a character does next: a new speed most of the time, else one of its moves.</summary>
        private void Decide(Agent agent)
        {
            var animation = agent.animation;
            float roll = Random.Range(0.0f, 1.0f);
            if (roll < 0.40f)
            {
                agent.targetSpeed = Random.Range(0.0f, 1.0f) < IDLE_CHANCE ? 0.0f : Random.Range(MIN_MOVING_SPEED, MAX_SPEED);
            }
            else if (roll < 0.50f)
            {
                agent.targetCrouch = agent.targetCrouch > 0.5f ? 0.0f : 1.0f;
            }
            else if (roll < 0.62f)
            {
                agent.attacksQueued = Random.Range(1, MAX_COMBO_HITS + 1);
                agent.attackTimer = 0.0f;
            }
            else if (roll < 0.70f)
            {
                animation.SetTrigger("Roll");
            }
            else if (roll < 0.76f)
            {
                animation.SetTrigger("Jump");
            }
            else if (roll < 0.80f)
            {
                animation.SetTrigger("Interact");
            }
            else if (roll < 0.88f)
            {
                agent.aiming = !agent.aiming;
                animation.SetBool("Aim", agent.aiming);
                if (agent.aiming)
                {
                    animation.SetTrigger(Random.Range(0.0f, 1.0f) < 0.5f ? "Shoot" : "Reload");
                }
            }
            else if (roll < 0.95f)
            {
                animation.SetInt("HitKind", Random.Range(0, 2));
                animation.SetTrigger("Hit");
            }
            else
            {
                animation.SetBool("Dance", true);
                agent.danceTimer = Random.Range(MIN_DANCE_SECONDS, MAX_DANCE_SECONDS);
            }
        }

        private void MoveLookTarget(float dt)
        {
            if (!LookTarget.IsValid())
            {
                return;
            }
            lookTime += dt;
            float x = Mathf.Sin(lookTime * LOOK_SWEEP_RATE) * (gridWidth * 0.5f + 1.0f);
            float y = LOOK_HEIGHT + Mathf.Sin(lookTime * LOOK_BOB_RATE) * LOOK_BOB;
            LookTarget.transform.position = new Vector3(x, y, -LOOK_DISTANCE_AHEAD);
        }

        private void UpdateHud(float dt)
        {
            smoothedFrame = smoothedFrame <= 0.0f ? dt : Mathf.Lerp(smoothedFrame, dt, FRAME_SMOOTHING);
            hudTimer += dt;
            if (hudTimer < HUD_INTERVAL)
            {
                return;
            }
            hudTimer = 0.0f;
            data.frame = string.Format("{0:0.0} ms   {1:0} fps", smoothedFrame * 1000.0f, 1.0f / Mathf.Max(smoothedFrame, 1e-4f));
            model?.Dirty("frame");
        }

        private void RefreshHud()
        {
            data.count = agents.Count.ToString();
            data.rigs = RigsEnabled ? "on" : "off";
            model?.Dirty("count");
            model?.Dirty("rigs");
        }
    }
}
