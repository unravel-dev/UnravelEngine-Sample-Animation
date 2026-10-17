using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Foot IK. Two walkers take the same path over steps, a slope and stones: the first plants its
    /// feet with a Foot IK component (ground probes, pelvis drop to the lower foot, feet tilted onto the
    /// slope, swing feet left alone), the second has none and floats or sinks. The hero has Foot IK too.
    /// </summary>
    [ScriptSourceFile]
    public class FootIkStation : Station
    {
        public Entity WithIk;
        public Entity WithoutIk;

        public override string GetStatus()
        {
            if (!WithIk.IsValid())
            {
                return "";
            }
            var foot = WithIk.GetComponent<FootIKComponent>();
            if (foot == null)
            {
                return "";
            }
            return "With Foot IK: left " + foot.leftFootWeight.ToString("0.00") + "  right " + foot.rightFootWeight.ToString("0.00") +
                   "  pelvis " + (foot.pelvisOffset * 100.0f).ToString("0") + " cm\nWithout: the clip as authored, root on the ground";
        }
    }
}
