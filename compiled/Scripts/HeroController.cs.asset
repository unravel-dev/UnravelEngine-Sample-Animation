using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// The playable hero.
    ///
    /// Movement goes through the character controller; the animation controller (Hero.actrl) only gets
    /// parameters - Speed / Crouch for the 2D locomotion blend space, Grounded, triggers for jump, roll,
    /// the sword combo, pick-up and hit reactions, Aim / AimPitch for the masked upper-body layer. The
    /// state machine decides; this script reads it back (IsInTag "Busy") to stop steering mid-attack.
    ///
    /// Root motion is on: the sword attacks, their recoveries and the roll carry their own travel, which
    /// the animation system moves through the character controller (so it still collides). Clips the
    /// script moves itself - locomotion, jumps, interactions - are imported "Keep In Place". While a
    /// Busy state plays the script adds no velocity of its own.
    ///
    /// Rig modifiers it drives: Foot IK (weight 0 in the air), the head's Look At IK (target: the nearest
    /// point of interest), the right hand's Two Bone IK on the lever (weighted by the clip's HandIK
    /// curve, so it needs no script), the sword trail from the clip's Trail curve. The jump leaves the
    /// ground on the clip's Takeoff event, through the public method of that name.
    /// </summary>
    [ScriptSourceFile]
    public class HeroController : ScriptComponent
    {
        [Header("Movement")]
        public float WalkSpeed = 1.6f;
        public float JogSpeed = 3.6f;
        public float SprintSpeed = 6.2f;
        public float CrouchSpeed = 1.4f;
        public float AimSpeed = 1.5f;
        [Tooltip("Speed change per second (m/s^2).")]
        public float Acceleration = 12.0f;
        [Tooltip("Turn responsiveness (1/s).")]
        public float TurnRate = 10.0f;
        [Tooltip("Upward speed of a jump (m/s).")]
        public float JumpSpeed = 4.4f;
        [Tooltip("Seconds off the ground that still count as grounded (steps, bumps).")]
        public float CoyoteTime = 0.12f;

        [Header("Scene")]
        public Entity Camera;
        [Tooltip("Free entity the head's Look At IK targets; moved to the nearest point of interest.")]
        public Entity LookTarget;
        public Entity Sword;
        public Entity Crossbow;
        [Tooltip("Particle emitter at the sword tip; its rate follows the clip's Trail curve.")]
        public Entity SwordTrail;
        [Tooltip("Particle emitter (world space) for muzzle sparks and hit sparks.")]
        public Entity Sparks;
        [Tooltip("The lever the hero can pull with E.")]
        public Entity Lever;

        /// <summary>Read by the HUD.</summary>
        public string StateLine { get; private set; } = "";

        private CharacterControllerComponent controller;
        private AnimationComponent animation;
        private FootIKComponent footIk;
        private LookAtIKComponent lookAt;
        private ThirdPersonCamera orbit;
        private ParticleEmitterComponent trail;
        private ParticleEmitterComponent sparks;
        private LeverStation lever;
        private Vector3 velocity;
        private float crouch;
        private bool crouched;
        private bool dancing;
        private bool aiming;
        private float footWeight = 1.0f;
        private bool jumpPending;
        private float jumpPendingTime;
        private int hitKind;
        private float airTime;
        private float sinceTakeoff = 10.0f;
        private bool crossbowShown = true;
        private AudioClip[] swingSounds;
        private AudioClip shootSound;
        private AudioClip reloadSound;

        public override void OnStart()
        {
            controller = owner.GetComponent<CharacterControllerComponent>();
            animation = owner.GetComponent<AnimationComponent>();
            footIk = owner.GetComponent<FootIKComponent>();
            Entity head = owner.transform.FindChild("Head", true);
            if (head.IsValid())
            {
                lookAt = head.GetComponent<LookAtIKComponent>();
            }
            if (Camera.IsValid())
            {
                orbit = Camera.GetComponent<ThirdPersonCamera>();
            }
            if (SwordTrail.IsValid())
            {
                trail = SwordTrail.GetComponent<ParticleEmitterComponent>();
            }
            if (Sparks.IsValid())
            {
                sparks = Sparks.GetComponent<ParticleEmitterComponent>();
            }
            if (Lever.IsValid())
            {
                lever = Lever.GetComponent<LeverStation>();
            }
            swingSounds = new[] { Assets.GetAsset<AudioClip>("app:/data/Audio/swing_0.wav"), Assets.GetAsset<AudioClip>("app:/data/Audio/swing_1.wav") };
            shootSound = Assets.GetAsset<AudioClip>("app:/data/Audio/shoot.wav");
            reloadSound = Assets.GetAsset<AudioClip>("app:/data/Audio/reload.wav");
        }

        public override void OnUpdate()
        {
            float dt = Time.deltaTime;
            EventLog.HeroPosition = owner.transform.position;
            // Brief losses of contact (steps, the first frame) do not count as falling; a jump that
            // just left does.
            airTime = controller.isGrounded ? 0.0f : airTime + dt;
            sinceTakeoff += dt;
            bool grounded = airTime < CoyoteTime && sinceTakeoff > 0.3f;
            bool busy = animation.IsInTag("Busy");
            HandleActions(grounded, busy);
            Move(dt, grounded, busy);
            FeedAnimation(dt, grounded);
            DriveRig(dt, grounded);
            StateLine = animation.GetCurrentStateName(0) + " / " + animation.GetCurrentStateName(1) + " / " +
                        animation.GetCurrentStateName(2);
        }

        private void HandleActions(bool grounded, bool busy)
        {
            aiming = Input.IsDown(MouseButton.Right) && grounded && !dancing;
            if (Input.IsPressed(KeyCode.C))
            {
                crouched = !crouched;
            }
            if (Input.IsPressed(KeyCode.G))
            {
                dancing = !dancing;
            }
            if (Input.IsPressed("Jump") && grounded && !busy && !jumpPending)
            {
                crouched = false;
                dancing = false;
                animation.SetTrigger("Jump");
                jumpPending = true;
                jumpPendingTime = 0.0f;
            }
            if (Input.IsPressed(MouseButton.Left))
            {
                animation.SetTrigger(aiming ? "Shoot" : "Attack");
            }
            if (aiming && Input.IsPressed(KeyCode.R))
            {
                animation.SetTrigger("Reload");
            }
            if (Input.IsPressed(KeyCode.Q) && grounded && !busy)
            {
                animation.SetTrigger("Roll");
            }
            if (Input.IsPressed(KeyCode.H))
            {
                animation.SetInt("HitKind", hitKind);
                animation.SetTrigger("Hit");
                hitKind = 1 - hitKind;
            }
            if (Input.IsPressed(KeyCode.E) && grounded && !busy)
            {
                // Faces the lever so the clip's reach lines up with it; the hand IK closes the rest.
                TryPullLever();
            }
        }

        private void Move(float dt, bool grounded, bool busy)
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (input.sqrMagnitude > 1.0f)
            {
                input = input.normalized;
            }
            float yaw = orbit != null ? orbit.Yaw : 0.0f;
            Quaternion yawRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            Vector3 desired = yawRotation * new Vector3(input.x, 0.0f, input.y);

            float targetSpeed = JogSpeed;
            if (Input.IsDown(KeyCode.LShift))
            {
                targetSpeed = SprintSpeed;
            }
            if (Input.IsDown(KeyCode.LCtrl))
            {
                targetSpeed = WalkSpeed;
            }
            if (crouched)
            {
                targetSpeed = CrouchSpeed;
            }
            if (aiming)
            {
                targetSpeed = AimSpeed;
            }
            Vector3 target = desired * targetSpeed;
            if (dancing && input.sqrMagnitude < 0.01f)
            {
                target = Vector3.zero;
            }
            float rate = grounded ? Acceleration : Acceleration * 0.3f;
            // Busy states move by root motion alone.
            velocity = busy ? Vector3.zero : Vector3.MoveTowards(velocity, target, rate * dt);
            if (input.sqrMagnitude > 0.01f && dancing)
            {
                dancing = false;
            }
            controller.Move(velocity * dt);

            Vector3 facing = aiming ? yawRotation * Vector3.forward : velocity;
            facing.y = 0.0f;
            if (facing.sqrMagnitude > 0.04f && !busy)
            {
                Quaternion wanted = Quaternion.LookRotation(facing.normalized);
                owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, wanted, 1.0f - Mathf.Exp(-TurnRate * dt));
            }

            if (jumpPending)
            {
                jumpPendingTime += dt;
                // The Takeoff event normally launches the jump; launch anyway if it never comes
                // (the jump clip faded in below the event's weight threshold).
                if (jumpPendingTime > 0.45f)
                {
                    Takeoff();
                }
            }
        }

        /// <summary>
        /// Puts the hero at a point facing a yaw (degrees), stopped - for tours and automated checks.
        /// </summary>
        public void TeleportTo(Vector3 position, float yaw)
        {
            // The transform is written too: a transform changed this frame is what the physics body
            // follows, so a warp alone would be undone by the rotation below.
            owner.transform.position = position;
            owner.transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            controller.Warp(position);
            velocity = Vector3.zero;
        }

        /// <summary>Starts the lever pull as E does, when the hero stands in reach.</summary>
        public bool TryPullLever()
        {
            if (lever == null || !lever.IsInReach(owner.transform.position))
            {
                return false;
            }
            Vector3 toLever = StationRegistry.Flat(lever.owner.transform.position - owner.transform.position);
            owner.transform.rotation = Quaternion.LookRotation(toLever.normalized);
            velocity = Vector3.zero;
            animation.SetTrigger("Interact");
            return true;
        }

        /// <summary>Called by the Takeoff event of the jump clip: the feet leave the ground now.</summary>
        public void Takeoff()
        {
            if (!jumpPending)
            {
                return;
            }
            jumpPending = false;
            sinceTakeoff = 0.0f;
            controller.Jump(JumpSpeed);
        }

        private void FeedAnimation(float dt, bool grounded)
        {
            Vector3 planar = velocity;
            planar.y = 0.0f;
            crouch = Mathf.MoveTowards(crouch, crouched ? 1.0f : 0.0f, dt * 3.0f);
            animation.SetFloat("Speed", planar.magnitude);
            animation.SetFloat("Crouch", crouch);
            // A jump waiting for its Takeoff event already counts as airborne, so the landing
            // transitions do not fire during the wind-up.
            animation.SetBool("Grounded", grounded && !jumpPending);
            animation.SetBool("Aim", aiming);
            animation.SetBool("Dance", dancing);
            float pitch = orbit != null ? orbit.Pitch : 0.0f;
            animation.SetFloat("AimPitch", Mathf.Clamp(-pitch / 45.0f, -1.0f, 1.0f));
            if (orbit != null)
            {
                orbit.Aiming = aiming;
            }
        }

        private void DriveRig(float dt, bool grounded)
        {
            // Foot IK only while standing: in the air or rolling the feet follow the clip.
            bool planted = grounded && !animation.IsInTag("Air") && !animation.IsInState("Roll") && !jumpPending;
            footWeight = Mathf.MoveTowards(footWeight, planted ? 1.0f : 0.0f, dt * 5.0f);
            if (footIk != null)
            {
                footIk.weight = footWeight;
            }
            if (lookAt != null && LookTarget.IsValid())
            {
                Station station = StationRegistry.FindAt(owner.transform.position);
                bool looking = station != null && !animation.IsInTag("Busy") && !dancing && !aiming;
                if (looking)
                {
                    LookTarget.transform.position = station.GetFocusPosition();
                }
                lookAt.weight = looking ? 1.0f : 0.0f;
            }
            if (trail != null)
            {
                trail.emissionRate = animation.GetCurve("Trail") * 160.0f;
            }
            // The crossbow replaces the sword in the hand while the upper body aims.
            bool showCrossbow = aiming || animation.GetCurrentStateName(1) != "None";
            if (showCrossbow != crossbowShown)
            {
                crossbowShown = showCrossbow;
                if (Sword.IsValid())
                {
                    Sword.SetActive(!showCrossbow);
                }
                if (Crossbow.IsValid())
                {
                    Crossbow.SetActive(showCrossbow);
                }
            }
        }

        public override void OnAnimationEvent(AnimationEvent animationEvent)
        {
            switch (animationEvent.name)
            {
                case "Swing":
                    Audio.PlayOneShot(swingSounds[Random.Range(0, swingSounds.Length)], owner.transform.position, 0.7f, Random.Range(0.95f, 1.1f));
                    break;
                case "Shoot":
                    Audio.PlayOneShot(shootSound, owner.transform.position, 0.8f);
                    BurstSparks(Crossbow.IsValid() ? Crossbow.transform.position : owner.transform.position, 14);
                    break;
                case "Reload":
                    Audio.PlayOneShot(reloadSound, owner.transform.position, 0.7f);
                    break;
                case "Grab":
                    lever?.Pull();
                    break;
            }
            if (animationEvent.name != "Footstep" && animationEvent.name != "Land")
            {
                EventLog.Add("Hero", animationEvent, owner.transform.position);
            }
        }

        private void BurstSparks(Vector3 position, int count)
        {
            if (sparks == null)
            {
                return;
            }
            Vector3 forward = owner.transform.forward;
            for (int i = 0; i < count; ++i)
            {
                var jitter = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-0.4f, 1.0f), Random.Range(-1.0f, 1.0f));
                sparks.Emit(1, new EmitParams
                {
                    position = position,
                    velocity = forward * 4.0f + jitter * 1.5f,
                    color = new Color(1.0f, 0.75f, 0.35f, 1.0f),
                    size = 0.05f,
                    lifetime = Random.Range(0.25f, 0.5f),
                });
            }
        }
    }
}
