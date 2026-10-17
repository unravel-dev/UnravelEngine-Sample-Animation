using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Look-at. Three characters follow the hero with Look At IK on their head bone: head only, head +
    /// chest, and head + chest + spine with a small share for the lower bones. Each bone turns at most
    /// Max Bone Angle from its animated facing, and the look fades out when the hero walks behind them
    /// (Max Turn Angle) instead of twisting the neck round.
    /// </summary>
    [ScriptSourceFile]
    public class LookAtStation : Station
    {
        public Entity HeadA;
        public Entity HeadB;
        public Entity HeadC;

        public override string GetStatus()
        {
            return Describe("Head only", HeadA) + "\n" + Describe("Head + chest", HeadB) + "\n" + Describe("Head + chest + spine", HeadC);
        }

        private static string Describe(string label, Entity head)
        {
            var look = head.IsValid() ? head.GetComponent<LookAtIKComponent>() : null;
            if (look == null)
            {
                return "";
            }
            return label + ": weight " + look.effectiveWeight.ToString("0.00") + (look.effectiveWeight < 0.05f ? "  (target behind - faded)" : "");
        }
    }
}
