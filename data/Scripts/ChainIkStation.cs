using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Chain IK on hierarchies built from plain entities. The robot arm (FABRIK, a pole keeps its
    /// elbow up) tracks a drone flying a figure-eight; the tentacle (CCD, which curls the tip first)
    /// reaches for the hero when the hero comes close and sways after a wandering point otherwise.
    /// Modifiers on plain entities show their solved pose in play mode; in the editor the gizmo
    /// previews it.
    /// </summary>
    [ScriptSourceFile]
    public class ChainIkStation : Station
    {
        public Entity ArmTip;
        public Entity TentacleTip;
        [Tooltip("Where the tentacle wanders when the hero is away.")]
        public Entity IdleTarget;
        [Tooltip("Point on the hero the tentacle reaches for.")]
        public Entity HeroPoint;
        [Tooltip("The tentacle reaches for the hero inside this distance (m).")]
        public float ReachDistance = 4.0f;

        private ChainIKComponent arm;
        private ChainIKComponent tentacle;
        private bool reaching;

        public override void OnStart()
        {
            base.OnStart();
            arm = ArmTip.IsValid() ? ArmTip.GetComponent<ChainIKComponent>() : null;
            tentacle = TentacleTip.IsValid() ? TentacleTip.GetComponent<ChainIKComponent>() : null;
        }

        public override void OnUpdate()
        {
            if (tentacle == null || !HeroPoint.IsValid())
            {
                return;
            }
            float distance = Vector3.Distance(StationRegistry.Flat(HeroPoint.transform.position), StationRegistry.Flat(TentacleTip.transform.position));
            bool wantReach = distance < ReachDistance;
            if (wantReach != reaching)
            {
                reaching = wantReach;
                tentacle.target = reaching ? HeroPoint : IdleTarget;
            }
        }

        public override string GetStatus()
        {
            string line = "";
            if (arm != null)
            {
                line += "Robot arm (FABRIK, " + arm.chainLength + " bones): " + (arm.isReached ? "on target" : "reaching");
            }
            if (tentacle != null)
            {
                line += "\nTentacle (CCD, " + tentacle.chainLength + " bones): " + (reaching ? "reaching for you" : "wandering");
            }
            return line;
        }
    }
}
