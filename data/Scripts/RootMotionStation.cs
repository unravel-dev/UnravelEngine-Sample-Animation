using Unravel.Core;

namespace AnimationSample
{
    /// <summary>Status of the root motion exhibit: what each actor plays and how far the clip moved it.</summary>
    [ScriptSourceFile]
    public class RootMotionStation : Station
    {
        public Entity Climber;
        public Entity Dasher;

        public override string GetStatus()
        {
            return Describe("Climber", Climber) + "\n" + Describe("Dasher", Dasher);
        }

        private static string Describe(string label, Entity actor)
        {
            var script = actor.IsValid() ? actor.GetComponent<RootMotionActor>() : null;
            if (script == null)
            {
                return "";
            }
            string playing = string.IsNullOrEmpty(script.Current) ? "resting" : script.Current;
            return label + ": " + playing + "   last move " + script.Travelled.ToString("0.00") + " m (from the clip)";
        }
    }
}
