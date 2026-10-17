using System.Collections;
using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// The lever the hero pulls with F. The pick-up clip reaches forward with the free left hand; its Two
    /// Bone IK (target: the lever grip, weight curve "HandIK") puts the hand on the grip and keeps it there
    /// through the throw while the clip's curve is up, and the clip's "Grab" event throws the lever, which
    /// switches the lamps.
    /// </summary>
    [ScriptSourceFile]
    public class LeverStation : Station
    {
        [Tooltip("Pivot that rotates when the lever is pulled.")]
        public Entity Handle;

        [Tooltip("Where the hero stands to reach the lever.")]
        public Entity StandPoint;

        [Tooltip("The hero can pull from this close to the stand point (m).")]
        public float ReachRadius = 1.4f;

        [Tooltip("Handle throw (degrees around its local X axis).")]
        public float ThrowAngle = 70.0f;

        [Tooltip("Lights the lever switches.")]
        public Entity LampA;
        public Entity LampB;

        private bool on;
        private float angle;
        private AudioClip clank;

        public override void OnStart()
        {
            base.OnStart();
            clank = Assets.GetAsset<AudioClip>("app:/data/Audio/lever.wav");
            SetLamps(false);
        }

        public bool IsInReach(Vector3 position)
        {
            Vector3 stand = StandPoint.IsValid() ? StandPoint.transform.position : owner.transform.position;
            return Vector3.Distance(StationRegistry.Flat(position), StationRegistry.Flat(stand)) < ReachRadius;
        }

        /// <summary>Throws the lever (called on the clip's Grab event).</summary>
        public void Pull()
        {
            on = !on;
            Audio.PlayOneShot(clank, owner.transform.position, 0.8f);
            StopAllCoroutines();
            StartCoroutine(Throw(on ? ThrowAngle : 0.0f));
        }

        private IEnumerator Throw(float target)
        {
            float start = angle;
            for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / 0.35f)
            {
                angle = Mathf.Lerp(start, target, Mathf.SmoothStep(0.0f, 1.0f, t));
                ApplyAngle();
                yield return null;
            }
            angle = target;
            ApplyAngle();
            SetLamps(on);
        }

        private void ApplyAngle()
        {
            if (Handle.IsValid())
            {
                Handle.transform.localRotation = Quaternion.Euler(angle, 0.0f, 0.0f);
            }
        }

        private void SetLamps(bool lit)
        {
            foreach (var lamp in new[] { LampA, LampB })
            {
                if (lamp.IsValid())
                {
                    var light = lamp.GetComponent<LightComponent>();
                    if (light != null)
                    {
                        light.enabled = lit;
                    }
                }
            }
        }

        public override string GetStatus()
        {
            Entity hero = Scene.FindEntityByName("Hero");
            float curve = 0.0f;
            if (hero.IsValid())
            {
                var animation = hero.GetComponent<AnimationComponent>();
                curve = animation != null ? animation.GetCurve("HandIK") : 0.0f;
            }
            return "Lamps: " + (on ? "ON" : "off") + "\nHandIK curve (hand IK weight): " + curve.ToString("0.00") +
                   (hero.IsValid() && IsInReach(hero.transform.position) ? "\n[F] pull the lever" : "\nStand at the marker and press F");
        }
    }
}
