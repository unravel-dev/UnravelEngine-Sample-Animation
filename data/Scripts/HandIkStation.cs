using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Hands on props with Two Bone IK: the driver's hands hold a steering wheel that turns on its own
    /// (targets are children of the wheel rim, poles keep the elbows down), and a second character
    /// reaches for an orb flying a figure-eight, with soften easing the arm out before it locks.
    /// </summary>
    [ScriptSourceFile]
    public class HandIkStation : Station
    {
        [Tooltip("Steering wheel pivot; turns around its local Y axis (the rim's axis).")]
        public Entity Wheel;

        [Tooltip("Largest wheel turn (degrees).")]
        public float WheelAngle = 70.0f;

        [Tooltip("Turns per second.")]
        public float WheelFrequency = 0.18f;

        [Tooltip("The driver's hands (for the status line).")]
        public Entity DriverLeftHand;
        public Entity DriverRightHand;

        [Tooltip("The reaching hand (for the status line).")]
        public Entity ReachHand;

        private float time;

        public override void OnUpdate()
        {
            time += Time.deltaTime;
            if (Wheel.IsValid())
            {
                float angle = Mathf.Sin(time * WheelFrequency * Mathf.PI * 2.0f) * WheelAngle;
                Wheel.transform.localRotation = Quaternion.Euler(0.0f, angle, 0.0f);
            }
        }

        public override string GetStatus()
        {
            string line = "";
            if (Wheel.IsValid())
            {
                line += "Wheel " + Mathf.DeltaAngle(0.0f, Wheel.transform.localEulerAngles.y).ToString("0") + " deg";
            }
            line += Describe("\nDriver hands", DriverLeftHand, DriverRightHand);
            if (ReachHand.IsValid())
            {
                var ik = ReachHand.GetComponent<TwoBoneIKComponent>();
                if (ik != null)
                {
                    line += "\nReaching hand: " + (ik.isReached ? "on the orb" : "stretching (soften)");
                }
            }
            return line;
        }

        private static string Describe(string label, Entity left, Entity right)
        {
            int reached = 0;
            foreach (var hand in new[] { left, right })
            {
                if (hand.IsValid())
                {
                    var ik = hand.GetComponent<TwoBoneIKComponent>();
                    if (ik != null && ik.isReached)
                    {
                        reached++;
                    }
                }
            }
            return label + ": " + reached + " / 2 on the rim";
        }
    }
}
