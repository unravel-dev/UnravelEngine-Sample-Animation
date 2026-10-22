using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Orbit camera around the hero: the mouse turns it, the wheel zooms, and while the hero aims it
    /// moves in over the right shoulder. A sphere cast keeps it in front of walls. The cursor is locked
    /// while playing (Cursor.lockState), so turning never runs out of screen. On a touch screen a finger
    /// dragged over the free screen turns it instead, and the cursor is left alone; the fingers on the
    /// on-screen controls (SampleTouchJoystick) are not among the touches.
    /// </summary>
    [ScriptSourceFile]
    public class ThirdPersonCamera : ScriptComponent
    {
        [Tooltip("Entity the camera orbits (the hero).")]
        public Entity Target;

        [Tooltip("Orbit distance (m).")]
        public float Distance = 4.4f;

        public float MinDistance = 1.8f;
        public float MaxDistance = 9.0f;

        [Tooltip("Height of the orbit pivot above the target (m).")]
        public float PivotHeight = 1.45f;

        [Tooltip("Degrees per unit of mouse motion.")]
        public float Sensitivity = 9.0f;

        [Tooltip("Orbit distance while aiming (m).")]
        public float AimDistance = 1.9f;

        [Tooltip("Shoulder offset while aiming, in the camera's frame (m).")]
        public Vector3 AimShoulder = new Vector3(0.55f, 0.15f, 0.0f);

        [Tooltip("Pitch limits (degrees).")]
        public float MinPitch = -30.0f;
        public float MaxPitch = 65.0f;

        [Tooltip("Lock the cursor while playing, so the mouse turns the camera endlessly. Escape frees it, a click takes it back.")]
        public bool LockCursor = true;

        [Tooltip("Degrees the camera turns for a finger dragged across the screen's height.")]
        public float TouchLookSpeed = 200.0f;

        /// <summary>The cursor is free (Escape): the mouse does not look around, and clicks only take it back.</summary>
        public bool IsCursorFree => LockCursor && Cursor.lockState != CursorLockMode.Locked;

        /// <summary>Set by the hero while it aims.</summary>
        public bool Aiming { get; set; }

        public float Yaw => yaw;
        public float Pitch => pitch;

        private float yaw;
        private float pitch = 14.0f;
        private float aimBlend;
        private float distance;
        /// <summary>The camera locks the cursor and lets Escape free it: LockCursor, off a touch screen.</summary>
        private bool holdsCursor;

        public override void OnStart()
        {
            // A touch screen has no cursor to hold.
            holdsCursor = LockCursor && !Input.touchSupported;
            if (holdsCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            distance = Distance;
            if (Target.IsValid())
            {
                Vector3 forward = Target.transform.forward;
                yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            }
        }

        public override void OnLateUpdate()
        {
            if (!Target.IsValid())
            {
                return;
            }
            float dt = Time.deltaTime;
            UpdateCursorLock();
            if (!IsCursorFree)
            {
                yaw += Input.GetAxis("Mouse X") * Sensitivity * dt;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * Sensitivity * dt, MinPitch, MaxPitch);
            }
            LookByTouch();
            Distance = Mathf.Clamp(Distance - Input.GetAxis("Mouse ScrollWheel") * 0.6f, MinDistance, MaxDistance);
            aimBlend = Mathf.MoveTowards(aimBlend, Aiming ? 1.0f : 0.0f, dt * 5.0f);
            float blend = Mathf.SmoothStep(0.0f, 1.0f, aimBlend);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0.0f);
            Vector3 pivot = Target.transform.position + new Vector3(0.0f, PivotHeight, 0.0f) + rotation * (AimShoulder * blend);
            float wanted = Mathf.Lerp(Distance, AimDistance, blend);
            Vector3 back = rotation * new Vector3(0.0f, 0.0f, -1.0f);
            RaycastHit? hit = Physics.SphereCast(pivot, back, 0.25f, wanted);
            if (hit.HasValue && hit.Value.entity != Target)
            {
                wanted = Mathf.Max(hit.Value.distance - 0.05f, 0.6f);
            }
            distance = wanted < distance ? wanted : Mathf.Lerp(distance, wanted, 1.0f - Mathf.Exp(-dt * 6.0f));
            owner.transform.position = pivot + back * distance;
            owner.transform.rotation = rotation;
        }

        /// <summary>
        /// The first finger that moves turns the camera as the mouse would: dragged right it looks right,
        /// dragged up it looks up. A finger's motion never reads as "Mouse X" / "Mouse Y".
        /// </summary>
        private void LookByTouch()
        {
            float degreesPerPixel = TouchLookSpeed / Mathf.Max(1.0f, Screen.height);
            for (int i = 0; i < Input.touchCount; ++i)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.Phase != TouchPhase.Moved)
                {
                    continue;
                }
                yaw += touch.DeltaPosition.x * degreesPerPixel;
                pitch = Mathf.Clamp(pitch + touch.DeltaPosition.y * degreesPerPixel, MinPitch, MaxPitch);
                return;
            }
        }

        /// <summary>Escape frees the cursor; a click takes it back (after the hero has read that click).</summary>
        private void UpdateCursorLock()
        {
            if (!holdsCursor)
            {
                return;
            }
            if (Input.IsPressed(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState != CursorLockMode.Locked && Input.IsPressed(MouseButton.Left))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
