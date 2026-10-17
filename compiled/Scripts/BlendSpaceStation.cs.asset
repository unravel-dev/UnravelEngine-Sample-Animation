using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Blend spaces driven by parameters. The runner's 1D space (Idle - Walk - Jog - Sprint on Speed)
    /// sweeps its speed up and down; the knight's 2D directional space (walk / run / strafes / backward
    /// on MoveX, MoveY) follows a direction that circles, shown by the arrow at its feet. Every clip in a
    /// space plays at one normalized phase, so the feet stay in step through the blend.
    /// </summary>
    [ScriptSourceFile]
    public class BlendSpaceStation : Station
    {
        public Entity Runner;
        public Entity Strafer;
        [Tooltip("Arrow pivot under the strafer, turned to the move direction.")]
        public Entity Arrow;
        [Tooltip("World texts above the characters.")]
        public Entity RunnerLabel;
        public Entity StraferLabel;

        [Tooltip("Sweeps per second of the runner's speed.")]
        public float SpeedFrequency = 0.06f;
        [Tooltip("Turns per second of the strafe direction.")]
        public float TurnFrequency = 0.05f;

        private AnimationComponent runner;
        private AnimationComponent strafer;
        private TextComponent runnerText;
        private TextComponent straferText;
        private float time;
        private float speed;
        private Vector2 move;

        public override void OnStart()
        {
            base.OnStart();
            runner = Runner.IsValid() ? Runner.GetComponent<AnimationComponent>() : null;
            strafer = Strafer.IsValid() ? Strafer.GetComponent<AnimationComponent>() : null;
            runnerText = RunnerLabel.IsValid() ? RunnerLabel.GetComponent<TextComponent>() : null;
            straferText = StraferLabel.IsValid() ? StraferLabel.GetComponent<TextComponent>() : null;
        }

        public override void OnUpdate()
        {
            time += Time.deltaTime;
            // Ease in and out of the ends so each clip of the space shows on its own for a moment.
            float sweep = 0.5f - 0.5f * Mathf.Cos(time * SpeedFrequency * Mathf.PI * 2.0f);
            speed = Mathf.SmoothStep(0.0f, 1.0f, sweep) * 6.2f;
            runner?.SetFloat("Speed", speed);
            float angle = time * TurnFrequency * Mathf.PI * 2.0f;
            float magnitude = 1.0f + 0.5f + 0.5f * Mathf.Sin(time * 0.21f);
            move = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * magnitude;
            strafer?.SetFloat("MoveX", move.x);
            strafer?.SetFloat("MoveY", move.y);
            if (Arrow.IsValid())
            {
                Arrow.transform.localRotation = Quaternion.Euler(0.0f, angle * Mathf.Rad2Deg, 0.0f);
                Arrow.transform.localScale = new Vector3(1.0f, 1.0f, magnitude * 0.5f);
            }
            if (runnerText != null)
            {
                runnerText.text = "Speed " + speed.ToString("0.0") + " m/s";
            }
            if (straferText != null)
            {
                straferText.text = "Move (" + move.x.ToString("0.0") + ", " + move.y.ToString("0.0") + ")";
            }
        }

        public override string GetStatus()
        {
            return "1D Speed " + speed.ToString("0.00") + "   2D Move (" + move.x.ToString("0.00") + ", " + move.y.ToString("0.00") + ")";
        }
    }
}
