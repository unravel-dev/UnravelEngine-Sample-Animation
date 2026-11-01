using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// The playable hero.
    ///
    /// Movement goes through the character controller; the animation controller (Hero.actrl) only gets
    /// parameters - Speed / Crouch for the 2D locomotion blend space, Grounded, triggers for jump, roll,
    /// dash, the sword combo, pick-up and hit reactions, Aim / AimPitch for the masked upper-body layer. The
    /// state machine decides; this script reads it back (IsInTag "Busy") to hand the body to root motion.
    ///
    /// Root motion is on: the sword attacks, their recoveries, the roll and the dash carry their own travel, which
    /// the animation system moves through the character controller (so it still collides). Clips the
    /// script moves itself - locomotion, jumps, interactions - are imported "Keep In Place". While a
    /// Busy state plays the script adds no motion of its own; the input only steers it.
    ///
    /// Nothing waits for a clip to end. Every move has a cut point (the exit time of its Cut transition, where
    /// the strike or the travel is over); past it, move input or a queued action ends it. The velocity keeps
    /// following the input through a Busy state, so a move that gets cut leaves at running speed. A press that
    /// cannot start yet waits a moment in a queue, a dodge cuts an attack at any time, and the combo remembers
    /// its place for a moment after a cut (the Combo parameter picks the next strike).
    ///
    /// Rig modifiers it drives: Foot IK (weight 0 in the air), the head's Look At IK (target: a character
    /// of the station around the hero, marked by the look marker), the left hand's Two Bone IK on the
    /// lever (weighted by the clip's HandIK curve, so it needs no script), the sword trail from the
    /// clip's Trail curve. The jump leaves the
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
        [Tooltip("Turn responsiveness at the start of a strike or a dodge (1/s): it goes where the input points.")]
        public float ActionTurnRate = 24.0f;
        [Tooltip("Turn responsiveness during a strike or a dodge (1/s): the input still steers it.")]
        public float BusyTurnRate = 3.0f;
        [Tooltip("Upward speed of a jump (m/s).")]
        public float JumpSpeed = 4.4f;
        [Tooltip("Seconds off the ground that still count as grounded (steps, bumps).")]
        public float CoyoteTime = 0.12f;
        [Tooltip("Seconds a press that cannot start yet waits for its chance.")]
        public float QueueTime = 0.4f;
        [Tooltip("Seconds after a strike in which another attack continues the combo.")]
        public float ComboMemory = 0.8f;

        [Header("Scene")]
        public Entity Camera;
        [Tooltip("Free entity the head's Look At IK targets; moved to what the station around the hero shows.")]
        public Entity LookTarget;
        [Tooltip("Marker (a child of LookTarget) hovering over what the hero looks at, shown while it looks.")]
        public Entity LookMarker;
        public Entity Sword;
        public Entity Crossbow;
        [Tooltip("The sword's slash trail: every particle emitter at or under it (the sheet across the blade, the tip's edge) emits at the rate the clip's Trail curve sets.")]
        public Entity SwordTrail;
        [Tooltip("Particle emitter (world space) for muzzle sparks and hit sparks.")]
        public Entity Sparks;
        [Tooltip("The lever the hero can pull with F.")]
        public Entity Lever;

        /// <summary>Read by the HUD.</summary>
        public string StateLine { get; private set; } = "";

        /// <summary>Height the hero looks from (m).</summary>
        private const float EyeHeight = 1.6f;
        /// <summary>Above this speed (m/s, between walk and jog) the sword arm is held in its carry pose.</summary>
        private const float CarrySpeed = 2.2f;
        /// <summary>How fast a glance follows a new point (1/s).</summary>
        private const float LookFollowRate = 6.0f;
        /// <summary>The look marker: size and gap between its center and the top of the head (m), bob
        /// amplitude (m) and rate (rad/s), fade rate (1/s).</summary>
        private const float MarkerSize = 0.13f;
        private const float MarkerGap = 0.14f;
        private const float MarkerBob = 0.03f;
        private const float MarkerBobRate = 3.0f;
        private const float MarkerFadeRate = 4.0f;
        /// <summary>Slash trail points per second at a Trail curve of 1 (spread along the blade's path).</summary>
        private const float TrailRate = 160.0f;
        /// <summary>Seconds a strike or a dodge turns at ActionTurnRate from its start.</summary>
        private const float ActionTurnTime = 0.12f;
        /// <summary>Seconds the script's motion takes to fade in and out, alongside the crossfades that hand the
        /// body between it and root motion.</summary>
        private const float ScriptMotionFade = 0.12f;
        /// <summary>Strikes in the combo; after the last one it starts over.</summary>
        private const int ComboLength = 4;

        private enum QueuedAction
        {
            None,
            Jump,
            Roll,
            Dash,
            Interact,
        }

        private CharacterControllerComponent controller;
        private AnimationComponent animation;
        private FootIKComponent footIk;
        private LookAtIKComponent lookAt;
        private ThirdPersonCamera orbit;
        private ParticleEmitterComponent[] trails = new ParticleEmitterComponent[0];
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
        private float lookWeight;
        private float markerFade;
        private float markerHeight;
        private float airTime;
        private float sinceTakeoff = 10.0f;
        private bool crossbowShown = true;
        private QueuedAction queuedAction;
        private float queuedActionTime;
        private float queuedAttackTime;
        private float actionTurnTime;
        private float scriptMotion = 1.0f;
        private int comboStep;
        private float comboTime;
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
                trails = SwordTrail.GetComponentsInChildren<ParticleEmitterComponent>();
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
            string shown = GetShownState();
            // A crossfade back to Locomotion already counts as free: the input is for what comes next.
            bool busy = animation.IsInTag("Busy") && shown != "Locomotion";
            Vector2 input = ReadMoveInput();
            TrackCombo(dt, shown);
            HandleActions(dt, grounded, shown);
            Move(dt, grounded, busy, input);
            FeedAnimation(dt, grounded, busy, input);
            DriveRig(dt, grounded);
            StateLine = animation.GetCurrentStateName(0) + " / " + animation.GetCurrentStateName(1) + " / " +
                        animation.GetCurrentStateName(2);
        }

        /// <summary>The state the base layer plays, or the one it is crossfading to.</summary>
        private string GetShownState()
        {
            string next = animation.GetNextStateName(0);
            return next.Length > 0 ? next : animation.GetCurrentStateName(0);
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            return input.sqrMagnitude > 1.0f ? input.normalized : input;
        }

        /// <summary>
        /// Follows the combo through the states that strike, and keeps its place for ComboMemory seconds
        /// after a cut, so a hero that runs between strikes goes on with the next one.
        /// </summary>
        private void TrackCombo(float dt, string shown)
        {
            int step = shown switch
            {
                "Slash" => 1,
                "SwordA" or "SwordARec" => 2,
                "SwordB" or "SwordBRec" => 3,
                "SwordC" => ComboLength,
                _ => 0,
            };
            if (step != 0)
            {
                if (step != comboStep)
                {
                    actionTurnTime = ActionTurnTime;
                }
                comboStep = step;
                comboTime = ComboMemory;
            }
            else if (comboStep != 0)
            {
                comboTime -= dt;
                comboStep = comboTime > 0.0f ? comboStep : 0;
            }
            animation.SetInt("Combo", comboStep % ComboLength);
        }

        private void HandleActions(float dt, bool grounded, string shown)
        {
            // With the cursor freed (Escape) the mouse belongs to the user: a click only takes it back.
            bool mouseFree = orbit != null && orbit.IsCursorFree;
            aiming = !mouseFree && Input.IsDown(MouseButton.Right) && grounded && !dancing;
            if (Input.IsPressed(KeyCode.C))
            {
                crouched = !crouched;
            }
            if (Input.IsPressed(KeyCode.G))
            {
                dancing = !dancing;
            }
            // "Attack", "Roll", "Dash" and "Interact" are input actions (Settings > Input): keys and gamepad
            // buttons, which the on-screen controls press on a touch screen.
            if ((!mouseFree && Input.IsPressed(MouseButton.Left)) || Input.IsPressed("Attack"))
            {
                if (aiming)
                {
                    animation.SetTrigger("Shoot");
                }
                else
                {
                    // The trigger itself is the queue: it waits for the transition that takes it.
                    animation.SetTrigger("Attack");
                    queuedAttackTime = QueueTime;
                }
            }
            if (aiming && Input.IsPressed(KeyCode.R))
            {
                animation.SetTrigger("Reload");
            }
            if (Input.IsPressed(KeyCode.H))
            {
                animation.SetInt("HitKind", hitKind);
                animation.SetTrigger("Hit");
                hitKind = 1 - hitKind;
            }
            if (Input.IsPressed("Jump"))
            {
                QueueAction(QueuedAction.Jump);
            }
            if (Input.IsPressed("Roll"))
            {
                QueueAction(QueuedAction.Roll);
            }
            if (Input.IsPressed("Dash"))
            {
                QueueAction(QueuedAction.Dash);
            }
            if (Input.IsPressed("Interact"))
            {
                QueueAction(QueuedAction.Interact);
            }
            RunActionQueue(dt, grounded, shown);
        }

        /// <summary>The latest press wins. Anything but the lever ends a dance, which holds up the rest.</summary>
        private void QueueAction(QueuedAction action)
        {
            queuedAction = action;
            queuedActionTime = QueueTime;
            dancing = dancing && action == QueuedAction.Interact;
        }

        private void RunActionQueue(float dt, bool grounded, string shown)
        {
            if (queuedAttackTime > 0.0f)
            {
                queuedAttackTime -= dt;
                if (queuedAttackTime <= 0.0f)
                {
                    animation.ResetTrigger("Attack");
                }
            }
            if (queuedAction == QueuedAction.None)
            {
                return;
            }
            queuedActionTime -= dt;
            if (TryStartAction(queuedAction, grounded, shown) || queuedActionTime <= 0.0f)
            {
                queuedAction = QueuedAction.None;
            }
        }

        private bool TryStartAction(QueuedAction action, bool grounded, string shown)
        {
            if (!grounded || jumpPending)
            {
                return false;
            }
            switch (action)
            {
                case QueuedAction.Jump when shown == "Locomotion":
                    crouched = false;
                    animation.SetTrigger("Jump");
                    jumpPending = true;
                    jumpPendingTime = 0.0f;
                    return true;
                case QueuedAction.Interact when shown == "Locomotion":
                    // Faces the lever so the clip's reach lines up with it; the hand IK closes the rest.
                    TryPullLever();
                    return true;
                // A dodge cuts an attack at any time; a dodge or a lever pull reach their end first.
                case QueuedAction.Roll when shown != "Roll" && shown != "Dash" && shown != "PickUp":
                    animation.SetTrigger("Roll");
                    actionTurnTime = ActionTurnTime;
                    return true;
                case QueuedAction.Dash when shown != "Roll" && shown != "Dash" && shown != "PickUp":
                    // The clip's root motion carries the dash, as it does the roll.
                    crouched = false;
                    animation.SetTrigger("Dash");
                    actionTurnTime = ActionTurnTime;
                    return true;
                default:
                    return false;
            }
        }

        private void Move(float dt, bool grounded, bool busy, Vector2 input)
        {
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
            // Busy states move by root motion alone, but the velocity keeps following the input: a move
            // that gets cut crossfades straight into the run.
            velocity = Vector3.MoveTowards(velocity, target, rate * dt);
            scriptMotion = Mathf.MoveTowards(scriptMotion, busy ? 0.0f : 1.0f, dt / ScriptMotionFade);
            if (input.sqrMagnitude > 0.01f && dancing)
            {
                dancing = false;
            }
            controller.Move(velocity * (scriptMotion * dt));
            Turn(dt, busy, desired, yawRotation);

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
        /// Faces where the hero runs or aims. A strike or a dodge turns to the input instead: quickly as it
        /// starts, so it goes where the input points, then slowly, since root motion moves along the facing.
        /// </summary>
        private void Turn(float dt, bool busy, Vector3 desired, Quaternion yawRotation)
        {
            actionTurnTime -= dt;
            if (animation.IsInState("PickUp"))
            {
                return;
            }
            bool starting = actionTurnTime > 0.0f;
            Vector3 facing = busy || starting ? desired : (aiming ? yawRotation * Vector3.forward : velocity);
            facing.y = 0.0f;
            if (facing.sqrMagnitude <= 0.04f)
            {
                return;
            }
            float turnRate = starting ? ActionTurnRate : (busy ? BusyTurnRate : TurnRate);
            Quaternion wanted = Quaternion.LookRotation(facing.normalized);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, wanted, 1.0f - Mathf.Exp(-turnRate * dt));
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

        private void FeedAnimation(float dt, bool grounded, bool busy, Vector2 input)
        {
            Vector3 planar = velocity;
            planar.y = 0.0f;
            crouch = Mathf.MoveTowards(crouch, crouched ? 1.0f : 0.0f, dt * 3.0f);
            animation.SetFloat("Speed", planar.magnitude);
            animation.SetFloat("Crouch", crouch);
            // Past a move's cut point this ends it, and Locomotion takes what is queued.
            animation.SetBool("Cut", input.sqrMagnitude > 0.01f || queuedAction != QueuedAction.None ||
                                     animation.GetBool("Attack") || !grounded);
            // A jump waiting for its Takeoff event already counts as airborne, so the landing
            // transitions do not fire during the wind-up.
            animation.SetBool("Grounded", grounded && !jumpPending);
            animation.SetBool("Aim", aiming);
            animation.SetBool("Dance", dancing);
            // Running swings the sword hand up to the shoulder and the blade through the head: above a
            // walk the SwordArm layer holds the arm in a carry pose. Attacks, aiming and the rest keep it.
            animation.SetBool("Carry", grounded && !busy && !aiming && !dancing && planar.magnitude > CarrySpeed);
            float pitch = orbit != null ? orbit.Pitch : 0.0f;
            animation.SetFloat("AimPitch", Mathf.Clamp(-pitch / 45.0f, -1.0f, 1.0f));
            if (orbit != null)
            {
                orbit.Aiming = aiming;
            }
        }

        private void DriveRig(float dt, bool grounded)
        {
            // Foot IK only while standing: in the air, rolling or dashing the feet follow the clip.
            bool planted = grounded && !animation.IsInTag("Air") && !animation.IsInState("Roll") &&
                           !animation.IsInState("Dash") && !jumpPending;
            footWeight = Mathf.MoveTowards(footWeight, planted ? 1.0f : 0.0f, dt * 5.0f);
            if (footIk != null)
            {
                footIk.weight = footWeight;
            }
            DriveLook(dt);
            float trailRate = animation.GetCurve("Trail") * TrailRate;
            foreach (var emitter in trails)
            {
                emitter.emissionRate = trailRate;
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

        /// <summary>
        /// The head's Look At IK glances at what the station around the hero shows (a character's head, else
        /// its Focus point) while the hero is free; the look marker hovers over it meanwhile.
        /// </summary>
        private void DriveLook(float dt)
        {
            if (lookAt == null || !LookTarget.IsValid())
            {
                return;
            }
            Station station = StationRegistry.FindAt(owner.transform.position);
            bool free = !animation.IsInTag("Busy") && !dancing && !aiming;
            Vector3 eyes = owner.transform.position + new Vector3(0.0f, EyeHeight, 0.0f);
            Vector3 point = Vector3.zero;
            float headTop = 0.0f;
            bool looking = free && station != null &&
                           station.TryGetFocus(eyes, owner.transform.forward, out point, out headTop);
            if (looking)
            {
                // A new glance starts at its point (the weight fades in); a switch eases across.
                float follow = lookWeight > 0.0f ? 1.0f - Mathf.Exp(-LookFollowRate * dt) : 1.0f;
                LookTarget.transform.position = Vector3.Lerp(LookTarget.transform.position, point, follow);
                markerHeight = Mathf.Lerp(markerHeight, headTop + MarkerGap, follow);
            }
            lookWeight = looking ? 1.0f : 0.0f;
            lookAt.weight = lookWeight;
            UpdateLookMarker(dt, looking);
        }

        private void UpdateLookMarker(float dt, bool looking)
        {
            if (!LookMarker.IsValid())
            {
                return;
            }
            markerFade = Mathf.MoveTowards(markerFade, looking ? 1.0f : 0.0f, dt * MarkerFadeRate);
            bool shown = markerFade > 0.0f;
            if (LookMarker.activeLocal != shown)
            {
                LookMarker.SetActive(shown);
            }
            if (shown)
            {
                LookMarker.transform.localScale = Vector3.one * (MarkerSize * markerFade);
                LookMarker.transform.localPosition =
                    new Vector3(0.0f, markerHeight + MarkerBob * Mathf.Sin(Time.time * MarkerBobRate), 0.0f);
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
